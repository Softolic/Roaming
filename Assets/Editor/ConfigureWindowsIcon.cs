using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class ConfigureWindowsIcon
{
    private const string IconPath = "Assets/Icons/RoamingIcon.jpeg";

[MenuItem("Tools/Roaming/Apply Windows Application Icon")]
    public static void Apply()
    {
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);

        if (icon == null)
        {
            Debug.LogError($"Roaming icon was not found at {IconPath}.");
            return;
        }

        var icons = new Texture2D[8];
        for (int i = 0; i < icons.Length; i++)
            icons[i] = icon;

        PlayerSettings.SetIcons(
            NamedBuildTarget.Standalone,
            icons,
            IconKind.Application);

        AssetDatabase.SaveAssets();
        Debug.Log("Roaming Windows application icon applied.");
    }

    [InitializeOnLoadMethod]
    private static void ApplyOnEditorLoad()
    {
        EditorApplication.delayCall += Apply;
    }
}
