using System.Collections;
using UnityEngine;

/// <summary>Keeps the chapter ball moving ahead of the player along the complete river route.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class ForestBallChase : MonoBehaviour
{
    [SerializeField] private PlayerControle player;
    [SerializeField] private TobyBallPickup pickup;
    [SerializeField] private MissionController mission;
    [SerializeField] private Vector3[] route;
    [Header("Efeito chiclete ao longo do rio")]
    [Tooltip("Distancia que a bolinha tenta manter na frente do jogador pela rota.")]
    [SerializeField, Min(.5f)] private float comfortableGap = 12f;
    [Tooltip("Avanco maximo permitido antes de a bolinha esperar.")]
    [SerializeField, Min(1f)] private float maximumGap = 18f;
    [SerializeField, Min(.1f)] private float maximumSpeed = 5.3f;
    [SerializeField, Min(.1f)] private float acceleration = 5f;
    [SerializeField, Min(0f)] private float startDelay = 2f;
    [Tooltip("Distancia restante do jogador na rota para concluir a perseguicao.")]
    [SerializeField, Min(.5f)] private float arrivalDistance = 5.5f;
    private Rigidbody body;
    private SphereCollider sphere;
    private float[] distances;
    private float progress;
    private float speed;
    private float radius;
    private bool ready;
    private bool hasRestoredProgress;
    private float restoredProgress;
    private bool restoredCompleted;
    private bool pickupWasEnabled;
    private CollisionDetectionMode originalDetection;
    private RigidbodyInterpolation originalInterpolation;
    private bool originalGravity;
    private bool originalKinematic;
    private bool originalTrigger;

    public bool IsCompleted { get; private set; }
    public float CurrentSpeed => Mathf.Abs(speed);
    public float Progress => progress;
    public float RouteLength => distances == null ? 0f : distances[distances.Length - 1];

    private IEnumerator Start()
    {
        body = GetComponent<Rigidbody>();
        sphere = GetComponent<SphereCollider>();
        if (player == null || mission == null || route == null || route.Length < 2)
        {
            Debug.LogError("ForestBallChase: configure jogador, missao e rota do rio.", this);
            enabled = false;
            yield break;
        }

        // Arrival, terrain grounding and pending save restoration finish on the first frames.
        yield return null;
        while (SaveSystem.HasPendingLoadFor(gameObject.scene.name))
            yield return null;

        if (pickup != null)
        {
            pickupWasEnabled = pickup.enabled;
            // The ball is a story guide in this chapter and cannot be picked up.
            pickup.enabled = false;
        }

        originalGravity = body.useGravity;
        originalKinematic = body.isKinematic;
        originalDetection = body.collisionDetectionMode;
        originalInterpolation = body.interpolation;
        originalTrigger = sphere.isTrigger;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        sphere.enabled = true;
        sphere.isTrigger = true;
        radius = sphere.radius * Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            Mathf.Abs(transform.lossyScale.z));

        distances = new float[route.Length];
        for (int i = 1; i < route.Length; i++)
            distances[i] = distances[i - 1] + Vector3.Distance(route[i - 1], route[i]);

        float playerProgress = NearestProgress(player.transform.position);
        progress = hasRestoredProgress
            ? Mathf.Clamp(restoredProgress, 0f, RouteLength)
            : Mathf.Clamp(playerProgress + comfortableGap, 0f, RouteLength);
        IsCompleted = hasRestoredProgress && restoredCompleted;
        if (IsCompleted)
            progress = RouteLength;

        body.position = PointAt(progress);
        transform.position = body.position;
        mission.SetMission("SIGA A BOLINHA");
        if (IsCompleted)
            mission.CompleteMission();

        ready = true;
        yield return new WaitForSeconds(startDelay);
        moving = true;
    }

    // SaveLoadRuntime can call this while Start is still waiting for the restored player position.
    public void RestoreProgress(float savedProgress, bool completed)
    {
        hasRestoredProgress = true;
        restoredProgress = float.IsNaN(savedProgress) || float.IsInfinity(savedProgress)
            ? 0f
            : savedProgress;
        restoredCompleted = completed;
        if (!ready)
            return;

        progress = completed ? RouteLength : Mathf.Clamp(restoredProgress, 0f, RouteLength);
        speed = 0f;
        IsCompleted = completed;
        body.position = PointAt(progress);
        transform.position = body.position;
        mission.SetMission("SIGA A BOLINHA");
        if (completed)
            mission.CompleteMission();
    }

    private bool moving;

    private void FixedUpdate()
    {
        if (!ready || !moving || IsCompleted || Time.timeScale <= 0f)
            return;
        Vector3 current = PointAt(progress);
        float playerProgress = NearestProgress(player.transform.position);
        float lead = progress - playerProgress;
        float targetSpeed = Mathf.Clamp(
            player.CurrentSpeed + (comfortableGap - lead) * 1.8f,
            0f,
            maximumSpeed);

        if (!player.isActiveAndEnabled || lead >= maximumGap)
            targetSpeed = 0f;

        speed = Mathf.MoveTowards(speed, targetSpeed, acceleration * Time.fixedDeltaTime);
        float nextProgress = Mathf.Clamp(progress + speed * Time.fixedDeltaTime, 0f, RouteLength);
        Vector3 next = PointAt(nextProgress);
        float nextLead = nextProgress - playerProgress;

        bool tooFarAhead = nextLead > maximumGap && nextLead > lead;
        if (tooFarAhead)
        {
            speed = 0f;
            return;
        }

        Vector3 delta = next - current;
        body.MovePosition(next);
        Vector3 axis = Vector3.Cross(Vector3.up, delta);
        if (axis.sqrMagnitude > .000001f)
        {
            float angle = delta.magnitude / Mathf.Max(.01f, radius) * Mathf.Rad2Deg;
            body.MoveRotation(Quaternion.AngleAxis(angle, axis.normalized) * body.rotation);
        }

        progress = nextProgress;
        float playerRemaining = RouteLength - playerProgress;
        if (progress >= RouteLength - .01f && playerRemaining <= arrivalDistance)
        {
            speed = 0f;
            IsCompleted = true;
            mission.CompleteMission();
        }
    }

    private Vector3 PointAt(float distance)
    {
        for (int i = 1; i < route.Length; i++)
        {
            if (distance <= distances[i])
            {
                return Vector3.Lerp(
                    route[i - 1],
                    route[i],
                    Mathf.InverseLerp(distances[i - 1], distances[i], distance));
            }
        }

        return route[route.Length - 1];
    }

    private float NearestProgress(Vector3 position)
    {
        float best = float.PositiveInfinity;
        float result = 0f;
        for (int i = 1; i < route.Length; i++)
        {
            Vector3 a = route[i - 1];
            Vector3 segment = route[i] - a;
            Vector3 flat = Vector3.ProjectOnPlane(segment, Vector3.up);
            float t = flat.sqrMagnitude < .0001f
                ? 0f
                : Mathf.Clamp01(
                    Vector3.Dot(Vector3.ProjectOnPlane(position - a, Vector3.up), flat)
                    / flat.sqrMagnitude);
            float sqr = Vector3.ProjectOnPlane(
                position - (a + segment * t),
                Vector3.up).sqrMagnitude;
            if (sqr >= best)
                continue;

            best = sqr;
            result = Mathf.Lerp(distances[i - 1], distances[i], t);
        }

        return result;
    }

    private void OnDisable()
    {
        if (!ready || body == null)
            return;

        body.isKinematic = originalKinematic;
        body.useGravity = originalGravity;
        body.collisionDetectionMode = originalDetection;
        body.interpolation = originalInterpolation;
        sphere.isTrigger = originalTrigger;
        if (pickup != null && pickupWasEnabled)
            pickup.enabled = true;
        ready = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (route == null)
            return;

        Gizmos.color = new Color(1f, .35f, .15f);
        for (int i = 0; i < route.Length; i++)
        {
            Gizmos.DrawWireSphere(route[i], .09f);
            if (i > 0)
                Gizmos.DrawLine(route[i - 1], route[i]);
        }
    }
}

