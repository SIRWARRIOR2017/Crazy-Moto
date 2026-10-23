using UnityEngine;

// Partículas de la partida. Se crea en tiempo de ejecución desde GameManager (igual
// que Hud y TrafficManager) y arma por código todos los sistemas:
//   - Choque:    estallido de chispas + pedazos cuando el juego pasa a terminado.
//   - Wheelie:   humo que sale de la rueda trasera mientras se sostiene el wheelie.
//   - Velocidad: líneas de viento a los costados y arriba de la cámara, que
//                aumentan con la velocidad.
//
// Material: Resources/Efectos/Particula.mat (shader URP Particles/Unlit, aditivo,
// con un círculo suave). Opcional: Resources/Efectos/ParticulaAlpha.mat, igual pero
// con mezcla "Alpha" normal, para el humo y los pedazos oscuros (con aditivo lo
// oscuro casi no se ve). Si falta alguno cae a un material armado por código.
public class EfectosParticulas : MonoBehaviour
{
    [Header("Choque - chispas")]
    public int cantidadChispas = 70;
    public Vector2 velocidadChispas = new Vector2(5f, 14f);
    public Vector2 tamanoChispas = new Vector2(0.08f, 0.2f);
    public Color colorChispasA = new Color(1f, 0.9f, 0.2f, 1f);     // amarillo
    public Color colorChispasB = new Color(1f, 0.45f, 0.05f, 1f);   // naranja
    public float gravedadChispas = 1.2f;

    [Header("Choque - pedazos")]
    public int cantidadPedazos = 14;
    public Vector2 velocidadPedazos = new Vector2(3f, 8f);
    public Vector2 tamanoPedazos = new Vector2(0.15f, 0.35f);
    public Color colorPedazosA = new Color(0.12f, 0.12f, 0.12f, 1f);
    public Color colorPedazosB = new Color(0.3f, 0.2f, 0.12f, 1f);
    public float gravedadPedazos = 2.5f;

    [Header("Choque - posición")]
    public Vector3 desfaseChoque = new Vector3(0f, 0.6f, 1f);       // respecto del jugador

    [Header("Wheelie - humo")]
    public float tasaHumo = 45f;                                     // partículas por segundo
    public Vector2 tamanoHumo = new Vector2(0.4f, 0.8f);
    public float crecimientoHumo = 2.5f;                             // cuántas veces crece hasta desvanecerse
    public Vector2 vidaHumo = new Vector2(1f, 1.8f);
    public Color colorHumo = new Color(0.85f, 0.85f, 0.85f, 0.5f);
    public Vector3 desfaseHumo = new Vector3(0f, -0.15f, 0f);        // respecto de la rueda trasera
    public Vector3 desfaseRuedaFallback = new Vector3(0f, 0.1f, -0.9f);

    [Header("Velocidad - líneas de viento")]
    public float tasaVientoMinima = 0.5f;                            // por emisor, a velocidad inicial
    public float tasaVientoMaxima = 25f;                             // por emisor, a velocidad máxima
    public float proporcionVelocidadViento = 0.5f;                   // qué tan rápido van, respecto de la velocidad del juego
    public Vector2 tamanoViento = new Vector2(0.04f, 0.08f);        // ancho de la línea
    public float largoViento = 2f;                                   // Length Scale del Stretch
    public float estiramientoPorVelocidad = 0.12f;                   // Speed Scale del Stretch
    public Color colorViento = new Color(1f, 1f, 1f, 0.45f);
    public float alejamientoViento = 15f;                            // dónde empieza la caja, delante de la cámara
    public float profundidadViento = 30f;
    public float separacionLateralViento = 4.5f;                     // se deja libre |x| < esto (la calle y los autos)
    public float anchoLateralViento = 8f;
    public float alturaViento = 8f;                                  // alto de las cajas laterales
    public float alturaCajaSuperior = 4f;
    public float alturaSobreCamara = 3f;                             // dónde arranca la caja de arriba

