using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SaveLoadRuntime : MonoBehaviour
{
    private static SaveLoadRuntime instance;

    public static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject runtimeObject = new GameObject("Save Load Runtime");
        instance = runtimeObject.AddComponent<SaveLoadRuntime>();
        DontDestroyOnLoad(runtimeObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SaveSystem.HasPendingLoadFor(scene.name))
            StartCoroutine(ApplySavedPositionAfterSceneSetup());
    }

    private IEnumerator ApplySavedPositionAfterSceneSetup()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();
        SaveSystem.ApplyPendingLoad();
    }
}