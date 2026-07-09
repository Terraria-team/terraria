using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class InventoryComponent : NetworkBehaviour
{
    public const int Depth = 4;
    
    private SyncList<ItemStack?> _slots = new();
    public int SelectedSlot { get; private set; }
    public ItemStack? GetItemAt(int slot) => _slots[slot];
    public ItemStack? SelectedItem => _slots[SelectedSlot];

    public event Action OnSelectionChanged;
    public event Action OnSlotsChanged;
    
    public static InventoryComponent ClientOnlyInstance;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        
        if (isLocalPlayer)
        {
            if (ClientOnlyInstance != null)
                Debug.LogError("Detecting duplicate InventoryComponent on client");
            
            ClientOnlyInstance = this;
        }
    }
    
    void Start()
    {
        if (isServer || isOwned)
        {
            for (int i = 0; i < 9 * Depth; i++)
            {
                _slots.Add(null);
            }

            // TODO for test only, remove
            _slots[0] = new ItemStack(new ItemID(0));
            _slots[1] = new ItemStack(new ItemID(1));
            _slots[2] = new ItemStack(new ItemID(2));
            _slots[3] = new ItemStack(new ItemID(3));
        }

        if (isLocalPlayer)
        {
            // TODO for test only, remove
            OnSlotsChanged?.Invoke();
            OnSelectionChanged?.Invoke();
        }
    }

    public void ChangeSelection(int slotID)
    {
        if (slotID < 0 || slotID > 9)
            Debug.LogError($"Invalid Slot ID for selection: {slotID}");
        
        SelectedSlot = slotID;
        OnSelectionChanged?.Invoke();
    }

    public void UseSelectedItem()
    {
        if (SelectedItem is null)
            return;

        var clientGeneratedContext = ActionFiller.GetActionContext(SelectedItem.Value);
        
        CmdUseSelectedItem(SelectedItem.Value, clientGeneratedContext);

        if (SelectedItem.Value.ItemID.Value == 1)
        {
            _slots[SelectedSlot] = null;
            OnSlotsChanged?.Invoke();
        }
    }

    [Command]
    public void CmdUseSelectedItem(ItemStack stack, ActionContext context)
    {
        // TODO validate
        
        context.userPosition = transform.position;
        ActionRegistry.ExecuteAction(stack.ItemID.ItemData.primaryAction, context);
        
        
        //RpcUseSelectedItem(stack, context);
    }

    [ClientRpc]
    public void RpcUseSelectedItem(ItemStack stack, ActionContext context)
    {
        
    }

    [Server]
    public int HowMuchCanAddOf(ItemStack stack)
    {
        int capacity = 0;

        foreach (var slot in _slots)
        {
            if (slot == null)
                return stack.ItemID.ItemData.stackSize;
            
            if (slot.Value.ItemID != stack.ItemID)
                continue;
            
            capacity += stack.ItemID.ItemData.stackSize - slot.Value.Count;
            
            if (capacity >= stack.ItemID.ItemData.stackSize)
                return capacity;
        }
        
        return capacity;
    }

    [TargetRpc]
    void UpdateSlots(NetworkConnectionToClient target)
    {
        OnSlotsChanged?.Invoke();
    }
    
    [Server]
    public void AddItem(ItemID item)
    {
        // Trying to find an existing stack and append to it
        foreach (var slot in _slots)
        {
            if (slot == null)
                continue;
            
            if (slot.Value.ItemID != item)
                continue;
            
            if (slot.Value.IsFilled)
                continue;

            slot.Value.Increment();
            UpdateSlots(connectionToClient);
            OnSlotsChanged?.Invoke();
            
            return;
        }

        // No existing stack found, resorting to creating a new one
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot == null)
            {
                _slots[i] = new ItemStack(item);
                UpdateSlots(connectionToClient);
                OnSlotsChanged?.Invoke();
                return;
            }
        }
        
        Debug.LogError("Trying to add item but no place found. Make sure you've checked whether it can fit first");
    }
    
}
