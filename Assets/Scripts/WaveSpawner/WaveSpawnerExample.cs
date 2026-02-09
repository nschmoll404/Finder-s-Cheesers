using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Example script demonstrating how to use the WaveSpawner component.
    /// Attach this to a GameObject with a WaveSpawner component to see events in action.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Wave Spawner Example")]
    public class WaveSpawnerExample : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the WaveSpawner component")]
        [SerializeField]
        private WaveSpawner waveSpawner;

        [Header("Debug Display")]
        [Tooltip("Show wave progress in the console")]
        [SerializeField]
        private bool showDebugInfo = true;

        private void Start()
        {
            // Get the WaveSpawner component if not assigned
            if (waveSpawner == null)
            {
                waveSpawner = GetComponent<WaveSpawner>();
            }

            if (waveSpawner == null)
            {
                Debug.LogError("[WaveSpawnerExample] No WaveSpawner component found!");
                return;
            }

            // Subscribe to wave spawner events
            waveSpawner.OnWaveStart += HandleWaveStart;
            waveSpawner.OnWaveComplete += HandleWaveComplete;
            waveSpawner.OnObjectSpawned += HandleObjectSpawned;
            waveSpawner.OnObjectDestroyed += HandleObjectDestroyed;
            waveSpawner.OnAllWavesComplete += HandleAllWavesComplete;

            if (showDebugInfo)
            {
                Debug.Log($"[WaveSpawnerExample] Subscribed to WaveSpawner events. Total waves: {waveSpawner.TotalWaves}");
            }
        }

        private void Update()
        {
            // Optional: Display active spawn count
            if (showDebugInfo && waveSpawner != null && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[WaveSpawnerExample] Active spawns: {waveSpawner.ActiveSpawnCount}, Current wave: {waveSpawner.CurrentWaveIndex + 1}/{waveSpawner.TotalWaves}");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (waveSpawner != null)
            {
                waveSpawner.OnWaveStart -= HandleWaveStart;
                waveSpawner.OnWaveComplete -= HandleWaveComplete;
                waveSpawner.OnObjectSpawned -= HandleObjectSpawned;
                waveSpawner.OnObjectDestroyed -= HandleObjectDestroyed;
                waveSpawner.OnAllWavesComplete -= HandleAllWavesComplete;
            }
        }

        #region Event Handlers

        private void HandleWaveStart(int waveIndex, Wave wave)
        {
            Debug.Log($"[WaveSpawnerExample] Wave {waveIndex + 1} started: {wave.waveName}");
        }

        private void HandleWaveComplete(int waveIndex, Wave wave)
        {
            Debug.Log($"[WaveSpawnerExample] Wave {waveIndex + 1} completed: {wave.waveName}");
        }

        private void HandleObjectSpawned(GameObject obj)
        {
            Debug.Log($"[WaveSpawnerExample] Object spawned: {obj.name}");
        }

        private void HandleObjectDestroyed(GameObject obj)
        {
            Debug.Log($"[WaveSpawnerExample] Object destroyed: {obj.name}");
        }

        private void HandleAllWavesComplete()
        {
            Debug.Log("[WaveSpawnerExample] All waves completed!");
        }

        #endregion

        #region Public Methods (for testing via Inspector or other scripts)

        /// <summary>
        /// Manually start the wave spawner.
        /// </summary>
        [ContextMenu("Start Spawning")]
        public void StartSpawning()
        {
            if (waveSpawner != null)
            {
                waveSpawner.StartSpawning();
            }
        }

        /// <summary>
        /// Manually stop the wave spawner.
        /// </summary>
        [ContextMenu("Stop Spawning")]
        public void StopSpawning()
        {
            if (waveSpawner != null)
            {
                waveSpawner.StopSpawning();
            }
        }

        /// <summary>
        /// Reset the wave spawner.
        /// </summary>
        [ContextMenu("Reset Spawner")]
        public void ResetSpawner()
        {
            if (waveSpawner != null)
            {
                waveSpawner.ResetSpawner();
            }
        }

        /// <summary>
        /// Destroy all active spawns.
        /// </summary>
        [ContextMenu("Destroy All Active Spawns")]
        public void DestroyAllActiveSpawns()
        {
            if (waveSpawner != null)
            {
                waveSpawner.DestroyAllActiveSpawns();
            }
        }

        #endregion
    }
}
