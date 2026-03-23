// WallCutOutController.cs
// Attach to the object that should be "seen through" walls.
//
// ── HOW IT WORKS ────────────────────────────────────────────────────────────────
// This controller detects which objects are blocking the line of sight between the
// camera and the target. When an object blocks the view, its material is swapped
// with WallCutOut material (preserving textures and colors). When the object
// no longer blocks the view, its original material is restored.
//
// ── SETUP ─────────────────────────────────────────────────────────────────────
//  1. Add this component to your player / tracked object.
//  2. Assign WallCutOut material to Cutout Material field.
//  3. Set WallLayers to the layers that should be affected.
//  4. Optionally set Target Override to a child bone (e.g., chest).
// 
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("Rendering/Wall Cut-Out Controller")]
public class WallCutOutController : MonoBehaviour
{
    // ────────────────────────────────────────────────────────────────────────
    #region Inspector Fields

    [Header("Target")]
    [Tooltip("Transform to track.  Leave null to use this GameObject's own transform.")]
    public Transform TargetOverride;

    [Tooltip("World-space offset applied to the tracked position.  " +
             "Use e.g. (0, 1, 0) to centre the hole on a character's torso.")]
    public Vector3 PositionOffset = Vector3.zero;

    [Header("Raycast Settings")]
    [Tooltip("If assigned, raycasts from this camera to the target. If null, uses Camera.main.")]
    public Camera RaycastCamera;

    [Tooltip("Layers that block the line of sight and get material swapped.")]
    public LayerMask WallLayers = -1;

    [Tooltip("Maximum raycast distance. Objects beyond this distance won't be affected.")]
    public float MaxRaycastDistance = 100f;

    [Header("Cutout Material")]
    [Tooltip("The WallCutOut material to apply to blocking objects.")]
    public Material CutoutMaterial;

    [Tooltip("If true, preserves the original material's main color when swapping.")]
    public bool PreserveMainColor = true;

    [Tooltip("If true, preserves the original material's main texture when swapping.")]
    public bool PreserveMainTexture = true;

    [Header("State")]
    [Tooltip("Uncheck to disable the cutout system entirely.")]
    public bool CutoutEnabled = true;

    #endregion

    // ────────────────────────────────────────────────────────────────────────
    #region Private State

    // Dictionary to store original materials for each renderer
    private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();

    // Set of renderers currently using cutout material
    private HashSet<Renderer> _cutoutRenderers = new HashSet<Renderer>();

    // Cached components for performance
    private Camera _camera;
    private Transform _target;

    #endregion

    // ────────────────────────────────────────────────────────────────────────
    #region Unity Events

    private void Awake()
    {
        _target = TargetOverride != null ? TargetOverride : transform;
        _camera = RaycastCamera != null ? RaycastCamera : Camera.main;
    }

    private void OnDisable()
    {
        // Restore all original materials when disabled
        RestoreAllMaterials();
    }

    private void OnDestroy()
    {
        RestoreAllMaterials();
    }

    private void LateUpdate()
    {
        if (!CutoutEnabled || _camera == null || _target == null)
            return;

        // Get target position
        Vector3 targetPos = _target.position + PositionOffset;
        Vector3 cameraPos = _camera.transform.position;
        Vector3 direction = (targetPos - cameraPos).normalized;
        float distance = Vector3.Distance(cameraPos, targetPos);

        // Clamp distance to max raycast distance
        if (distance > MaxRaycastDistance)
            distance = MaxRaycastDistance;

        // Perform raycast to find all objects blocking the view
        RaycastHit[] hits = Physics.RaycastAll(cameraPos, direction, distance, WallLayers);

        // Get all renderers from hit objects with their hit points
        Dictionary<Renderer, Vector3> rendererHitPoints = new Dictionary<Renderer, Vector3>();
        HashSet<Renderer> blockingRenderers = new HashSet<Renderer>();
        foreach (RaycastHit hit in hits)
        {
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null)
            {
                blockingRenderers.Add(renderer);
                rendererHitPoints[renderer] = hit.point;
            }
        }

        // Apply cutout material to blocking renderers
        foreach (Renderer renderer in blockingRenderers)
        {
            Vector3 hitPoint = rendererHitPoints.ContainsKey(renderer) ? rendererHitPoints[renderer] : targetPos;
            ApplyCutoutMaterial(renderer, hitPoint);
        }

