using UnityEngine;
using UnityEngine.UIElements;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(UIDocument))]
public sealed class SceneChapterTitle : MonoBehaviour
{
    [SerializeField] private string chapterName = "PROLOGO";
    [SerializeField] private string subtitle = "REMAKE";

private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;
        var chapter = root.Q<Label>("chapter-name");
        var detail = root.Q<Label>("chapter-subtitle");
        if (chapter != null) chapter.text = chapterName;
        if (detail != null) detail.text = subtitle;
    }
}
