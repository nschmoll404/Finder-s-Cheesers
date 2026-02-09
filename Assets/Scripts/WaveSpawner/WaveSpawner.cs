using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace FindersCheesers
{
    /// <summary>
    /// Defines when and how waves should start.
    /// </summary>
    public enum WaveStartMode
    {
        /// <summary>
        /// Waves start sequentially - waits for the previous wave to complete before starting the next.
        /// </summary>
        Sequential,
        
        /// <summary>
        /// Waves start instantaneously - all waves can be active simultaneously.
        /// </summary>
        Instantaneous
    }

    /// <summary>
    /// Defines a single spawnable object within a wave.
    /// </summary>
    [System.Serializable]
    public class SpawnableObject
    {
        [Header("Object Configuration")]
        [Tooltip("The prefab to spawn")]
        public GameObject prefab;
        
        [Tooltip("Number of this object to spawn in the wave")]
        [Min(1)]
        public int count = 1;
        
        [Header("Positioning")]
        [Tooltip("Transform to use as the spawn point. If null, uses the WaveSpawner's transform")]
        public Transform spawnPoint;
        
        [Tooltip("Random offset from the spawn point")]
        public Vector3 spawnOffset = Vector3.zero;
        
        [Tooltip("Random radius around the spawn point")]
        public float spawnRadius = 0f;

        [Header("Timing")]
        [Tooltip("Delay before spawning this specific object (in seconds)")]
        [Min(0f)]
        public float spawnDelay = 0f;
        
        [Tooltip("Minimum random delay added to spawn delay")]
        [Min(0f)]
        public float minRandomDelay = 0f;
        
        [Tooltip("Maximum random delay added to spawn delay")]
        [Min(0f)]
        public float maxRandomDelay = 0f;

        [Header("Active Spawn Limits")]
        [Tooltip("Minimum number of active spawns to maintain for this object type")]
        [Min(0)]
        public int minActiveSpawns = 0;

        /// <summary>
        /// Runtime tracking of how many objects have been spawned from this spawnable.
        /// Not serialized, reset when spawner starts.
        /// </summary>
        [System.NonSerialized]
        public int spawnedCount = 0;
    }

    /// <summary>
    /// Defines a wave of enemies/objects to spawn.
    /// </summary>
    [System.Serializable]
    public class Wave
    {
        [Header("Wave Configuration")]
        [Tooltip("Name of this wave for identification")]
        public string waveName = "Wave";
        
        [Tooltip("List of objects to spawn in this wave")]
        public List<SpawnableObject> spawnableObjects = new List<SpawnableObject>();

        [Header("Timing")]
        [Tooltip("Delay before this wave starts (in seconds)")]
        [Min(0f)]
        public float waveStartDelay = 0f;
    }

    /// <summary>
    /// A wave spawner that manages spawning waves of objects with various configuration options.
    /// Tracks spawned objects and maintains min/max active spawn counts.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Wave Spawner")]
    public class WaveSpawner : MonoBehaviour
    {
        #region Settings

        [Header("Wave Configuration")]
        [Tooltip("List of waves to spawn")]
        [SerializeField]
        private List<Wave> waves = new List<Wave>();

        [Tooltip("Mode for when waves should start")]
        [SerializeField]
        private WaveStartMode waveStartMode = WaveStartMode.Sequential;

        [Tooltip("Whether to start spawning waves automatically on Awake")]
        [SerializeField]
        private bool autoStart = true;

        [Header("Active Spawn Limits")]
        [Tooltip("Maximum number of active spawns allowed (global limit)")]
        [SerializeField]
        [Min(0)]
        private int maxActiveSpawns = 10;

        [Header("Spawn Timing")]
        [Tooltip("Default delay between spawns (in seconds)")]
        [SerializeField]
        [Min(0f)]
        private float spawnDelay = 0.5f;

        [Tooltip("Default wave start delay (in seconds)")]
        [SerializeField]
        [Min(0f)]
        private float waveStartDelay = 1f;

        [Header("Spawn Point")]
        [Tooltip("Default spawn point. If null, uses this transform")]
        [SerializeField]
        private Transform defaultSpawnPoint;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        #endregion

        #region State

        /// <summary>
        /// List of currently active spawned objects.
        /// </summary>
        private readonly List<GameObject> activeSpawns = new List<GameObject>();

        /// <summary>
        /// Maps spawned GameObjects to their SpawnableObject configuration.
        /// Used to track active spawns per object type.
        /// </summary>
        private readonly Dictionary<GameObject, SpawnableObject> spawnableObjectMap = new Dictionary<GameObject, SpawnableObject>();

        /// <summary>
        /// Index of the current wave being processed.
        /// </summary>
        private int currentWaveIndex = 0;

        /// <summary>
        /// Whether the spawner is currently active.
        /// </summary>
        private bool isSpawning = false;

        /// <summary>
        /// Whether a wave is currently in progress.
        /// </summary>
        private bool waveInProgress = false;

        /// <summary>
        /// Tracks how many objects have been spawned from the current wave.
        /// </summary>
        private int currentWaveSpawnCount = 0;

        /// <summary>
        /// Total number of objects in the current wave.
        /// </summary>
        private int currentWaveTotalCount = 0;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current number of active spawns.
        /// </summary>
        public int ActiveSpawnCount => activeSpawns.Count;

        /// <summary>
        /// Gets the index of the current wave (0-based).
        /// </summary>
        public int CurrentWaveIndex => currentWaveIndex;

        /// <summary>
        /// Gets whether the spawner is currently spawning.
        /// </summary>
        public bool IsSpawning => isSpawning;

        /// <summary>
        /// Gets whether a wave is currently in progress.
        /// </summary>
        public bool WaveInProgress => waveInProgress;

        /// <summary>
        /// Gets the total number of waves.
        /// </summary>
        public int TotalWaves => waves.Count;

        /// <summary>
        /// Gets whether all waves have been completed.
        /// </summary>
        public bool AllWavesComplete => currentWaveIndex >= waves.Count && !waveInProgress;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when a wave starts.
        /// </summary>
        public event System.Action<int, Wave> OnWaveStart;

        /// <summary>
        /// Event fired when a wave completes.
        /// </summary>
        public event System.Action<int, Wave> OnWaveComplete;

        /// <summary>
        /// Event fired when an object is spawned.
        /// </summary>
        public event System.Action<GameObject> OnObjectSpawned;

        /// <summary>
        /// Event fired when an object is destroyed/removed.
        /// </summary>
        public event System.Action<GameObject> OnObjectDestroyed;

        /// <summary>
        /// Event fired when all waves are complete.
        /// </summary>
        public event System.Action OnAllWavesComplete;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Validate settings
            maxActiveSpawns = Mathf.Max(0, maxActiveSpawns);
            spawnDelay = Mathf.Max(0f, spawnDelay);
            waveStartDelay = Mathf.Max(0f, waveStartDelay);

            if (waves == null)
            {
                waves = new List<Wave>();
            }
        }

        private void Start()
        {
            if (autoStart)
            {
                StartSpawning();
            }
        }

        private void Update()
        {
            if (!isSpawning)
            {
                return;
            }

            // Clean up destroyed objects from the active spawns list
            CleanupDestroyedObjects();

            // Check if we need to spawn more objects to maintain per-object minimums
            CheckAndMaintainMinActiveSpawns();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Starts the wave spawning process.
        /// </summary>
        public void StartSpawning()
        {
            if (isSpawning)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[WaveSpawner] Already spawning, ignoring StartSpawning call.");
                }
                return;
            }

            if (waves.Count == 0)
            {
                Debug.LogWarning("[WaveSpawner] No waves configured to spawn.");
                return;
            }

            isSpawning = true;
            currentWaveIndex = 0;
            
            if (debugMode)
            {
                Debug.Log($"[WaveSpawner] Starting wave spawning with {waves.Count} waves in {waveStartMode} mode.");
            }

            // Start the first wave
            StartWave(currentWaveIndex);
        }

        /// <summary>
        /// Stops the wave spawning process.
        /// </summary>
        /// <param name="destroyActiveSpawns">Whether to destroy all currently active spawns.</param>
        public void StopSpawning(bool destroyActiveSpawns = false)
        {
            isSpawning = false;
            waveInProgress = false;
            CancelInvoke(nameof(SpawnNextObject));
            CancelInvoke(nameof(StartWave));
            CancelInvoke(nameof(SpawnForMinActive));

            if (destroyActiveSpawns)
            {
                DestroyAllActiveSpawns();
            }

            if (debugMode)
            {
                Debug.Log("[WaveSpawner] Stopped wave spawning.");
            }
        }

        /// <summary>
        /// Resets the wave spawner to its initial state.
        /// </summary>
        /// <param name="destroyActiveSpawns">Whether to destroy all currently active spawns.</param>
        public void ResetSpawner(bool destroyActiveSpawns = false)
        {
            StopSpawning(destroyActiveSpawns);
            currentWaveIndex = 0;
            currentWaveSpawnCount = 0;
            currentWaveTotalCount = 0;
            spawnableObjectMap.Clear();

            if (debugMode)
            {
                Debug.Log("[WaveSpawner] Spawner reset to initial state.");
            }
        }

        /// <summary>
        /// Destroys all currently active spawned objects.
        /// </summary>
        public void DestroyAllActiveSpawns()
        {
            for (int i = activeSpawns.Count - 1; i >= 0; i--)
            {
                if (activeSpawns[i] != null)
                {
                    Destroy(activeSpawns[i]);
                }
            }
            activeSpawns.Clear();
            spawnableObjectMap.Clear();

            if (debugMode)
            {
                Debug.Log("[WaveSpawner] All active spawns destroyed.");
            }
        }

        /// <summary>
        /// Gets a list of all currently active spawned objects.
        /// </summary>
        /// <returns>List of active GameObjects.</returns>
        public List<GameObject> GetActiveSpawns()
        {
            CleanupDestroyedObjects();
            return new List<GameObject>(activeSpawns);
        }

        /// <summary>
        /// Gets all active spawns of a specific type.
        /// </summary>
        /// <typeparam name="T">The component type to filter by.</typeparam>
        /// <returns>List of GameObjects with the specified component.</returns>
        public List<GameObject> GetActiveSpawns<T>() where T : Component
        {
            CleanupDestroyedObjects();
            return activeSpawns.Where(obj => obj != null && obj.GetComponent<T>() != null).ToList();
        }

        /// <summary>
        /// Manually adds an object to be tracked as an active spawn.
        /// </summary>
        /// <param name="obj">The GameObject to track.</param>
        public void TrackSpawnedObject(GameObject obj)
        {
            TrackSpawnedObject(obj, null);
        }

        /// <summary>
        /// Manually adds an object to be tracked as an active spawn.
        /// </summary>
        /// <param name="obj">The GameObject to track.</param>
        /// <param name="spawnable">The SpawnableObject configuration this object came from.</param>
        public void TrackSpawnedObject(GameObject obj, SpawnableObject spawnable)
        {
            if (obj == null)
            {
                Debug.LogWarning("[WaveSpawner] Cannot track null object.");
                return;
            }

            if (!activeSpawns.Contains(obj))
            {
                activeSpawns.Add(obj);
                
                // Subscribe to death event if the object has Health component
                Health health = obj.GetComponent<Health>();
                if (health != null)
                {
                    health.OnDeath += () => OnSpawnedObjectDied(obj);
                }

                // Map the object to its spawnable configuration
                if (spawnable != null)
                {
                    spawnableObjectMap[obj] = spawnable;
                }

                OnObjectSpawned?.Invoke(obj);

                if (debugMode)
                {
                    Debug.Log($"[WaveSpawner] Tracking object: {obj.name}");
                }
            }
        }

        /// <summary>
        /// Manually removes an object from tracking.
        /// </summary>
        /// <param name="obj">The GameObject to stop tracking.</param>
        public void UntrackSpawnedObject(GameObject obj)
        {
            if (activeSpawns.Remove(obj))
            {
                // Unsubscribe from death event if the object has Health component
                Health health = obj.GetComponent<Health>();
                if (health != null)
                {
                    health.OnDeath -= () => OnSpawnedObjectDied(obj);
                }

                // Remove from spawnable object map
                spawnableObjectMap.Remove(obj);

                OnObjectDestroyed?.Invoke(obj);

                if (debugMode)
                {
                    Debug.Log($"[WaveSpawner] Untracking object: {obj.name}");
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Starts spawning the specified wave.
        /// </summary>
        /// <param name="waveIndex">Index of the wave to start.</param>
        private void StartWave(int waveIndex)
        {
            if (waveIndex >= waves.Count)
            {
                if (debugMode)
                {
                    Debug.Log("[WaveSpawner] All waves completed.");
                }
                OnAllWavesComplete?.Invoke();
                return;
            }

            Wave wave = waves[waveIndex];
            waveInProgress = true;
            currentWaveSpawnCount = 0;
            currentWaveTotalCount = wave.spawnableObjects.Sum(obj => obj.count);

            if (debugMode)
            {
                Debug.Log($"[WaveSpawner] Starting wave {waveIndex}: {wave.waveName} with {currentWaveTotalCount} objects.");
            }

            OnWaveStart?.Invoke(waveIndex, wave);

            // Apply wave start delay
            float delay = wave.waveStartDelay > 0f ? wave.waveStartDelay : waveStartDelay;
            
            if (delay > 0f)
            {
                Invoke(nameof(SpawnNextObject), delay);
            }
            else
            {
                SpawnNextObject();
            }
        }

        /// <summary>
        /// Spawns the next object in the current wave.
        /// </summary>
        private void SpawnNextObject()
        {
            if (!isSpawning || currentWaveIndex >= waves.Count)
            {
                return;
            }

            Wave wave = waves[currentWaveIndex];

            // Find the next spawnable object that still needs to be spawned
            foreach (SpawnableObject spawnable in wave.spawnableObjects)
            {
                if (spawnable.spawnedCount < spawnable.count)
                {
                    // Check if we're at max active spawns
                    if (ActiveSpawnCount >= maxActiveSpawns)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[WaveSpawner] At max active spawns ({maxActiveSpawns}), waiting...");
                        }
                        // Try again later
                        Invoke(nameof(SpawnNextObject), spawnDelay);
                        return;
                    }

                    // Spawn the object
                    SpawnObject(spawnable);
                    spawnable.spawnedCount++;
                    currentWaveSpawnCount++;

                    // Check if the wave is complete
                    if (currentWaveSpawnCount >= currentWaveTotalCount)
                    {
                        waveInProgress = false;
                        OnWaveComplete?.Invoke(currentWaveIndex, wave);

                        if (debugMode)
                        {
                            Debug.Log($"[WaveSpawner] Wave {currentWaveIndex} completed.");
                        }

                        // Move to next wave based on mode
                        if (waveStartMode == WaveStartMode.Sequential)
                        {
                            currentWaveIndex++;
                            StartWave(currentWaveIndex);
                        }
                        else
                        {
                            // Instantaneous mode - start next wave immediately
                            currentWaveIndex++;
                            if (currentWaveIndex < waves.Count)
                            {
                                StartWave(currentWaveIndex);
                            }
                            else
                            {
                                OnAllWavesComplete?.Invoke();
                            }
                        }
                    }
                    else
                    {
                        // Spawn next object after delay
                        float delay = spawnable.spawnDelay > 0f ? spawnable.spawnDelay : spawnDelay;
                        float randomDelay = Random.Range(spawnable.minRandomDelay, spawnable.maxRandomDelay);
                        Invoke(nameof(SpawnNextObject), delay + randomDelay);
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Spawns a single object from the given spawnable configuration.
        /// </summary>
        /// <param name="spawnable">The spawnable object configuration.</param>
        private void SpawnObject(SpawnableObject spawnable)
        {
            if (spawnable.prefab == null)
            {
                Debug.LogError($"[WaveSpawner] No prefab assigned for spawnable object.");
                return;
            }

            // Determine spawn point
            Transform spawnPoint = spawnable.spawnPoint != null ? spawnable.spawnPoint : defaultSpawnPoint;
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;

            // Apply offset and radius
            if (spawnable.spawnRadius > 0f)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnable.spawnRadius;
                spawnPosition += new Vector3(randomCircle.x, 0f, randomCircle.y);
            }
            spawnPosition += spawnable.spawnOffset;

            // Spawn the object
            GameObject spawnedObject = Instantiate(spawnable.prefab, spawnPosition, Quaternion.identity);
            
            // Track the spawned object with its spawnable configuration
            TrackSpawnedObject(spawnedObject, spawnable);

            if (debugMode)
            {
                Debug.Log($"[WaveSpawner] Spawned {spawnedObject.name} at {spawnPosition}");
            }
        }

        /// <summary>
        /// Cleans up destroyed objects from the active spawns list.
        /// </summary>
        private void CleanupDestroyedObjects()
        {
            for (int i = activeSpawns.Count - 1; i >= 0; i--)
            {
                if (activeSpawns[i] == null)
                {
                    activeSpawns.RemoveAt(i);
                }
            }
            
            // Clean up the spawnable object map for destroyed objects
            List<GameObject> keysToRemove = new List<GameObject>();
            foreach (var kvp in spawnableObjectMap)
            {
                if (kvp.Key == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (GameObject key in keysToRemove)
            {
                spawnableObjectMap.Remove(key);
            }
        }

        /// <summary>
        /// Called when a spawned object dies (via Health component).
        /// </summary>
        /// <param name="obj">The object that died.</param>
        private void OnSpawnedObjectDied(GameObject obj)
        {
            if (obj != null)
            {
                UntrackSpawnedObject(obj);
            }
        }

        /// <summary>
        /// Checks and maintains minimum active spawns for each SpawnableObject.
        /// Spawns more objects of a type if its active count falls below its minimum.
        /// </summary>
        private void CheckAndMaintainMinActiveSpawns()
        {
            // Only check after waves have started spawning
            if (currentWaveIndex == 0 && !waveInProgress && activeSpawns.Count == 0)
            {
                return;
            }

            // Collect all SpawnableObjects from all waves (including past waves)
            List<SpawnableObject> allSpawnables = new List<SpawnableObject>();
            for (int i = 0; i <= currentWaveIndex && i < waves.Count; i++)
            {
                allSpawnables.AddRange(waves[i].spawnableObjects);
            }

            // Check each spawnable object type
            foreach (SpawnableObject spawnable in allSpawnables)
            {
                if (spawnable.minActiveSpawns <= 0)
                {
                    continue;
                }

                // Count active spawns for this spawnable type
                int activeCountForType = 0;
                foreach (var kvp in spawnableObjectMap)
                {
                    if (kvp.Value == spawnable && kvp.Key != null)
                    {
                        activeCountForType++;
                    }
                }

                // Spawn more if below minimum
                if (activeCountForType < spawnable.minActiveSpawns)
                {
                    // Check global max limit
                    if (ActiveSpawnCount >= maxActiveSpawns)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[WaveSpawner] At global max active spawns ({maxActiveSpawns}), cannot spawn more {spawnable.prefab.name}");
                        }
                        continue;
                    }

                    int needed = spawnable.minActiveSpawns - activeCountForType;
                    int canSpawn = Mathf.Min(needed, maxActiveSpawns - ActiveSpawnCount);

                    if (canSpawn > 0)
                    {
                        // Schedule spawns with delay instead of spawning immediately
                        float delay = spawnable.spawnDelay > 0f ? spawnable.spawnDelay : spawnDelay;
                        float randomDelay = Random.Range(spawnable.minRandomDelay, spawnable.maxRandomDelay);
                        
                        if (debugMode)
                        {
                            Debug.Log($"[WaveSpawner] Scheduling {canSpawn} more {spawnable.prefab.name} to maintain minimum ({spawnable.minActiveSpawns}) with delay {delay + randomDelay}s");
                        }

                        // Use Invoke to spawn with delay
                        for (int i = 0; i < canSpawn; i++)
                        {
                            Invoke(nameof(SpawnForMinActive), delay + randomDelay);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Spawns a single object to maintain minimum active spawns.
        /// Called via Invoke to respect spawn delays.
        /// </summary>
        private void SpawnForMinActive()
        {
            // Only check after waves have started spawning
            if (currentWaveIndex == 0 && !waveInProgress && activeSpawns.Count == 0)
            {
                return;
            }

            // Collect all SpawnableObjects from all waves (including past waves)
            List<SpawnableObject> allSpawnables = new List<SpawnableObject>();
            for (int i = 0; i <= currentWaveIndex && i < waves.Count; i++)
            {
                allSpawnables.AddRange(waves[i].spawnableObjects);
            }

            // Find the first spawnable object that needs more spawns
            foreach (SpawnableObject spawnable in allSpawnables)
            {
                if (spawnable.minActiveSpawns <= 0)
                {
                    continue;
                }

                // Count active spawns for this spawnable type
                int activeCountForType = 0;
                foreach (var kvp in spawnableObjectMap)
                {
                    if (kvp.Value == spawnable && kvp.Key != null)
                    {
                        activeCountForType++;
                    }
                }

                // Spawn if below minimum
                if (activeCountForType < spawnable.minActiveSpawns)
                {
                    // Check global max limit
                    if (ActiveSpawnCount >= maxActiveSpawns)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[WaveSpawner] At global max active spawns ({maxActiveSpawns}), cannot spawn more {spawnable.prefab.name}");
                        }
                        return;
                    }

                    SpawnObject(spawnable);
                    
                    if (debugMode)
                    {
                        Debug.Log($"[WaveSpawner] Spawned {spawnable.prefab.name} to maintain minimum ({spawnable.minActiveSpawns})");
                    }
                    
                    return;
                }
            }
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            // Ensure values are valid in editor
            maxActiveSpawns = Mathf.Max(0, maxActiveSpawns);
            spawnDelay = Mathf.Max(0f, spawnDelay);
            waveStartDelay = Mathf.Max(0f, waveStartDelay);
        }

        #endregion
    }

}