    private PlayerController jugador;
    private Transform ruedaTrasera;
    private ParticleSystem chispas, pedazos, humo;
    private ParticleSystem[] vientos;                                // izquierda, derecha, arriba
    private Vector3[] desfasesViento;
    private Transform camara;
    private bool terminadoAntes;

    private Material matAditivo, matAlpha;
    private Material fallbackAditivo, fallbackAlpha;
    private Texture2D texturaFallback;

    void Start()
    {
        jugador = FindAnyObjectByType<PlayerController>();
        if (jugador == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) jugador = go.GetComponent<PlayerController>();
        }
        if (jugador == null)
        {
            Debug.LogError("EfectosParticulas: no encontré al jugador (tag 'Player'). Me desactivo.");
            enabled = false;
            return;
        }

        foreach (Transform t in jugador.GetComponentsInChildren<Transform>())
        {
            if (t.name == "wheel_r") { ruedaTrasera = t; break; }
        }

        CargarMateriales();
        ConstruirChoque();
        ConstruirHumo();
        ConstruirViento();

        if (Camera.main != null) camara = Camera.main.transform;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        // Choque: se detecta comparando el estado entre frames, sin tocar GameOver().
        if (gm.juegoTerminado && !terminadoAntes)
            Explotar();
        terminadoAntes = gm.juegoTerminado;

        // El humo y el viento solo salen con la partida andando (no en el menú, la
        // pausa ni el Game Over).
        bool activo = !gm.juegoTerminado && Time.timeScale > 0f;

        var emisionHumo = humo.emission;
        emisionHumo.rateOverTime = (activo && jugador.haciendoWheelie) ? tasaHumo : 0f;

