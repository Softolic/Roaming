using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PauseMenuController : MonoBehaviour
{
    private VisualElement pauseRoot;
    private bool isPaused;

private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (GetComponent<OptionsOverlay>() == null)
            gameObject.AddComponent<OptionsOverlay>();

        pauseRoot = root.Q<VisualElement>("pause-root");
        root.Q<Button>("options-button").clicked += OpenOptions;
        root.Q<Button>("exit-button").clicked += ExitToTitle;
        SetPaused(false);
    }

private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        var options = GetComponent<OptionsOverlay>();
        if (options != null && options.IsOpen)
        {
            options.Hide();
            return;
        }

        SetPaused(!isPaused);
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        pauseRoot.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;
        Time.timeScale = paused ? 0f : 1f;
        UnityEngine.Cursor.visible = paused;
        UnityEngine.Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
    }

private void OpenOptions()
    {
        GetComponent<OptionsOverlay>().Show();
    }

    private void ExitToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TelaTitulo");
    }
}