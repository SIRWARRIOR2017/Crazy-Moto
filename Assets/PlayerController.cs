using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento lateral")]
    public float velocidadLateral = 8f;
    public float limiteLateral = 4f;

    [Header("Wheelie")]
    public Transform cuerpo;          // la moto (se inclina fuerte)
    public Transform pivotCamara;     // la cámara (se inclina poco)
    public float anguloWheelieMoto = 30f;
    public float anguloWheelieCamara = 10f;   // mucho menos que la moto
    public float velocidadInclinacion = 120f;

    public bool haciendoWheelie { get; private set; }
    private float anguloMoto = 0f;
    private float anguloCamara = 0f;

    void Update()
    {
        if (GameManager.Instance.juegoTerminado) return;

        Avanzar();
        MoverLateral();
        Wheelie();
    }

    void Avanzar()
    {
        transform.Translate(Vector3.forward * GameManager.Instance.velocidadActual * Time.deltaTime, Space.World);
    }

    void MoverLateral()
    {
        float input = Input.GetAxis("Horizontal");
        float nuevaX = transform.position.x + input * velocidadLateral * Time.deltaTime;
        nuevaX = Mathf.Clamp(nuevaX, -limiteLateral, limiteLateral);
        transform.position = new Vector3(nuevaX, transform.position.y, transform.position.z);
    }

    void Wheelie()
    {
        haciendoWheelie = Input.GetKey(KeyCode.Space);

        // La moto se inclina fuerte
        float objetivoMoto = haciendoWheelie ? anguloWheelieMoto : 0f;
        anguloMoto = Mathf.MoveTowards(anguloMoto, objetivoMoto, velocidadInclinacion * Time.deltaTime);
        cuerpo.localRotation = Quaternion.Euler(-anguloMoto, 0f, 0f);

        // La cámara se inclina poco (así no perdés la calle)
        float objetivoCam = haciendoWheelie ? anguloWheelieCamara : 0f;
        anguloCamara = Mathf.MoveTowards(anguloCamara, objetivoCam, velocidadInclinacion * Time.deltaTime);
        pivotCamara.localRotation = Quaternion.Euler(-anguloCamara, 0f, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstaculo"))
            GameManager.Instance.GameOver();
    }
}