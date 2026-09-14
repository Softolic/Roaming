using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TobyBallPickup : MonoBehaviour
{
    [SerializeField] private Rigidbody ball;
    [SerializeField] private Transform mouth;
    [SerializeField] private PlayerControle movement;
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.1f)] private float pickupDistance = 1.2f;
    [SerializeField, Min(0f)] private float pickupAttachDelay = 0.72f;
    [SerializeField, Min(0f)] private float pickupAnimationDuration = 1.48f;
    private Collider ballCollider;
    private Coroutine pickupRoutine;
    private bool pickupInProgress;
    private bool movementWasEnabled;
    private static readonly int PickUp = Animator.StringToHash("PickUp");
    public bool IsCarrying { get; private set; }

    private void Awake()
    {
        if (movement == null) movement = GetComponent<PlayerControle>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (ball != null) ballCollider = ball.GetComponent<Collider>();
    }

    private void Update()
    {
        if (pickupInProgress || !Input.GetKeyDown(KeyCode.E)) return;
        if (IsCarrying) TryPutDown();
        else TryPickUp();
    }

    public bool TryPickUp()
    {
        if (!isActiveAndEnabled || IsCarrying || ball == null || mouth == null
            || Time.timeScale <= 0f || movement == null || !movement.isActiveAndEnabled)
            return false;
        if ((ball.position - transform.position).sqrMagnitude > pickupDistance * pickupDistance)
            return false;

        if (animator == null)
        {
            AttachBall();
            return true;
        }

        pickupInProgress = true;
        movementWasEnabled = movement.enabled;
        movement.enabled = false;
        animator.ResetTrigger(PickUp);
        animator.SetTrigger(PickUp);
        pickupRoutine = StartCoroutine(FinishPickUp());
        return true;
    }

    public bool TryPickUpImmediately()
    {
        if (!isActiveAndEnabled || IsCarrying || ball == null || mouth == null) return false;
        AttachBall();
        return true;
    }

    public bool TryPutDown()
    {
        if (!isActiveAndEnabled || pickupInProgress || !IsCarrying || ball == null || mouth == null
            || Time.timeScale <= 0f || movement == null || !movement.isActiveAndEnabled)
            return false;

        var sphere = ballCollider as SphereCollider;
        Vector3 scale = ball.transform.lossyScale;
        float radius = sphere != null
            ? sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))
            : 0.09f;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 target = transform.position + forward * 0.8f;
        RaycastHit ground = default;
        bool foundGround = false;
        foreach (var hit in Physics.RaycastAll(target + Vector3.up * 0.75f,
                     Vector3.down, 2.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform) || hit.rigidbody == ball || hit.normal.y < 0.5f)
                continue;
            if (!foundGround || hit.distance < ground.distance)
            {
                ground = hit;
                foundGround = true;
            }
        }
        if (!foundGround) return false;
        Vector3 position = ground.point + ground.normal * (radius + 0.015f);
        foreach (var obstacle in Physics.OverlapSphere(position, radius,
                     ~0, QueryTriggerInteraction.Ignore))
        {
            if (obstacle.attachedRigidbody != ball) return false;
        }

        ball.position = position;
        ball.transform.position = position;
        ReleaseBall();
        ball.linearVelocity = Vector3.zero;
        ball.angularVelocity = Vector3.zero;
        return true;
    }

    private IEnumerator FinishPickUp()
    {
        yield return new WaitForSeconds(pickupAttachDelay);
        if (ball != null && mouth != null) AttachBall();

        float remaining = Mathf.Max(0f, pickupAnimationDuration - pickupAttachDelay);
        if (remaining > 0f) yield return new WaitForSeconds(remaining);
        FinishPickupLock();
    }

    private void AttachBall()
    {
        ball.linearVelocity = Vector3.zero;
        ball.angularVelocity = Vector3.zero;
        ball.collisionDetectionMode = CollisionDetectionMode.Discrete;
        ball.interpolation = RigidbodyInterpolation.None;
        ball.isKinematic = true;
        ball.useGravity = false;
        ball.detectCollisions = false;
        if (ballCollider != null) ballCollider.enabled = false;
        IsCarrying = true;
        FollowMouth();
    }

    private void FinishPickupLock()
    {
        pickupRoutine = null;
        pickupInProgress = false;
        if (movement != null && movementWasEnabled) movement.enabled = true;
        movementWasEnabled = false;
    }


    private void LateUpdate()
    {
        if (IsCarrying && ball != null && mouth != null) FollowMouth();
    }

    private void FollowMouth()
    {
        // Follow in world space so the FBX's scaled bones never resize the ball.
        ball.transform.SetPositionAndRotation(mouth.position, mouth.rotation);
    }

    private void ReleaseBall()
    {
        IsCarrying = false;
        if (ballCollider != null) ballCollider.enabled = true;
        ball.detectCollisions = true;
        ball.isKinematic = false;
        ball.useGravity = true;
        ball.interpolation = RigidbodyInterpolation.Interpolate;
        ball.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void OnDisable()
    {
        if (pickupRoutine != null) StopCoroutine(pickupRoutine);
        if (pickupInProgress) FinishPickupLock();
        if (IsCarrying && ball != null) ReleaseBall();
    }
}
