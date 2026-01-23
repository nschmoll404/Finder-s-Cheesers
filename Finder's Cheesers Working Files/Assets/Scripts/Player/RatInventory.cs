using UnityEngine;
using System.Collections.Generic;

namespace FindersCheesers.Player
{
    /// <summary>
    /// Manages inventory of items grabbed by SentRats.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/Player/Rat Inventory")]
    public class RatInventory : MonoBehaviour
    {
        [System.Serializable]
        public class InventoryItem
        {
            public string itemName;
            public Sprite icon;
            public int quantity;
        }
        
        private List<InventoryItem> inventory = new List<InventoryItem>();
        
        // Events
        public event System.Action<List<InventoryItem>> OnInventoryChanged;
        
        /// <summary>
        /// Adds an item to inventory when a SentRat recalls with a grabbed item.
        /// </summary>
        public void AddItem(string itemName, Sprite icon)
        {
            // Check if item already exists
            InventoryItem existingItem = inventory.Find(item => item.itemName == itemName);
            
            if (existingItem != null)
            {
                existingItem.quantity++;
            }
            else
            {
                inventory.Add(new InventoryItem
                {
                    itemName = itemName,
                    icon = icon,
                    quantity = 1
                });
            }
            
            OnInventoryChanged?.Invoke(inventory);
        }
        
        /// <summary>
        /// Gets current inventory.
        /// </summary>
        public List<InventoryItem> GetInventory() => inventory;
        
        /// <summary>
        /// Checks if inventory contains a specific item.
        /// </summary>
        public bool HasItem(string itemName)
        {
            return inventory.Exists(item => item.itemName == itemName && item.quantity > 0);
        }
    }
}
