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
//   - shake: un sacudón corto cuando termina la partida (choque). Usa tiempo SIN
//     escala porque en el Game Over `Time.timeScale` es 0.
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

    [Header("Shake al chocar")]
    public float duracionShake = 0.45f;         // seg (tiempo sin escala)
    public float fuerzaShakePosicion = 0.12f;   // metros, al principio del sacudón
    public float fuerzaShakeRotacion = 3f;      // grados de pitch/roll, al principio
    public float frecuenciaShake = 28f;         // qué tan rápido tiembla

    private PlayerController jugador;
    private float pitchWheelieActual;
    private float rollEntrada, velRoll;
    private Quaternion rotSuave;                // rotación sin shake: el shake se suma encima
    private bool juegoTerminadoPrevio;
    private bool shakeActivo;
    private float tiempoShake;

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
        rotSuave = objetivo.rotation * Quaternion.Euler(pitchBase, 0f, 0f);
        transform.position = objetivo.position;
        transform.rotation = rotSuave;
    }

    void LateUpdate()
    {
        // El choque se detecta mirando cuándo cambia `juegoTerminado`.
        GameManager gm = GameManager.Instance;
        bool terminado = gm != null && gm.juegoTerminado;
        if (terminado && !juegoTerminadoPrevio)
        {
            shakeActivo = true;
            tiempoShake = 0f;
        }
        juegoTerminadoPrevio = terminado;

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
        rotSuave = Quaternion.Slerp(rotSuave, rotObjetivo, t);

        // Shake: se suma encima de la pose normal (la posición sigue pegada al
        // objetivo, sin retardo; el shake es un desfase puntual).
        Vector3 desfase = Vector3.zero;
        Quaternion rotShake = Quaternion.identity;
        if (shakeActivo)
        {
            tiempoShake += Time.unscaledDeltaTime;
            float resto = 1f - tiempoShake / Mathf.Max(0.01f, duracionShake);
            if (resto <= 0f)
            {
                shakeActivo = false;
            }
            else
            {
                float fuerza = resto * resto;   // decae rápido al principio
                float tr = tiempoShake * frecuenciaShake;
                desfase = new Vector3(Ruido(tr, 0), Ruido(tr, 1), Ruido(tr, 2)) * (fuerzaShakePosicion * fuerza);
                rotShake = Quaternion.Euler(Ruido(tr, 3) * fuerzaShakeRotacion * fuerza, 0f,
                                            Ruido(tr, 4) * fuerzaShakeRotacion * fuerza);
            }
        }

        transform.position = objetivo.position + rotSuave * desfase;
        transform.rotation = rotSuave * rotShake;
    }

    // Ruido Perlin en −1..1; cada 'semilla' da una curva distinta.
    static float Ruido(float t, int semilla)
    {
        return Mathf.PerlinNoise(t, semilla * 10.37f + 0.5f) * 2f - 1f;
    }
}
