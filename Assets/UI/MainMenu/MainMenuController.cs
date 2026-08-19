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
    
    private VisualElement root;
    private Label title;
    private VisualElement titleRule;
    private Label[] menuLabels;
private int selectedIndex = -1;

private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        if (GetComponent<OptionsOverlay>() == null)
            gameObject.AddComponent<OptionsOverlay>();

        title = root.Q<Label>("title");
        titleRule = root.Q<VisualElement>(className: "title-rule");
        menuItems = new VisualElement[itemNames.Length];
        menuLabels = new Label[itemNames.Length];

        for (int i = 0; i < itemNames.Length; i++)
        {
            int index = i;
            menuItems[i] = root.Q<VisualElement>(itemNames[i]);
            menuLabels[i] = menuItems[i].Q<Label>();
            menuItems[i].RegisterCallback<ClickEvent>(_ => Activate(index));
            menuItems[i].RegisterCallback<MouseEnterEvent>(_ => SetSelection(index));
        }

        root.focusable = true;
        root.Focus();
        root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

private void OnKeyDown(KeyDownEvent evt)
    {
        var options = GetComponent<OptionsOverlay>();
        if (options != null && options.IsOpen)
        {
            if (evt.keyCode == KeyCode.Escape)
                options.Hide();

            evt.StopPropagation();
            return;
        }

        if (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.S)
            MoveSelection(1);
        else if (evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.W)
            MoveSelection(-1);
        else if ((evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Space) && selectedIndex >= 0)
            Activate(selectedIndex);
    }

private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        if (evt.newRect.width <= 0f || evt.newRect.height <= 0f)
            return;

        float scale = Mathf.Clamp(
            Mathf.Min(evt.newRect.width / 1280f, evt.newRect.height / 720f),
            0.55f,
            1.3f);

        title.style.fontSize = 76f * scale;
        titleRule.style.width = Mathf.Clamp(220f * scale, 140f, 300f);
        titleRule.style.marginBottom = 24f * scale;

        for (int i = 0; i < menuItems.Length; i++)
        {
            menuItems[i].style.width = Mathf.Clamp(300f * scale, 210f, 380f);
            menuItems[i].style.height = Mathf.Clamp(55f * scale, 42f, 70f);
            menuItems[i].style.marginBottom = Mathf.Clamp(12f * scale, 6f, 16f);
            menuLabels[i].style.fontSize = Mathf.Clamp(16f * scale, 12f, 20f);
        }
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
                GetComponent<OptionsOverlay>().Show();
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
