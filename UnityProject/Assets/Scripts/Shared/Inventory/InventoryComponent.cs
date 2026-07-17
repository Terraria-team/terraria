using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class InventoryComponent : NetworkBehaviour
{
    public const int Depth = 4;
    
    private List<ItemStack?> _slots = new();
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
}
