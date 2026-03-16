using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaveSpawning
{
    /// <summary>
    /// Manages wave-based enemy spawning with configurable timing and sequencing options.
    /// Supports both instantaneous and sequential wave start modes.
    /// </summary>
    public class WaveSpawnerManager : MonoBehaviour
    {
        [Header("Wave Configuration")]
        [Tooltip("List of waves to spawn")]
        public List<Wave> waves = new List<Wave>();
        
        [Tooltip("How waves should start relative to each other")]
        public WaveStartOption waveStartOption = WaveStartOption.Sequential;
        
        [Header("Runtime Settings")]
        [Tooltip("Should waves start automatically when the game begins?")]
        public bool autoStart = true;
        
        [Tooltip("Delay before the first wave starts")]
        public float initialDelay = 0f;
        
        [Tooltip("Should waves loop indefinitely?")]
        public bool loopWaves = false;
        
        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        public bool showDebugInfo = false;
        
        // Runtime tracking
        private int currentWaveIndex = 0;
        private bool isRunning = false;
        private bool allWavesComplete = false;
        private int totalEnemiesSpawned = 0;
        private Coroutine waveCoroutine;
        
        // Dictionary to track spawn timers for each spawner
        private Dictionary<WaveSpawner, float> spawnTimers = new Dictionary<WaveSpawner, float>();
        
        // Dictionary to track wave start timers
        private Dictionary<int, float> waveStartTimers = new Dictionary<int, float>();
        
        private void Start()
        {
            if (autoStart)
            {
                StartWaves();
            }
        }
        
        private void Update()
        {
            if (!isRunning || allWavesComplete)
            {
                return;
            }
            
            UpdateWaveSpawning();
        }
        
        /// <summary>
        /// Starts the wave spawning process.
        /// </summary>
        public void StartWaves()
        {
            if (isRunning)
            {
                Debug.LogWarning("WaveSpawnerManager is already running!");
                return;
            }
            
            ResetAllWaves();
            isRunning = true;
            allWavesComplete = false;
            totalEnemiesSpawned = 0;
            currentWaveIndex = 0;
            
            if (waveStartOption == WaveStartOption.Sequential)
            {
                StartCoroutine(SequentialWaveSpawning());
            }
            else
            {
                StartCoroutine(InstantaneousWaveSpawning());
            }
        }
        
        /// <summary>
        /// Stops the wave spawning process.
        /// </summary>
        public void StopWaves()
        {
            isRunning = false;
            if (waveCoroutine != null)
            {
                StopCoroutine(waveCoroutine);
                waveCoroutine = null;
            }
        }
        
        /// <summary>
        /// Resets all waves and spawners to their initial state.
        /// </summary>
        public void ResetAllWaves()
        {
            StopWaves();
            currentWaveIndex = 0;
            allWavesComplete = false;
            totalEnemiesSpawned = 0;
            spawnTimers.Clear();
            waveStartTimers.Clear();
            
            foreach (var wave in waves)
            {
                wave.Reset();
            }
            
            if (showDebugInfo)
            {
                Debug.Log("WaveSpawnerManager: All waves reset.");
            }
        }
        
        /// <summary>
        /// Handles sequential wave spawning where each wave waits for the previous to complete.
        /// </summary>
        private IEnumerator SequentialWaveSpawning()
        {
            yield return new WaitForSeconds(initialDelay);
            
            while (currentWaveIndex < waves.Count)
            {
                Wave currentWave = waves[currentWaveIndex];
                
                if (showDebugInfo)
                {
                    Debug.Log($"WaveSpawnerManager: Starting wave {currentWaveIndex} with {currentWave.GetTotalEnemiesToSpawn()} enemies.");
                }
                
                // Wait for the wave's start delay
                yield return new WaitForSeconds(currentWave.startDelay);
                
                currentWave.hasStarted = true;
                
                // Wait until this wave is complete
                while (!currentWave.IsComplete())
                {
                    yield return null;
                }
                
                currentWave.isComplete = true;
                currentWave.UpdateTotalSpawned();
                
                if (showDebugInfo)
                {
                    Debug.Log($"WaveSpawnerManager: Wave {currentWaveIndex} complete. Spawned {currentWave.totalSpawned} enemies.");
                }
                
                currentWaveIndex++;
            }
            
            HandleAllWavesComplete();
        }
        
        /// <summary>
        /// Handles instantaneous wave spawning where all waves start based on their delays.
        /// </summary>
        private IEnumerator InstantaneousWaveSpawning()
        {
            yield return new WaitForSeconds(initialDelay);
            
            // Initialize wave start timers
            for (int i = 0; i < waves.Count; i++)
            {
                waveStartTimers[i] = waves[i].startDelay;
            }
            
            // Wait for all waves to complete
            while (!AreAllWavesComplete())
            {
                // Check which waves should start
                for (int i = 0; i < waves.Count; i++)
                {
                    Wave wave = waves[i];
                    
                    if (!wave.hasStarted && waveStartTimers[i] <= 0f)
                    {
                        wave.hasStarted = true;
                        
                        if (showDebugInfo)
                        {
                            Debug.Log($"WaveSpawnerManager: Starting wave {i} with {wave.GetTotalEnemiesToSpawn()} enemies.");
                        }
                    }
                    
                    if (wave.hasStarted && !wave.isComplete && wave.IsComplete())
                    {
                        wave.isComplete = true;
                        wave.UpdateTotalSpawned();
                        
                        if (showDebugInfo)
                        {
                            Debug.Log($"WaveSpawnerManager: Wave {i} complete. Spawned {wave.totalSpawned} enemies.");
                        }
                    }
                }
                
                yield return null;
            }
            
            HandleAllWavesComplete();
        }
        
        /// <summary>
        /// Updates the spawning logic for all active spawners.
        /// </summary>
        private void UpdateWaveSpawning()
        {
            foreach (var wave in waves)
            {
                if (!wave.hasStarted || wave.isComplete)
                {
                    continue;
                }
                
                foreach (var spawner in wave.spawners)
                {
                    if (spawner.IsComplete())
                    {
                        continue;
                    }
                    
                    // Initialize timer if not exists
                    if (!spawnTimers.ContainsKey(spawner))
                    {
                        spawnTimers[spawner] = 0f;
                    }
                    
                    // Update timer
                    spawnTimers[spawner] += Time.deltaTime;
                    
                    // Check if it's time to spawn
                    if (spawnTimers[spawner] >= spawner.spawnDelay)
                    {
                        spawner.Spawn(transform.position);
                        totalEnemiesSpawned++;
                        spawnTimers[spawner] = 0f;
                    }
                }
            }
        }
        
        /// <summary>
        /// Checks if all waves have completed spawning.
        /// </summary>
        private bool AreAllWavesComplete()
        {
            foreach (var wave in waves)
            {
                if (!wave.isComplete)
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Handles the completion of all waves.
        /// </summary>
        private void HandleAllWavesComplete()
        {
            allWavesComplete = true;
            
            if (showDebugInfo)
            {
                Debug.Log($"WaveSpawnerManager: All waves complete! Total enemies spawned: {totalEnemiesSpawned}");
            }
            
            if (loopWaves)
            {
                StartCoroutine(LoopWaves());
            }
        }
        
        /// <summary>
        /// Handles looping waves if enabled.
        /// </summary>
        private IEnumerator LoopWaves()
        {
            yield return new WaitForSeconds(2f); // Wait before restarting
            
            if (showDebugInfo)
            {
                Debug.Log("WaveSpawnerManager: Restarting waves...");
            }
            
            ResetAllWaves();
            StartWaves();
        }
        
        /// <summary>
        /// Gets the total number of enemies spawned across all waves.
        /// </summary>
        public int GetTotalEnemiesSpawned()
        {
            return totalEnemiesSpawned;
        }
        
        /// <summary>
        /// Gets the current wave index being processed.
        /// </summary>
        public int GetCurrentWaveIndex()
        {
            return currentWaveIndex;
        }
        
        /// <summary>
        /// Checks if all waves have completed.
        /// </summary>
        public bool IsComplete()
        {
            return allWavesComplete;
        }
        
        /// <summary>
        /// Checks if the wave spawner is currently running.
        /// </summary>
        public bool IsRunning()
        {
            return isRunning;
        }
        
        /// <summary>
        /// Gets the total number of enemies that will be spawned across all waves.
        /// </summary>
        public int GetTotalEnemiesToSpawn()
        {
            int total = 0;
            foreach (var wave in waves)
            {
                total += wave.GetTotalEnemiesToSpawn();
            }
            return total;
        }
        
        /// <summary>
        /// Gets progress as a percentage (0.0 to 1.0).
        /// </summary>
        public float GetProgress()
        {
            int totalToSpawn = GetTotalEnemiesToSpawn();
            if (totalToSpawn == 0) return 1f;
            return (float)totalEnemiesSpawned / totalToSpawn;
        }
        
        private void OnDrawGizmos()
        {
            // Draw spawn points for all spawners
            Gizmos.color = Color.green;
            foreach (var wave in waves)
            {
                foreach (var spawner in wave.spawners)
                {
                    if (spawner.spawnPoint != null)
                    {
                        Gizmos.DrawWireSphere(spawner.spawnPoint.position, 0.5f);
                    }
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw larger spheres when selected
            Gizmos.color = Color.yellow;
            foreach (var wave in waves)
            {
                foreach (var spawner in wave.spawners)
                {
                    if (spawner.spawnPoint != null)
                    {
                        Gizmos.DrawWireSphere(spawner.spawnPoint.position, 1f);
                        Gizmos.DrawLine(transform.position, spawner.spawnPoint.position);
                    }
                }
            }
        }
    }
}
