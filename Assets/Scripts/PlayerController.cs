using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento lateral")]
    public float velocidadLateral = 8f;
    public float limiteLateral = 3f;          // 3 carriles: -3 (izq), 0 (medio), +3 (der)

    [Header("Wheelie")]
    public Transform cuerpo;                   // la moto (se inclina fuerte)
    public Transform pivotCamara;              // la cámara (se inclina poco)
    public float anguloWheelieMoto = 30f;
    public float anguloWheelieCamara = 10f;    // mucho menos que la moto
    public float velocidadInclinacion = 120f;

    [Range(0f, 1f)]
    public float factorLateralEnWheelie = 0.3f;   // qué tanto se frena de costado al hacer wheelie (0.3 = 30% de lo normal)

    public bool haciendoWheelie { get; private set; }
    // Entrada lateral cruda (-1..1), para que la anime quien la necesite
    // (ej. el manubrio de la moto). No dispersar llamadas a Input.* por otros
    // scripts: leerla de acá.
    public float EntradaLateral { get; private set; }

    private float anguloMoto = 0f;
    private float anguloCamara = 0f;

    void Start()
    {
        if (cuerpo == null || pivotCamara == null)
            Debug.LogError("PlayerController: falta asignar 'cuerpo' o 'pivotCamara' en el Inspector.");
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.juegoTerminado) return;

        haciendoWheelie = LeerWheelie();

        Avanzar();
        MoverLateral();
        AnimarWheelie();
    }

    void Avanzar()
    {
        transform.Translate(Vector3.forward * GameManager.Instance.velocidadActual * Time.deltaTime, Space.World);
    }

    void MoverLateral()
    {
        float input = LeerLateral();
        EntradaLateral = input;

        // Durante el wheelie la moto responde mucho menos de costado: ese es el
        // riesgo de sostenerlo para sumar puntos.
        float velocidad = haciendoWheelie ? velocidadLateral * factorLateralEnWheelie : velocidadLateral;

        float nuevaX = transform.position.x + input * velocidad * Time.deltaTime;
        nuevaX = Mathf.Clamp(nuevaX, -limiteLateral, limiteLateral);
        transform.position = new Vector3(nuevaX, transform.position.y, transform.position.z);
    }

    void AnimarWheelie()
    {
        // La moto se inclina fuerte
        float objetivoMoto = haciendoWheelie ? anguloWheelieMoto : 0f;
        anguloMoto = Mathf.MoveTowards(anguloMoto, objetivoMoto, velocidadInclinacion * Time.deltaTime);
        cuerpo.localRotation = Quaternion.Euler(-anguloMoto, 0f, 0f);

        // La cámara se inclina poco (así no perdés la calle)
        float objetivoCam = haciendoWheelie ? anguloWheelieCamara : 0f;
        anguloCamara = Mathf.MoveTowards(anguloCamara, objetivoCam, velocidadInclinacion * Time.deltaTime);
        pivotCamara.localRotation = Quaternion.Euler(-anguloCamara, 0f, 0f);
    }

    // ---- Entrada del jugador ----
    // Todo el input crudo se lee SOLO en estos dos métodos. El día que armemos el
    // manubrio con Arduino, se cambian estos dos (leer del puerto serie en vez
    // del teclado) y la lógica de movimiento queda igual.

    float LeerLateral()
    {
        return Input.GetAxisRaw("Horizontal");   // A / D (y flechas)
    }

    bool LeerWheelie()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstaculo"))
            GameManager.Instance.GameOver();
    }
}
