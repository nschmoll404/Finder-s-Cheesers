using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Interface for AI components that can be prioritized.
    /// Components with higher priority values take precedence when triggered.
    /// </summary>
    public interface IEnemyAIComponent
    {
        /// <summary>
        /// Gets the priority of this AI component.
        /// Higher values take precedence when multiple AI components are triggered.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets whether this AI component is currently triggered and wants to run.
        /// </summary>
        bool IsTriggered { get; }

        /// <summary>
        /// Gets or sets whether this AI component is currently allowed to run its behavior.
        /// </summary>
        bool IsRunning { get; set; }

        /// <summary>
        /// Called by EnemyAI when this component transitions into the running state.
        /// Use this to perform any setup logic (e.g. starting movement, enabling behaviors).
        /// </summary>
        void OnStartRunning();

        /// <summary>
        /// Called by EnemyAI when this component transitions out of the running state.
        /// Use this to perform any cleanup logic (e.g. stopping movement, resetting state).
        /// </summary>
        void OnExitRunning();

        /// <summary>
        /// Event fired when this AI component is activated.
        /// </summary>
        event System.Action OnActivated;

        /// <summary>
        /// Event fired when this AI component is deactivated.
        /// </summary>
        event System.Action OnDeactivated;
    }
}
