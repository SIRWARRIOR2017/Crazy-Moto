using UnityEngine;
using TMPro;

// Paleta, tipografías y sprites de la dirección visual del juego ("Ruta de noche").
//
// Existe para que toda la UI que se arma por código (el ranking, Opciones, la
// pausa, el HUD) use los mismos colores y las mismas fuentes que los objetos que
// ya están puestos a mano en la escena. Si algún día cambia la dirección visual,
// se cambia acá y no en cinco scripts.
//
// Las fuentes y los sprites se cargan de Assets/Resources/ (mismo mecanismo que
// usa TrafficManager con Resources/Autos/): es la única forma de que un script
// llegue a un asset sin tener que arrastrarlo en el Inspector.
public static class EstiloUI
{
    // ---- Colores ----
    // Los hex son los del boceto que eligió el alumno.
    public static readonly Color Magenta = Hex("#FF3D9A");
    public static readonly Color Cyan = Hex("#37E0FF");
    public static readonly Color Ambar = Hex("#FFB454");

    public static readonly Color Tinta = Hex("#EAF4FF");        // texto normal
    public static readonly Color TintaClara = Hex("#BDEFFF");   // texto destacado
    public static readonly Color TintaApagada = Hex("#6E7399"); // texto secundario
    public static readonly Color TintaOscura = Hex("#14062A");  // texto sobre relleno claro

    public static readonly Color FondoOpaco = Hex("#08061A");
    public static readonly Color FondoPanel = new Color(9f / 255f, 11f / 255f, 36f / 255f, 0.76f);
    public static readonly Color FondoBoton = new Color(10f / 255f, 12f / 255f, 38f / 255f, 0.62f);

    public static readonly Color BordePanel = new Color(55f / 255f, 224f / 255f, 255f / 255f, 0.34f);
    public static readonly Color BordeBoton = new Color(55f / 255f, 224f / 255f, 255f / 255f, 0.60f);
    public static readonly Color BordeApagado = new Color(189f / 255f, 239f / 255f, 255f / 255f, 0.20f);

    public static readonly Color FilaDestacada = new Color(255f / 255f, 61f / 255f, 154f / 255f, 0.14f);

    // ---- Tamaños de texto (en el canvas de referencia de 1920x1080) ----
    public const float TamTitulo = 118f;
    public const float TamEncabezado = 25f;
    public const float TamBoton = 32f;
    public const float TamTexto = 26f;
    public const float TamChico = 22f;

    // Espaciado entre letras de los rótulos en mayúscula (el look del boceto).
    public const float EspaciadoRotulo = 24f;

    // ---- Tipografías ----

    private static TMP_FontAsset titular, negrita, media;

    // Itálica pesada: sólo para el título del juego.
    public static TMP_FontAsset Titular
    {
        get
        {
            if (titular == null) titular = CargarFuente("ChakraPetch-BoldItalic SDF");
            return titular;
        }
    }

    // Negrita: botones y encabezados.
    public static TMP_FontAsset Negrita
    {
        get
        {
            if (negrita == null) negrita = CargarFuente("ChakraPetch-Bold SDF");
            return negrita;
        }
    }

    // Semi negrita: el texto normal de la interfaz.
    public static TMP_FontAsset Media
    {
        get
        {
            if (media == null) media = CargarFuente("ChakraPetch-SemiBold SDF");
            return media;
        }
    }

    private static TMP_FontAsset CargarFuente(string nombre)
    {
        TMP_FontAsset f = Resources.Load<TMP_FontAsset>("Fuentes/" + nombre);

        if (f == null)
            Debug.LogWarning("EstiloUI: no encontré la fuente Resources/Fuentes/" + nombre +
                             ". Se usa la de TextMeshPro por defecto.");
        return f;
    }

    // ---- Sprites ----

    private static Sprite marco, linea;

    // Marco hueco de 1 px. Va SIEMPRE en modo Sliced (si no, se deforma).
    public static Sprite Marco
    {
        get
        {
            if (marco == null) marco = CargarSprite("MarcoUI");
            return marco;
        }
    }

    // Línea que se desvanece en las puntas.
    public static Sprite Linea
    {
        get
        {
            if (linea == null) linea = CargarSprite("LineaHorizonte");
            return linea;
        }
    }

    private static Sprite CargarSprite(string nombre)
    {
        Sprite s = Resources.Load<Sprite>("Sprites/" + nombre);

        if (s == null)
            Debug.LogWarning("EstiloUI: no encontré el sprite Resources/Sprites/" + nombre + ".");
        return s;
    }

    // ---- Ayudas para armar UI por código ----

    // Deja un TMP con la tipografía y el color del estilo, de una sola línea.
    public static void Aplicar(TMP_Text texto, TMP_FontAsset fuente, float tamano, Color color,
                               float espaciado = 0f)
    {
        if (fuente != null) texto.font = fuente;

        texto.fontSize = tamano;
        texto.enableAutoSizing = false;
        texto.characterSpacing = espaciado;
        texto.color = color;
        texto.enableVertexGradient = false;
        texto.raycastTarget = false;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
