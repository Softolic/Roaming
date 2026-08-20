using UnityEngine;
using UnityEngine.SceneManagement;

public class CarPlayerCollision : MonoBehaviour
{
    private bool reloading;

    private void OnTriggerEnter(Collider other)
    {
        if (reloading || !other.CompareTag("Player"))
            return;

        reloading = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}