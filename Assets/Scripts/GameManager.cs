using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Dificultad")]
    public float velocidadInicial = 10f;
    public float aceleracion = 0.5f;   // m/s por segundo: llega a la máxima en ~100 s
    public float velocidadMaxima = 60f;

    [Header("Puntaje (solo se suma mientras se sostiene el wheelie)")]
    public float puntosPorSegundoEnWheelie = 100f;         // a la velocidad inicial
    public float puntosPorSegundoEnWheelieMaximo = 300f;   // a la velocidad máxima

    public float velocidadActual { get; private set; }
    public bool juegoTerminado { get; private set; }
    public int puntaje { get; private set; }

    // El puntaje entró al Top 10 y el Game Over está pidiendo el nombre (ver Hud):
    // todavía no se guardó en el ranking.
    public bool esperandoNombre { get; private set; }

    private float puntajeExacto;
    private PlayerController jugador;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // La copia del juego que sólo muestra el ranking (ver PantallaRanking) no
        // crea nada de la partida: ni tráfico, ni sonidos, ni el receptor de la
        // cámara, que le robaría el puerto UDP al juego de verdad.
        if (PantallaRanking.ModoRanking) return;

        velocidadActual = velocidadInicial;
        juegoTerminado = false;
        puntaje = 0;
        puntajeExacto = 0f;

        jugador = FindAnyObjectByType<PlayerController>();

        // El tráfico y el HUD se crean en tiempo de ejecución: la escena se
        // recarga entera para reiniciar, así que no hace falta que existan a mano
        // en la jerarquía (misma idea que el resto de los managers).
        if (FindAnyObjectByType<TrafficManager>() == null)
            new GameObject("TrafficManager").AddComponent<TrafficManager>();

        if (FindAnyObjectByType<EntornoManager>() == null)
            new GameObject("EntornoManager").AddComponent<EntornoManager>();

        if (FindAnyObjectByType<Hud>() == null)
            new GameObject("Hud").AddComponent<Hud>();

        if (FindAnyObjectByType<SonidosPartida>() == null)
            new GameObject("SonidosPartida").AddComponent<SonidosPartida>();

        if (FindAnyObjectByType<EfectosVisuales>() == null)
            new GameObject("EfectosVisuales").AddComponent<EfectosVisuales>();

        if (FindAnyObjectByType<EfectosParticulas>() == null)
            new GameObject("EfectosParticulas").AddComponent<EfectosParticulas>();

        // El receptor de la cámara web también se crea acá, pero se crea una sola
        // vez para todo el juego (sobrevive a la recarga de escena) porque es
        // dueño del puerto UDP. Ver EntradaCamara.
        EntradaCamara.CrearSiNoExiste();
    }

    void Update()
    {
        if (juegoTerminado) return;

        velocidadActual += aceleracion * Time.deltaTime;
        velocidadActual = Mathf.Min(velocidadActual, velocidadMaxima);

        // Solo se suma puntaje mientras el jugador sostiene el wheelie, y paga más
        // cuanto más rápido vas: arriesgarse cuando es difícil vale la pena.
        if (jugador != null && jugador.haciendoWheelie)
        {
            puntajeExacto += PuntosPorSegundo() * Time.deltaTime;
            puntaje = Mathf.FloorToInt(puntajeExacto);
        }
    }

    public float PuntosPorSegundo()
    {
        float t = Mathf.InverseLerp(velocidadInicial, velocidadMaxima, velocidadActual);
        return Mathf.Lerp(puntosPorSegundoEnWheelie, puntosPorSegundoEnWheelieMaximo, t);
    }

    public void GameOver()
    {
        if (juegoTerminado) return;

        juegoTerminado = true;
        Time.timeScale = 0f;

        // Guardar el puntaje en el ranking persistente (JSON en disco). Una
        // partida sin ningún punto (chocaste sin hacer wheelie) no entra.
        //
        // Si entra al Top 10, primero se pide el nombre: en la feria juega gente
        // distinta una partida tras otra, y con el nombre que quedó en Opciones el
        // puntaje se guardaba a nombre del visitante anterior (y, como hay una
        // fila por nombre, si era menor que el de él se perdía).
        if (puntaje > 0)
        {
            if (RankingData.Instance.EntraAlTop(puntaje))
                esperandoNombre = true;
            else
                RankingData.Instance.Agregar(RankingData.NombreActual(), puntaje);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.ReproducirChoque();

        MenuManager menu = FindAnyObjectByType<MenuManager>();
        if (menu != null)
            menu.MostrarGameOver();
    }

    // Guarda el puntaje que estaba esperando el nombre. La llama el Hud (botón
    // Guardar o Enter) y también al salir del Game Over sin guardar, para que el
    // puntaje no se pierda: queda con lo que se haya escrito, o "Jugador".
    public void GuardarConNombre(string nombre)
    {
        if (!esperandoNombre) return;
        esperandoNombre = false;

        nombre = nombre == null ? "" : nombre.Trim();
        if (nombre.Length > 0)
            PlayerPrefs.SetString(RankingData.ClaveNombre, nombre);
        else
            nombre = RankingData.NombrePorDefecto;

        RankingData.Instance.Agregar(nombre, puntaje);
    }
}
