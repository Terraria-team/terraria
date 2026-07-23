using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class InventoryComponent : NetworkBehaviour
{
    public const int Depth = 4;
     
    private readonly SyncList<NullableItemStack> _slots = new();
    public int SelectedSlot { get; private set; }
    public NullableItemStack GetItemAt(int slot) => _slots[slot];
    
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
            _slots.OnChange += OnSlotsReplicated;
            OnSlotsChanged?.Invoke();
        }
    }
    
    void Start()
    {
        if (isServer)
        {
            for (int i = 0; i < 9 * Depth; i++)
            {
                _slots.Add(new NullableItemStack());
            }

            // TODO for test only, remove
            _slots[0] = new ItemStack(new ItemID(0));
            _slots[1] = new ItemStack(new ItemID(1));
            _slots[2] = new ItemStack(new ItemID(2));
            _slots[3] = new ItemStack(new ItemID(3));
            _slots[4] = new ItemStack(new ItemID(6));
        }

        if (isLocalPlayer)
        {
            OnSlotsChanged?.Invoke();
            OnSelectionChanged?.Invoke();
        }
    }

    private void OnSlotsReplicated(SyncList<NullableItemStack>.Operation op, int index, NullableItemStack newItem)
    {
        OnSlotsChanged?.Invoke();
    }

    public void ChangeSelection(int slotID)
    {
        if (slotID < 0 || slotID > 9)
            Debug.LogError($"Invalid Slot ID for selection: {slotID}");
        
        SelectedSlot = slotID;
        OnSelectionChanged?.Invoke();
    }

    [Client]
    public void UseSelectedItem()
    {
        if (!_slots[SelectedSlot].HasValue)
            return;

        var clientGeneratedContext = ActionFiller.GetStrippedClientContext(_slots[SelectedSlot].ItemStack);
        
        CmdUseSelectedItem(SelectedSlot, clientGeneratedContext);
    }

    [Command]
    public void CmdUseSelectedItem(int selectedSlot, ActionContext context)
    {
        // TODO validate selected slot value
        
        if (!_slots[selectedSlot].HasValue)
            return;

        context = ActionFiller.ExpandClientContext(context, _slots[selectedSlot].ItemStack, transform.position);
        
        bool result = ActionRegistry.ExecuteAction(_slots[selectedSlot].ItemStack.ItemID.ItemData.primaryAction, context);
        
        if (_slots[selectedSlot].ItemStack.ItemID.ItemData.consumeOnAction && result)
        {
            if (_slots[selectedSlot].ItemStack.Count == 1)
                _slots[selectedSlot] = new NullableItemStack();
            else
                _slots[selectedSlot] = new NullableItemStack(_slots[selectedSlot].ItemStack.Decremented());
        }
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
            if (!slot.HasValue)
                return stack.ItemID.ItemData.stackSize;
            
            if (slot.ItemStack.ItemID != stack.ItemID)
                continue;
            
            capacity += stack.ItemID.ItemData.stackSize - slot.ItemStack.Count;
            
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
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (!slot.HasValue)
                continue;

            if (slot.ItemStack.ItemID != item)
                continue;

            if (slot.ItemStack.IsFilled)
                continue;

            _slots[i] = new NullableItemStack(_slots[i].ItemStack.Incremented());
            UpdateSlots(connectionToClient);
            OnSlotsChanged?.Invoke();

            return;
        }

        // No existing stack found, resorting to creating a new one
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (!slot.HasValue)
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
