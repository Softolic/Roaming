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
        // 1) Tela preta (o logo já começa com opacity 0, definido no USS)
        yield return new WaitForSeconds(iniciopreto);

        // 2) Fade-in do logo
        logo.AddToClassList("visible");
        yield return new WaitForSeconds(fadein);

        // 3) Logo visível, parado
        yield return new WaitForSeconds(logovi);

        // 4) Fade-out do logo
        logo.RemoveFromClassList("visible");
        yield return new WaitForSeconds(fadeout);

        // 5) Tela preta de novo (logo já está com opacity 0)
        yield return new WaitForSeconds(fimpreto);

        // 6) Carrega o menu principal
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
