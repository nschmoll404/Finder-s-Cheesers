using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Debug tool for designers to visualize and measure King Rat throw arcs in the editor.
    /// Uses the same calculations as KingRatHandler for accurate representation.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Debug/King Rat Throw Debug Tool")]
    public class KingRatThrowDebugTool : MonoBehaviour
    {
        #region Throw Settings

        [Header("Rat Count")]
        [Tooltip("Simulated number of rats in inventory")]
        [SerializeField]
        [Range(0, 50)]
        private int ratCount = 5;

        [Header("Distance Settings")]
        [Tooltip("Base throw distance without any rats")]
        [SerializeField]
        private float baseThrowDistance = 5f;

        [Tooltip("Additional throw distance per rat in inventory")]
        [SerializeField]
        private float distancePerRat = 1f;

        [Tooltip("Maximum throw distance")]
        [SerializeField]
        private float maxThrowDistance = 20f;

        [Header("Launch Settings")]
        [Tooltip("Base launch speed for the King Rat")]
        [SerializeField]
        private float baseLaunchSpeed = 10f;

        [Tooltip("Additional launch speed per rat in inventory")]
        [SerializeField]
        private float speedPerRat = 2f;

        [Tooltip("Maximum launch speed")]
        [SerializeField]
        private float maxLaunchSpeed = 30f;

        [Tooltip("Height offset for the launch position")]
        [SerializeField]
        private float launchHeightOffset = 1f;

        [Header("Arc Visualization")]
        [Tooltip("Number of segments in the arc visualization")]
        [SerializeField]
        [Range(10, 100)]
        private int arcSegments = 30;

        [Tooltip("Show the arc in Scene view")]
        [SerializeField]
        private bool showArc = true;

        [Tooltip("Show max distance circle")]
        [SerializeField]
        private bool showMaxDistanceCircle = true;

        [Tooltip("Show distance markers")]
        [SerializeField]
        private bool showDistanceMarkers = true;

        [Tooltip("Color of the arc line")]
        [SerializeField]
        private Color arcColor = Color.yellow;

        [Tooltip("Color of the max distance circle")]
        [SerializeField]
        private Color maxDistanceColor = new Color(1f, 1f, 0f, 0.3f);

        [Tooltip("Color of the distance markers")]
        [SerializeField]
        private Color markerColor = Color.cyan;

        [Header("Target Direction")]
        [Tooltip("Use transform's local space for throw direction")]
        [SerializeField]
        private bool useLocalSpace = true;

        [Tooltip("Direction of the throw (in local or world space depending on useLocalSpace)")]
        [SerializeField]
        private Vector3 throwDirection = Vector3.forward;

        [Tooltip("Custom target point (overrides throwDirection if set)")]
        [SerializeField]
        private bool useCustomTarget = false;

        [Tooltip("Custom target position in world space")]
        [SerializeField]
        private Vector3 customTargetPosition = Vector3.zero;

        [Tooltip("Target height (Y position) for the throw")]
        [SerializeField]
        private float targetHeight = 0f;

        #endregion

        #region Calculated Properties

        /// <summary>
        /// Gets the calculated maximum throw distance based on rat count.
        /// </summary>
        public float CalculatedMaxThrowDistance
        {
            get
            {
                float distance = baseThrowDistance + (ratCount * distancePerRat);
                return Mathf.Min(distance, maxThrowDistance);
            }
        }

        /// <summary>
        /// Gets the calculated launch speed based on rat count.
        /// </summary>
        public float CalculatedLaunchSpeed
        {
            get
            {
                float speed = baseLaunchSpeed + (ratCount * speedPerRat);
                return Mathf.Min(speed, maxLaunchSpeed);
            }
        }

        /// <summary>
        /// Gets the launch position based on transform position and height offset.
        /// </summary>
        public Vector3 LaunchPosition => transform.position + Vector3.up * launchHeightOffset;

        /// <summary>
        /// Gets the world-space throw direction.
        /// </summary>
        public Vector3 WorldThrowDirection => useLocalSpace 
            ? transform.TransformDirection(throwDirection.normalized) 
            : throwDirection.normalized;

        /// <summary>
        /// Gets the target position for the throw.
        /// </summary>
        public Vector3 TargetPosition
        {
            get
            {
                if (useCustomTarget)
                {
                    return customTargetPosition;
                }
                else
                {
                    return LaunchPosition + WorldThrowDirection * CalculatedMaxThrowDistance;
                }
            }
        }

        #endregion

        #region Unity Editor

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Vector3 launchPos = LaunchPosition;
            float maxDistance = CalculatedMaxThrowDistance;
            float launchSpeed = CalculatedLaunchSpeed;

            // Draw launch position indicator
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(launchPos, 0.2f);

            // Draw max distance circle
            if (showMaxDistanceCircle)
            {
                Gizmos.color = maxDistanceColor;
                DrawCircle(launchPos, maxDistance, 32);
            }

            // Draw distance markers
            if (showDistanceMarkers)
            {
                DrawDistanceMarkers(launchPos, maxDistance);
            }

            // Draw arc
            if (showArc)
            {
                DrawThrowArc(launchPos, TargetPosition, launchSpeed);
            }

            // Draw target position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(TargetPosition, 0.3f);

            // Draw direction arrow
            if (!useCustomTarget)
            {
                Vector3 worldDir = WorldThrowDirection;
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.ArrowHandleCap(
                    0,
                    launchPos,
                    Quaternion.LookRotation(worldDir),
                    1f,
                    EventType.Repaint
                );
            }

            // Draw info label
            DrawInfoLabel(launchPos, maxDistance, launchSpeed);
        }

        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }

        private void DrawDistanceMarkers(Vector3 launchPos, float maxDistance)
        {
            Gizmos.color = markerColor;
            Vector3 worldDir = WorldThrowDirection;

            // Draw markers at 25%, 50%, 75%, and 100% of max distance
            float[] percentages = { 0.25f, 0.5f, 0.75f, 1f };

            foreach (float percentage in percentages)
            {
                float distance = maxDistance * percentage;
                Vector3 markerPos = launchPos + worldDir * distance;

                // Draw small cross at marker position
                float crossSize = 0.15f;
                Gizmos.DrawLine(markerPos - Vector3.right * crossSize, markerPos + Vector3.right * crossSize);
                Gizmos.DrawLine(markerPos - Vector3.forward * crossSize, markerPos + Vector3.forward * crossSize);

                // Draw distance label
                string label = $"{distance:F1}m ({percentage * 100:F0}%)";
                UnityEditor.Handles.color = markerColor;
                UnityEditor.Handles.Label(markerPos + Vector3.up * 0.3f, label);
            }
        }

        private void DrawThrowArc(Vector3 start, Vector3 end, float launchSpeed)
        {
            // Calculate arc points using the same method as KingRatHandler
            Vector3[] arcPoints = CalculateArcPoints(start, end, arcSegments, launchSpeed);

            // Draw the arc
            Gizmos.color = arcColor;
            for (int i = 0; i < arcPoints.Length - 1; i++)
            {
                Gizmos.DrawLine(arcPoints[i], arcPoints[i + 1]);
            }
        }

        private void DrawInfoLabel(Vector3 launchPos, float maxDistance, float launchSpeed)
        {
            // Create info string
            string info = $"Rats: {ratCount}\n" +
                          $"Max Distance: {maxDistance:F2}m\n" +
                          $"Launch Speed: {launchSpeed:F2}m/s\n" +
                          $"Base Distance: {baseThrowDistance:F1}m\n" +
                          $"Distance/Rat: {distancePerRat:F1}m";

            // Draw label above launch position
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(launchPos + Vector3.up * 0.5f, info);
        }

        /// <summary>
        /// Calculates arc points using the same quadratic Bezier curve as KingRatHandler.
        /// </summary>
        private Vector3[] CalculateArcPoints(Vector3 start, Vector3 end, int segments, float launchSpeed)
        {
            Vector3[] points = new Vector3[segments + 1];

            // Calculate horizontal distance
            float distance = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z));

            // Calculate control point for the arc (midpoint horizontally, elevated vertically)
            Vector3 midPoint = Vector3.Lerp(start, end, 0.5f);
            float arcHeight = Mathf.Max(distance * 0.3f, launchSpeed * 0.15f);
            Vector3 controlPoint = new Vector3(midPoint.x, Mathf.Max(start.y, end.y) + arcHeight, midPoint.z);

            // Generate arc points using quadratic Bezier curve
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;

                // Quadratic Bezier formula: (1-t)² * P0 + 2(1-t)t * P1 + t² * P2
                float oneMinusT = 1f - t;
                Vector3 point = (oneMinusT * oneMinusT * start) +
                                (2f * oneMinusT * t * controlPoint) +
                                (t * t * end);

                points[i] = point;
            }

            return points;
        }
