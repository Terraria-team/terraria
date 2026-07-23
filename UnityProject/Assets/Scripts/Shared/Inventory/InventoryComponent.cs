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

            _slots[0] = new ItemStack(new ItemID(0));
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

        // Client-side hit detection for melee (Swing) attacks
        var itemData = _slots[SelectedSlot].ItemStack.ItemID.ItemData;
        if (itemData.primaryAction == ActionType.Swing && itemData.swingData != null)
        {
            float swingDamage = itemData.swingData.swingDamage;
            float swingRange = itemData.swingData.swingSize;

            // Detect nearby enemies around the player
            var colliders = Physics2D.OverlapCircleAll(transform.position, swingRange > 0 ? swingRange : 2f);
            foreach (var col in colliders)
            {
                // Skip self
                if (col.transform.IsChildOf(transform) || col.gameObject == gameObject)
                    continue;

                var health = col.GetComponent<Shared.Components.HealthComponent>();
                if (health != null)
                {
                    health.ApplyDamageServerRpc(Mathf.Max(1, (int)swingDamage));
                }
            }
        }
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
            SubtractFromSlot(selectedSlot);
    }

    [Server]
    private void SubtractFromSlot(int slotID)
    {
        if (_slots[slotID].ItemStack.Count == 1)
            _slots[slotID] = new NullableItemStack();
        else
            _slots[slotID] = new NullableItemStack(_slots[slotID].ItemStack.Decremented());
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

    [Server]
    private int GetCountOf(ItemID item)
    {
        int count = 0;
        
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            
            if (!slot.HasValue)
                continue;
            
            var stack = slot.ItemStack;
                
            if (stack.ItemID != item)
                continue;

            count += stack.Count;
        }
        
        return count;
    }
    
    [Server]
    private bool HasIngredientsFor(CraftData data)
    {
        for (int i = 0; i < data.ingredients.Length; i++)
        {
            var ingredient = data.ingredients[i].id;
            var amount = data.ingredientAmounts[i];

            if (GetCountOf(new ItemID(ingredient)) < amount)
                return false;
        }

        return true;
    }

    private void RemoveIngredients(CraftData data)
    {
        for (int i = 0; i < data.ingredients.Length; i++)
        {
            var ingredient = data.ingredients[i].id;
            var amount = data.ingredientAmounts[i];

            for (int j = 0; j < _slots.Count; j++)
            {
                var slot = _slots[j];
                
                if (!slot.HasValue)
                    continue;
                
                var stack = slot.ItemStack;
                
                if (stack.ItemID != new ItemID(ingredient))
                    continue;

                while (_slots[j].HasValue && amount > 0)
                {
                    SubtractFromSlot(j);
                    amount--;
                }
            }
        }
    }
    
    [Command]
    public void TryCrafting(int craftID)
    {
        var data = DataManager.Crafts[craftID];
        var stack = new ItemStack(new ItemID(data.result.id), data.resultAmount);
        
        if (HowMuchCanAddOf(stack) < data.resultAmount)
            return;
            
        if (!HasIngredientsFor(data))
            return;
        
        RemoveIngredients(data);
        
        if (HowMuchCanAddOf(stack) >= data.resultAmount)
            for (int i = 0; i < data.resultAmount; i++)
            {
                AddItem(stack.ItemID);
            }
    }
}
