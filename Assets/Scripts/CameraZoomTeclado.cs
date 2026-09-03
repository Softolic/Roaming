using UnityEngine;
using Unity.Cinemachine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public class CameraZoomTeclado : MonoBehaviour
{
    [Header("Zoom: U aproxima / O afasta")]
    [SerializeField, Min(0.1f)] private float tamanhoMinimo = 3f;
    [SerializeField, Min(0.1f)] private float tamanhoMaximo = 14f;
    [SerializeField, Min(0.1f)] private float velocidadeZoom = 5f;

    private CinemachineCamera cameraCinemachine;

    private void Awake()
    {
        cameraCinemachine = GetComponent<CinemachineCamera>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        float direcao = 0f;
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        if (teclado == null)
            return;

        if (teclado.uKey.isPressed) direcao -= 1f;
        if (teclado.oKey.isPressed) direcao += 1f;
#else
        if (Input.GetKey(KeyCode.U)) direcao -= 1f;
        if (Input.GetKey(KeyCode.O)) direcao += 1f;
#endif
        if (direcao == 0f)
            return;

        var lente = cameraCinemachine.Lens;
        lente.OrthographicSize = Mathf.Clamp(
            lente.OrthographicSize + direcao * velocidadeZoom * Time.deltaTime,
            tamanhoMinimo,
            tamanhoMaximo);
        cameraCinemachine.Lens = lente;
    }

    private void OnValidate()
    {
        tamanhoMinimo = Mathf.Max(0.1f, tamanhoMinimo);
        tamanhoMaximo = Mathf.Max(tamanhoMinimo, tamanhoMaximo);
        velocidadeZoom = Mathf.Max(0.1f, velocidadeZoom);
    }
}
