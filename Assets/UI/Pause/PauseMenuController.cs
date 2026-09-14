using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PauseMenuController : MonoBehaviour
{
    private VisualElement pauseRoot;
    private Label saveNotification;
    private Coroutine notificationRoutine;
    private bool isPaused;

private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (GetComponent<OptionsOverlay>() == null)
            gameObject.AddComponent<OptionsOverlay>();

        pauseRoot = root.Q<VisualElement>("pause-root");
        saveNotification = root.Q<Label>("save-notification");
        saveNotification.style.display = DisplayStyle.None;
        root.Q<Button>("save-button").clicked += SaveGame;
        root.Q<Button>("load-button").clicked += LoadGame;
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

    private void SaveGame()
    {
        if (SaveSystem.SaveCurrentGame())
            ShowSaveConfirmation();
    }

    private void ShowSaveConfirmation()
    {
        if (notificationRoutine != null)
            StopCoroutine(notificationRoutine);

        notificationRoutine = StartCoroutine(ShowSaveConfirmationRoutine());
    }

    private IEnumerator ShowSaveConfirmationRoutine()
    {
        saveNotification.style.display = DisplayStyle.Flex;
        yield return new WaitForSecondsRealtime(2.5f);
        saveNotification.style.display = DisplayStyle.None;
        notificationRoutine = null;
    }

    private void LoadGame()
    {
        if (!SaveSystem.PrepareLoad())
            return;

        Time.timeScale = 1f;
        SceneManager.LoadScene("carregamento");
    }

    private void ExitToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TelaTitulo");
    }
}
