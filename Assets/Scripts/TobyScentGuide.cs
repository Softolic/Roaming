using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Shows an animated scent trail from Toby to the chapter objective when Q is pressed.</summary>
[DisallowMultipleComponent]
public sealed class TobyScentGuide : MonoBehaviour
{
    [SerializeField] private ForestBallChase target;
    [SerializeField] private KeyCode guideKey = KeyCode.Q;
    [SerializeField, Min(.5f)] private float visibleDuration = 4.5f;
    [SerializeField, Range(6, 24)] private int puffCount = 14;
    [SerializeField, Min(.1f)] private float scentHeight = .65f;
    [SerializeField, Min(.05f)] private float flowSpeed = .28f;
    [SerializeField] private Color scentColor = new Color(.62f, 1f, .36f, .9f);

    private Transform waveRoot;
    private LineRenderer[] waves;
    private Material scentMaterial;
    private float visibleUntil;

private void Awake()
    {
        EnsureInitialized();
    }

private void EnsureInitialized()
    {
        if (target == null)
            target = FindFirstObjectByType<ForestBallChase>();

        if (waveRoot != null)
            return;

        CreateVisuals();
        SetVisible(false);
    }


private void Update()
    {
        EnsureInitialized();

        if (Time.timeScale > 0f
            && target != null
            && !target.IsCompleted
            && Input.GetKeyDown(guideKey))
        {
            visibleUntil = Time.unscaledTime + visibleDuration;
        }

        bool visible = target != null
            && !target.IsCompleted
            && Time.unscaledTime < visibleUntil;

        SetVisible(visible);
        if (visible)
            UpdateWaves();
    }

private void CreateVisuals()
    {
        var rootObject = new GameObject("Ondas de Cheiro");
        waveRoot = rootObject.transform;
        waveRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rootObject, gameObject.scene);

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Sprites/Default");
        scentMaterial = new Material(shader)
        {
            name = "Ondas de Cheiro (Runtime)",
            color = Color.white
        };

        if (scentMaterial.HasProperty("_BaseColor"))
            scentMaterial.SetColor("_BaseColor", Color.white);
        if (scentMaterial.HasProperty("_Surface"))
            scentMaterial.SetFloat("_Surface", 1f);
        if (scentMaterial.HasProperty("_SrcBlend"))
            scentMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (scentMaterial.HasProperty("_DstBlend"))
            scentMaterial.SetFloat("_DstBlend", (float)BlendMode.One);
        if (scentMaterial.HasProperty("_ZWrite"))
            scentMaterial.SetFloat("_ZWrite", 0f);
        scentMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        scentMaterial.renderQueue = (int)RenderQueue.Transparent;

        waves = new LineRenderer[puffCount];
        const int segments = 32;
        for (int i = 0; i < waves.Length; i++)
        {
            var waveObject = new GameObject("Onda de Cheiro");
            waveObject.transform.SetParent(waveRoot, false);

            var line = waveObject.AddComponent<LineRenderer>();
            line.sharedMaterial = scentMaterial;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = segments;
            line.widthMultiplier = .11f;
            line.numCornerVertices = 3;
            line.numCapVertices = 3;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.sortingOrder = 20;

            for (int point = 0; point < segments; point++)
            {
                float angle = point / (float)segments * Mathf.PI * 2f;
                line.SetPosition(point, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            }

            waves[i] = line;
        }
    }

private void UpdateWaves()
    {
        Vector3 start = transform.position + Vector3.up * .2f;
        Vector3 end = target.transform.position + Vector3.up * .35f;
        Vector3 flatDirection = Vector3.ProjectOnPlane(end - start, Vector3.up);
        Vector3 sideways = flatDirection.sqrMagnitude > .001f
            ? Vector3.Cross(Vector3.up, flatDirection.normalized)
            : Vector3.right;

        float clock = Time.unscaledTime * flowSpeed;
        for (int i = 0; i < waves.Length; i++)
        {
            float spacing = i / (float)waves.Length;
            float t = Mathf.Repeat(clock + spacing, 1f);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float envelope = Mathf.Sin(t * Mathf.PI);
            float sway = Mathf.Sin(Time.unscaledTime * 2.1f + i * 1.35f) * .22f * envelope;

            Vector3 position = Vector3.Lerp(start, end, smoothT);
            position += sideways * sway;
            position += Vector3.up * (scentHeight + envelope * .18f);

            float radius = Mathf.Lerp(.3f, .9f, t)
                * (1f + Mathf.Sin(Time.unscaledTime * 3.2f + i) * .08f);
            var wave = waves[i];
            wave.transform.SetPositionAndRotation(position, Quaternion.identity);
            wave.transform.localScale = new Vector3(radius * 1.25f, 1f, radius);
            wave.widthMultiplier = Mathf.Lerp(.06f, .14f, envelope);

            Color color = scentColor;
            color.a *= envelope;
            wave.startColor = color;
            wave.endColor = new Color(color.r * .72f, color.g, 1f, color.a * .7f);
        }
    }

private void SetVisible(bool visible)
    {
        if (waveRoot != null && waveRoot.gameObject.activeSelf != visible)
            waveRoot.gameObject.SetActive(visible);
    }

private void OnDestroy()
    {
        if (waveRoot != null)
            Destroy(waveRoot.gameObject);
        if (scentMaterial != null)
            Destroy(scentMaterial);
    }
}
