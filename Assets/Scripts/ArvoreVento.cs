using System.Collections.Generic;
using UnityEngine;

/// <summary>Deforma a copa com vento e toque, mantendo tronco, raiz e colisoes fixos.</summary>
public class ArvoreVento : MonoBehaviour
{
    [Min(0f)] public float velocidadeVento = 0.8f;
    [Range(0f, 5f)] public float intensidadeVento = 1.2f;
    [Tooltip("Submesh do tronco, cujos vertices permanecem fixos. -1 usa somente a altura.")]
    public int submeshTronco = 0;
    [Range(0f, 0.8f)] public float alturaFixa = 0.25f;
    [Min(0f)] public float forcaToque = 0.7f;
    [Min(0.1f)] public float amortecimento = 3f;

    private sealed class Copa
    {
        public MeshFilter filter;
        public Renderer renderer;
        public Mesh original;
        public Mesh mesh;
        public Vector3[] rest;
        public Vector3[] vertices;
        public float[] weights;
        public float worldHeight;
    }

    private readonly List<Copa> copas = new List<Copa>();
    private float fase;
    private float impacto;
    private bool started;

    private void Start()
    {
        started = true;
        Initialize();
    }

    private void OnEnable()
    {
        if (started) Initialize();
    }

    private void Initialize()
    {
        if (copas.Count > 0) return;
        fase = Mathf.Repeat(transform.position.x * 1.37f + transform.position.z * 0.73f, 100f);
        foreach (var filter in GetComponentsInChildren<MeshFilter>())
        {
            // Cada malha pertence ao controlador mais proximo na hierarquia.
            if (filter.GetComponentInParent<ArvoreVento>() != this) continue;
            var original = filter.sharedMesh;
            var renderer = filter.GetComponent<Renderer>();
            if (original == null || renderer == null) continue;
            if (!original.isReadable)
            {
                Debug.LogWarning("ArvoreVento: habilite Read/Write no modelo " + original.name, this);
                continue;
            }

            var rest = original.vertices;
            if (rest.Length == 0) continue;
            float bottom = float.PositiveInfinity;
            float top = float.NegativeInfinity;
            foreach (var vertex in rest)
            {
                float y = filter.transform.TransformPoint(vertex).y;
                bottom = Mathf.Min(bottom, y);
                top = Mathf.Max(top, y);
            }
            float height = Mathf.Max(0.01f, top - bottom);
            var fixedVertices = new bool[rest.Length];
            if (submeshTronco >= 0 && submeshTronco < original.subMeshCount)
                foreach (int index in original.GetTriangles(submeshTronco))
                    fixedVertices[index] = true;

            var weights = new float[rest.Length];
            for (int i = 0; i < rest.Length; i++)
            {
                float normalized = (filter.transform.TransformPoint(rest[i]).y - bottom) / height;
                float weight = Mathf.InverseLerp(alturaFixa, 1f, normalized);
                weights[i] = fixedVertices[i] ? 0f : weight * weight;
            }

            var mesh = Instantiate(original);
            mesh.name = original.name + " - Copa com Vento";
            mesh.MarkDynamic();
            var bounds = original.bounds;
            float minScale = Mathf.Max(0.001f, Mathf.Min(
                Mathf.Abs(filter.transform.lossyScale.x),
                Mathf.Abs(filter.transform.lossyScale.y),
                Mathf.Abs(filter.transform.lossyScale.z)));
            bounds.Expand(height * 0.4f / minScale);
            mesh.bounds = bounds;
            filter.sharedMesh = mesh;
            copas.Add(new Copa
            {
                filter = filter, renderer = renderer, original = original, mesh = mesh,
                rest = rest, vertices = new Vector3[rest.Length],
                weights = weights, worldHeight = height
            });
        }
    }

    private void Update()
    {
        impacto = Mathf.MoveTowards(impacto, 0f, amortecimento * Time.deltaTime);
        ApplyWind(Time.time);
    }

    private void ApplyWind(float time)
    {
        float t = time * velocidadeVento + fase;
        float gust = Mathf.Sin(time * 17f + fase) * impacto;
        foreach (var copa in copas)
        {
            if (copa.renderer == null || !copa.renderer.isVisible) continue;
            float amplitude = copa.worldHeight * Mathf.Tan(intensidadeVento * Mathf.Deg2Rad);
            float touch = copa.worldHeight * Mathf.Tan(gust * Mathf.Deg2Rad);
            var displacement = new Vector3(
                Mathf.Sin(t) * amplitude + touch,
                0f,
                Mathf.Cos(t * 0.85f) * amplitude * 0.7f + touch * 0.5f);
            Vector3 local = copa.filter.transform.InverseTransformVector(displacement);
            for (int i = 0; i < copa.rest.Length; i++)
                copa.vertices[i] = copa.rest[i] + local * copa.weights[i];
            copa.mesh.vertices = copa.vertices;
            copa.mesh.RecalculateNormals();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.GetComponentInParent<PlayerControle>() != null)
            impacto = forcaToque;
    }

    private void OnDisable()
    {
        ReleaseMeshes();
    }

    private void OnDestroy()
    {
        ReleaseMeshes();
    }

    private void ReleaseMeshes()
    {
        foreach (var copa in copas)
        {
            if (copa.filter != null && copa.filter.sharedMesh == copa.mesh)
                copa.filter.sharedMesh = copa.original;
            if (copa.mesh != null) Destroy(copa.mesh);
        }
        copas.Clear();
        impacto = 0f;
    }
}
