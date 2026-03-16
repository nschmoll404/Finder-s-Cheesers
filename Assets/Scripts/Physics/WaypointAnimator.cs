using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Animates a transform through waypoints with configurable speed/time and easing curves.
/// Ideal for moving platforms, elevators, and other animated objects.
/// </summary>
public class WaypointAnimator : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("The transforms to move through in sequence")]
    public List<Transform> waypoints = new List<Transform>();
    
    [Tooltip("If true, the animation will loop back to the first waypoint")]
    public bool loop = true;
    
    [Tooltip("If true, movement will reverse direction when reaching the end instead of looping")]
    public bool pingPong = false;
    
    [Header("Movement Settings")]
    [Tooltip("Movement mode: Speed (units per second) or Time (seconds to complete path)")]
    public MovementMode movementMode = MovementMode.Speed;
    
    [Tooltip("Movement speed in units per second (only used when MovementMode is Speed)")]
    public float speed = 2f;
    
    [Tooltip("Total time to complete the path in seconds (only used when MovementMode is Time)")]
    public float totalTime = 5f;
    
    [Header("Easing")]
    [Tooltip("The easing curve to apply to movement")]
    public AnimationCurve easingCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    
    [Tooltip("If true, easing is applied to the entire path. If false, easing is applied between each waypoint.")]
    public bool easeEntirePath = false;
    
    [Header("Options")]
    [Tooltip("If true, the object will rotate to face the direction of movement")]
    public bool faceDirection = false;
    
    [Tooltip("Rotation speed when facing direction")]
    public float rotationSpeed = 5f;
    
    [Tooltip("If true, starts playing automatically when the scene loads")]
    public bool playOnAwake = true;
    
    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnWaypointReached;
    public UnityEngine.Events.UnityEvent OnPathComplete;
    public UnityEngine.Events.UnityEvent OnMovementStarted;
    public UnityEngine.Events.UnityEvent OnMovementStopped;
    
    // Public state
    public bool IsPlaying { get; private set; }
    public bool IsPaused { get; private set; }
    public int CurrentWaypointIndex { get; private set; }
    public float Progress { get; private set; }
    public int Direction { get; private set; } = 1;
    
    // Private state
    private Transform _transform;
    private float _currentProgress;
    private int _currentSegment;
    private bool _movingForward = true;
    private float _totalPathLength;
    private List<float> _segmentLengths = new List<float>();
    
    public enum MovementMode
    {
        Speed,
        Time
    }
    
    private void Awake()
    {
        _transform = transform;
        CalculatePathLength();
    }
    
    private void Start()
    {
        if (playOnAwake)
        {
            Play();
        }
    }
    
    private void CalculatePathLength()
    {
        _totalPathLength = 0f;
        _segmentLengths.Clear();
        
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                float segmentLength = Vector3.Distance(waypoints[i].position, waypoints[i + 1].position);
                _segmentLengths.Add(segmentLength);
                _totalPathLength += segmentLength;
            }
        }
        
        // Add segment for loop back if needed
        if (loop && !pingPong && waypoints.Count > 1)
        {
            if (waypoints[waypoints.Count - 1] != null && waypoints[0] != null)
            {
                float segmentLength = Vector3.Distance(waypoints[waypoints.Count - 1].position, waypoints[0].position);
                _segmentLengths.Add(segmentLength);
                _totalPathLength += segmentLength;
            }
        }
    }
    
    private void Update()
    {
        if (!IsPlaying || IsPaused || waypoints.Count < 2)
            return;
        
        UpdateMovement();
    }
    
    private void UpdateMovement()
    {
        // Calculate movement delta
        float movementDelta;
        if (movementMode == MovementMode.Speed)
        {
            movementDelta = speed * Time.deltaTime;
        }
        else
        {
            movementDelta = _totalPathLength / totalTime * Time.deltaTime;
        }
        
        // Apply easing if easing entire path
        float easedProgress = easeEntirePath ? easingCurve.Evaluate(_currentProgress / _totalPathLength) : _currentProgress / _totalPathLength;
        
        // Update progress
        _currentProgress += movementDelta * Direction;
        
        // Handle path completion
        if (_currentProgress >= _totalPathLength || _currentProgress <= 0)
        {
            HandlePathCompletion();
        }
        
        // Calculate position based on progress
        UpdatePosition();
    }
    
    private void UpdatePosition()
    {
        float progress = _currentProgress;
        float accumulatedLength = 0f;
        
        // Find which segment we're in
        for (int i = 0; i < _segmentLengths.Count; i++)
        {
            if (progress <= accumulatedLength + _segmentLengths[i])
            {
                _currentSegment = i;
                float segmentProgress = (progress - accumulatedLength) / _segmentLengths[i];
                
                // Apply easing to segment if not easing entire path
                if (!easeEntirePath)
                {
                    segmentProgress = easingCurve.Evaluate(segmentProgress);
                }
                
                // Get waypoints for this segment
                int startIndex = i;
                int endIndex = (i + 1) % waypoints.Count;
                
                if (waypoints[startIndex] != null && waypoints[endIndex] != null)
                {
                    Vector3 targetPosition = Vector3.Lerp(waypoints[startIndex].position, waypoints[endIndex].position, segmentProgress);
                    _transform.position = targetPosition;
                    
                    if (faceDirection)
                    {
                        Vector3 direction = (waypoints[endIndex].position - waypoints[startIndex].position).normalized;
                        if (Direction < 0) direction = -direction;
                        
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                    }
                }
                
                break;
            }
            
            accumulatedLength += _segmentLengths[i];
        }
        
        // Update public progress property (0-1)
        Progress = Mathf.Clamp01(_currentProgress / _totalPathLength);
    }
    
    private void HandlePathCompletion()
    {
        if (pingPong)
        {
            // Reverse direction
            Direction *= -1;
            _currentProgress = Mathf.Clamp(_currentProgress, 0, _totalPathLength);
            OnPathComplete?.Invoke();
        }
        else if (loop)
        {
            // Loop back to start
            _currentProgress = _currentProgress >= _totalPathLength ? 0 : _totalPathLength;
            OnPathComplete?.Invoke();
        }
        else
        {
            // Stop at end
            _currentProgress = _currentProgress >= _totalPathLength ? _totalPathLength : 0;
            Stop();
            OnPathComplete?.Invoke();
        }
        
        // Update waypoint index
        UpdateWaypointIndex();
    }
    
    private void UpdateWaypointIndex()
    {
        if (Direction > 0)
        {
            CurrentWaypointIndex = _currentSegment + 1;
            if (CurrentWaypointIndex >= waypoints.Count)
            {
                CurrentWaypointIndex = 0;
            }
        }
        else
        {
            CurrentWaypointIndex = _currentSegment;
            if (CurrentWaypointIndex < 0)
            {
                CurrentWaypointIndex = waypoints.Count - 1;
            }
        }
        
        OnWaypointReached?.Invoke();
    }
    
    #region Public Methods
    
    /// <summary>
    /// Starts or resumes the waypoint animation
    /// </summary>
    public void Play()
    {
        if (waypoints.Count < 2)
        {
            Debug.LogWarning("WaypointAnimator: Need at least 2 waypoints to animate.");
            return;
        }
        
        if (!IsPlaying)
        {
            IsPlaying = true;
            IsPaused = false;
            OnMovementStarted?.Invoke();
        }
        else if (IsPaused)
        {
            IsPaused = false;
            OnMovementStarted?.Invoke();
        }
    }
    
    /// <summary>
    /// Pauses the animation at the current position
    /// </summary>
    public void Pause()
    {
        if (IsPlaying && !IsPaused)
        {
            IsPaused = true;
            OnMovementStopped?.Invoke();
        }
    }
    
    /// <summary>
    /// Stops the animation and resets to the first waypoint
    /// </summary>
    public void Stop()
    {
        IsPlaying = false;
        IsPaused = false;
        OnMovementStopped?.Invoke();
    }
    
    /// <summary>
    /// Resets the animation to the first waypoint
    /// </summary>
    public void Reset()
    {
        Stop();
        _currentProgress = 0f;
        CurrentWaypointIndex = 0;
        Direction = 1;
        Progress = 0f;
        
        if (waypoints.Count > 0 && waypoints[0] != null)
        {
            _transform.position = waypoints[0].position;
        }
    }
    
    /// <summary>
    /// Resets and immediately starts playing
    /// </summary>
    public void Restart()
    {
        Reset();
        Play();
    }
    
    /// <summary>
    /// Jumps to a specific waypoint
    /// </summary>
    /// <param name="index">The waypoint index to jump to</param>
    public void GoToWaypoint(int index)
    {
        if (index < 0 || index >= waypoints.Count)
        {
            Debug.LogWarning($"WaypointAnimator: Invalid waypoint index {index}. Must be between 0 and {waypoints.Count - 1}.");
            return;
        }
        
        if (waypoints[index] != null)
        {
            _transform.position = waypoints[index].position;
            CurrentWaypointIndex = index;
            
            // Calculate progress for this waypoint
            float accumulatedLength = 0f;
            for (int i = 0; i < index && i < _segmentLengths.Count; i++)
            {
                accumulatedLength += _segmentLengths[i];
            }
            _currentProgress = accumulatedLength;
            Progress = Mathf.Clamp01(_currentProgress / _totalPathLength);
        }
    }
    
    /// <summary>
    /// Sets the movement speed
    /// </summary>
    /// <param name="newSpeed">New speed in units per second</param>
    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0.01f, newSpeed);
    }
    
    /// <summary>
    /// Sets the total time to complete the path
    /// </summary>
    /// <param name="newTime">New time in seconds</param>
    public void SetTotalTime(float newTime)
    {
        totalTime = Mathf.Max(0.01f, newTime);
    }
    
    /// <summary>
    /// Sets the movement mode
    /// </summary>
    /// <param name="mode">The new movement mode</param>
    public void SetMovementMode(MovementMode mode)
    {
        movementMode = mode;
    }
    
    /// <summary>
    /// Sets the easing curve
    /// </summary>
    /// <param name="curve">The new easing curve</param>
    public void SetEasingCurve(AnimationCurve curve)
    {
        if (curve != null && curve.length > 0)
        {
            easingCurve = curve;
        }
    }
    
    /// <summary>
    /// Adds a waypoint to the end of the list
    /// </summary>
    /// <param name="waypoint">The waypoint transform to add</param>
    public void AddWaypoint(Transform waypoint)
    {
        if (waypoint != null)
        {
            waypoints.Add(waypoint);
            CalculatePathLength();
        }
    }
    
    /// <summary>
    /// Removes a waypoint at the specified index
    /// </summary>
    /// <param name="index">The index of the waypoint to remove</param>
    public void RemoveWaypoint(int index)
    {
        if (index >= 0 && index < waypoints.Count)
        {
            waypoints.RemoveAt(index);
            CalculatePathLength();
        }
    }
    
    /// <summary>
    /// Clears all waypoints
    /// </summary>
    public void ClearWaypoints()
    {
        waypoints.Clear();
        CalculatePathLength();
        Stop();
    }
    
    /// <summary>
    /// Gets the total length of the path
    /// </summary>
    /// <returns>The total path length in units</returns>
    public float GetPathLength()
    {
        return _totalPathLength;
    }
    
    /// <summary>
    /// Gets the estimated time to complete the path based on current speed
    /// </summary>
    /// <returns>Estimated time in seconds</returns>
    public float GetEstimatedTime()
    {
        if (movementMode == MovementMode.Speed)
        {
            return _totalPathLength / speed;
        }
        else
        {
            return totalTime;
        }
    }
    
    /// <summary>
    /// Gets the current position along the path (0-1)
    /// </summary>
    /// <returns>Progress value between 0 and 1</returns>
    public float GetProgress()
    {
        return Progress;
    }
    
    /// <summary>
    /// Sets the progress along the path (0-1)
    /// </summary>
    /// <param name="progress">Progress value between 0 and 1</param>
    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        _currentProgress = progress * _totalPathLength;
        UpdatePosition();
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2)
            return;
        
        // Draw path
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
        
        // Draw loop back line
        if (loop && !pingPong && waypoints.Count > 1)
        {
            if (waypoints[waypoints.Count - 1] != null && waypoints[0] != null)
            {
                Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
            }
        }
        
        // Draw waypoints
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.color = i == CurrentWaypointIndex ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
                
                // Draw waypoint number
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(waypoints[i].position + Vector3.up * 0.5f, i.ToString());
                #endif
            }
        }
    }
    
    #endregion
    
    private void OnValidate()
    {
        // Ensure valid values
        speed = Mathf.Max(0.01f, speed);
        totalTime = Mathf.Max(0.01f, totalTime);
        
        // Recalculate path length when waypoints change
        if (Application.isPlaying)
        {
            CalculatePathLength();
        }
    }
}
