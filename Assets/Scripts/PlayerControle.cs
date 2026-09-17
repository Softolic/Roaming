using UnityEngine; 

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerControle : MonoBehaviour
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField, Min(.1f)] private float _speed = 5f;
    [SerializeField, Min(1f)] private float _turnSpeed = 540f;
    [SerializeField, Min(.1f)] private float _acceleration = 22f;
    [SerializeField, Min(.1f)] private float _braking = 30f;
    [SerializeField, Range(0f, 1f)] private float _airControl = .25f;
    [SerializeField, Range(1f, 65f)] private float _maxSlope = 48f;
    private Vector3 _input;
    private CapsuleCollider _capsule;
    private Vector3 _lastPosition;
    private readonly RaycastHit[] _groundHits = new RaycastHit[16];
    private PhysicsMaterial _movementMaterial;
    private PhysicsMaterial _originalMaterial;
    public float CurrentSpeed { get; private set; }
    public float MaxSpeed => _speed;
    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.angularVelocity = Vector3.zero;
        _originalMaterial = _capsule.sharedMaterial;
        _movementMaterial = new PhysicsMaterial("Toby Movement")
        {
            dynamicFriction = 0f, staticFriction = 0f, bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        _capsule.sharedMaterial = _movementMaterial;
        _lastPosition = _rb.position;
    }

    private void OnEnable()
    {
        _input = Vector3.zero;
        if (_rb != null) _lastPosition = _rb.position;
        CurrentSpeed = 0f;
    }

    private void Update()
    {
        _input = Time.timeScale > 0f
            ? Vector3.ClampMagnitude(new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")), 1f)
            : Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (_rb == null || _rb.isKinematic) return;
        float dt = Time.fixedDeltaTime;
        Vector3 displacement = _rb.position - _lastPosition;
        _lastPosition = _rb.position;
        // Ignore teleports when measuring animation speed.
        float distance = Vector3.ProjectOnPlane(displacement, Vector3.up).magnitude;
        CurrentSpeed = distance < _speed * dt * 3f ? distance / dt : 0f;

        Vector3 normal;
        IsGrounded = FindGround(out normal);
        Vector3 desired = Vector3.ClampMagnitude(_input, 1f).ToIso();
        Vector3 current = _rb.linearVelocity;
        Vector3 horizontal = new Vector3(current.x, 0, current.z);
        Vector3 goal = desired * _speed;
        float rate = desired.sqrMagnitude > .001f ? _acceleration : _braking;
        horizontal = Vector3.MoveTowards(horizontal, goal, rate * dt * (IsGrounded ? 1f : _airControl));
        if (IsGrounded)
        {
            Vector3 tangentVelocity = Vector3.ProjectOnPlane(horizontal, normal);
            // Drive along the surface instead of repeatedly teleporting into it.
            if (tangentVelocity.sqrMagnitude > .0001f)
                tangentVelocity = tangentVelocity.normalized * horizontal.magnitude;
            // Cancel gravity along the slope so stopping does not produce downhill drift.
            Vector3 slopeGravity = Vector3.ProjectOnPlane(Physics.gravity, normal);
            _rb.linearVelocity = tangentVelocity - normal * .35f - slopeGravity * dt;
        }
        else _rb.linearVelocity = new Vector3(horizontal.x, current.y, horizontal.z);

        if (desired.sqrMagnitude > .001f)
        {
            Quaternion rotation = Quaternion.LookRotation(desired, Vector3.up);
            _rb.MoveRotation(Quaternion.RotateTowards(_rb.rotation, rotation, _turnSpeed * dt));
        }
    }

    private bool FindGround(out Vector3 normal)
    {
        normal = Vector3.up;
        Vector3 scale = transform.lossyScale;
        float radius = _capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float halfHeight = Mathf.Max(radius, _capsule.height * Mathf.Abs(scale.y) * .5f);
        // Use the physics pose, not the interpolated render transform.
        Vector3 center = _rb.position + _rb.rotation * Vector3.Scale(_capsule.center, scale);
        float probeRadius = radius * .88f;
        float castDistance = halfHeight - probeRadius + .16f;
        int count = Physics.SphereCastNonAlloc(center, probeRadius, Vector3.down,
            _groundHits, castDistance, ~0, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        float slopeLimit = Mathf.Cos(_maxSlope * Mathf.Deg2Rad);
        for (int i = 0; i < count; i++)
        {
            var hit = _groundHits[i];
            if (hit.collider == null || hit.rigidbody == _rb || hit.transform.IsChildOf(transform)
                || hit.normal.y < slopeLimit || hit.distance >= nearest) continue;
            // Loose objects such as the ball are not terrain.
            if (hit.rigidbody != null && !hit.rigidbody.isKinematic) continue;
            nearest = hit.distance;
            normal = hit.normal;
        }
        return !float.IsPositiveInfinity(nearest);
    }

    private void OnDisable()
    {
        _input = Vector3.zero;
        CurrentSpeed = 0f;
        // Ball pickup temporarily disables movement; stop horizontal drift immediately.
        if (_rb != null && !_rb.isKinematic)
        {
            Vector3 velocity = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(0, velocity.y, 0);
            _rb.angularVelocity = Vector3.zero;
        }
    }

    private void OnDestroy()
    {
        if (_capsule != null) _capsule.sharedMaterial = _originalMaterial;
        if (_movementMaterial != null) Destroy(_movementMaterial);
    }
}