        float t = Mathf.InverseLerp(gm.velocidadInicial, gm.velocidadMaxima, gm.velocidadActual);
        float tasa = activo ? Mathf.Lerp(tasaVientoMinima, tasaVientoMaxima, t) : 0f;
        float velocidad = gm.velocidadActual * proporcionVelocidadViento;
        for (int i = 0; i < vientos.Length; i++)
        {
            var emision = vientos[i].emission;
            emision.rateOverTime = tasa;
            var main = vientos[i].main;
            main.startSpeed = velocidad;
        }
    }

    void LateUpdate()
    {
        if (jugador == null) return;

        // Humo: sale de la rueda trasera (o de un punto fijo de la moto si no está).
        if (ruedaTrasera != null)
            humo.transform.position = ruedaTrasera.position + desfaseHumo;
        else
            humo.transform.position = jugador.transform.TransformPoint(desfaseRuedaFallback);

        // Viento: las cajas siguen a la cámara, pero solo en posición (no heredan
        // su roll ni su cabeceo, así quedan alineadas con la calle).
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        if (camara != null)
        {
            for (int i = 0; i < vientos.Length; i++)
                vientos[i].transform.position = camara.position + desfasesViento[i];
        }
    }

    void Explotar()
    {
        Vector3 pos = jugador.transform.position + desfaseChoque;
        chispas.transform.position = pos;
        pedazos.transform.position = pos;
        chispas.Clear();
        pedazos.Clear();
        chispas.Play();
        pedazos.Play();
    }

    // ---- Armado de los sistemas ----

    void ConstruirChoque()
    {
        // Chispas: rápidas, estiradas, con gravedad.
        chispas = CrearSistema("Chispas", matAditivo, 300, false);
        var main = chispas.main;
        main.duration = 1f;
        main.useUnscaledTime = true;    // en el Game Over timeScale = 0
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velocidadChispas.x, velocidadChispas.y);
        main.startSize = new ParticleSystem.MinMaxCurve(tamanoChispas.x, tamanoChispas.y);
        main.startColor = new ParticleSystem.MinMaxGradient(colorChispasA, colorChispasB);
        main.gravityModifier = gravedadChispas;

        ConfigurarEstallido(chispas, cantidadChispas);

        var forma = chispas.shape;
        forma.enabled = true;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.3f;

        var rend = chispas.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Stretch;
        rend.lengthScale = 1.5f;
        rend.velocityScale = 0.04f;

        var color = chispas.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(GradienteDesvanecer(Color.white, new Color(1f, 0.5f, 0.3f, 1f)));

        var tam = chispas.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        // Pedazos: más grandes, oscuros, giran.
        pedazos = CrearSistema("Pedazos", matAlpha, 100, false);
        main = pedazos.main;
        main.duration = 1f;
        main.useUnscaledTime = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velocidadPedazos.x, velocidadPedazos.y);
        main.startSize = new ParticleSystem.MinMaxCurve(tamanoPedazos.x, tamanoPedazos.y);
        main.startColor = new ParticleSystem.MinMaxGradient(colorPedazosA, colorPedazosB);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
        main.gravityModifier = gravedadPedazos;

        ConfigurarEstallido(pedazos, cantidadPedazos);

        forma = pedazos.shape;
        forma.enabled = true;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.4f;

        var giro = pedazos.rotationOverLifetime;
        giro.enabled = true;
        giro.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

        color = pedazos.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(GradienteDesvanecer(Color.white, Color.white));
    }

    void ConstruirHumo()
    {
        humo = CrearSistema("HumoWheelie", matAlpha, 200, true);
        var main = humo.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vidaHumo.x, vidaHumo.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(tamanoHumo.x, tamanoHumo.y);
        main.startColor = colorHumo;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
        main.gravityModifier = -0.05f;   // sube de a poquito

        // Sale para arriba (el cono apunta a +Z local; se lo gira para que mire a +Y).
        humo.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        var forma = humo.shape;
        forma.enabled = true;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 20f;
        forma.radius = 0.1f;

        var tam = humo.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, crecimientoHumo));

        var color = humo.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(GradienteDesvanecer(Color.white, Color.white));

        var emision = humo.emission;
        emision.rateOverTime = 0f;
    }

    void ConstruirViento()
    {
        // Tres cajas alrededor de la cámara, dejando libre el centro de la vista:
        // izquierda, derecha y arriba.
        float zCentro = alejamientoViento + profundidadViento * 0.5f;
        float xLado = separacionLateralViento + anchoLateralViento * 0.5f;
        float yLado = alturaViento * 0.5f - 1f;     // arranca apenas abajo de la cámara (el piso tapa lo demás)

        Vector3[] tamanos =
        {
            new Vector3(anchoLateralViento, alturaViento, profundidadViento),
            new Vector3(anchoLateralViento, alturaViento, profundidadViento),
            new Vector3(2f * (separacionLateralViento + anchoLateralViento), alturaCajaSuperior, profundidadViento),
        };
        desfasesViento = new[]
        {
            new Vector3(-xLado, yLado, zCentro),
            new Vector3(xLado, yLado, zCentro),
            new Vector3(0f, alturaSobreCamara + alturaCajaSuperior * 0.5f, zCentro),
        };
        string[] nombres = { "VientoIzquierda", "VientoDerecha", "VientoArriba" };

        vientos = new ParticleSystem[3];
        for (int i = 0; i < vientos.Length; i++)
        {
            ParticleSystem ps = CrearSistema(nombres[i], matAditivo, 300, true);
            vientos[i] = ps;

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSpeed = 0f;     // se actualiza en Update según la velocidad del juego
            main.startSize = new ParticleSystem.MinMaxCurve(tamanoViento.x, tamanoViento.y);
            main.startColor = colorViento;

            // El box emite hacia su +Z: se lo gira 180° para que las líneas vayan
            // hacia la cámara (hacia -Z), como los autos.
            ps.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var forma = ps.shape;
            forma.enabled = true;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = tamanos[i];

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Stretch;
            rend.lengthScale = largoViento;
            rend.velocityScale = estiramientoPorVelocidad;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(GradienteDesvanecer(Color.white, Color.white));

            var emision = ps.emission;
            emision.rateOverTime = 0f;
        }
    }

    // ---- Ayudas ----

    // Crea un sistema como hijo de este objeto, ya detenido y listo para configurar.
    // 'continuo' = true lo deja andando (con tasa 0 hasta que se le dé emisión);
    // false = se dispara a mano con Play().
    ParticleSystem CrearSistema(string nombre, Material material, int maxParticulas, bool continuo)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(transform, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // AddComponent lo arranca solo

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = continuo;
        main.maxParticles = maxParticulas;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emision = ps.emission;
        emision.enabled = true;
        emision.rateOverTime = 0f;

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = material;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        if (continuo) ps.Play();
        return ps;
    }

    // Un solo estallido en el instante 0, sin emisión continua.
    void ConfigurarEstallido(ParticleSystem ps, int cantidad)
    {
        var emision = ps.emission;
        emision.rateOverTime = 0f;
        emision.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)Mathf.Clamp(cantidad, 0, short.MaxValue))
        });
    }

    // Aparece enseguida y se desvanece hacia el final de la vida. 'inicio' y 'fin'
    // son colores que multiplican al color de la partícula.
    Gradient GradienteDesvanecer(Color inicio, Color fin)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(inicio, 0f), new GradientColorKey(fin, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    void CargarMateriales()
    {
        matAditivo = Resources.Load<Material>("Efectos/Particula");
        matAlpha = Resources.Load<Material>("Efectos/ParticulaAlpha");

        if (matAditivo == null)
        {
            Debug.LogWarning("EfectosParticulas: no encontré Resources/Efectos/Particula.mat. Uso un material armado por código.");
            fallbackAditivo = CrearMaterialFallback(true);
            matAditivo = fallbackAditivo;
        }

        if (matAlpha == null)
        {
            // Sin material "Alpha" propio: si el principal es el de reserva, se arma
            // uno con mezcla normal; si no, se usa el aditivo (lo oscuro se verá tenue).
            if (fallbackAditivo != null)
            {
                fallbackAlpha = CrearMaterialFallback(false);
                matAlpha = fallbackAlpha;
            }
            else
            {
                matAlpha = matAditivo;
            }
        }
    }

    Material CrearMaterialFallback(bool aditivo)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;

        Material m = new Material(shader);
        m.name = aditivo ? "ParticulaFallbackAditivo" : "ParticulaFallbackAlpha";

        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);                          // Transparent
            m.SetFloat("_Blend", aditivo ? 2f : 0f);             // Additive / Alpha
            m.SetFloat("_SrcBlend", 5f);                         // SrcAlpha
            m.SetFloat("_DstBlend", aditivo ? 1f : 10f);         // One / OneMinusSrcAlpha
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        if (texturaFallback == null) texturaFallback = CrearTexturaCirculo();
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texturaFallback);
        else m.mainTexture = texturaFallback;
        return m;
    }

    // Círculo blanco con alfa radial suave, de 64x64.
    Texture2D CrearTexturaCirculo()
    {
        const int tam = 64;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixeles = new Color[tam * tam];
        float centro = (tam - 1) * 0.5f;
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float d = Mathf.Sqrt((x - centro) * (x - centro) + (y - centro) * (y - centro)) / centro;
                float a = Mathf.Clamp01(1f - d);
                pixeles[y * tam + x] = new Color(1f, 1f, 1f, a * a);
            }
        }
        tex.SetPixels(pixeles);
        tex.Apply();
        return tex;
    }

    void OnDestroy()
    {
        if (fallbackAditivo != null) Destroy(fallbackAditivo);
        if (fallbackAlpha != null) Destroy(fallbackAlpha);
        if (texturaFallback != null) Destroy(texturaFallback);
    }
}
