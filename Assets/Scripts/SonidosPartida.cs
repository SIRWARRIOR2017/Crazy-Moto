using System.Collections.Generic;
using UnityEngine;

// Sonidos de la partida, generados por código (sin archivos de audio): el motor
// de la moto en loop y un "whoosh" cada vez que un auto pasa al lado. Se crea en
// runtime desde GameManager, igual que el HUD.
//
// Ojo: esto existe también en el menú (la escena es la misma, con timeScale 0),
// por eso el motor se silencia cuando el juego está frenado o terminó. Todo el
// fade usa tiempo sin escalar, porque con timeScale 0 el tiempo normal no avanza.
public class SonidosPartida : MonoBehaviour
{
    [Header("Motor")]
    public float volumenMotor = 0.5f;           // se multiplica por el volumen de "Efectos" de Opciones
    public float frecuenciaBase = 55f;          // Hz de las "explosiones" del motor
    public float pitchMinimo = 0.8f;            // a velocidad inicial
    public float pitchMaximo = 1.9f;            // a velocidad máxima
    public float pitchExtraWheelie = 0.25f;     // el motor "sube de vueltas" en el wheelie
    public float suavizadoPitch = 4f;           // más alto = sigue más rápido al objetivo
    public float fadeEntrada = 0.3f;            // segundos para llegar al volumen completo
    public float fadeSalida = 0.12f;            // segundos para callarse (menú, pausa, choque)

    [Header("Autos pasando (whoosh)")]
    public float volumenWhoosh = 0.8f;          // se multiplica por el volumen de "Efectos"
    public float duracionWhoosh = 0.5f;         // segundos del primer clip (el segundo es más largo)
    public float distanciaXLejana = 7f;         // a esta distancia lateral el volumen ya es el mínimo
    [Range(0f, 1f)]
    public float volumenLejano = 0.3f;          // fracción del volumen cuando pasa lejos
    public float distanciaXPan = 4f;            // a esta distancia lateral el sonido sale todo de un lado
    [Range(0f, 1f)]
    public float panMaximo = 0.85f;
    public float pitchWhooshMin = 0.88f;
    public float pitchWhooshMax = 1.12f;
    public float saltoMaximoCruce = 60f;        // un salto mayor es un auto reciclado, no un cruce

    private const int FrecuenciaMuestreo = 44100;
    private const int FuentesWhoosh = 4;

    private AudioSource fuenteMotor;
    private AudioSource[] fuentesWhoosh;
    private int siguienteFuenteWhoosh;
    private AudioClip[] clipsWhoosh;

    private PlayerController jugador;
    private Transform trafico;
    private readonly Dictionary<Transform, float> dzAnterior = new Dictionary<Transform, float>();

    private float pitchActual;

    void Start()
    {
        fuenteMotor = gameObject.AddComponent<AudioSource>();
        fuenteMotor.clip = CrearMotor();
        fuenteMotor.loop = true;
        fuenteMotor.playOnAwake = false;
        fuenteMotor.spatialBlend = 0f;
        fuenteMotor.volume = 0f;
        pitchActual = pitchMinimo;
        fuenteMotor.pitch = pitchActual;
        fuenteMotor.Play();

        clipsWhoosh = new AudioClip[]
        {
            CrearWhoosh("Whoosh1", duracionWhoosh, 0.7f, 0.04f, 0.30f, 11),
            CrearWhoosh("Whoosh2", duracionWhoosh * 1.4f, 0.6f, 0.03f, 0.20f, 23)
        };

        // Cada whoosh necesita su propio pan y pitch; como eso es de la fuente y
        // no del clip, hay unas pocas fuentes que se turnan.
        fuentesWhoosh = new AudioSource[FuentesWhoosh];
        for (int i = 0; i < FuentesWhoosh; i++)
        {
            AudioSource f = gameObject.AddComponent<AudioSource>();
            f.playOnAwake = false;
            f.loop = false;
            f.spatialBlend = 0f;
            fuentesWhoosh[i] = f;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        bool enPartida = Time.timeScale > 0f && !GameManager.Instance.juegoTerminado;

        ActualizarMotor(enPartida);

        if (enPartida)
            DetectarCruces();
        else
            dzAnterior.Clear();
    }

    // ---- Motor ----

    void ActualizarMotor(bool enPartida)
    {
        if (jugador == null)
            jugador = FindAnyObjectByType<PlayerController>();

        GameManager gm = GameManager.Instance;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);

        float t = Mathf.InverseLerp(gm.velocidadInicial, gm.velocidadMaxima, gm.velocidadActual);
        float pitchObjetivo = Mathf.Lerp(pitchMinimo, pitchMaximo, t);
        if (jugador != null && jugador.haciendoWheelie)
            pitchObjetivo += pitchExtraWheelie;

        pitchActual = Mathf.Lerp(pitchActual, pitchObjetivo, 1f - Mathf.Exp(-suavizadoPitch * dt));
        fuenteMotor.pitch = pitchActual;

        float objetivo = enPartida ? volumenMotor * VolumenEfectos() : 0f;
        float fade = enPartida ? fadeEntrada : fadeSalida;
        float paso = fade > 0.0001f ? Mathf.Abs(volumenMotor) * dt / fade : 1f;
        fuenteMotor.volume = Mathf.MoveTowards(fuenteMotor.volume, objetivo, Mathf.Max(paso, 0.0001f));
    }

