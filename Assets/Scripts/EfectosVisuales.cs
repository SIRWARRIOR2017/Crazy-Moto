using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Efectos de pantalla de la partida. Se crea en runtime desde GameManager.Start().
//   - Flash blanco cálido al chocar (imagen de pantalla completa en el Canvas).
//   - Post-proceso que reacciona al juego, con un Volume global propio (prioridad
//     mayor que el de la escena): viñeta que sube con la velocidad y más con el
//     wheelie, aberración cromática y una distorsión de lente muy leve que crecen
//     con la velocidad, y un pico de aberración al chocar.
// Todo se suaviza con tiempo SIN escala: en el menú, la pausa y el Game Over
// `Time.timeScale` es 0 y los valores se quedan quietos en vez de saltar.
public class EfectosVisuales : MonoBehaviour
{
    [Header("Flash al chocar")]
    public Color colorFlash = new Color(1f, 0.93f, 0.8f, 1f);
    [Range(0f, 1f)] public float alfaFlash = 0.6f;
    public float duracionFlash = 0.35f;         // seg (tiempo sin escala)

    [Header("Volume")]
    public int prioridadVolume = 10;            // mayor que el Global Volume de la escena
    public float suavizado = 4f;                // 1/seg: qué tan rápido siguen los valores

    [Header("Viñeta")]
    public float vinetaBase = 0.18f;
    public float vinetaPorVelocidad = 0.12f;    // se suma a velocidad máxima
    public float vinetaPorWheelie = 0.2f;       // se suma mientras se sostiene el wheelie
    public float vinetaGameOver = 0.15f;        // se suma al chocar
    public float suavidadVineta = 0.45f;

    [Header("Aberración cromática")]
    public float aberracionPorVelocidad = 0.25f;   // a velocidad máxima (casi 0 al principio)
    public float aberracionChoque = 0.7f;          // pico al chocar, decae con el flash

    [Header("Distorsión de lente")]
    public float distorsionPorVelocidad = 0.12f;   // a velocidad máxima; 0 la apaga

    private Image flash;
    private float tiempoFlash = -1f;            // < 0 = sin flash en curso

    private Volume volume;
    private VolumeProfile perfil;
    private Vignette vineta;
    private ChromaticAberration aberracion;
    private LensDistortion distorsion;

    private PlayerController jugador;
    private bool juegoTerminadoPrevio;
    private float velocidad01, wheelie01, choque01, gameOver01;

    void Start()
    {
        jugador = FindAnyObjectByType<PlayerController>();
        ConstruirFlash();
        ConstruirVolume();
        ActivarPostProceso();
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        float dt = Time.unscaledDeltaTime;

        // El choque se detecta mirando cuándo cambia `juegoTerminado`.
        if (gm.juegoTerminado && !juegoTerminadoPrevio)
        {
            tiempoFlash = 0f;
            choque01 = 1f;
        }
        juegoTerminadoPrevio = gm.juegoTerminado;

        ActualizarFlash(dt);
        ActualizarPostProceso(gm, dt);
    }

    void OnDestroy()
    {
        if (perfil != null) Destroy(perfil);
    }

    // ---- Flash ----

    void ConstruirFlash()
    {
        MenuManager menu = FindAnyObjectByType<MenuManager>();
        Canvas canvas = null;
        if (menu != null && menu.panelGameOver != null)
            canvas = menu.panelGameOver.GetComponentInParent<Canvas>(true);

        if (canvas == null)
        {
            Debug.LogWarning("EfectosVisuales: no encontré el Canvas. No habrá flash de choque.");
            return;
        }

        GameObject go = new GameObject("FlashChoque", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(canvas.rootCanvas.transform, false);
        EstiloUI.Estirar(rt);
        rt.SetAsLastSibling();

        flash = go.GetComponent<Image>();
        flash.sprite = null;                    // sin sprite = rectángulo liso, que es lo que queremos
        flash.color = new Color(colorFlash.r, colorFlash.g, colorFlash.b, 0f);
        flash.raycastTarget = false;            // no bloquea los botones del Game Over
        flash.enabled = false;
    }

    void ActualizarFlash(float dt)
    {
        if (flash == null || tiempoFlash < 0f) return;

        tiempoFlash += dt;
        float resto = 1f - tiempoFlash / Mathf.Max(0.01f, duracionFlash);
        if (resto <= 0f)
        {
            tiempoFlash = -1f;
            flash.enabled = false;
            return;
        }

        flash.enabled = true;
        flash.color = new Color(colorFlash.r, colorFlash.g, colorFlash.b, alfaFlash * resto);
    }

    // ---- Post-proceso ----

    void ConstruirVolume()
    {
        perfil = ScriptableObject.CreateInstance<VolumeProfile>();

        vineta = perfil.Add<Vignette>(true);
        vineta.active = true;
        vineta.intensity.Override(vinetaBase);
        vineta.smoothness.Override(Mathf.Clamp01(suavidadVineta));
        vineta.color.Override(EstiloUI.FondoOpaco);

        aberracion = perfil.Add<ChromaticAberration>(true);
        aberracion.active = true;
        aberracion.intensity.Override(0f);

        distorsion = perfil.Add<LensDistortion>(true);
        distorsion.active = true;
        distorsion.intensity.Override(0f);

        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = prioridadVolume;
        volume.weight = 1f;
        volume.sharedProfile = perfil;
    }

    // Si la cámara no tiene el post-proceso prendido, los efectos no se verían.
    void ActivarPostProceso()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        UniversalAdditionalCameraData datos = cam.GetUniversalAdditionalCameraData();
        if (datos != null && !datos.renderPostProcessing)
        {
            datos.renderPostProcessing = true;
            Debug.Log("EfectosVisuales: la Main Camera no tenía el post-proceso activado; lo prendí.");
        }
    }

    void ActualizarPostProceso(GameManager gm, float dt)
    {
        if (vineta == null) return;

        float k = 1f - Mathf.Exp(-suavizado * dt);

        float objVelocidad = Mathf.InverseLerp(gm.velocidadInicial, gm.velocidadMaxima, gm.velocidadActual);
        bool conWheelie = !gm.juegoTerminado && jugador != null && jugador.haciendoWheelie;
        velocidad01 = Mathf.Lerp(velocidad01, objVelocidad, k);
        wheelie01 = Mathf.Lerp(wheelie01, conWheelie ? 1f : 0f, k);

        // El pico de aberración del choque decae junto con el flash.
        choque01 = Mathf.MoveTowards(choque01, 0f, dt / Mathf.Max(0.01f, duracionFlash));
        gameOver01 = Mathf.Lerp(gameOver01, gm.juegoTerminado ? 1f : 0f, k);

        vineta.intensity.value = Mathf.Clamp01(vinetaBase + vinetaPorVelocidad * velocidad01
                                               + vinetaPorWheelie * wheelie01 + vinetaGameOver * gameOver01);
        aberracion.intensity.value = Mathf.Clamp01(Mathf.Max(aberracionPorVelocidad * velocidad01,
                                                             aberracionChoque * choque01));
        distorsion.intensity.value = Mathf.Clamp(distorsionPorVelocidad * velocidad01, -1f, 1f);
    }
}
