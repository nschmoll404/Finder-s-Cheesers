using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Interface for objects that can be interacted with by the player.
    /// Implementations should handle the logic of what happens when an interaction occurs.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Event raised when this object is interacted with.
        /// Passes the interactor GameObject and whether the interaction was successful.
        /// </summary>
        event System.Action<GameObject, bool> OnInteracted;

        /// <summary>
        /// Gets a description of what happens when this object is interacted with.
        /// This can be used for UI or tooltips.
        /// </summary>
        string InteractionDescription { get; }

        /// <summary>
        /// Gets the transform of this interactable for positioning or distance calculations.
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Checks if this object can currently be interacted with.
        /// </summary>
        /// <returns>True if interaction is available, false otherwise.</returns>
        bool CanInteract();

        /// <summary>
        /// Performs the interaction with this object.
        /// </summary>
        /// <param name="interactor">The GameObject performing the interaction.</param>
        /// <returns>True if the interaction was successful, false otherwise.</returns>
        bool Interact(GameObject interactor);
    }
}
