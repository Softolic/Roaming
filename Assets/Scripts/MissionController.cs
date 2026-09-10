using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class MissionController : MonoBehaviour
{
    [SerializeField] private string initialMission = "VOLTE PARA CASA";

    public string CurrentMission { get; private set; }
    public bool IsCompleted { get; private set; }
    public event Action<string, bool> MissionChanged;

    private Label missionLabel;
    private bool initialized;

    private void Start()
    {
        if (!initialized) SetMission(initialMission);
        BindUI();
    }

    private void OnEnable()
    {
        if (initialized) BindUI();
    }

    private void OnDisable()
    {
        missionLabel = null;
    }

    public void SetMission(string objective)
    {
        initialized = true;
        CurrentMission = WithoutAccents(objective).Trim().ToUpperInvariant();
        IsCompleted = false;
        RefreshUI();
        MissionChanged?.Invoke(CurrentMission, IsCompleted);
    }

    public void CompleteMission()
    {
        if (string.IsNullOrWhiteSpace(CurrentMission) || IsCompleted) return;
        IsCompleted = true;
        RefreshUI();
        MissionChanged?.Invoke(CurrentMission, IsCompleted);
    }

    public void ClearMission()
    {
        SetMission(string.Empty);
    }

    private void BindUI()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;
        root.pickingMode = PickingMode.Ignore;
        missionLabel = root.Q<Label>("mission-text");
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (missionLabel == null) return;
        bool visible = !string.IsNullOrWhiteSpace(CurrentMission);
        missionLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        missionLabel.text = (IsCompleted ? "MISSAO CONCLUIDA: " : "MISSAO: ") + CurrentMission;
    }

    private static string WithoutAccents(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var result = new System.Text.StringBuilder();
        foreach (char character in text.Normalize(System.Text.NormalizationForm.FormD))
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                result.Append(character);
        return result.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}
