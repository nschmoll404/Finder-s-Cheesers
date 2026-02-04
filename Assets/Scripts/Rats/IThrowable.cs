using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Interface for objects that can be thrown.
    /// Provides a common contract for throwable behavior across different objects.
    /// </summary>
    public interface IThrowable
    {
        /// <summary>
        /// Gets whether the object is currently being thrown.
        /// </summary>
        bool IsThrowing { get; }

        /// <summary>
        /// Event fired when the object is thrown.
        /// </summary>
        event System.Action<Vector3> OnThrown;

        /// <summary>
        /// Event fired when the object lands.
        /// </summary>
        event System.Action<Vector3> OnLanded;

        /// <summary>
        /// Starts a throw to the specified destination.
        /// </summary>
        /// <param name="destination">The target destination for the throw.</param>
        /// <param name="speed">The launch speed for the arc.</param>
        void ThrowTo(Vector3 destination, float speed);

        /// <summary>
        /// Cancels the current throw (if any).
        /// </summary>
        void CancelThrow();

        /// <summary>
        /// Simply drops the object, allowing it to fall with physics.
        /// This is used when releasing the object without throwing.
        /// </summary>
        void Drop();

        /// <summary>
        /// Sets the throw duration.
        /// </summary>
        /// <param name="duration">The duration of the throw animation.</param>
        void SetThrowDuration(float duration);

        /// <summary>
        /// Gets the current throw duration.
        /// </summary>
        /// <returns>The current throw duration.</returns>
        float GetThrowDuration();
    }
}
