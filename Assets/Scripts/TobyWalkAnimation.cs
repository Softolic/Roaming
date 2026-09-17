using UnityEngine;

[DisallowMultipleComponent]
public sealed class TobyWalkAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerControle movement;
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int WalkRate = Animator.StringToHash("WalkRate");
    private bool hasWalkRate;
    private bool walking;
    private float smoothedSpeed;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (movement == null) movement = GetComponent<PlayerControle>();
        if (animator == null) return;
        foreach (var parameter in animator.parameters)
            if (parameter.nameHash == WalkRate && parameter.type == AnimatorControllerParameterType.Float)
                hasWalkRate = true;
    }

    private void Update()
    {
        if (animator == null) return;
        bool canMove = movement != null && movement.isActiveAndEnabled && Time.timeScale > 0f;
        float speed = canMove ? movement.CurrentSpeed : 0f;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 1f - Mathf.Exp(-12f * Time.deltaTime));
        if (!canMove) walking = false;
        else if (walking) walking = smoothedSpeed > .10f;
        else walking = smoothedSpeed > .22f;
        animator.SetBool(IsWalking, walking);
        if (hasWalkRate)
        {
            float ratio = movement != null ? smoothedSpeed / Mathf.Max(.1f, movement.MaxSpeed) : 0f;
            // Only the Walk state uses this multiplier; pickup keeps its authored timing.
            animator.SetFloat(WalkRate, Mathf.Clamp(ratio, .28f, 1.15f), .10f, Time.deltaTime);
        }
    }

    private void OnDisable()
    {
        walking = false;
        smoothedSpeed = 0f;
        if (animator != null && animator.isActiveAndEnabled) animator.SetBool(IsWalking, false);
    }
}
