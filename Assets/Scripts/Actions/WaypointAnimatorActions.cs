using UnityEngine;
using System;

namespace Actions
{
    #region Waypoint Animator Actions

    /// <summary>
    /// Plays the waypoint animation on a target WaypointAnimator component.
    /// </summary>
    [Serializable]
    public class PlayWaypointAnimatorAction : IAction
    {
        [Tooltip("The GameObject containing the WaypointAnimator component")]
        public GameObject target;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                WaypointAnimator animator = searchInChildren 
                    ? target.GetComponentInChildren<WaypointAnimator>()
                    : target.GetComponent<WaypointAnimator>();

                if (animator != null)
                {
                    animator.Play();
                }
                else
                {
                    Debug.LogWarning($"PlayWaypointAnimatorAction: WaypointAnimator component not found on {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("PlayWaypointAnimatorAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Pauses the waypoint animation on a target WaypointAnimator component.
    /// </summary>
    [Serializable]
    public class PauseWaypointAnimatorAction : IAction
    {
        [Tooltip("The GameObject containing the WaypointAnimator component")]
        public GameObject target;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                WaypointAnimator animator = searchInChildren
                    ? target.GetComponentInChildren<WaypointAnimator>()
                    : target.GetComponent<WaypointAnimator>();

                if (animator != null)
                {
                    animator.Pause();
                }
                else
                {
                    Debug.LogWarning($"PauseWaypointAnimatorAction: WaypointAnimator component not found on {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("PauseWaypointAnimatorAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Stops the waypoint animation on a target WaypointAnimator component.
    /// </summary>
    [Serializable]
    public class StopWaypointAnimatorAction : IAction
    {
        [Tooltip("The GameObject containing the WaypointAnimator component")]
        public GameObject target;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                WaypointAnimator animator = searchInChildren
                    ? target.GetComponentInChildren<WaypointAnimator>()
                    : target.GetComponent<WaypointAnimator>();

                if (animator != null)
                {
                    animator.Stop();
                }
                else
                {
                    Debug.LogWarning($"StopWaypointAnimatorAction: WaypointAnimator component not found on {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("StopWaypointAnimatorAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Resets the waypoint animation on a target WaypointAnimator component.
    /// </summary>
    [Serializable]
    public class ResetWaypointAnimatorAction : IAction
    {
        [Tooltip("The GameObject containing the WaypointAnimator component")]
        public GameObject target;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                WaypointAnimator animator = searchInChildren
                    ? target.GetComponentInChildren<WaypointAnimator>()
                    : target.GetComponent<WaypointAnimator>();

                if (animator != null)
                {
                    animator.Reset();
                }
                else
                {
                    Debug.LogWarning($"ResetWaypointAnimatorAction: WaypointAnimator component not found on {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("ResetWaypointAnimatorAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Restarts the waypoint animation on a target WaypointAnimator component.
    /// </summary>
    [Serializable]
    public class RestartWaypointAnimatorAction : IAction
    {
        [Tooltip("The GameObject containing the WaypointAnimator component")]
        public GameObject target;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                WaypointAnimator animator = searchInChildren
                    ? target.GetComponentInChildren<WaypointAnimator>()
                    : target.GetComponent<WaypointAnimator>();

                if (animator != null)
                {
                    animator.Restart();
                }
                else
                {
                    Debug.LogWarning($"RestartWaypointAnimatorAction: WaypointAnimator component not found on {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("RestartWaypointAnimatorAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Makes the waypoint animator jump to a specific waypoint.
    /// </summary>
    [Serializable]
    public class GoToWaypointAction : IAction
    {
        [Tooltip("The GameObject containing the WaypointAnimator component")]
        public GameObject target;

        [Tooltip("The waypoint index to jump to")]
        public int waypointIndex = 0;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                WaypointAnimator animator = searchInChildren
                    ? target.GetComponentInChildren<WaypointAnimator>()
                    : target.GetComponent<WaypointAnimator>();

                if (animator != null)
                {
                    animator.GoToWaypoint(waypointIndex);
                }
                else
                {
                    Debug.LogWarning($"GoToWaypointAction: WaypointAnimator component not found on {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("GoToWaypointAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Sets the movement speed of a waypoint animator.
    /// </summary>
    [Serializable]
    public class SetWaypointAnimatorSpeedAction : IAction
    {
        [Tooltip("The GameObject containing the WaypointAnimator component")]
        public GameObject target;

        [Tooltip("The new speed in units per second")]
        public float speed = 2f;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                WaypointAnimator animator = searchInChildren
                    ? target.GetComponentInChildren<WaypointAnimator>()
                    : target.GetComponent<WaypointAnimator>();

                if (animator != null)
                {
                    animator.SetSpeed(speed);
                }
                else
                {
                    Debug.LogWarning($"SetWaypointAnimatorSpeedAction: WaypointAnimator component not found on {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("SetWaypointAnimatorSpeedAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Sets the total time to complete the path for a waypoint animator.
    /// </summary>
    [Serializable]
    public class SetWaypointAnimatorTotalTimeAction : IAction
    {
        [Tooltip("The GameObject containing the WaypointAnimator component")]
        public GameObject target;

        [Tooltip("The new total time in seconds")]
        public float totalTime = 5f;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                WaypointAnimator animator = searchInChildren
                    ? target.GetComponentInChildren<WaypointAnimator>()
                    : target.GetComponent<WaypointAnimator>();

                if (animator != null)
                {
                    animator.SetTotalTime(totalTime);
                }
                else
                {
                    Debug.LogWarning($"SetWaypointAnimatorTotalTimeAction: WaypointAnimator component not found on {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("SetWaypointAnimatorTotalTimeAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Sets the movement mode of a waypoint animator.
    /// </summary>
    [Serializable]
    public class SetWaypointAnimatorModeAction : IAction
    {
        [Tooltip("The GameObject containing the WaypointAnimator component")]
        public GameObject target;

        [Tooltip("The new movement mode")]
        public WaypointAnimator.MovementMode movementMode = WaypointAnimator.MovementMode.Speed;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                WaypointAnimator animator = searchInChildren
                    ? target.GetComponentInChildren<WaypointAnimator>()
                    : target.GetComponent<WaypointAnimator>();

                if (animator != null)
                {
                    animator.SetMovementMode(movementMode);
                }
                else
                {
                    Debug.LogWarning($"SetWaypointAnimatorModeAction: WaypointAnimator component not found on {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("SetWaypointAnimatorModeAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Sets the progress along the path for a waypoint animator.
    /// </summary>
    [Serializable]
    public class SetWaypointAnimatorProgressAction : IAction
    {
        [Tooltip("The GameObject containing the WaypointAnimator component")]
        public GameObject target;

        [Tooltip("The progress value between 0 and 1")]
        [Range(0f, 1f)]
        public float progress = 0f;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                WaypointAnimator animator = searchInChildren
                    ? target.GetComponentInChildren<WaypointAnimator>()
                    : target.GetComponent<WaypointAnimator>();

                if (animator != null)
                {
                    animator.SetProgress(progress);
                }
                else
                {
                    Debug.LogWarning($"SetWaypointAnimatorProgressAction: WaypointAnimator component not found on {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("SetWaypointAnimatorProgressAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Adds a waypoint to a waypoint animator.
    /// </summary>
    [Serializable]
    public class AddWaypointAction : IAction
    {
        [Tooltip("The GameObject containing the WaypointAnimator component")]
        public GameObject target;

        [Tooltip("The waypoint transform to add")]
        public Transform waypoint;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                WaypointAnimator animator = searchInChildren
                    ? target.GetComponentInChildren<WaypointAnimator>()
                    : target.GetComponent<WaypointAnimator>();

                if (animator != null)
                {
                    animator.AddWaypoint(waypoint);
                }
                else
                {
                    Debug.LogWarning($"AddWaypointAction: WaypointAnimator component not found on {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("AddWaypointAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Removes a waypoint from a waypoint animator at a specific index.
    /// </summary>
    [Serializable]
    public class RemoveWaypointAction : IAction
    {
        [Tooltip("The GameObject containing the WaypointAnimator component")]
        public GameObject target;

        [Tooltip("The index of the waypoint to remove")]
        public int index = 0;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                WaypointAnimator animator = searchInChildren
                    ? target.GetComponentInChildren<WaypointAnimator>()
                    : target.GetComponent<WaypointAnimator>();

                if (animator != null)
                {
                    animator.RemoveWaypoint(index);
                }
                else
                {
                    Debug.LogWarning($"RemoveWaypointAction: WaypointAnimator component not found on {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("RemoveWaypointAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Clears all waypoints from a waypoint animator.
    /// </summary>
    [Serializable]
    public class ClearWaypointsAction : IAction
    {
        [Tooltip("The GameObject containing the WaypointAnimator component")]
        public GameObject target;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                WaypointAnimator animator = searchInChildren
                    ? target.GetComponentInChildren<WaypointAnimator>()
                    : target.GetComponent<WaypointAnimator>();

                if (animator != null)
                {
                    animator.ClearWaypoints();
                }
                else
                {
                    Debug.LogWarning($"ClearWaypointsAction: WaypointAnimator component not found on {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("ClearWaypointsAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Controls multiple waypoint animators at once.
    /// </summary>
    [Serializable]
    public class PlayWaypointAnimatorListAction : IAction
    {
        [Tooltip("The list of GameObjects containing WaypointAnimator components")]
        public GameObject[] targets;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        [Tooltip("Whether to log warnings for null GameObjects")]
        public bool logWarnings = true;

        public void Execute(object context = null)
        {
            if (targets == null || targets.Length == 0)
            {
                Debug.LogWarning("PlayWaypointAnimatorListAction: No GameObjects in the list");
                return;
            }

            int nullCount = 0;
            int notFoundCount = 0;

            foreach (var target in targets)
            {
                if (target != null)
                {
                    WaypointAnimator animator = searchInChildren
                        ? target.GetComponentInChildren<WaypointAnimator>()
                        : target.GetComponent<WaypointAnimator>();

                    if (animator != null)
                    {
                        animator.Play();
                    }
                    else
                    {
                        notFoundCount++;
                    }
                }
                else
                {
                    nullCount++;
                }
            }

            if (nullCount > 0 && logWarnings)
            {
                Debug.LogWarning($"PlayWaypointAnimatorListAction: {nullCount} null GameObject(s) found in the list");
            }

            if (notFoundCount > 0 && logWarnings)
            {
                Debug.LogWarning($"PlayWaypointAnimatorListAction: {notFoundCount} GameObject(s) without WaypointAnimator component found");
            }
        }
    }

    /// <summary>
    /// Stops multiple waypoint animators at once.
    /// </summary>
    [Serializable]
    public class StopWaypointAnimatorListAction : IAction
    {
        [Tooltip("The list of GameObjects containing WaypointAnimator components")]
        public GameObject[] targets;

        [Tooltip("Whether to search in children if not found on the target")]
        public bool searchInChildren = true;

        [Tooltip("Whether to log warnings for null GameObjects")]
        public bool logWarnings = true;

        public void Execute(object context = null)
        {
            if (targets == null || targets.Length == 0)
            {
                Debug.LogWarning("StopWaypointAnimatorListAction: No GameObjects in the list");
                return;
            }

            int nullCount = 0;
            int notFoundCount = 0;

            foreach (var target in targets)
            {
                if (target != null)
                {
                    WaypointAnimator animator = searchInChildren
                        ? target.GetComponentInChildren<WaypointAnimator>()
                        : target.GetComponent<WaypointAnimator>();

                    if (animator != null)
                    {
                        animator.Stop();
                    }
                    else
                    {
                        notFoundCount++;
                    }
                }
                else
                {
                    nullCount++;
                }
            }

            if (nullCount > 0 && logWarnings)
            {
                Debug.LogWarning($"StopWaypointAnimatorListAction: {nullCount} null GameObject(s) found in the list");
            }

            if (notFoundCount > 0 && logWarnings)
            {
                Debug.LogWarning($"StopWaypointAnimatorListAction: {notFoundCount} GameObject(s) without WaypointAnimator component found");
            }
        }
    }

    #endregion
}
