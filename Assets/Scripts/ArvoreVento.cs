using UnityEngine;

/// <summary>
/// Balanco de vento sutil para arvores. Inclina a arvore pela base (espaco de mundo),
/// entao funciona mesmo com rotacoes de base complexas (ex: modelos do Blender com 270 + giro Y).
/// O pivo do objeto deve estar na BASE da arvore.
/// </summary>
public class ArvoreVento : MonoBehaviour
{
    [Tooltip("Velocidade do balanco. Arvores costumam ser lentas.")]
    public float velocidadeVento = 0.8f;

    [Tooltip("Intensidade do balanco em graus. Mantenha sutil (1-2) pra nao virar borracha.")]
    public float intensidadeVento = 1.2f;

    private Quaternion _baseRot;
    private float _offset;

    void Start()
    {
        _baseRot = transform.rotation;          // guarda a rotacao de base (mundo)
        _offset = Random.value * 100f;          // cada arvore balanca em fase diferente
    }

    void Update()
    {
        float t = Time.time * velocidadeVento + _offset;
        float x = Mathf.Sin(t) * intensidadeVento;
        float z = Mathf.Cos(t * 0.85f) * intensidadeVento * 0.7f;

        // pre-multiplica em espaco de mundo: inclina ao redor da base, topo balanca
        transform.rotation = Quaternion.Euler(x, 0f, z) * _baseRot;
    }
}
