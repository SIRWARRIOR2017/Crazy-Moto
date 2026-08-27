using System.Collections.Generic;
using UnityEngine;

// Autos que vienen de frente (autopista en contramano) por 3 carriles. El jugador
// los esquiva moviéndose de costado con A-D. Se crea en tiempo de ejecución desde
// GameManager, igual que el HUD.
public class TrafficManager : MonoBehaviour
{
    [Header("Carriles (posición X)")]
    public float[] carriles = { -3f, 0f, 3f };

    [Header("Autos")]
    public int cantidadAutos = 20;                              // tamaño del pool
    public Vector3 tamanoAuto = new Vector3(1.8f, 1.4f, 3f);
    public float distanciaSpawn = 100f;                         // qué tan adelante del jugador aparecen
    public float velocidadAutoBase = 10f;                       // se suma a la velocidad del juego
    public float distanciaReciclaje = 12f;                      // detrás del jugador, cuánto tarda en desaparecer

    [Header("Dificultad")]
    public float distanciaEntreFilasInicial = 40f;
    public float distanciaEntreFilasMinima = 16f;

    private Transform jugador;
    private readonly List<Transform> pool = new List<Transform>();
    private float proximaZFila;

    void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go == null)
        {
            Debug.LogError("TrafficManager: no encontré al jugador (tag 'Player'). Me desactivo.");
            enabled = false;
            return;
        }
        jugador = go.transform;

        for (int i = 0; i < cantidadAutos; i++)
            pool.Add(CrearAuto());

        proximaZFila = jugador.position.z + distanciaSpawn;
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.juegoTerminado) return;

        float velAuto = velocidadAutoBase + GameManager.Instance.velocidadActual;

        // Mover los autos activos hacia el jugador y apagar los que quedaron atrás.
        foreach (Transform auto in pool)
        {
            if (!auto.gameObject.activeSelf) continue;

            auto.position += Vector3.back * velAuto * Time.deltaTime;

            if (auto.position.z < jugador.position.z - distanciaReciclaje)
                auto.gameObject.SetActive(false);
        }

        // Generar filas de autos a medida que el jugador avanza.
        while (jugador.position.z + distanciaSpawn >= proximaZFila)
        {
            GenerarFila(proximaZFila);
            proximaZFila += DistanciaEntreFilas();
        }
    }

    float DistanciaEntreFilas()
    {
        // A más velocidad de juego, las filas se juntan (hasta un mínimo).
        float t = Mathf.InverseLerp(GameManager.Instance.velocidadInicial,
                                    GameManager.Instance.velocidadMaxima,
                                    GameManager.Instance.velocidadActual);
        return Mathf.Lerp(distanciaEntreFilasInicial, distanciaEntreFilasMinima, t);
    }

    // Cada fila ocupa 1 o 2 carriles (nunca los 3): siempre queda al menos uno
    // libre para poder esquivar.
    void GenerarFila(float z)
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < carriles.Length; i++)
            indices.Add(i);

        int cuantos = Random.Range(1, carriles.Length);   // 1 .. (carriles-1)

        for (int i = 0; i < cuantos; i++)
        {
            int k = Random.Range(0, indices.Count);
            int carril = indices[k];
            indices.RemoveAt(k);

            Transform auto = TomarAutoLibre();
            if (auto == null) return;   // pool agotado: se saltea esta fila

            auto.position = new Vector3(carriles[carril], tamanoAuto.y * 0.5f, z);
            auto.gameObject.SetActive(true);
        }
    }

    Transform TomarAutoLibre()
    {
        foreach (Transform auto in pool)
            if (!auto.gameObject.activeSelf)
                return auto;
        return null;
    }

    Transform CrearAuto()
    {
        GameObject auto = GameObject.CreatePrimitive(PrimitiveType.Cube);
        auto.name = "Auto";
        auto.tag = "Obstaculo";
        auto.transform.localScale = tamanoAuto;
        auto.transform.SetParent(transform);

        auto.GetComponent<Renderer>().material.color = new Color(0.85f, 0.15f, 0.15f);

        auto.SetActive(false);
        return auto.transform;
    }
}
