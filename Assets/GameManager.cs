using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Dificultad")]
    public float velocidadInicial = 10f;
    public float aceleracion = 0.2f;
    public float velocidadMaxima = 60f;

    [Header("Obstáculo de prueba")]
    [SerializeField] private float distanciaObstaculo = 50f;

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
        CrearObstaculoDePrueba();
    }

    void Update()
    {
        if (juegoTerminado) return;

        velocidadActual += aceleracion * Time.deltaTime;
        velocidadActual = Mathf.Min(velocidadActual, velocidadMaxima);
    }

    // Obstáculo temporal para comprobar el ciclo completo de la entrega.
    void CrearObstaculoDePrueba()
    {
        GameObject obstaculo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstaculo.name = "Obstaculo";
        obstaculo.tag = "Obstaculo";
        obstaculo.transform.position = new Vector3(0f, 1f, distanciaObstaculo);
        obstaculo.transform.localScale = new Vector3(10f, 2f, 1.5f);

        Renderer rendererObstaculo = obstaculo.GetComponent<Renderer>();
        rendererObstaculo.material.color = Color.red;
    }

    public void GameOver()
    {
        if (juegoTerminado) return;

        juegoTerminado = true;
        Time.timeScale = 0f;

        MenuManager menu = FindFirstObjectByType<MenuManager>();
        if (menu != null)
            menu.MostrarGameOver();
    }
}