    float VolumenEfectos()
    {
        AudioManager am = AudioManager.Instance;
        if (am != null && am.efectos != null)
            return am.efectos.volume;
        return 1f;
    }

    // ---- Autos pasando ----

    void DetectarCruces()
    {
        if (trafico == null)
        {
            TrafficManager tm = FindAnyObjectByType<TrafficManager>();
            if (tm != null) trafico = tm.transform;
        }
        if (trafico == null || jugador == null) return;

        Vector3 posJugador = jugador.transform.position;

        for (int i = 0; i < trafico.childCount; i++)
        {
            Transform auto = trafico.GetChild(i);

            // Un auto apagado vuelve al pool: se olvida para que, cuando reaparezca
            // adelante, no se lo compare con su posición vieja.
            if (!auto.gameObject.activeSelf)
            {
                dzAnterior.Remove(auto);
                continue;
            }

            float dz = auto.position.z - posJugador.z;

            if (dzAnterior.TryGetValue(auto, out float previo)
                && previo > 0f && dz <= 0f && previo - dz < saltoMaximoCruce)
            {
                ReproducirWhoosh(auto.position.x - posJugador.x);
            }

            dzAnterior[auto] = dz;
        }
    }

    void ReproducirWhoosh(float dx)
    {
        float distancia = Mathf.Abs(dx);
        float cercania = 1f - Mathf.Clamp01(distancia / Mathf.Max(distanciaXLejana, 0.01f));
        float volumen = volumenWhoosh * VolumenEfectos() * Mathf.Lerp(volumenLejano, 1f, cercania);

        AudioSource f = fuentesWhoosh[siguienteFuenteWhoosh];
        siguienteFuenteWhoosh = (siguienteFuenteWhoosh + 1) % fuentesWhoosh.Length;

        f.Stop();
        f.clip = clipsWhoosh[Random.Range(0, clipsWhoosh.Length)];
        f.volume = Mathf.Clamp01(volumen);
        f.pitch = Random.Range(pitchWhooshMin, pitchWhooshMax);
        f.panStereo = Mathf.Clamp(dx / Mathf.Max(distanciaXPan, 0.01f), -1f, 1f) * panMaximo;
        f.Play();
    }

    // ---- Síntesis ----

