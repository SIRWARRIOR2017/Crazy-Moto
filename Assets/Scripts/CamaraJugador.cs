using UnityEngine;

// Cámara del jugador en primera persona. En primera persona la posición va
// **pegada** al punto de la moto (`objetivo` = PuntoCamara, hijo del "Jugador"):
// si la cámara persigue con retardo, todo lo cercano (manubrio, rueda) se
// desliza de lado en la pantalla y marea. Lo "cómodo" va en la ROTACIÓN, que sí
// se suaviza.
//
// Efectos de rotación:
//   - roll: se tumba un poco hacia el lado al que te movés (según la entrada
//     lateral suavizada, no la velocidad, así no tiembla).
//   - wheelie: mientras `PlayerController.haciendoWheelie` la vista sube hacia el
//     cielo, así cuesta ver los autos de cerca.
//
// La Main Camera dejó de ser hija de `PivotCamara`: la maneja este script.
public class CamaraJugador : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform objetivo;                  // PuntoCamara (hijo del Jugador)

    [Header("Postura base")]
    public float pitchBase = 3f;                // grados que mira hacia abajo (a la calle) en reposo
    public float suavizadoRotacion = 12f;       // 1/seg

    [Header("Inclinación en giros (roll)")]
    public float gradosRoll = 5f;               // cuánto se tumba a fondo
    public float suavizadoRoll = 0.15f;         // SmoothDamp (seg) de la entrada lateral

    [Header("Wheelie")]
    public float gradosPitchWheelie = 18f;      // cuánto sube la vista al cielo
    public float velocidadPitch = 80f;          // grados/seg

    private PlayerController jugador;
    private float pitchWheelieActual;
    private float rollEntrada, velRoll;

    void Start()
    {
        if (objetivo == null)
        {
            GameObject p = GameObject.Find("PuntoCamara");
            if (p != null) objetivo = p.transform;
        }
        if (objetivo == null)
        {
            Debug.LogError("CamaraJugador: falta asignar 'objetivo' (PuntoCamara). Me desactivo.");
            enabled = false;
            return;
        }

        jugador = FindAnyObjectByType<PlayerController>();
        transform.position = objetivo.position;
        transform.rotation = objetivo.rotation * Quaternion.Euler(pitchBase, 0f, 0f);
    }

    void LateUpdate()
    {
        // Posición: pegada, sin retardo.
        transform.position = objetivo.position;

        // Wheelie: la vista sube hacia el cielo.
        bool wheelie = jugador != null && jugador.haciendoWheelie;
        float objetivoPitch = wheelie ? gradosPitchWheelie : 0f;
        pitchWheelieActual = Mathf.MoveTowards(pitchWheelieActual, objetivoPitch, velocidadPitch * Time.deltaTime);

        // Roll: desde la entrada lateral suavizada (−1..1), no desde la velocidad.
        float entrada = jugador != null ? jugador.EntradaLateral : 0f;
        rollEntrada = Mathf.SmoothDamp(rollEntrada, entrada, ref velRoll, suavizadoRoll);
        float roll = -rollEntrada * gradosRoll;

        Quaternion rotObjetivo = objetivo.rotation * Quaternion.Euler(pitchBase - pitchWheelieActual, 0f, roll);
        float t = 1f - Mathf.Exp(-suavizadoRotacion * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotObjetivo, t);
    }
}
