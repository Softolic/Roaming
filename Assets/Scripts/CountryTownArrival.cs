using UnityEngine;

// Consumes the existing chapter hand-off without the forest-specific title or chase.
public sealed class CountryTownArrival : MonoBehaviour
{
    [SerializeField] private TobyBallPickup pickup;
    [SerializeField] private Rigidbody ball;

    private void Start()
    {
        bool bringBall;
        bool arrived = ForestChapterTransition.ConsumeArrival(out bringBall);
        if (ball == null) return;
        bool carry = arrived && bringBall && pickup != null;
        ball.gameObject.SetActive(carry);
        if (carry)
        {
            ball.position = pickup.transform.position + Vector3.up * .1f;
            pickup.TryPickUpImmediately();
        }
    }
}
