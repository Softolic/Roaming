using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private string targetScene = "Game";
    [SerializeField, Min(0f)] private float minimumDisplayTime = 2.2f;
    [SerializeField, Min(0.05f)] private float dotInterval = 0.4f;

    private Label loadingText;
    private VisualElement progressFill;

    private IEnumerator Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        loadingText = root.Q<Label>("loading-text");
        progressFill = root.Q<VisualElement>("progress-fill");

        if (loadingText == null || progressFill == null)
        {
            Debug.LogError("LoadingScreenController: elementos da interface não encontrados.", this);
            yield break;
        }

        StartCoroutine(AnimateDots());

        float startedAt = Time.realtimeSinceStartup;
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            UpdateProgress(operation.progress / 0.9f);
            yield return null;
        }

        UpdateProgress(1f);

        float elapsed = Time.realtimeSinceStartup - startedAt;
        if (elapsed < minimumDisplayTime)
            yield return new WaitForSecondsRealtime(minimumDisplayTime - elapsed);

        operation.allowSceneActivation = true;
    }

    private IEnumerator AnimateDots()
    {
        int dots = 1;

        while (true)
        {
            loadingText.text = "CARREGANDO" + new string('.', dots);
            dots = dots == 3 ? 1 : dots + 1;
            yield return new WaitForSecondsRealtime(dotInterval);
        }
    }

    private void UpdateProgress(float normalizedProgress)
    {
        progressFill.style.width = Length.Percent(Mathf.Clamp01(normalizedProgress) * 100f);
    }
}
