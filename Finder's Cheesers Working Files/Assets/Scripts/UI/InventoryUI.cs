using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace FindersCheesers.UI
{
    /// <summary>
    /// UI component that displays inventory of grabbed items.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/UI/Inventory UI")]
    public class InventoryUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Transform inventoryContainer;
        [SerializeField] private GameObject inventoryItemPrefab;
        
        private void Start()
        {
            // Find RatInventory and subscribe to events
            Player.RatInventory ratInventory = FindFirstObjectByType<Player.RatInventory>();
            if (ratInventory != null)
            {
                ratInventory.OnInventoryChanged += UpdateInventoryUI;
                UpdateInventoryUI(ratInventory.GetInventory());
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            Player.RatInventory ratInventory = FindFirstObjectByType<Player.RatInventory>();
            if (ratInventory != null)
            {
                ratInventory.OnInventoryChanged -= UpdateInventoryUI;
            }
        }
        
        private void UpdateInventoryUI(List<Player.RatInventory.InventoryItem> inventory)
        {
            // Clear existing items
            foreach (Transform child in inventoryContainer)
            {
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Create new inventory items
            foreach (Player.RatInventory.InventoryItem item in inventory)
            {
                if (inventoryItemPrefab != null)
                {
                    GameObject itemObj = Instantiate(inventoryItemPrefab, inventoryContainer);
                    
                    // Set icon
                    Image iconImage = itemObj.GetComponentInChildren<Image>();
                    if (iconImage != null)
                    {
                        iconImage.sprite = item.icon;
                    }
                    
                    // Set quantity text
                    Text quantityText = itemObj.GetComponentInChildren<Text>();
                    if (quantityText != null)
                    {
                        quantityText.text = item.quantity.ToString();
                    }
                }
            }
        }
    }
}
