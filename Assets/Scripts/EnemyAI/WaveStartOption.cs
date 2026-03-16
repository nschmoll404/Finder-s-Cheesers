using UnityEngine;

namespace WaveSpawning
{
    /// <summary>
    /// Defines how waves should start in relation to each other.
    /// </summary>
    public enum WaveStartOption
    {
        /// <summary>
        /// All waves start immediately based on their start delays, regardless of other waves.
        /// </summary>
        Instantaneous,
        
        /// <summary>
        /// Each wave waits until the previous wave has finished spawning all its enemies before starting.
        /// </summary>
        Sequential
    }
}
