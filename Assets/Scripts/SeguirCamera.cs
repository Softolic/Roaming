using UnityEngine;

public class SeguirCamera : MonoBehaviour
{
    [Tooltip("O alvo que a câmera deve seguir (normalmente o player).")]
    public Transform alvo;

    [Tooltip("Deslocamento em relação ao alvo.")]
    public Vector3 deslocamento = Vector3.zero;

    [Tooltip("Velocidade de suavização do movimento da câmera.")]
    public float velocidadeSuavizacao = 8f;

    [Tooltip("Se desativado, a câmera mantém sua altura (Y) original e só segue X/Z.")]
    public bool seguirAltura = false;

    [Header("Limites do Cenário")]
    [Tooltip("Se ativado, a câmera para nas bordas do nível em vez de mostrar o vazio.")]
    public bool usarLimites = false;

    [Tooltip("Posição mínima da câmera (X = horizontal, Y = profundidade Z do mundo).")]
    public Vector2 limiteMin = new Vector2(-50f, -50f);

    [Tooltip("Posição máxima da câmera (X = horizontal, Y = profundidade Z do mundo).")]
    public Vector2 limiteMax = new Vector2(50f, 50f);

    [Header("Anti-Clipping")]
    [Tooltip("Referência à câmera filha. Preencha para ativar anti-clipping.")]
    public Camera cameraFilha;

    [Tooltip("Layers que podem obstruir a câmera. Desmarque a layer do player e da UI.")]
    public LayerMask layersObstaculo = ~0;

    [Tooltip("Raio da esfera de detecção de colisão.")]
    [Range(0.05f, 1f)]
    public float raioColisao = 0.2f;

    [Header("Look Ahead")]
    [Tooltip("Quanto a câmera se antecipa na direção do movimento. 0 = desativado.")]
    [Range(0f, 5f)]
    public float lookAhead = 1.5f;

    [Tooltip("Velocidade de suavização do look ahead.")]
    public float velocidadeLookAhead = 3f;

    private Vector3 _posicaoLocalIdeal;
    private Vector3 _offsetLookAhead;
    private Vector3 _posicaoAnteriorAlvo;

    void Start()
    {
        if (cameraFilha != null)
            _posicaoLocalIdeal = cameraFilha.transform.localPosition;

        if (alvo != null)
            _posicaoAnteriorAlvo = alvo.position;
    }

    void LateUpdate()
    {
        if (alvo == null) return;

        AtualizarLookAhead();

        Vector3 posicaoDesejada = alvo.position + deslocamento + _offsetLookAhead;
        if (!seguirAltura)
            posicaoDesejada.y = transform.position.y;

        if (usarLimites)
        {
            posicaoDesejada.x = Mathf.Clamp(posicaoDesejada.x, limiteMin.x, limiteMax.x);
            posicaoDesejada.z = Mathf.Clamp(posicaoDesejada.z, limiteMin.y, limiteMax.y);
        }

        transform.position = Vector3.Lerp(transform.position, posicaoDesejada, velocidadeSuavizacao * Time.deltaTime);

        if (cameraFilha != null)
            AjustarColisaoCamera();
    }

    void AtualizarLookAhead()
    {
        Vector3 delta = alvo.position - _posicaoAnteriorAlvo;
        _posicaoAnteriorAlvo = alvo.position;

        Vector3 direcao = new Vector3(delta.x, 0f, delta.z).normalized;
        _offsetLookAhead = Vector3.Lerp(_offsetLookAhead, direcao * lookAhead, velocidadeLookAhead * Time.deltaTime);
    }

    void AjustarColisaoCamera()
    {
        Vector3 origem = transform.position;
        Vector3 destino = transform.TransformPoint(_posicaoLocalIdeal);
        Vector3 direcao = (destino - origem).normalized;
        float distanciaIdeal = Vector3.Distance(origem, destino);

        if (Physics.SphereCast(origem, raioColisao, direcao, out RaycastHit hit, distanciaIdeal, layersObstaculo))
        {
            float distanciaSegura = Mathf.Max(0f, hit.distance - raioColisao);
            cameraFilha.transform.position = origem + direcao * distanciaSegura;
        }
        else
        {
            cameraFilha.transform.localPosition = Vector3.Lerp(
                cameraFilha.transform.localPosition,
                _posicaoLocalIdeal,
                velocidadeSuavizacao * Time.deltaTime
            );
        }
    }
}
