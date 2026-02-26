using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A lattice vertex modifier that distorts vertices within a spherical radius.
/// Vertices are pushed outward from the center, similar to a spherify tool.
/// Useful for creating expansion effects when objects travel through tubes.
/// </summary>
[ExecuteInEditMode]
public class LatticeVertexModifier : MonoBehaviour
{
    [Header("Effect Settings")]
    [Tooltip("The radius of the sphere gizmo and the area where vertices will be affected")]
    [Min(0.1f)]
    public float radius = 2f;

    [Tooltip("How strongly vertices are pushed outward. 0 = no effect, 1 = full spherify")]
    [Range(0f, 1f)]
    public float strength = 0.5f;

    [Tooltip("Falloff curve - controls how the effect diminishes toward the edge of the radius")]
    public AnimationCurve falloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Tooltip("If true, the effect will only be applied in the Unity Editor")]
    public bool editorOnly = false;

    [Header("Gizmo Settings")]
    [Tooltip("Color of the sphere gizmo")]
    public Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f);

    [Tooltip("Color of the wireframe sphere")]
    public Color wireframeColor = new Color(1f, 0.8f, 0f, 0.8f);

    [Tooltip("Show the gizmo in the scene view")]
    public bool showGizmo = true;

    [Header("Target Settings")]
    [Tooltip("If assigned, only this mesh will be affected. If null, all meshes in range will be affected")]
    public MeshFilter targetMeshFilter;

    [Tooltip("If true, the effect will affect all MeshFilters within the radius (useful for modifier on empty GameObject)")]
    public bool affectAllInRange = false;

    [Tooltip("If true, the effect will be applied continuously in Update")]
    public bool continuousUpdate = false;

    [Tooltip("If true, the original mesh vertices will be preserved and restored when the effect is disabled")]
    public bool preserveOriginalMesh = true;

    // Cache for original vertices
    private Vector3[] originalVertices;
    private MeshFilter meshFilter;
    private Mesh mesh;
    private bool isInitialized = false;
    
    // Dictionary to cache original vertices for external meshes
    private static Dictionary<string, Vector3[]> meshOriginalVerticesCache = new Dictionary<string, Vector3[]>();

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            Initialize();
        }
        
        if (preserveOriginalMesh && mesh != null)
        {
            StoreOriginalVertices();
        }
        
        ApplyVertexModification();
    }

    private void OnDisable()
    {
        if (preserveOriginalMesh && mesh != null && originalVertices != null)
        {
            RestoreOriginalVertices();
        }
        
        // If affecting all in range, restore all affected meshes
        if (affectAllInRange && preserveOriginalMesh)
        {
            RestoreAllInRange();
        }
        
        // Clear cache for this specific mesh filter
        if (meshFilter != null)
        {
            string cacheKey = meshFilter.GetInstanceID().ToString();
            if (meshOriginalVerticesCache.ContainsKey(cacheKey))
            {
                meshOriginalVerticesCache.Remove(cacheKey);
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up cache when component is destroyed
        if (meshFilter != null)
        {
            string cacheKey = meshFilter.GetInstanceID().ToString();
            if (meshOriginalVerticesCache.ContainsKey(cacheKey))
            {
                meshOriginalVerticesCache.Remove(cacheKey);
            }
        }
        
        // If affecting all in range, restore all affected meshes and clear their cache
        if (affectAllInRange && preserveOriginalMesh)
        {
            RestoreAllInRange();
            
            // Clear cache for all meshes we might have affected
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider collider in colliders)
            {
                MeshFilter mf = collider.GetComponent<MeshFilter>();
                if (mf != null)
                {
                    string cacheKey = mf.GetInstanceID().ToString();
                    if (meshOriginalVerticesCache.ContainsKey(cacheKey))
                    {
                        meshOriginalVerticesCache.Remove(cacheKey);
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (continuousUpdate && (!editorOnly || Application.isEditor))
        {
            ApplyVertexModification();
        }
    }

    private void Initialize()
    {
        if (targetMeshFilter != null)
        {
            meshFilter = targetMeshFilter;
        }
        else
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            mesh = meshFilter.sharedMesh;
            isInitialized = true;
        }
    }

    private void StoreOriginalVertices()
    {
        if (mesh != null)
        {
            originalVertices = mesh.vertices.Clone() as Vector3[];
        }
    }

    private void RestoreOriginalVertices()
    {
        if (mesh != null && originalVertices != null)
        {
            mesh.vertices = originalVertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }

    private void ApplyVertexModification()
    {
        if (editorOnly && Application.isPlaying)
        {
            return;
        }

        // If affectAllInRange is true, apply to all meshes within radius
        if (affectAllInRange)
        {
            ApplyToAllInRange();
            return;
        }

        if (mesh == null)
        {
            Initialize();
            if (mesh == null) return;
        }

        // Ensure we have the original vertices stored
        if (originalVertices == null || originalVertices.Length != mesh.vertexCount)
        {
            StoreOriginalVertices();
        }

        Vector3[] vertices = (Vector3[])originalVertices.Clone();
        Vector3 center = transform.position;
        float radiusSquared = radius * radius;

        for (int i = 0; i < vertices.Length; i++)
        {
            // Start from the original vertex position
            Vector3 worldVertex = transform.TransformPoint(originalVertices[i]);
            Vector3 direction = worldVertex - center;
            float distanceSquared = direction.sqrMagnitude;

            if (distanceSquared < radiusSquared && distanceSquared > 0.001f)
            {
                float distance = Mathf.Sqrt(distanceSquared);
                float normalizedDistance = distance / radius;
                float falloff = falloffCurve.Evaluate(normalizedDistance);

                // Calculate the spherify direction
                Vector3 spherifyDirection = direction.normalized;

                // Calculate the target position on the sphere surface
                Vector3 targetPosition = center + spherifyDirection * radius;

                // Interpolate between original position and target position
                Vector3 modifiedPosition = Vector3.Lerp(worldVertex, targetPosition, strength * falloff);

                // Convert back to local space
                vertices[i] = transform.InverseTransformPoint(modifiedPosition);
            }
            // Vertices outside the radius remain at their original position
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    /// <summary>
    /// Applies the vertex modification to all MeshFilters within the radius
    /// </summary>
    public void ApplyToAllInRange()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
        
        foreach (Collider collider in colliders)
        {
            MeshFilter mf = collider.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                ApplyToMeshFilter(mf);
            }
        }
    }

    /// <summary>
    /// Restores all MeshFilters within the radius to their original state
    /// </summary>
    public void RestoreAllInRange()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
        
        foreach (Collider collider in colliders)
        {
            MeshFilter mf = collider.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                RestoreMeshFilter(mf);
            }
        }
    }

    /// <summary>
    /// Applies the vertex modification to a specific MeshFilter
    /// </summary>
    public void ApplyToMeshFilter(MeshFilter targetMf)
    {
        if (targetMf == null || targetMf.sharedMesh == null) return;

        Mesh targetMesh = targetMf.sharedMesh;
        
        // Get or create original vertices cache for this mesh
        string cacheKey = targetMf.GetInstanceID().ToString();
        if (!meshOriginalVerticesCache.ContainsKey(cacheKey))
        {
            meshOriginalVerticesCache[cacheKey] = targetMesh.vertices.Clone() as Vector3[];
        }
        
        Vector3[] originalVerts = meshOriginalVerticesCache[cacheKey];
        Vector3[] vertices = (Vector3[])originalVerts.Clone();
        Vector3 center = transform.position;
        float radiusSquared = radius * radius;

        for (int i = 0; i < vertices.Length; i++)
        {
            // Start from the original vertex position
            Vector3 worldVertex = targetMf.transform.TransformPoint(originalVerts[i]);
            Vector3 direction = worldVertex - center;
            float distanceSquared = direction.sqrMagnitude;

            if (distanceSquared < radiusSquared && distanceSquared > 0.001f)
            {
                float distance = Mathf.Sqrt(distanceSquared);
                float normalizedDistance = distance / radius;
                float falloff = falloffCurve.Evaluate(normalizedDistance);

                Vector3 spherifyDirection = direction.normalized;
                Vector3 targetPosition = center + spherifyDirection * radius;
                Vector3 modifiedPosition = Vector3.Lerp(worldVertex, targetPosition, strength * falloff);

                vertices[i] = targetMf.transform.InverseTransformPoint(modifiedPosition);
            }
            // Vertices outside the radius remain at their original position
        }

        targetMesh.vertices = vertices;
        targetMesh.RecalculateNormals();
        targetMesh.RecalculateBounds();
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        // Draw the sphere gizmo
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, radius);

        // Draw wireframe
        Gizmos.color = wireframeColor;
        Gizmos.DrawWireSphere(transform.position, radius);

        // Draw direction indicator
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.5f);
    }

    /// <summary>
    /// Sets the radius of the effect
    /// </summary>
    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0.1f, newRadius);
        if (continuousUpdate)
        {
            ApplyVertexModification();
        }
        else if (affectAllInRange)
        {
            // Restore all affected meshes first, then apply with new radius
            if (preserveOriginalMesh)
            {
                RestoreAllInRange();
            }
            ApplyToAllInRange();
        }
    }

    /// <summary>
    /// Sets the strength of the effect
    /// </summary>
    public void SetStrength(float newStrength)
    {
        strength = Mathf.Clamp01(newStrength);
        if (continuousUpdate)
        {
            ApplyVertexModification();
        }
        else if (affectAllInRange)
        {
            ApplyToAllInRange();
        }
    }

    /// <summary>
    /// Triggers a one-time application of the vertex modification
    /// </summary>
    [ContextMenu("Apply Modification")]
    public void TriggerModification()
    {
        ApplyVertexModification();
    }

    /// <summary>
    /// Resets the mesh to its original state
    /// </summary>
    [ContextMenu("Reset Mesh")]
    public void ResetMesh()
    {
        if (affectAllInRange)
        {
            RestoreAllInRange();
        }
        else if (preserveOriginalMesh)
        {
            RestoreOriginalVertices();
        }
        else if (mesh != null)
        {
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }

    /// <summary>
    /// Clears the cached original vertices for all external meshes
    /// Call this when you want to reset all modified meshes
    /// </summary>
    public static void ClearMeshCache()
    {
        meshOriginalVerticesCache.Clear();
    }

    /// <summary>
    /// Restores a specific MeshFilter to its original state
    /// </summary>
    public void RestoreMeshFilter(MeshFilter targetMf)
    {
        if (targetMf == null || targetMf.sharedMesh == null) return;

        string cacheKey = targetMf.GetInstanceID().ToString();
        if (meshOriginalVerticesCache.ContainsKey(cacheKey))
        {
            targetMf.sharedMesh.vertices = meshOriginalVerticesCache[cacheKey];
            targetMf.sharedMesh.RecalculateNormals();
            targetMf.sharedMesh.RecalculateBounds();
        }
    }
}
