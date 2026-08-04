using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    private readonly string[] itemNames =
    {
        "item-start",
        "item-load",
        "item-options",
        "item-credits",
        "item-exit"
    };

    private VisualElement[] menuItems;
    private int selectedIndex = -1;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        menuItems = new VisualElement[itemNames.Length];

        for (int i = 0; i < itemNames.Length; i++)
        {
            int index = i;
            menuItems[i] = root.Q<VisualElement>(itemNames[i]);
            menuItems[i].RegisterCallback<ClickEvent>(_ => Activate(index));
            menuItems[i].RegisterCallback<MouseEnterEvent>(_ => SetSelection(index));
        }

        root.focusable = true;
        root.Focus();
        root.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.S)
            MoveSelection(1);
        else if (evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.W)
            MoveSelection(-1);
        else if ((evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Space) && selectedIndex >= 0)
            Activate(selectedIndex);
    }

    private void MoveSelection(int direction)
    {
        int nextIndex = selectedIndex < 0
            ? 0
            : (selectedIndex + direction + menuItems.Length) % menuItems.Length;

        SetSelection(nextIndex);
    }

    private void SetSelection(int index)
    {
        selectedIndex = index;
        for (int i = 0; i < menuItems.Length; i++)
            menuItems[i].EnableInClassList("selected", i == selectedIndex);
    }

    private void Activate(int index)
    {
        switch (itemNames[index])
        {
            case "item-start":
                SceneManager.LoadScene("carregamento");
                break;
            case "item-load":
                Debug.Log("Abrir carregamento");
                break;
            case "item-options":
                Debug.Log("Abrir configurações");
                break;
            case "item-credits":
                Debug.Log("Abrir créditos");
                break;
            case "item-exit":
                Application.Quit();
                break;
        }
    }
}
