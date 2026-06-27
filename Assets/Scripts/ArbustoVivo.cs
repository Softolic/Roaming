using UnityEngine;

/// <summary>
/// Faz um arbusto (ou qualquer planta) balancar suavemente sozinho (vento)
/// e vibrar quando o player passa por ele. Animacao procedural - sem Rigidbody.
/// IMPORTANTE: o pivo do objeto deve estar na BASE pra ele inclinar como planta.
/// O Collider deve estar como "Is Trigger" pra detectar o player passando.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ArbustoVivo : MonoBehaviour
{
    [Header("Balanco do vento (idle)")]
    [Tooltip("Velocidade do balanco natural.")]
    public float velocidadeVento = 1.5f;

    [Tooltip("Intensidade do balanco em graus.")]
    public float intensidadeVento = 3f;

    [Header("Reacao ao player")]
    [Tooltip("Forca da vibracao quando o player encosta (graus).")]
    public float forcaImpacto = 18f;

    [Tooltip("Quao rapido a vibracao se acalma. Maior = acalma mais rapido.")]
    public float amortecimento = 5f;

    [Tooltip("Velocidade do tremor da vibracao.")]
    public float frequenciaTremor = 28f;

    [Tooltip("Tag do objeto que dispara a vibracao.")]
    public string tagPlayer = "Player";

    private Quaternion _rotacaoBase;
    private float _offsetAleatorio;
    private float _tremorAtual = 0f;
    private Vector3 _eixoTremor = Vector3.right;

    void Start()
    {
        _rotacaoBase = transform.localRotation;
        // cada arbusto balanca em fase diferente, fica natural
        _offsetAleatorio = Random.value * 100f;
    }

    void Update()
    {
        // 1) balanco suave do vento (seno)
        float t = Time.time * velocidadeVento + _offsetAleatorio;
        float ventoX = Mathf.Sin(t) * intensidadeVento;
        float ventoZ = Mathf.Cos(t * 0.8f) * intensidadeVento * 0.6f;
        Quaternion vento = Quaternion.Euler(ventoX, 0f, ventoZ);

        // 2) tremor decaindo (vibracao do toque)
        Quaternion tremor = Quaternion.identity;
        if (_tremorAtual > 0.05f)
        {
            float vib = Mathf.Sin(Time.time * frequenciaTremor) * _tremorAtual;
            tremor = Quaternion.AngleAxis(vib, _eixoTremor);
            _tremorAtual = Mathf.Lerp(_tremorAtual, 0f, amortecimento * Time.deltaTime);
        }

        transform.localRotation = _rotacaoBase * vento * tremor;
    }

    void OnTriggerEnter(Collider other)
    {
        // vibra uma vez ao pisar/entrar
        if (other.CompareTag(tagPlayer))
            Vibrar(other.transform.position);
    }

    void OnTriggerExit(Collider other)
    {
        // vibra de novo ao sair de cima
        if (other.CompareTag(tagPlayer))
            Vibrar(other.transform.position);
    }

    void Vibrar(Vector3 origemPlayer)
    {
        // eixo de tremor perpendicular a direcao do player -> parece "empurrado"
        Vector3 dir = transform.position - origemPlayer;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            _eixoTremor = Vector3.Cross(Vector3.up, dir.normalized);
        _tremorAtual = forcaImpacto;
    }
}
