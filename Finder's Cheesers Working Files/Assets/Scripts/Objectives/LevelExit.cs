using UnityEngine;
using UnityEngine.SceneManagement;

namespace FindersCheesers.Objectives
{
    /// <summary>
    /// Level exit trigger that ends level when KingRat touches it.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/Objectives/Level Exit")]
    public class LevelExit : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string nextSceneName;
        [SerializeField] private float exitDelay = 1f;
        
        [Header("Visuals")]
        [SerializeField] private Color greenColor = Color.green;
        
        // Components
        private Renderer exitRenderer;
        private Material exitMaterial;
        
        private void Awake()
        {
            exitRenderer = GetComponent<Renderer>();
            if (exitRenderer != null)
            {
                exitMaterial = exitRenderer.material;
                exitMaterial.color = greenColor;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Check if KingRat entered
            Player.KingRatController kingRat = other.GetComponent<Player.KingRatController>();
            if (kingRat != null)
            {
                OnKingRatEnter();
            }
        }
        
        private void OnKingRatEnter()
        {
            Debug.Log("[LevelExit] KingRat reached level exit!");
            
            // Load next scene after delay
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                Invoke(nameof(LoadNextScene), exitDelay);
            }
        }
        
        private void LoadNextScene()
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