        // Restore original materials for renderers that are no longer blocking
        HashSet<Renderer> renderersToRestore = new HashSet<Renderer>(_cutoutRenderers);
        renderersToRestore.ExceptWith(blockingRenderers);
        foreach (Renderer renderer in renderersToRestore)
        {
            RestoreOriginalMaterial(renderer);
        }
    }

    #endregion

    // ────────────────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>Enable or disable the cutout system.</summary>
    public void SetCutoutEnabled(bool enabled)
    {
        if (CutoutEnabled == enabled) return;
        CutoutEnabled = enabled;
        
        if (!enabled)
            RestoreAllMaterials();
    }

    /// <summary>Toggle the cutout system on/off.</summary>
    public void ToggleCutout() => SetCutoutEnabled(!CutoutEnabled);

    /// <summary>Force restore all original materials.</summary>
    public void RestoreAllMaterials()
    {
        List<Renderer> renderers = new List<Renderer>(_cutoutRenderers);
        foreach (Renderer renderer in renderers)
        {
            RestoreOriginalMaterial(renderer);
        }
    }

    #endregion

    // ────────────────────────────────────────────────────────────────────────
    #region Material Management

    private void ApplyCutoutMaterial(Renderer renderer, Vector3 cutoutPosition)
    {
        // Store original materials if not already stored
        if (!_originalMaterials.ContainsKey(renderer))
        {
            _originalMaterials[renderer] = renderer.sharedMaterials;
        }

        // Get or create the cutout material for this renderer
        Material cutoutMat = renderer.material;
        
        // If renderer is not using a cutout material yet, create one
        if (!_cutoutRenderers.Contains(renderer))
        {
            // Create a new instance of cutout material
            cutoutMat = new Material(CutoutMaterial);

            // Set all cutout properties individually
            // This ensures all values are applied and override CBUFFER defaults
            cutoutMat.SetVector("_WC_Position", new Vector4(cutoutPosition.x, cutoutPosition.y, cutoutPosition.z, 0f));
            cutoutMat.SetFloat("_WC_Enabled", 1.0f);
            cutoutMat.SetFloat("_WC_Radius", 2.0f); // Larger radius for better visibility
            cutoutMat.SetFloat("_WC_Softness", 0.5f); // More softness for smoother edge
            
            // Debug: Log values being set
            if (Application.isPlaying)
            {
                Debug.Log($"[WallCutOut] Set on {renderer.name} - Pos: {cutoutPosition}, Radius: 2.0, Softness: 0.5");
            }

            // Preserve original material properties
            if (_originalMaterials.ContainsKey(renderer))
            {
                Material[] originalMats = _originalMaterials[renderer];
                if (originalMats != null && originalMats.Length > 0)
                {
                    Material originalMat = originalMats[0];
                    
                    if (PreserveMainColor && originalMat.HasProperty("_BaseColor"))
                    {
                        cutoutMat.SetColor("_BaseColor", originalMat.GetColor("_BaseColor"));
                    }
                    
                    if (PreserveMainTexture && originalMat.HasProperty("_BaseColorMap"))
                    {
                        cutoutMat.SetTexture("_BaseColorMap", originalMat.GetTexture("_BaseColorMap"));
                    }
                }
            }

            // Apply material to renderer
            renderer.material = cutoutMat;
            _cutoutRenderers.Add(renderer);
        }

        // Update cutout position every frame (even if already using cutout material)
        cutoutMat.SetVector("_WC_Position", new Vector4(cutoutPosition.x, cutoutPosition.y, cutoutPosition.z, 0f));
        cutoutMat.SetFloat("_WC_Enabled", 1.0f);
        
        // Pull Radius and Softness from the reference material if they exist,
        // otherwise use reasonable defaults. This ensures settings from the Editor are used.
        if (CutoutMaterial != null)
        {
            if (CutoutMaterial.HasProperty("_WC_Radius"))
                cutoutMat.SetFloat("_WC_Radius", CutoutMaterial.GetFloat("_WC_Radius"));
            if (CutoutMaterial.HasProperty("_WC_Softness"))
                cutoutMat.SetFloat("_WC_Softness", CutoutMaterial.GetFloat("_WC_Softness"));
        }
    }

    private void RestoreOriginalMaterial(Renderer renderer)
    {
        if (!_cutoutRenderers.Contains(renderer))
            return;

        // Restore original materials
        if (_originalMaterials.ContainsKey(renderer))
        {
            renderer.sharedMaterials = _originalMaterials[renderer];
        }

        _cutoutRenderers.Remove(renderer);
    }

    #endregion

    // ────────────────────────────────────────────────────────────────────────
    #region Editor Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_target == null || _camera == null)
            return;

        Vector3 center = _target.position + PositionOffset;
        float   radius = 1.5f;

        // Draw line from camera to target
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.5f);
        Gizmos.DrawLine(_camera.transform.position, center);

        // Draw sphere at target
        Gizmos.color = new Color(1f, 0.25f, 0.05f, 0.3f);
        Gizmos.DrawWireSphere(center, radius);

        GUIStyle lbl = new GUIStyle { normal = { textColor = new Color(1f, 0.85f, 0.1f) } };
        Handles.Label(center + Vector3.up * (radius + 0.15f), "WallCutOut Target", lbl);
    }
#endif

    #endregion
}
