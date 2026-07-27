using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class ChapterIntroController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float delayBeforeShowing = 0.35f;
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.8f;
    [SerializeField, Min(0f)] private float visibleDuration = 2.4f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 1.2f;

    private VisualElement card;

    private void Start()
    {
        card = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("chapter-card");

        if (card == null)
        {
            Debug.LogError("ChapterIntroController: elemento 'chapter-card' não foi encontrado.", this);
            return;
        }

        card.style.opacity = 0f;
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        if (delayBeforeShowing > 0f)
            yield return WaitUnscaled(delayBeforeShowing);

        yield return Fade(0f, 1f, fadeInDuration);
        yield return WaitUnscaled(visibleDuration);
        yield return Fade(1f, 0f, fadeOutDuration);

        card.style.display = DisplayStyle.None;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            card.style.opacity = Mathf.Lerp(from, to, t);
            yield return null;
        }

        card.style.opacity = to;
    }

    private static IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
