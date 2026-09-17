using UnityEngine;

// Corrects initial/saved positions after the forest terrain changes height.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class ForestSpawnGrounding : MonoBehaviour
{
    [SerializeField] private Collider[] groundSurfaces;
    private const float Clearance = 0.06f;

    public void Configure(Collider[] surfaces) => groundSurfaces = surfaces;

    private void Start() => PlaceAboveGround();

    public bool TryGetGroundedPosition(Vector3 requested, out Vector3 grounded)
    {
        grounded = requested;
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule == null || !capsule.enabled || groundSurfaces == null) return false;
        Physics.SyncTransforms();
        Bounds bounds = capsule.bounds;
        Vector3 centerOffset = bounds.center - transform.position;
        float footOffset = transform.position.y - bounds.min.y;
        float highest = float.NegativeInfinity;
        // Check the footprint as well as the center, so slopes cannot intersect the capsule.
        for (int sample = 0; sample < 5; sample++)
        {
            Vector3 origin = requested + centerOffset;
            if (sample == 1) origin.x += bounds.extents.x * .85f;
            if (sample == 2) origin.x -= bounds.extents.x * .85f;
            if (sample == 3) origin.z += bounds.extents.z * .85f;
            if (sample == 4) origin.z -= bounds.extents.z * .85f;
            foreach (Collider surface in groundSurfaces)
            {
                if (surface == null || !surface.enabled || !surface.gameObject.activeInHierarchy || surface.isTrigger) continue;
                origin.y = surface.bounds.max.y + 5f;
                if (surface.Raycast(new Ray(origin, Vector3.down), out RaycastHit hit, surface.bounds.size.y + 10f)
                    && hit.normal.y > .5f)
                    highest = Mathf.Max(highest, hit.point.y);
            }
        }
        if (float.IsNegativeInfinity(highest)) return false;
        grounded.y = Mathf.Max(requested.y, highest + footOffset + Clearance);
        return true;
    }

    public void PlaceAboveGround()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (!TryGetGroundedPosition(body.position, out Vector3 safePosition)) return;
        if (safePosition.y <= body.position.y + .001f) return;
        body.position = safePosition;
        transform.position = safePosition;
        if (Application.isPlaying)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        Physics.SyncTransforms();
    }
}
