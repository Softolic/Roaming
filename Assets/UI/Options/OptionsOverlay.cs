using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class OptionsOverlay : MonoBehaviour
{
    private const string VolumeKey = "settings-volume";
    private const string WidthKey = "settings-width";
    private const string HeightKey = "settings-height";
    private const string DisplayModeKey = "settings-display-mode";


    private readonly List<Resolution> resolutions = new();
    private DropdownField resolutionDropdown;
    private DropdownField displayModeDropdown;

    private Slider volumeSlider;
    private Label volumeLabel;
    private int pendingResolutionIndex;
    private FullScreenMode pendingFullScreenMode;

    private VisualElement optionsRoot;

    public bool IsOpen => optionsRoot != null && optionsRoot.style.display == DisplayStyle.Flex;

private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        optionsRoot = root.Q<VisualElement>("options-root");
        displayModeDropdown = root.Q<DropdownField>("display-mode-dropdown");
        resolutionDropdown = root.Q<DropdownField>("resolution-dropdown");
        volumeSlider = root.Q<Slider>("volume-slider");
        volumeLabel = root.Q<Label>("volume-value");

        ConfigureDisplayMode();
        BuildResolutionList();
        LoadVolume();

        root.Q<Button>("options-apply-button").clicked += ApplyResolution;
        root.Q<Button>("options-back-button").clicked += Hide;
        displayModeDropdown.RegisterValueChangedCallback(OnDisplayModeChanged);
        resolutionDropdown.RegisterValueChangedCallback(OnResolutionChanged);
        volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
        Hide();
    }

    public void Show()
    {
        optionsRoot.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        optionsRoot.style.display = DisplayStyle.None;
    }

    private void BuildResolutionList()
    {
        foreach (var candidate in Screen.resolutions)
        {
            bool alreadyAdded = false;
            foreach (var existing in resolutions)
            {
                if (existing.width == candidate.width && existing.height == candidate.height)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded)
                resolutions.Add(candidate);
        }

        if (resolutions.Count == 0)
            resolutions.Add(Screen.currentResolution);

        resolutions.Sort((left, right) =>
        {
            int widthComparison = left.width.CompareTo(right.width);
            return widthComparison != 0 ? widthComparison : left.height.CompareTo(right.height);
        });

        var labels = new List<string>();
        for (int i = 0; i < resolutions.Count; i++)
            labels.Add($"{resolutions[i].width} x {resolutions[i].height}");

        resolutionDropdown.choices = labels;

        int savedWidth = PlayerPrefs.GetInt(WidthKey, Screen.width);
        int savedHeight = PlayerPrefs.GetInt(HeightKey, Screen.height);
        int selectedIndex = FindResolution(savedWidth, savedHeight);
        pendingResolutionIndex = selectedIndex;
        resolutionDropdown.SetValueWithoutNotify(labels[selectedIndex]);
    }

private void ConfigureDisplayMode()
    {
        displayModeDropdown.choices = new List<string> { "JANELA", "TELA CHEIA" };

        int savedMode = PlayerPrefs.GetInt(DisplayModeKey, (int)Screen.fullScreenMode);
        pendingFullScreenMode = savedMode == (int)FullScreenMode.Windowed
            ? FullScreenMode.Windowed
            : FullScreenMode.FullScreenWindow;

        displayModeDropdown.SetValueWithoutNotify(
            pendingFullScreenMode == FullScreenMode.Windowed ? "JANELA" : "TELA CHEIA");
    }

    private void OnDisplayModeChanged(ChangeEvent<string> evt)
    {
        pendingFullScreenMode = evt.newValue == "JANELA"
            ? FullScreenMode.Windowed
            : FullScreenMode.FullScreenWindow;
    }


    private void LoadVolume()
    {
        float volume = PlayerPrefs.GetFloat(VolumeKey, AudioListener.volume);
        AudioListener.volume = volume;
        volumeSlider.SetValueWithoutNotify(volume);
        UpdateVolumeLabel(volume);
    }

private void OnResolutionChanged(ChangeEvent<string> evt)
    {
        pendingResolutionIndex = resolutionDropdown.choices.IndexOf(evt.newValue);
    }

private void ApplyResolution()
    {
        if (pendingResolutionIndex < 0 || pendingResolutionIndex >= resolutions.Count)
            return;

        Resolution selected = resolutions[pendingResolutionIndex];
        Screen.SetResolution(selected.width, selected.height, pendingFullScreenMode);
        PlayerPrefs.SetInt(WidthKey, selected.width);
        PlayerPrefs.SetInt(HeightKey, selected.height);
        PlayerPrefs.SetInt(DisplayModeKey, (int)pendingFullScreenMode);
        PlayerPrefs.Save();
    }


    private void OnVolumeChanged(ChangeEvent<float> evt)
    {
        AudioListener.volume = evt.newValue;
        PlayerPrefs.SetFloat(VolumeKey, evt.newValue);
        PlayerPrefs.Save();
        UpdateVolumeLabel(evt.newValue);
    }

    private void UpdateVolumeLabel(float volume)
    {
        volumeLabel.text = $"{Mathf.RoundToInt(volume * 100f)}%";
    }

    private int FindResolution(int width, int height)
    {
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == width && resolutions[i].height == height)
                return i;
        }

        return resolutions.Count - 1;
    }
}