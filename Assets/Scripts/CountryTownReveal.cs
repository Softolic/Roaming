using UnityEngine;
using Unity.Cinemachine;

// Opens the existing isometric framing gradually as Toby approaches the village.
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public sealed class CountryTownReveal : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float startZ = 375f;
    [SerializeField] private float endZ = 408f;
    [SerializeField] private float extraViewSize = 6f;
    private CinemachineCamera chapterCamera;
    private float appliedSize;

    private void Awake() => chapterCamera = GetComponent<CinemachineCamera>();

    private void Update()
    {
        if (player == null || chapterCamera == null || Time.timeScale <= 0f) return;
        float amount = Mathf.SmoothStep(0f, extraViewSize, Mathf.InverseLerp(startZ, endZ, player.position.z));
        var lens = chapterCamera.Lens;
        // Apply only the change, keeping the player's keyboard zoom adjustments.
        lens.OrthographicSize += amount - appliedSize;
        chapterCamera.Lens = lens;
        appliedSize = amount;
    }

    private void OnDisable()
    {
        if (chapterCamera == null) return;
        var lens = chapterCamera.Lens;
        lens.OrthographicSize -= appliedSize;
        chapterCamera.Lens = lens;
        appliedSize = 0f;
    }
}
