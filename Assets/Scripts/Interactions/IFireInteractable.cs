using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Interface for objects that can be lit on fire by a match or other ignition source.
    /// Provides a common contract for fire interaction behavior across different objects.
    /// </summary>
    public interface IFireInteractable
    {
        /// <summary>
        /// Gets whether the object is currently on fire.
        /// </summary>
        bool IsOnFire { get; }

        /// <summary>
        /// Gets the transform of this interactable.
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Called when the object is ignited by a fire source.
        /// </summary>
        /// <param name="ignitionSource">The GameObject that ignited this object (e.g., a match).</param>
        /// <returns>True if the object was successfully ignited, false otherwise.</returns>
        bool Ignite(GameObject ignitionSource);

        /// <summary>
        /// Called when the object should extinguish its fire.
        /// </summary>
        void Extinguish();

        /// <summary>
        /// Checks if this object can currently be ignited.
        /// </summary>
        /// <returns>True if the object can be ignited, false otherwise.</returns>
        bool CanIgnite();
    }
}
