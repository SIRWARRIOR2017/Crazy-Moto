using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Dificultad")]
    public float velocidadInicial = 10f;
    public float aceleracion = 0.2f;
    public float velocidadMaxima = 60f;

    [Header("Puntaje")]
    public float puntosPorSegundoEnWheelie = 100f;   // solo se suma mientras se sostiene el wheelie

    public float velocidadActual { get; private set; }
    public bool juegoTerminado { get; private set; }
    public int puntaje { get; private set; }

    private float puntajeExacto;
    private PlayerController jugador;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
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

        if (FindAnyObjectByType<Hud>() == null)
            new GameObject("Hud").AddComponent<Hud>();

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

        // Solo se suma puntaje mientras el jugador sostiene el wheelie.
        if (jugador != null && jugador.haciendoWheelie)
        {
            puntajeExacto += puntosPorSegundoEnWheelie * Time.deltaTime;
            puntaje = Mathf.FloorToInt(puntajeExacto);
        }
    }

    public void GameOver()
    {
        if (juegoTerminado) return;

        juegoTerminado = true;
        Time.timeScale = 0f;

        // Guardar el puntaje en el ranking persistente (JSON en disco).
        string nombre = PlayerPrefs.GetString("NombreJugador", "Jugador");
        RankingData.Instance.Agregar(nombre, puntaje);

        if (AudioManager.Instance != null)
            AudioManager.Instance.ReproducirChoque();

        MenuManager menu = FindAnyObjectByType<MenuManager>();
        if (menu != null)
            menu.MostrarGameOver();
    }
}
