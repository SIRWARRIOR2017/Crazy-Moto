using System.Collections.Generic;
using UnityEngine;

// Decorado a los costados de la ruta: tramos de CIUDAD y de CAMPO que se van
// alternando. Se crea en tiempo de ejecución desde GameManager (igual que
// TrafficManager) y funciona como RoadManager: pone tramos de decorado adelante
// del jugador y recicla los que quedan atrás.
//
// Cada tramo de decorado mide lo mismo que un tramo de ruta (30 m) y trae el suelo
// de los dos costados y lo que hay encima (edificios, árboles, postes...). Se
// arman TODOS al arrancar, con contenido al azar, y después sólo se mueven:
// instanciar modelos en plena partida trabaría el juego.
//
// Los modelos son de Kenney (CC0) y están en Assets/Resources/Entorno/. Vienen en
// "unidades de Kenney", no en metros: por eso cada categoría tiene su escala.
public class EntornoManager : MonoBehaviour
{
    [Header("Tramos")]
    public float largoTramo = 30f;              // igual que RoadManager
    public int tramosAdelante = 10;             // ~300 m: más allá de donde termina la niebla
    public int tramosPorBioma = 8;              // cuántos tramos seguidos de ciudad o de campo

    [Header("Suelo")]
    public float mitadCalle = 5f;               // la calle ocupa x = -5..5
    public float anchoSuelo = 300f;             // hasta dónde llega el suelo a cada lado
    public float alturaSuelo = 0.44f;           // apenas abajo de la calle (su superficie está en 0.5)

    [Header("Escalas de los modelos (unidades de Kenney -> metros)")]
    public float escalaEdificios = 11f;
    public float escalaCasas = 9f;
    public float escalaArboles = 5.5f;
    public float escalaPlantas = 5f;
    public float escalaRocas = 2.5f;
    public float escalaPostesLuz = 13f;
    public float escalaPostesElectricos = 16f;
    public float escalaCercas = 4f;
    public float escalaDetalles = 9f;

    [Header("Orientación")]
    [Tooltip("Hacia dónde mira el frente de los modelos de Kenney (0 = +Z). Si los edificios muestran la espalda, poné 180.")]
    public float giroFrente = 0f;

    // Altura de la vereda de la ciudad (la calle está en 0.5).
    private const float AlturaVereda = 0.72f;

    private enum Bioma { Ciudad, Campo }

    private class Activo
    {
        public Transform t;
        public Bioma bioma;
    }

    private Transform jugador;
    private readonly Queue<Transform> libresCiudad = new Queue<Transform>();
    private readonly Queue<Transform> libresCampo = new Queue<Transform>();
    private readonly List<Activo> activos = new List<Activo>();
    private int proximoIndice;

    // Modelos, por categoría
    private GameObject[] edificios, rascacielos, casas, arboles, plantas, rocas, cercas;
    private GameObject[] postesLuz, postesElectricos, detallesCiudad;

    // Materiales del suelo
    private Material matPasto, matTierra, matVereda, matHormigon;

    private System.Random azar;

