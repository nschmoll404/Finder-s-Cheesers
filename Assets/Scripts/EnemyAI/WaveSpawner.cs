using UnityEngine;

namespace WaveSpawning
{
    /// <summary>
    /// Defines a single spawner configuration within a wave.
    /// Handles the spawning of a specific enemy type with configurable timing and quantity.
    /// </summary>
    [System.Serializable]
    public class WaveSpawner
    {
        [Tooltip("The prefab to spawn")]
        public GameObject spawnPrefab;
        
        [Tooltip("Delay between each individual spawn")]
        public float spawnDelay = 1f;
        
        [Tooltip("Total number of enemies to spawn from this spawner")]
        public int spawnAmount = 5;
        
        [Tooltip("Optional: Transform position to spawn from. If null, uses the WaveSpawnerManager's position")]
        public Transform spawnPoint;
        
        // Runtime tracking
        [HideInInspector]
        public int totalSpawned = 0;
        
        /// <summary>
        /// Spawns a single instance of the configured prefab at the specified position.
        /// </summary>
        /// <param name="fallbackPosition">Position to use if spawnPoint is null</param>
        /// <returns>The spawned GameObject</returns>
        public GameObject Spawn(Vector3 fallbackPosition)
        {
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : fallbackPosition;
            GameObject spawnedObject = Object.Instantiate(spawnPrefab, spawnPosition, Quaternion.identity);
            totalSpawned++;
            return spawnedObject;
        }
        
        /// <summary>
        /// Checks if this spawner has finished spawning all its enemies.
        /// </summary>
        public bool IsComplete()
        {
            return totalSpawned >= spawnAmount;
        }
        
        /// <summary>
        /// Resets the spawner's runtime tracking.
        /// </summary>
        public void Reset()
        {
            totalSpawned = 0;
        }
    }
}
