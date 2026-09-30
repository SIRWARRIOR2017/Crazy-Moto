using System.Collections.Generic;
using UnityEngine;

// Autos que vienen de frente (autopista en contramano) por 3 carriles. El jugador
// los esquiva moviéndose de costado con A-D. Se crea en tiempo de ejecución desde
// GameManager, igual que el HUD.
//
// Los modelos de auto se cargan de Assets/Resources/Autos/: cualquier prefab que
// haya en esa carpeta entra a la rotación (se elige uno al azar por cada auto del
// pool). Si la carpeta está vacía, cae a un cubo rojo de CreatePrimitive para que
// el juego siga andando mientras no haya arte.
public class TrafficManager : MonoBehaviour
{
    [Header("Carriles (posición X)")]
    public float[] carriles = { -3f, 0f, 3f };

    [Header("Autos")]
    public int cantidadAutos = 20;                              // tamaño del pool
    public float alturaAuto = 0.5f;                             // Y del auto = superficie del camino
    public float distanciaSpawn = 100f;                         // qué tan adelante del jugador aparecen
    public float velocidadAutoBase = 10f;                       // se suma a la velocidad del juego
    public float distanciaReciclaje = 12f;                      // detrás del jugador, cuánto tarda en desaparecer

    [Header("Cubo de reserva (solo si Resources/Autos está vacío)")]
    public Vector3 tamanoCuboFallback = new Vector3(1.8f, 1.4f, 3f);

    [Header("Dificultad")]
    public float distanciaEntreFilasInicial = 40f;
    public float distanciaEntreFilasMinima = 16f;

    private Transform jugador;
    private GameObject[] modelosAuto;                           // prefabs de Assets/Resources/Autos
    private readonly List<Transform> pool = new List<Transform>();
    private float proximaZFila;

    // Qué carriles quedaron libres en la última fila (null = todavía no hubo).
    private bool[] libresAnterior;
    private const int IntentosPorFila = 12;

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

        modelosAuto = Resources.LoadAll<GameObject>("Autos");
        if (modelosAuto == null || modelosAuto.Length == 0)
            Debug.LogWarning("TrafficManager: no hay prefabs en Assets/Resources/Autos/. Uso cubos rojos de reserva.");

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
    //
    // Además, el carril libre tiene que quedar AL LADO (o en el mismo lugar) de un
    // carril que estaba libre en la fila anterior. Sin esto, a alta velocidad
    // podían salir dos filas seguidas con el hueco en los extremos opuestos
    // (izquierda y después derecha): hay que cruzar ~3 m de costado en ~0,2 s y
    // no se puede, hagas lo que hagas. Con la regla, entre una fila y la siguiente
    // alcanza con correrse unos centímetros.
    void GenerarFila(float z)
    {
        int n = carriles.Length;
        bool[] ocupados = ElegirOcupados(n);

        bool[] libres = new bool[n];
        for (int carril = 0; carril < n; carril++)
        {
            libres[carril] = true;
            if (!ocupados[carril]) continue;

            Transform auto = TomarAutoLibre();
            if (auto == null) continue;   // pool agotado: ese carril queda libre

            auto.position = new Vector3(carriles[carril], alturaAuto, z);
            auto.gameObject.SetActive(true);
            libres[carril] = false;
        }

        libresAnterior = libres;
    }

    // Sortea qué carriles ocupar (entre 1 y n-1) hasta que la fila sea alcanzable
    // desde la anterior. Si no sale en unos intentos, pone un solo auto, que
    // siempre es alcanzable.
    bool[] ElegirOcupados(int n)
    {
        for (int intento = 0; intento < IntentosPorFila; intento++)
        {
            bool[] ocupados = Sortear(n, Random.Range(1, n));   // 1 .. (n-1)
            if (EsAlcanzable(ocupados)) return ocupados;
        }

        return Sortear(n, 1);
    }

    bool[] Sortear(int n, int cuantos)
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < n; i++) indices.Add(i);

        bool[] ocupados = new bool[n];
        for (int i = 0; i < cuantos; i++)
        {
            int k = Random.Range(0, indices.Count);
            ocupados[indices[k]] = true;
            indices.RemoveAt(k);
        }
        return ocupados;
    }

    // ¿Algún carril libre de esta fila está en el mismo lugar o al lado de un
    // carril que estaba libre en la anterior?
    bool EsAlcanzable(bool[] ocupados)
    {
        if (libresAnterior == null) return true;

        for (int i = 0; i < ocupados.Length; i++)
        {
            if (ocupados[i]) continue;

            for (int j = Mathf.Max(0, i - 1); j <= Mathf.Min(ocupados.Length - 1, i + 1); j++)
                if (libresAnterior[j]) return true;
        }
        return false;
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
        GameObject auto;

        if (modelosAuto != null && modelosAuto.Length > 0)
        {
            GameObject prefab = modelosAuto[Random.Range(0, modelosAuto.Length)];
            auto = Instantiate(prefab, transform);
        }
        else
        {
            auto = GameObject.CreatePrimitive(PrimitiveType.Cube);
            auto.transform.SetParent(transform);
            auto.transform.localScale = tamanoCuboFallback;
            auto.GetComponent<Renderer>().material.color = new Color(0.85f, 0.15f, 0.15f);
        }

        auto.name = "Auto";
        auto.tag = "Obstaculo";

        // El jugador detecta el choque con su propio trigger (OnTriggerEnter), así
        // que al auto le alcanza con tener un collider sólido en la raíz. Lo normal
        // es que el prefab ya traiga un BoxCollider ajustado a mano; si no, le
        // pongo uno genérico y aviso para que lo ajusten.
        if (auto.GetComponent<Collider>() == null && auto.GetComponentInChildren<Collider>() == null)
        {
            auto.AddComponent<BoxCollider>();
            Debug.LogWarning("TrafficManager: al auto le faltaba collider; le puse un BoxCollider sin ajustar. Conviene agregarlo al prefab.");
        }

        auto.SetActive(false);
        return auto.transform;
    }
}
