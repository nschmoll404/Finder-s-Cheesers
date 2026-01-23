using UnityEngine;
using Unity.Cinemachine;

namespace FindersCheesers.Camera
{
    /// <summary>
    /// Fixed aerial drone-like camera using Cinemachine 3.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/Camera/Drone Camera")]
    public class DroneCamera : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private CinemachineCamera virtualCamera;
        [SerializeField] private Transform followTarget;
        
        [Header("Drone Settings")]
        [SerializeField] private float heightOffset = 10f;
        [SerializeField] private float distanceOffset = 5f;
        [SerializeField] private float lookAngle = 60f;
        
        private void Awake()
        {
            // Find CinemachineCamera if not assigned
            if (virtualCamera == null)
            {
                virtualCamera = FindFirstObjectByType<CinemachineCamera>();
            }
        }
        
        private void OnEnable()
        {
            if (virtualCamera != null)
            {
                SetupDroneCamera();
            }
        }
        
        private void SetupDroneCamera()
        {
            // Set follow target
            if (followTarget != null)
            {
                virtualCamera.Follow = followTarget;
                virtualCamera.LookAt = followTarget;
            }
            
            // Configure camera position
            Vector3 cameraOffset = Vector3.back * distanceOffset + Vector3.up * heightOffset;
            virtualCamera.transform.localPosition = cameraOffset;
            
            // Configure field of view
            virtualCamera.Lens.FieldOfView = lookAngle;
            
            // Add subtle noise for drone-like feel
            AddDroneNoise();
        }
        
        private void AddDroneNoise()
        {
            var noise = virtualCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
            if (noise == null)
            {
                noise = virtualCamera.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
            }
            
            if (noise != null)
            {
                noise.AmplitudeGain = 0.05f;
                noise.FrequencyGain = 0.3f;
            }
        }
        
        /// <summary>
        /// Gets CinemachineCamera reference.
        /// </summary>
        public CinemachineCamera GetVirtualCamera() => virtualCamera;
    }
}
