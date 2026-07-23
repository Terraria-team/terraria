using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject craftPrefab;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform parent;

    [SerializeField] private float slotObjectBaseOffset;
    [SerializeField] private float slotObjectOffset;
    [SerializeField] private float slotObjectScale;
    
    [SerializeField] private float craftAreaOffset;
    
    private readonly List<InventorySlotUI> _slotObjects = new();

    void Build()
    {
        for (int y = 0; y < InventoryComponent.Depth; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                var result = Instantiate(slotPrefab, parent);

                result.transform.localPosition = (
                    new Vector3(x, -y, 0) * slotObjectOffset +
                    new Vector3(slotObjectBaseOffset, -slotObjectOffset, 0)
                ) * slotObjectScale;
                result.transform.localScale = new Vector3(slotObjectScale, slotObjectScale, slotObjectScale);

                _slotObjects.Add(result.GetComponent<InventorySlotUI>());
            }
        }
        
        int craftID = -1;
        foreach (var craft in DataManager.Crafts)
        {
            craftID++;
            var result = Instantiate(craftPrefab, parent);

            result.transform.localPosition = (
                new Vector3(craftID, -InventoryComponent.Depth, 0) * slotObjectOffset +
                new Vector3(slotObjectBaseOffset, -slotObjectOffset - craftAreaOffset, 0)
            ) * slotObjectScale;
            result.transform.localScale = new Vector3(slotObjectScale, slotObjectScale, slotObjectScale);

            var craftComponent = result.GetComponent<CraftSlot>();
            craftComponent.SetItem(craft);

            //_slotObjects.Add(result.GetComponent<InventorySlotUI>());
        }
    }

    void Awake()
    {
        if (!NetworkClient.active)
        {
            Destroy(this);
        }
    }

    void Start()
    {
        Build();
        
        Debug.Log(InventoryComponent.ClientOnlyInstance);
        
        InventoryComponent.ClientOnlyInstance.OnSelectionChanged += UpdateSelection;
        InventoryComponent.ClientOnlyInstance.OnSlotsChanged += UpdateSlots;
        UpdateSelection();
        UpdateSlots();
    }

    void UpdateSelection()
    {
        for (int i = 0; i < _slotObjects.Count; i++)
        {
            _slotObjects[i].UpdateSelection(i == InventoryComponent.ClientOnlyInstance.SelectedSlot);
        }
    }

    void UpdateSlots()
    {
        for (int i = 0; i < _slotObjects.Count; i++)
        {
            _slotObjects[i].UpdateItem(InventoryComponent.ClientOnlyInstance.GetItemAt(i));
        }
    }
}
