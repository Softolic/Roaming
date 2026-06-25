using UnityEngine;
using UnityEngine.UIElements;

// Anexe este script no mesmo GameObject que tem o componente UIDocument.
[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [Tooltip("Nomes dos itens na ordem em que aparecem na UXML (deve bater com o atributo 'name').")]
    private readonly string[] itemNames =
    {
        "item-continue",
        "item-newgame",
        "item-chapters",
        "item-load",
        "item-options",
        "item-credits",
        "item-exit"
    };

    private VisualElement[] menuItems;
    // -1 = nenhum item selecionado (estado inicial, igual ao print de referência)
    private int selectedIndex = -1;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        menuItems = new VisualElement[itemNames.Length];
        for (int i = 0; i < itemNames.Length; i++)
        {
            int index = i; // captura local pro callback
            var element = root.Q<VisualElement>(itemNames[i]);
            menuItems[i] = element;

            // clique do mouse
            element.RegisterCallback<ClickEvent>(evt => OnItemActivated(index));

            // passar o mouse por cima também marca a caixinha, igual teclado
            element.RegisterCallback<MouseEnterEvent>(evt => SetSelection(index));
        }

        // não chama UpdateSelectionVisuals aqui de propósito:
        // com selectedIndex = -1 nenhuma classe "selected"/"checkbox-filled" é aplicada,
        // então o menu abre limpo, sem nenhuma caixinha marcada.

        // captura teclado/controle na raiz
        root.focusable = true;
        root.Focus();
        root.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        switch (evt.keyCode)
        {
            case KeyCode.DownArrow:
            case KeyCode.S:
                MoveSelection(1);
                break;

            case KeyCode.UpArrow:
            case KeyCode.W:
                MoveSelection(-1);
                break;

            case KeyCode.Return:
            case KeyCode.KeypadEnter:
            case KeyCode.Space:
                // só ativa se já existir alguma seleção (evita ativar item errado sem querer)
                if (selectedIndex >= 0)
                    OnItemActivated(selectedIndex);
                break;
        }
    }

    private void MoveSelection(int direction)
    {
        // se ainda não tinha nada selecionado, a primeira tecla pressionada
        // seleciona o item 0 (CONTINUE) antes de mover
        int newIndex = selectedIndex < 0
            ? 0
            : (selectedIndex + direction + menuItems.Length) % menuItems.Length;

        SetSelection(newIndex);
    }

    private void SetSelection(int index)
    {
        selectedIndex = index;
        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < menuItems.Length; i++)
        {
            var checkbox = menuItems[i].Q<VisualElement>(className: "menu-checkbox");

            if (i == selectedIndex)
            {
                menuItems[i].AddToClassList("selected");
                checkbox.AddToClassList("checkbox-filled");
            }
            else
            {
                menuItems[i].RemoveFromClassList("selected");
                checkbox.RemoveFromClassList("checkbox-filled");
            }
        }
    }

    private void OnItemActivated(int index)
    {
        selectedIndex = index;
        UpdateSelectionVisuals();

        string item = itemNames[index];
        Debug.Log($"Menu item ativado: {item}");

        switch (item)
        {
            case "item-continue":
                // TODO: carregar último save
                break;
            case "item-newgame":
                // TODO: SceneManager.LoadScene("Intro");
                break;
            case "item-chapters":
                // TODO: abrir tela de capítulos
                break;
            case "item-load":
                // TODO: abrir tela de load
                break;
            case "item-options":
                // TODO: abrir tela de opções
                break;
            case "item-credits":
                // TODO: abrir tela de créditos
                break;
            case "item-exit":
                Application.Quit();
                break;
        }
    }
}