    // Motor de 1 cilindro: diente de sierra (armónicos) + un poco de ruido,
    // con un pulso de amplitud por cada "explosión". El clip dura 1 s y contiene
    // un número ENTERO de ciclos de la frecuencia base, así que el final empalma
    // con el principio sin clic.
    AudioClip CrearMotor()
    {
        const float duracion = 1f;
        const int armonicos = 20;

        int n = Mathf.RoundToInt(FrecuenciaMuestreo * duracion);
        int ciclos = Mathf.Max(1, Mathf.RoundToInt(frecuenciaBase * duracion));

        float[] pesos = new float[armonicos];
        for (int h = 1; h <= armonicos; h++)
            pesos[h - 1] = Mathf.Pow(h, -0.8f);

        System.Random azar = new System.Random(1234);

        // Cada explosión sale un poco distinta para que no suene a máquina.
        float[] amplitudCiclo = new float[ciclos];
        for (int c = 0; c < ciclos; c++)
            amplitudCiclo[c] = 0.8f + 0.2f * (float)azar.NextDouble();

        float[] ruido = new float[n];
        for (int i = 0; i < n; i++)
            ruido[i] = (float)(azar.NextDouble() * 2.0 - 1.0);
        ruido = FiltrarCircular(ruido, 0.25f);

        float[] datos = new float[n];
        float maximo = 0f;
        for (int i = 0; i < n; i++)
        {
            double fase = (double)ciclos * i / n;
            int c = Mathf.Min((int)fase, ciclos - 1);
            float frac = (float)(fase - c);

            float sierra = 0f;
            for (int h = 1; h <= armonicos; h++)
                sierra += pesos[h - 1] * Mathf.Sin(2f * Mathf.PI * h * frac);

            float pulso = 0.3f + 0.7f * Mathf.Exp(-6f * frac);
            float muestra = (sierra * 0.5f + ruido[i] * 0.25f) * pulso * amplitudCiclo[c];

            datos[i] = muestra;
            maximo = Mathf.Max(maximo, Mathf.Abs(muestra));
        }

        Normalizar(datos, maximo, 0.9f);

        AudioClip clip = AudioClip.Create("MotorMoto", n, 1, FrecuenciaMuestreo, false);
        clip.SetData(datos, 0);
        return clip;
    }

    // Pasa-bajos de un polo aplicado dos veces dando la vuelta al array, para que
    // el último valor empalme con el primero (el ruido también loopea sin clic).
    float[] FiltrarCircular(float[] entrada, float alfa)
    {
        float[] salida = new float[entrada.Length];
        float y = 0f;
        for (int pasada = 0; pasada < 2; pasada++)
        {
            for (int i = 0; i < entrada.Length; i++)
            {
                y += alfa * (entrada[i] - y);
                if (pasada == 1) salida[i] = y;
            }
        }
        return salida;
    }

    // Ruido blanco con pasa-bajos que se abre cuando sube el volumen (el auto
    // "se acerca y se aleja") y envolvente que sube y baja, siempre a 0 en las
    // puntas. `pico` < 1 corre el máximo hacia el principio.
    AudioClip CrearWhoosh(string nombre, float duracion, float pico, float brilloMin, float brilloMax, int semilla)
    {
        int n = Mathf.Max(64, Mathf.RoundToInt(FrecuenciaMuestreo * duracion));
        System.Random azar = new System.Random(semilla);

        float[] datos = new float[n];
        float maximo = 0f;
        float y1 = 0f, y2 = 0f;

        for (int i = 0; i < n; i++)
        {
            float u = (float)i / (n - 1);
            float s = Mathf.Sin(Mathf.PI * Mathf.Pow(u, pico));
            float envolvente = s * s;

            float alfa = Mathf.Lerp(brilloMin, brilloMax, envolvente);
            float blanco = (float)(azar.NextDouble() * 2.0 - 1.0);
            y1 += alfa * (blanco - y1);
            y2 += alfa * (y1 - y2);

            float muestra = y2 * envolvente;
            datos[i] = muestra;
            maximo = Mathf.Max(maximo, Mathf.Abs(muestra));
        }

        Normalizar(datos, maximo, 0.9f);

        AudioClip clip = AudioClip.Create(nombre, n, 1, FrecuenciaMuestreo, false);
        clip.SetData(datos, 0);
        return clip;
    }

    void Normalizar(float[] datos, float maximo, float objetivo)
    {
        if (maximo < 0.000001f) return;

        float k = objetivo / maximo;
        for (int i = 0; i < datos.Length; i++)
            datos[i] *= k;
    }
}
