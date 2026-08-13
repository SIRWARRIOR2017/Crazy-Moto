using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Dificultad")]
    public float velocidadInicial = 10f;
    public float aceleracion = 0.2f;        // cuánto sube la velocidad por segundo
    public float velocidadMaxima = 60f;

    public float velocidadActual { get; private set; }
    public bool juegoTerminado { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        velocidadActual = velocidadInicial;
        juegoTerminado = false;
    }

    void Update()
    {
        if (juegoTerminado) return;

        // El juego se hace más rápido con el tiempo
        velocidadActual += aceleracion * Time.deltaTime;
        velocidadActual = Mathf.Min(velocidadActual, velocidadMaxima);
    }

    public void GameOver()
    {
        if (juegoTerminado) return;   // evita llamarlo dos veces

        juegoTerminado = true;
        Time.timeScale = 0f;

        // Le avisa al menú que muestre la pantalla de Game Over
        MenuManager menu = FindObjectOfType<MenuManager>();
        if (menu != null)
            menu.MostrarGameOver();
    }
}