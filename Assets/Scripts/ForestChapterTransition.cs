using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public sealed class ForestChapterTransition : MonoBehaviour
{
    [SerializeField] private string destinationScene = "Capitulo 1";
    private bool transitioning;
    private static bool pendingArrival;
    private static bool carryingBall;

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRequest()
    {
        pendingArrival = false;
        carryingBall = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerControle>();
        if (player != null) TryTransition(player);
    }

    public bool TryTransition(PlayerControle player)
    {
        if (transitioning || !isActiveAndEnabled || player == null || Time.timeScale <= 0f)
            return false;
        if (!Application.CanStreamedLevelBeLoaded(destinationScene)
            || !Application.CanStreamedLevelBeLoaded("carregamento"))
        {
            Debug.LogError("A cena de destino ou carregamento nao esta na lista de cenas do jogo.", this);
            return false;
        }

        transitioning = true;
        var pickup = player.GetComponent<TobyBallPickup>();
        carryingBall = pickup != null && pickup.IsCarrying;
        SceneLoadRequest.Request(destinationScene);
        pendingArrival = true;
        player.enabled = false;
        var body = player.GetComponent<Rigidbody>();
        if (body != null) body.linearVelocity = Vector3.zero;
        Time.timeScale = 1f;
        SceneManager.LoadScene("carregamento");
        return true;
    }



    public static bool ConsumeArrival(out bool bringBall)
    {
        bringBall = carryingBall;
        bool arrived = pendingArrival;
        pendingArrival = false;
        carryingBall = false;
        return arrived;
    }
}