    void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go == null)
        {
            Debug.LogError("EntornoManager: no encontré al jugador (tag 'Player'). Me desactivo.");
            enabled = false;
            return;
        }
        jugador = go.transform;

        CargarModelos();
        CargarMateriales();

        // Semilla fija: el decorado sale igual en cada partida (se nota menos que se
        // repite, y si algo queda feo se puede ajustar sabiendo que va a volver).
        azar = new System.Random(1234);

        // Tantos como puede haber a la vista de un mismo bioma, más un margen.
        int porBioma = tramosAdelante + 3;
        for (int i = 0; i < porBioma; i++)
        {
            libresCiudad.Enqueue(ArmarTramo(Bioma.Ciudad, i));
            libresCampo.Enqueue(ArmarTramo(Bioma.Campo, i));
        }

        proximoIndice = Mathf.FloorToInt(jugador.position.z / largoTramo) - 1;
    }

    void Update()
    {
        if (jugador == null) return;

        // Reciclar lo que quedó atrás.
        for (int i = activos.Count - 1; i >= 0; i--)
        {
            Activo a = activos[i];
            if (a.t.position.z + largoTramo < jugador.position.z)
            {
                a.t.gameObject.SetActive(false);
                (a.bioma == Bioma.Ciudad ? libresCiudad : libresCampo).Enqueue(a.t);
                activos.RemoveAt(i);
            }
        }

        // Poner adelante lo que falta. "while" (como en RoadManager) por si en un
        // frame el jugador avanza más de un tramo.
        while (proximoIndice * largoTramo < jugador.position.z + tramosAdelante * largoTramo)
            Colocar(proximoIndice++);
    }

    void Colocar(int indice)
    {
        Bioma bioma = BiomaDe(indice);
        Queue<Transform> libres = bioma == Bioma.Ciudad ? libresCiudad : libresCampo;
        if (libres.Count == 0) return;   // no debería pasar: el pool alcanza para lo visible

        Transform t = libres.Dequeue();
        t.position = new Vector3(0f, 0f, indice * largoTramo);
        t.gameObject.SetActive(true);
        activos.Add(new Activo { t = t, bioma = bioma });
    }

    // Arranca en ciudad y cambia cada 'tramosPorBioma' tramos.
    Bioma BiomaDe(int indice)
    {
        int bloque = Mathf.FloorToInt((float)indice / tramosPorBioma);
        return (bloque & 1) == 0 ? Bioma.Ciudad : Bioma.Campo;
    }

    // ---- Armado de tramos ----

    Transform ArmarTramo(Bioma bioma, int numero)
    {
        GameObject raiz = new GameObject((bioma == Bioma.Ciudad ? "TramoCiudad " : "TramoCampo ") + numero);
        raiz.transform.SetParent(transform, false);

        for (int lado = -1; lado <= 1; lado += 2)
        {
            if (bioma == Bioma.Ciudad) ArmarLadoCiudad(raiz.transform, lado);
            else ArmarLadoCampo(raiz.transform, lado);
        }

        raiz.SetActive(false);
        return raiz.transform;
    }

    void ArmarLadoCiudad(Transform raiz, int lado)
    {
        // Suelo de hormigón y una vereda más alta pegada a la calle.
        Suelo(raiz, lado, mitadCalle, anchoSuelo, alturaSuelo, matHormigon);
        Bloque(raiz, "Vereda", lado, mitadCalle, 4f, alturaSuelo, AlturaVereda, matVereda);

        // Primera fila de edificios, uno al lado del otro, mirando a la calle.
        float z = -largoTramo * 0.5f + Rango(0f, 3f);
        int puestos = 0;
        while (z < largoTramo * 0.5f - 6f && puestos < 4)
        {
            Bounds b;
            Transform e = Poner(raiz, Elegir(edificios), escalaEdificios * Rango(0.95f, 1.15f), lado, 0f, out b);
            if (e == null) break;

            // Pegado a la vereda (frente a x = 11) y empezando en z.
            float frente = mitadCalle + 6f;
            float dx = lado > 0 ? frente - b.min.x : -frente - b.max.x;
            float dz = z - b.min.z;
            e.position += new Vector3(dx, 0f, dz);

            z = b.max.z + dz + Rango(1f, 4f);
            puestos++;
        }

        // Rascacielos más atrás: dan el horizonte de ciudad.
        if (Rango(0f, 1f) < 0.75f)
        {
            Bounds b;
            Transform r = Poner(raiz, Elegir(rascacielos), escalaEdificios * Rango(1f, 1.3f), lado, 0f, out b);
            if (r != null)
                r.position += new Vector3(lado * Rango(38f, 70f) - b.center.x, 0f, Rango(-8f, 8f) - b.center.z);
        }

        // Poste de luz sobre la vereda, con el brazo hacia la calle.
        Bounds bl;
        Transform poste = Poner(raiz, Elegir(postesLuz), escalaPostesLuz, lado, 0f, out bl);
        if (poste != null)
            poste.position = new Vector3(lado * (mitadCalle + 0.8f), AlturaVereda, Rango(-12f, 12f));

        // Algún detalle en la vereda (contenedor, cono...).
        if (Rango(0f, 1f) < 0.4f)
        {
            Bounds bd;
            Transform d = Poner(raiz, Elegir(detallesCiudad), escalaDetalles, lado, Rango(-20f, 20f), out bd);
            if (d != null)
                d.position = new Vector3(lado * (mitadCalle + 3f), AlturaVereda, Rango(-12f, 12f));
        }
    }

    void ArmarLadoCampo(Transform raiz, int lado)
    {
        // Pasto y una banquina de tierra pegada a la calle.
        Suelo(raiz, lado, mitadCalle, anchoSuelo, alturaSuelo, matPasto);
        Suelo(raiz, lado, mitadCalle, 2.5f, alturaSuelo + 0.03f, matTierra);

        // Cerca a lo largo de la ruta, en la mitad de los tramos.
        if (Rango(0f, 1f) < 0.5f && cercas.Length > 0)
        {
            GameObject modelo = Elegir(cercas);
            for (float z = -largoTramo * 0.5f; z < largoTramo * 0.5f; z += escalaCercas)
            {
                Bounds b;
                Transform c = Poner(raiz, modelo, escalaCercas, lado, 0f, out b);
                if (c == null) break;
                // El largo de la cerca va a lo largo de la ruta.
                c.rotation = Quaternion.Euler(0f, 90f, 0f);
                c.position = new Vector3(lado * (mitadCalle + 4f), alturaSuelo, z + escalaCercas * 0.5f);
            }
        }

        // Poste de luz eléctrica, cada tanto. Bien afuera: el travesaño mide ~9 m y,
        // más cerca, quedaba colgando sobre el carril de la punta.
        if (Rango(0f, 1f) < 0.45f)
        {
            Bounds b;
            Transform p = Poner(raiz, Elegir(postesElectricos), escalaPostesElectricos, lado, 0f, out b);
            if (p != null)
                p.position += new Vector3(lado * (mitadCalle + 5.5f), 0f, Rango(-12f, 12f));
        }

        // Alguna casa de campo, más atrás, mirando a la ruta.
        if (Rango(0f, 1f) < 0.3f)
        {
            Bounds b;
            Transform casa = Poner(raiz, Elegir(casas), escalaCasas, lado, 0f, out b);
            if (casa != null)
                casa.position += new Vector3(lado * Rango(24f, 38f) - b.center.x, 0f, Rango(-6f, 6f) - b.center.z);
        }

        // Árboles desparramados, sin encimarse.
        List<Vector2> ocupados = new List<Vector2>();
        int cantidad = (int)Rango(5f, 9f);
        for (int i = 0; i < cantidad; i++)
        {
            Vector2 lugar = new Vector2(lado * Rango(12f, 75f), Rango(-largoTramo * 0.5f, largoTramo * 0.5f));
            if (Cerca(ocupados, lugar, 7f)) continue;
            ocupados.Add(lugar);

            Bounds b;
            Transform a = Poner(raiz, Elegir(arboles), escalaArboles * Rango(0.8f, 1.35f), lado, Rango(0f, 360f), out b);
            if (a != null) a.position += new Vector3(lugar.x, 0f, lugar.y);
        }

        // Arbustos, pasto y piedras cerca de la ruta.
        int chicos = (int)Rango(4f, 8f);
        for (int i = 0; i < chicos; i++)
        {
            bool esRoca = Rango(0f, 1f) >= 0.7f;
            GameObject modelo = esRoca ? Elegir(rocas) : Elegir(plantas);
            float escala = (esRoca ? escalaRocas : escalaPlantas) * Rango(0.8f, 1.4f);
            Bounds b;
            Transform p = Poner(raiz, modelo, escala, lado, Rango(0f, 360f), out b);
            if (p != null)
                p.position += new Vector3(lado * Rango(mitadCalle + 3f, 22f), 0f, Rango(-largoTramo * 0.5f, largoTramo * 0.5f));
        }
    }

    // Instancia un modelo como hijo de 'raiz', escalado y girado. Por defecto mira
    // hacia la calle (según el lado); 'giroExtra' lo gira además sobre Y. Devuelve
    // también sus bounds, ya escalado y girado, para poder acomodarlo.
    Transform Poner(Transform raiz, GameObject modelo, float escala, int lado, float giroExtra, out Bounds bounds)
    {
        bounds = new Bounds();
        if (modelo == null) return null;

        GameObject go = Instantiate(modelo, raiz);
        Transform t = go.transform;
        t.localScale = Vector3.one * escala;

        // Frente hacia la calle: el lado derecho (x > 0) mira a -X y el izquierdo a +X.
        float haciaCalle = lado > 0 ? -90f : 90f;
        t.rotation = Quaternion.Euler(0f, giroFrente + haciaCalle + giroExtra, 0f);
        t.position = new Vector3(0f, alturaSuelo, 0f);

        // Decorado: no bloquea nada ni suma física.
        foreach (Collider c in go.GetComponentsInChildren<Collider>()) Destroy(c);

        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            bounds = rs[0].bounds;
            foreach (Renderer r in rs) bounds.Encapsulate(r.bounds);
        }
        return t;
    }

    // Plano de suelo a un costado de la calle, de 'desde' a 'desde + ancho' en x.
    void Suelo(Transform raiz, int lado, float desde, float ancho, float altura, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);   // 10 x 10
        go.name = "Suelo";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(raiz, false);
        go.transform.localScale = new Vector3(ancho / 10f, 1f, largoTramo / 10f);
        go.transform.localPosition = new Vector3(lado * (desde + ancho * 0.5f), altura, 0f);

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // Caja baja (vereda, cordón), de 'desde' a 'desde + ancho' en x.
    void Bloque(Transform raiz, string nombre, int lado, float desde, float ancho, float abajo, float arriba, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(raiz, false);
        go.transform.localScale = new Vector3(ancho, arriba - abajo, largoTramo);
        go.transform.localPosition = new Vector3(lado * (desde + ancho * 0.5f), (abajo + arriba) * 0.5f, 0f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // ---- Carga ----

    void CargarModelos()
    {
        GameObject[] ciudad = Resources.LoadAll<GameObject>("Entorno/Ciudad");
        GameObject[] suburbio = Resources.LoadAll<GameObject>("Entorno/Casas");
        GameObject[] ruta = Resources.LoadAll<GameObject>("Entorno/Ruta");
        GameObject[] naturaleza = Resources.LoadAll<GameObject>("Entorno/Naturaleza");

        rascacielos = Filtrar(ciudad, "skyscraper");
        edificios = Sin(Filtrar(ciudad, "building-"), "skyscraper");
        casas = Filtrar(suburbio, "building-type");
        arboles = Filtrar(naturaleza, "tree_");
        plantas = Filtrar(naturaleza, "plant_", "grass", "flower_");
        rocas = Sin(Filtrar(naturaleza, "rock_", "stump", "log_"), "rock_tall");   // las altas tapaban la vista
        cercas = Filtrar(naturaleza, "fence_");
        postesLuz = Filtrar(ruta, "light-");
        postesElectricos = Sin(Filtrar(ruta, "electricity-pole"), "wide");         // el ancho cruza cables sobre la calle
        detallesCiudad = Filtrar(ruta, "dumpster", "construction-");

        if (edificios.Length == 0 || arboles.Length == 0)
            Debug.LogWarning("EntornoManager: faltan modelos en Assets/Resources/Entorno/. El entorno va a salir vacío.");
    }

    void CargarMateriales()
    {
        matPasto = Resources.Load<Material>("Entorno/Materiales/Pasto");
        matTierra = Resources.Load<Material>("Entorno/Materiales/Tierra");
        matVereda = Resources.Load<Material>("Entorno/Materiales/Vereda");
        matHormigon = Resources.Load<Material>("Entorno/Materiales/Hormigon");

        if (matPasto == null || matTierra == null || matVereda == null || matHormigon == null)
            Debug.LogWarning("EntornoManager: faltan materiales en Assets/Resources/Entorno/Materiales/.");
    }

    // Se queda con los modelos cuyo nombre contiene alguna de las claves.
    static GameObject[] Filtrar(GameObject[] modelos, params string[] claves)
    {
        List<GameObject> lista = new List<GameObject>();
        foreach (GameObject m in modelos)
        {
            foreach (string clave in claves)
            {
                if (!m.name.Contains(clave)) continue;
                lista.Add(m);
                break;
            }
        }

        return lista.ToArray();
    }

    // Saca los modelos cuyo nombre contiene 'clave'.
    static GameObject[] Sin(GameObject[] modelos, string clave)
    {
        List<GameObject> lista = new List<GameObject>(modelos);
        lista.RemoveAll(m => m.name.Contains(clave));
        return lista.ToArray();
    }

    // ---- Azar ----

    GameObject Elegir(GameObject[] lista)
    {
        if (lista == null || lista.Length == 0) return null;
        return lista[azar.Next(lista.Length)];
    }

    float Rango(float min, float max)
    {
        return min + (float)azar.NextDouble() * (max - min);
    }

    static bool Cerca(List<Vector2> puntos, Vector2 p, float distancia)
    {
        foreach (Vector2 q in puntos)
            if ((q - p).sqrMagnitude < distancia * distancia) return true;
        return false;
    }
}
