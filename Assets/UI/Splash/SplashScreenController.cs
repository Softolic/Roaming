using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UIDocument))]
public class SplashScreenController : MonoBehaviour
{
    public float iniciopreto = 0.5f;

    public float fadein = 1f;

    public float logovi = 2f;

    public float fadeout = 1f;

    public float fimpreto = 0.5f;

    //tela de titulo
    
    public string mainMenuSceneName = "TelaTitulo";

    private VisualElement logo;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        logo = root.Q<VisualElement>("logo");

        StartCoroutine(PlaySplashSequence());
    }

    private IEnumerator PlaySplashSequence()
    {
        yield return new WaitForSeconds(iniciopreto);

        logo.AddToClassList("visible");
        yield return new WaitForSeconds(fadein);

        yield return new WaitForSeconds(logovi);

        logo.RemoveFromClassList("visible");
        yield return new WaitForSeconds(fadeout);

        yield return new WaitForSeconds(fimpreto);

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
