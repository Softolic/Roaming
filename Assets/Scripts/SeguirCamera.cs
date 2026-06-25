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

    void LateUpdate()
    {
        if (alvo == null) return;

        Vector3 posicaoDesejada = alvo.position + deslocamento;

        if (!seguirAltura)
        {
            posicaoDesejada.y = transform.position.y;
        }

        transform.position = Vector3.Lerp(transform.position, posicaoDesejada, velocidadeSuavizacao * Time.deltaTime);
    }
}
