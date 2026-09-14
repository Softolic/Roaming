using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    private readonly string[] itemNames =
    {
        "item-start",
        "item-chapters",
        "item-load",
        "item-options",
        "item-credits",
        "item-exit"
    };


    private bool showingChapters;
    private bool loadingScene;
    private int VisibleItemCount => showingChapters ? 3 : itemNames.Length;

    private void ShowChapters()
    {
        showingChapters = true;
        string[] labels = { "PROLOGO", "CAPITULO 1", "VOLTAR" };
        for (int i = 0; i < menuItems.Length; i++)
        {
            menuItems[i].style.display = i < labels.Length ? DisplayStyle.Flex : DisplayStyle.None;
            if (i < labels.Length) menuLabels[i].text = labels[i];
        }
        root.Q<Label>("chapters-title").style.display = DisplayStyle.Flex;
        SetSelection(0);
        root.Focus();
    }

    private void ShowMainMenu()
    {
        showingChapters = false;
        string[] labels = { "NOVO JOGO", "CAPITULOS", "CARREGAR", "OPCOES", "CREDITOS", "SAIR" };
        for (int i = 0; i < menuItems.Length; i++)
        {
            menuItems[i].style.display = DisplayStyle.Flex;
            menuLabels[i].text = labels[i];
        }
        root.Q<Label>("chapters-title").style.display = DisplayStyle.None;
        SetSelection(1);
        root.Focus();
    }

    private void StartChapter(string sceneName)
    {
        if (loadingScene) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName)
            || !Application.CanStreamedLevelBeLoaded("carregamento"))
        {
            Debug.LogError("A cena selecionada nao esta disponivel: " + sceneName);
            return;
        }
        loadingScene = true;
        SaveSystem.CancelPendingLoad();
        bool unused;
        ForestChapterTransition.ConsumeArrival(out unused);
        Time.timeScale = 1f;
        SceneLoadRequest.Request(sceneName);
        SceneManager.LoadScene("carregamento");
    }

    private VisualElement[] menuItems;
    
    private VisualElement root;
    private VisualElement title;
    private VisualElement titleRule;
    private Label[] menuLabels;
    private readonly List<FireflyMotion> fireflies = new();
    private Coroutine fireflyLoop;


    private sealed class FireflyMotion
    {
        public VisualElement Element;
        public Vector2 Offset;
        public Vector2 TargetOffset;
        public float Phase;
        public float TravelSpeed;
        public float NextRetargetAt;

    }

private int selectedIndex = -1;

private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        if (GetComponent<OptionsOverlay>() == null)
            gameObject.AddComponent<OptionsOverlay>();

        title = root.Q<VisualElement>("title");
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
        CreateFireflyMotions();
        if (fireflyLoop != null)
            StopCoroutine(fireflyLoop);

        fireflyLoop = StartCoroutine(AnimateFireflies());

    }

private void OnDisable()
    {
        if (fireflyLoop != null)
        {
            StopCoroutine(fireflyLoop);
            fireflyLoop = null;
        }
    }

    private IEnumerator AnimateFireflies()
    {
        while (true)
        {
            UpdateFireflies();
            yield return null;
        }
    }


private void OnKeyDown(KeyDownEvent evt)
    {
        if (showingChapters && evt.keyCode == KeyCode.Escape)
        {
            ShowMainMenu();
            evt.StopPropagation();
            return;
        }

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

        title.style.height = Mathf.Clamp(210f * scale, 140f, 245f);
        titleRule.style.width = Mathf.Clamp(220f * scale, 140f, 300f);
        titleRule.style.marginBottom = 24f * scale;

        for (int i = 0; i < menuItems.Length; i++)
        {
            menuItems[i].style.width = Mathf.Clamp(300f * scale, 210f, 380f);
            menuItems[i].style.height = Mathf.Clamp(48f * scale, 40f, 60f);
            menuItems[i].style.marginBottom = Mathf.Clamp(8f * scale, 5f, 12f);
            menuLabels[i].style.fontSize = Mathf.Clamp(16f * scale, 12f, 20f);
        }
    }

private void CreateFireflyMotions()
    {
        fireflies.Clear();
        root.Query<VisualElement>(className: "firefly").ForEach(element =>
        {
            fireflies.Add(new FireflyMotion
            {
                Element = element,
                Offset = Vector2.zero,
                TargetOffset = Random.insideUnitCircle * Random.Range(18f, 42f),
                Phase = Random.Range(0f, Mathf.PI * 2f),
                TravelSpeed = Random.Range(0.85f, 1.35f),
                NextRetargetAt = Time.unscaledTime + Random.Range(1.5f, 4f)
            });
        });
    }

private void UpdateFireflies()
    {
        float time = Time.unscaledTime;

        foreach (FireflyMotion firefly in fireflies)
        {
            if (time >= firefly.NextRetargetAt)
            {
                firefly.TargetOffset = Random.insideUnitCircle * Random.Range(18f, 42f);
                firefly.NextRetargetAt = time + Random.Range(1.5f, 4f);
            }

            firefly.Offset = Vector2.Lerp(
                firefly.Offset,
                firefly.TargetOffset,
                1f - Mathf.Exp(-firefly.TravelSpeed * Time.unscaledDeltaTime));

            firefly.Element.style.translate = new Translate(
                new Length(firefly.Offset.x, LengthUnit.Pixel),
                new Length(firefly.Offset.y, LengthUnit.Pixel),
                0f);

            float pulse = 0.62f + 0.38f * Mathf.Sin(time * 1.35f + firefly.Phase);
            firefly.Element.style.opacity = Mathf.Clamp01(pulse);
        }
    }



    private void MoveSelection(int direction)
    {
        int nextIndex = selectedIndex < 0
            ? 0
            : (selectedIndex + direction + VisibleItemCount) % VisibleItemCount;

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
        if (loadingScene) return;
        if (showingChapters)
        {
            switch (index)
            {
                case 0: StartChapter("Prologo Remake"); break;
                case 1: StartChapter("Capitulo 1"); break;
                case 2: ShowMainMenu(); break;
            }
            return;
        }
        switch (itemNames[index])
        {
            case "item-start":
                StartChapter("Prologo Remake");
                break;
            case "item-chapters":
                ShowChapters();
                break;
            case "item-load":
                SceneLoadRequest.Clear();
                if (SaveSystem.PrepareLoad())
                    SceneManager.LoadScene("carregamento");
                break;
            case "item-options":
                GetComponent<OptionsOverlay>().Show();
                break;
            case "item-credits":
                Debug.Log("Abrir creditos");
                break;
            case "item-exit":
                Application.Quit();
                break;
        }
    }
}
