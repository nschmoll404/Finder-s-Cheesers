using System.Collections.Generic;
using UnityEngine;

namespace WaveSpawning
{
    /// <summary>
    /// Represents a single wave containing multiple spawners.
    /// Manages the timing and execution of spawning within the wave.
    /// </summary>
    [System.Serializable]
    public class Wave
    {
        [Tooltip("Delay before this wave starts spawning")]
        public float startDelay = 0f;
        
        [Tooltip("List of spawners that will run during this wave")]
        public List<WaveSpawner> spawners = new List<WaveSpawner>();
        
        // Runtime tracking
        [HideInInspector]
        public int totalSpawned = 0;
        
        [HideInInspector]
        public bool hasStarted = false;
        
        [HideInInspector]
        public bool isComplete = false;
        
        /// <summary>
        /// Calculates the total number of enemies this wave will spawn.
        /// </summary>
        public int GetTotalEnemiesToSpawn()
        {
            int total = 0;
            foreach (var spawner in spawners)
            {
                total += spawner.spawnAmount;
            }
            return total;
        }
        
        /// <summary>
        /// Gets the current number of enemies spawned in this wave.
        /// </summary>
        public int GetCurrentSpawnedCount()
        {
            int total = 0;
            foreach (var spawner in spawners)
            {
                total += spawner.totalSpawned;
            }
            return total;
        }
        
        /// <summary>
        /// Checks if this wave has finished spawning all enemies from all spawners.
        /// </summary>
        public bool IsComplete()
        {
            foreach (var spawner in spawners)
            {
                if (!spawner.IsComplete())
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Resets the wave's runtime tracking.
        /// </summary>
        public void Reset()
        {
            totalSpawned = 0;
            hasStarted = false;
            isComplete = false;
            foreach (var spawner in spawners)
            {
                spawner.Reset();
            }
        }
        
        /// <summary>
        /// Updates the total spawned count based on all spawners.
        /// </summary>
        public void UpdateTotalSpawned()
        {
            totalSpawned = GetCurrentSpawnedCount();
        }
    }
}