#endif

        #endregion

        #region Public API

        /// <summary>
        /// Sets the simulated rat count.
        /// </summary>
        public void SetRatCount(int count)
        {
            ratCount = Mathf.Clamp(count, 0, 50);
        }

        /// <summary>
        /// Sets the throw direction.
        /// </summary>
        public void SetThrowDirection(Vector3 direction)
        {
            throwDirection = direction.normalized;
        }

        /// <summary>
        /// Sets a custom target position.
        /// </summary>
        public void SetCustomTarget(Vector3 target)
        {
            useCustomTarget = true;
            customTargetPosition = target;
        }

        /// <summary>
        /// Clears custom target and uses throw direction instead.
        /// </summary>
        public void ClearCustomTarget()
        {
            useCustomTarget = false;
        }

        /// <summary>
        /// Copies settings from a KingRatHandler component.
        /// </summary>
        public void CopyFromKingRatHandler(KingRatHandler handler)
        {
            if (handler == null) return;

            baseThrowDistance = handler.BaseThrowDistance;
            distancePerRat = handler.DistancePerRat;
            maxThrowDistance = handler.MaxThrowDistance;
            baseLaunchSpeed = handler.BaseLaunchSpeed;
            speedPerRat = handler.SpeedPerRat;
            maxLaunchSpeed = handler.MaxLaunchSpeed;
            launchHeightOffset = handler.LaunchHeightOffset;
        }

        #endregion

        #region Reset

        private void Reset()
        {
            ratCount = 5;
            baseThrowDistance = 5f;
            distancePerRat = 1f;
            maxThrowDistance = 20f;
            baseLaunchSpeed = 10f;
            speedPerRat = 2f;
            maxLaunchSpeed = 30f;
            launchHeightOffset = 1f;
            arcSegments = 30;
            showArc = true;
            showMaxDistanceCircle = true;
            showDistanceMarkers = true;
            arcColor = Color.yellow;
            maxDistanceColor = new Color(1f, 1f, 0f, 0.3f);
            markerColor = Color.cyan;
            useLocalSpace = true;
            throwDirection = Vector3.forward;
            useCustomTarget = false;
            targetHeight = 0f;
        }

        #endregion
    }
}
