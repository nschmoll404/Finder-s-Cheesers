using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FindersCheesers.UI
{
    /// <summary>
    /// UI component that displays current rat count.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/UI/Rat Counter UI")]
    public class RatCounterUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text ratCountText;
        [SerializeField] private Slider ratCountSlider;
        
        [Header("Settings")]
        [SerializeField] private int maxRats = 10;
        
        private void Start()
        {
            // Find KingRatController and subscribe to events
            Player.KingRatController kingRat = FindFirstObjectByType<Player.KingRatController>();
            if (kingRat != null)
            {
                kingRat.OnRatsChanged += UpdateRatCount;
                UpdateRatCount(kingRat.GetCurrentRats());
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            Player.KingRatController kingRat = FindFirstObjectByType<Player.KingRatController>();
            if (kingRat != null)
            {
                kingRat.OnRatsChanged -= UpdateRatCount;
            }
        }
        
        private void UpdateRatCount(int currentRats)
        {
            // Update text
            if (ratCountText != null)
            {
                ratCountText.text = $"Rats: {currentRats}/{maxRats}";
            }
            
            // Update slider
            if (ratCountSlider != null)
            {
                ratCountSlider.maxValue = maxRats;
                ratCountSlider.value = currentRats;
            }
        }
    }
}
