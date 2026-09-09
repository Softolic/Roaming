using UnityEngine;

[DisallowMultipleComponent]
public sealed class TobyWalkAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerControle movement;
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (movement == null) movement = GetComponent<PlayerControle>();
    }

    private void Update()
    {
        if (animator == null) return;
        bool canMove = movement != null && movement.isActiveAndEnabled && Time.timeScale > 0f;
        bool hasInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f
            || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f;
        animator.SetBool(IsWalking, canMove && hasInput);
    }

    private void OnDisable()
    {
        if (animator != null && animator.isActiveAndEnabled)
            animator.SetBool(IsWalking, false);
    }
}
