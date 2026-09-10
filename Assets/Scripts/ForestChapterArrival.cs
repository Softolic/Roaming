using UnityEngine;
using UnityEngine.UIElements;

public sealed class ForestChapterArrival : MonoBehaviour
{
    [SerializeField] private TobyBallPickup pickup;
    [SerializeField] private Rigidbody ball;
    [SerializeField] private UIDocument chapterIntro;

    private void Start()
    {
        if (chapterIntro != null)
        {
            var root = chapterIntro.rootVisualElement;
            var title = root.Q<Label>("chapter-name");
            var subtitle = root.Q<Label>("chapter-subtitle");
            if (title != null) title.text = "CAPITULO 1";
            if (subtitle != null) subtitle.text = "ENTRE AS ARVORES";
        }
        bool bringBall;
        bool arrived = ForestChapterTransition.ConsumeArrival(out bringBall);
        if (ball == null || pickup == null || !arrived) return;
        if (bringBall)
        {
            ball.gameObject.SetActive(true);
            ball.position = pickup.transform.position + Vector3.up * 0.1f;
            pickup.TryPickUp();
        }
        else ball.gameObject.SetActive(false);
    }
}
