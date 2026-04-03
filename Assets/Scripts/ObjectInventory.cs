using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ObjectInventory : MonoBehaviour
{
    [System.Serializable]
    public enum ItemType
    {
        Fuel,
        Oxygen,
        Energy,
        WarpMaterial,
        ScrapMetal,
        CrystalCore,
        ElectronicComponent,
        HydrogenCell,
        TitaniumOre,
        RareEarth
    }

    [System.Serializable]
    public class InventoryItem
    {
        public ItemType itemType;
        public float quantity;

        public InventoryItem(ItemType type, float amount)
        {
            itemType = type;
            quantity = amount;
        }
    }

    [SerializeField] private string inventoryName = "Default Inventory";
    [SerializeField] private float maxCapacity = 100f;
    [SerializeField] private bool hasCapacityLimit = true;

    private List<InventoryItem> items = new List<InventoryItem>();
    private float currentWeight = 0f;

    void Start()
    {
        Debug.Log($"Inventory '{inventoryName}' initialized. Capacity: {maxCapacity}");
    }

    public bool AddItem(ItemType itemType, float quantity)
    {
        if (quantity <= 0)
        {
            Debug.LogWarning($"Cannot add {quantity} of {itemType}. Quantity must be greater than 0.");
            return false;
        }

        // Check capacity
        if (hasCapacityLimit && currentWeight + quantity > maxCapacity)
        {
            Debug.LogWarning($"Inventory full! Cannot add {quantity} {itemType}. Current: {currentWeight}/{maxCapacity}");
            return false;
        }

        // Find existing item of this type
        InventoryItem existingItem = items.FirstOrDefault(item => item.itemType == itemType);

        if (existingItem != null)
        {
            existingItem.quantity += quantity;
        }
        else
        {
            items.Add(new InventoryItem(itemType, quantity));
        }

        currentWeight += quantity;
        Debug.Log($"Added {quantity} {itemType} to {inventoryName}. Total: {GetItemCount(itemType)}");
        return true;
    }

    public bool RemoveItem(ItemType itemType, float quantity)
    {
        if (quantity <= 0)
        {
            Debug.LogWarning($"Cannot remove {quantity} of {itemType}. Quantity must be greater than 0.");
            return false;
        }

        InventoryItem item = items.FirstOrDefault(i => i.itemType == itemType);

        if (item == null)
        {
            Debug.LogWarning($"Item {itemType} not found in {inventoryName}");
            return false;
        }

        if (item.quantity < quantity)
        {
            Debug.LogWarning($"Not enough {itemType}. Have: {item.quantity}, Need: {quantity}");
            return false;
        }

        item.quantity -= quantity;
        currentWeight -= quantity;

        // Remove item if quantity reaches 0
        if (item.quantity <= 0)
        {
            items.Remove(item);
        }

        Debug.Log($"Removed {quantity} {itemType} from {inventoryName}. Remaining: {GetItemCount(itemType)}");
        return true;
    }

    public float GetItemCount(ItemType itemType)
    {
        InventoryItem item = items.FirstOrDefault(i => i.itemType == itemType);
        return item?.quantity ?? 0f;
    }

    public bool HasItem(ItemType itemType, float quantity)
    {
        return GetItemCount(itemType) >= quantity;
    }

    public void Clear()
    {
        items.Clear();
        currentWeight = 0f;
        Debug.Log($"Inventory '{inventoryName}' cleared");
    }

    public float GetCurrentWeight()
    {
        return currentWeight;
    }

    public float GetAvailableCapacity()
    {
        if (!hasCapacityLimit)
        {
            return float.MaxValue;
        }
        return maxCapacity - currentWeight;
    }

    public float GetCapacityPercentage()
    {
        if (!hasCapacityLimit)
        {
            return 0f;
        }
        return (currentWeight / maxCapacity) * 100f;
    }

    public bool IsInventoryFull()
    {
        if (!hasCapacityLimit)
        {
            return false;
        }
        return currentWeight >= maxCapacity;
    }

    public List<InventoryItem> GetAllItems()
    {
        return new List<InventoryItem>(items);
    }

    public int GetItemTypeCount()
    {
        return items.Count;
    }

    public bool TransferTo(ObjectInventory targetInventory, ItemType itemType, float quantity)
    {
        if (targetInventory == null)
        {
            Debug.LogError("Target inventory is null");
            return false;
        }

        if (!HasItem(itemType, quantity))
        {
            Debug.LogWarning($"Not enough {itemType} to transfer");
            return false;
        }

        if (!targetInventory.AddItem(itemType, quantity))
        {
            Debug.LogWarning($"Target inventory could not accept {quantity} {itemType}");
            return false;
        }

        RemoveItem(itemType, quantity);
        Debug.Log($"Transferred {quantity} {itemType} from '{inventoryName}' to '{targetInventory.inventoryName}'");
        return true;
    }

    public void PrintInventory()
    {
        if (items.Count == 0)
        {
            Debug.Log($"'{inventoryName}' is empty");
            return;
        }

        string inventoryInfo = $"\n--- Inventory: {inventoryName} ---\n";
        inventoryInfo += $"Capacity: {currentWeight}/{(hasCapacityLimit ? maxCapacity.ToString() : "Unlimited")}\n";

        foreach (InventoryItem item in items)
        {
            inventoryInfo += $"  {item.itemType}: {item.quantity}\n";
        }

        Debug.Log(inventoryInfo);
    }
}
