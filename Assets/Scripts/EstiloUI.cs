using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Paleta, tipografías, sprites y "kit" de piezas de UI de la dirección visual del
// juego ("Ruta de noche").
//
// Existe para que toda la UI que se arma por código (el ranking, Opciones, la
// pausa, el HUD, Probar cámara) use los mismos colores, fuentes y botones que los
// objetos puestos a mano en la escena (el menú principal y el Game Over). Si algún
// día cambia la dirección visual, se cambia acá y no en cinco scripts.
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
    public static readonly Color Crema = Hex("#FFF3D0");        // arriba del degradado de los títulos

    public static readonly Color Tinta = Hex("#EAF4FF");        // texto normal
    public static readonly Color TintaClara = Hex("#BDEFFF");   // texto destacado
    public static readonly Color TintaSuave = Hex("#8F93B5");   // texto de apoyo, botón "apagado"
    public static readonly Color TintaApagada = Hex("#6E7399"); // texto secundario
    public static readonly Color TintaOscura = Hex("#14062A");  // texto sobre relleno claro

    public static readonly Color FondoOpaco = Hex("#08061A");
    public static readonly Color VeloOscuro = new Color(8f / 255f, 6f / 255f, 26f / 255f, 0.86f);
    public static readonly Color FondoPanel = new Color(9f / 255f, 11f / 255f, 36f / 255f, 0.76f);
    public static readonly Color FondoBoton = new Color(10f / 255f, 12f / 255f, 38f / 255f, 0.62f);

    public static readonly Color BordePanel = new Color(55f / 255f, 224f / 255f, 255f / 255f, 0.34f);
    public static readonly Color BordeBoton = new Color(55f / 255f, 224f / 255f, 255f / 255f, 0.60f);
    public static readonly Color BordeApagado = new Color(189f / 255f, 239f / 255f, 255f / 255f, 0.20f);

    public static readonly Color FilaDestacada = new Color(255f / 255f, 61f / 255f, 154f / 255f, 0.14f);

    // Colores con significado (estado de la cámara, avisos). No son decoración.
    public static readonly Color Alerta = Hex("#FF5C7A");
    public static readonly Color Ok = Hex("#5CF2B0");

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

    // Itálica pesada: sólo para títulos.
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
            if (marco == null) marco = ObtenerSprite("MarcoUI");
            return marco;
        }
    }

    // Línea que se desvanece en las puntas.
    public static Sprite Linea
    {
        get
        {
            if (linea == null) linea = ObtenerSprite("LineaHorizonte");
            return linea;
        }
    }

    // Cualquier sprite de Assets/Resources/Sprites/ (los que dibuja
    // GeneradorSpritesMenu).
    public static Sprite ObtenerSprite(string nombre)
    {
        Sprite s = Resources.Load<Sprite>("Sprites/" + nombre);

        if (s == null)
            Debug.LogWarning("EstiloUI: no encontré el sprite Resources/Sprites/" + nombre + ".");
        return s;
    }

    // =====================================================================
    //  Kit de piezas. Todas las pantallas armadas por código salen de acá.
    // =====================================================================

    public enum TipoBoton
    {
        Principal,   // relleno magenta: la acción que se espera (Jugar, Continuar)
        Secundario,  // marco de neón cyan
        Apagado,     // marco tenue: salir, volver al menú
        Peligro      // marco magenta: acciones que borran algo
    }

    // Pantalla completa, hija del Canvas.
    //   conCielo = true  -> el cielo nocturno del menú (tapa todo lo de atrás).
    //   conCielo = false -> un velo oscuro translúcido: se sigue viendo el juego
    //                       congelado detrás (pausa, Game Over).
    // La imagen de fondo frena los clics: mientras esta pantalla está abierta no
    // se puede tocar lo que quedó atrás.
    public static RectTransform CrearPantalla(Transform canvas, string nombre, bool conCielo)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(canvas, false);
        Estirar(rt);

        Image fondo = go.GetComponent<Image>();
        fondo.sprite = null;
        fondo.raycastTarget = true;

        if (conCielo)
        {
            fondo.color = FondoOpaco;
            Capa(rt, "Cielo", ObtenerSprite("FondoNoche"), Color.white);

            RectTransform grilla = Capa(rt, "Grilla", ObtenerSprite("GrillaNeon"), new Color(1f, 1f, 1f, 0.55f));
            grilla.anchorMin = new Vector2(0f, 0f);
            grilla.anchorMax = new Vector2(1f, 0f);
            grilla.pivot = new Vector2(0.5f, 0f);
            grilla.sizeDelta = new Vector2(0f, 360f);
            grilla.anchoredPosition = Vector2.zero;

            RectTransform horizonte = Capa(rt, "Horizonte", Linea, new Color(1f, 1f, 1f, 0.6f));
            horizonte.anchorMin = new Vector2(0f, 0f);
            horizonte.anchorMax = new Vector2(1f, 0f);
            horizonte.pivot = new Vector2(0.5f, 0.5f);
            horizonte.sizeDelta = new Vector2(0f, 3f);
            horizonte.anchoredPosition = new Vector2(0f, 360f);
        }
        else
        {
            fondo.color = VeloOscuro;
        }

        Capa(rt, "Vineta", ObtenerSprite("Vineta"), Color.white);
        return rt;
    }

    // Título grande con el degradado crema -> magenta del título del juego,
    // pegado arriba y centrado.
    public static TMP_Text CrearTitulo(Transform pantalla, string texto, float tamano = 76f)
    {
        TMP_Text t = CrearTexto(pantalla, "Titulo", texto, Titular, tamano, Color.white, TextAlignmentOptions.Center);
        t.characterSpacing = 3f;
        t.enableVertexGradient = true;
        t.colorGradient = new VertexGradient(Crema, Crema, Magenta, Magenta);

        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -60f);
        rt.sizeDelta = new Vector2(1400f, tamano + 30f);
        return t;
    }

    // Tarjeta: fondo azul noche translúcido + marco de neón, con el contenido
    // apilado en vertical. Devuelve el "Contenido": los hijos van ahí.
    public static RectTransform CrearTarjeta(Transform padre, string nombre, Vector2 tamano, Vector2 posicion)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        Image img = go.GetComponent<Image>();
        img.sprite = null;
        img.color = FondoPanel;
        img.raycastTarget = false;

        AgregarMarco(rt, BordePanel);

        GameObject cont = new GameObject("Contenido", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform crt = (RectTransform)cont.transform;
        crt.SetParent(rt, false);
        Estirar(crt);

        VerticalLayoutGroup vlg = cont.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(32, 32, 28, 28);
        vlg.spacing = 14f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        return crt;
    }

    // Rótulo de sección: MAYÚSCULAS cyan con las letras separadas.
    public static TMP_Text CrearRotulo(Transform padre, string texto)
    {
        TMP_Text t = CrearTexto(padre, "Rotulo " + texto, texto, Negrita, 21f, Cyan, TextAlignmentOptions.Left);
        t.characterSpacing = EspaciadoRotulo;
        Alto(t.gameObject, 30f);
        return t;
    }

    public static TMP_Text CrearTexto(Transform padre, string nombre, string contenido, TMP_FontAsset fuente,
                                      float tamano, Color color, TextAlignmentOptions alineacion)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = contenido;
        Aplicar(t, fuente, tamano, color);
        t.alignment = alineacion;
        return t;
    }

    // Botón con el mismo look que los del menú principal. Si el padre tiene un
    // Layout Group, el alto lo respeta; si no, el tamaño queda en 480 x alto.
    public static Button CrearBoton(Transform padre, string texto, TipoBoton tipo, UnityAction alClickear,
                                    float alto = 64f, float tamTexto = 24f)
    {
        GameObject go = new GameObject("Boton " + texto, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.sizeDelta = new Vector2(480f, alto);
        Alto(go, alto);

        Color fondo, tinta, borde;
        switch (tipo)
        {
            case TipoBoton.Principal:
                fondo = Magenta; tinta = TintaOscura; borde = Color.clear;
                break;
            case TipoBoton.Secundario:
                fondo = FondoBoton; tinta = TintaClara; borde = BordeBoton;
                break;
            case TipoBoton.Peligro:
                fondo = FondoBoton; tinta = Magenta; borde = new Color(Magenta.r, Magenta.g, Magenta.b, 0.7f);
                break;
            default:
                fondo = new Color(FondoBoton.r, FondoBoton.g, FondoBoton.b, 0.5f); tinta = TintaSuave; borde = BordeApagado;
                break;
        }

        Image img = go.GetComponent<Image>();
        img.sprite = null;
        img.color = fondo;
        img.raycastTarget = true;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = ColoresInteractivos();
        if (alClickear != null) btn.onClick.AddListener(alClickear);

        if (tipo != TipoBoton.Principal)
            AgregarMarco(rt, borde);

        TMP_Text lbl = CrearTexto(rt, "Texto", texto.ToUpperInvariant(), Negrita, tamTexto, tinta, TextAlignmentOptions.Center);
        lbl.characterSpacing = 16f;
        Estirar(lbl.rectTransform);

        return btn;
    }

    // Slider de pista fina con marco, relleno cyan y manija magenta.
    public static Slider CrearSlider(Transform padre, float valor)
    {
        GameObject go = new GameObject("Slider", typeof(RectTransform));
        RectTransform srt = (RectTransform)go.transform;
        srt.SetParent(padre, false);

        RectTransform pista = Hijo(srt, "Background", typeof(Image));
        pista.anchorMin = new Vector2(0f, 0.5f);
        pista.anchorMax = new Vector2(1f, 0.5f);
        pista.pivot = new Vector2(0.5f, 0.5f);
        pista.sizeDelta = new Vector2(0f, 14f);
        pista.anchoredPosition = Vector2.zero;
        Image imgPista = pista.GetComponent<Image>();
        imgPista.color = new Color(FondoOpaco.r, FondoOpaco.g, FondoOpaco.b, 0.9f);
        AgregarMarco(pista, BordePanel);

        RectTransform area = Hijo(srt, "Fill Area");
        area.anchorMin = new Vector2(0f, 0.5f);
        area.anchorMax = new Vector2(1f, 0.5f);
        area.pivot = new Vector2(0.5f, 0.5f);
        area.sizeDelta = new Vector2(-4f, 10f);
        area.anchoredPosition = Vector2.zero;

        RectTransform relleno = Hijo(area, "Fill", typeof(Image));
        Estirar(relleno);
        Image imgRelleno = relleno.GetComponent<Image>();
        imgRelleno.color = Cyan;
        imgRelleno.raycastTarget = false;

        RectTransform zona = Hijo(srt, "Handle Slide Area");
        Estirar(zona);
        zona.offsetMin = new Vector2(11f, 0f);
        zona.offsetMax = new Vector2(-11f, 0f);

        RectTransform manija = Hijo(zona, "Handle", typeof(Image));
        manija.anchorMin = new Vector2(0f, 0.15f);
        manija.anchorMax = new Vector2(0f, 0.85f);
        manija.sizeDelta = new Vector2(22f, 0f);
        Image imgManija = manija.GetComponent<Image>();
        imgManija.color = Magenta;

        Slider slider = go.AddComponent<Slider>();
        slider.fillRect = relleno;
        slider.handleRect = manija;
        slider.targetGraphic = imgManija;
        slider.colors = ColoresInteractivos();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(Mathf.Clamp01(valor));
        return slider;
    }

    // Campo de texto de una línea (ej. el nombre para el ranking).
    public static TMP_InputField CrearCampoTexto(Transform padre, string placeholder, int limite)
    {
        GameObject campo = new GameObject("Campo", typeof(RectTransform), typeof(Image));
        RectTransform crt = (RectTransform)campo.transform;
        crt.SetParent(padre, false);

        Image img = campo.GetComponent<Image>();
        img.sprite = null;
        img.color = new Color(FondoOpaco.r, FondoOpaco.g, FondoOpaco.b, 0.9f);
        AgregarMarco(crt, BordeBoton);

        RectTransform area = Hijo(crt, "Text Area", typeof(RectMask2D));
        Estirar(area);
        area.offsetMin = new Vector2(16f, 6f);
        area.offsetMax = new Vector2(-16f, -6f);

        TMP_Text ph = CrearTexto(area, "Placeholder", placeholder, Media, 22f, TintaApagada, TextAlignmentOptions.Left);
        ph.fontStyle = FontStyles.Italic;
        Estirar(ph.rectTransform);

        TMP_Text txt = CrearTexto(area, "Text", "", Media, 22f, Tinta, TextAlignmentOptions.Left);
        Estirar(txt.rectTransform);

        TMP_InputField input = campo.AddComponent<TMP_InputField>();
        input.textViewport = area;
        input.textComponent = txt;
        input.placeholder = ph;
        input.characterLimit = limite;
        input.targetGraphic = img;
        input.colors = ColoresInteractivos();
        input.customCaretColor = true;
        input.caretColor = Cyan;
        input.caretWidth = 2;
        input.selectionColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f);
        return input;
    }

    // ---- Piezas chicas ----

    // Marco de neón estirado sobre un rectángulo. No bloquea clics.
    public static Image AgregarMarco(RectTransform sobre, Color color)
    {
        RectTransform rt = Hijo(sobre, "Marco", typeof(Image));
        Estirar(rt);

        Image img = rt.GetComponent<Image>();
        img.sprite = Marco;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;

        // Un Image sin sprite se dibuja como un rectángulo blanco lleno: mejor
        // apagarlo que tapar lo que hay abajo.
        if (img.sprite == null) img.enabled = false;
        return img;
    }

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

    public static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // Alto fijo dentro de un Layout Group.
    public static void Alto(GameObject go, float alto)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.minHeight = alto;
        le.preferredHeight = alto;
    }

    // Hover más brillante, apretado más oscuro. Los valores > 1 son a propósito:
    // el tinte multiplica el color del botón.
    public static ColorBlock ColoresInteractivos()
    {
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        return cb;
    }

    private static RectTransform Hijo(Transform padre, string nombre, params System.Type[] extra)
    {
        System.Type[] tipos = new System.Type[extra.Length + 1];
        tipos[0] = typeof(RectTransform);
        for (int i = 0; i < extra.Length; i++) tipos[i + 1] = extra[i];

        GameObject go = new GameObject(nombre, tipos);
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        return rt;
    }

    // Capa de fondo que no bloquea clics. Si el sprite no está, queda apagada
    // (un Image sin sprite taparía todo con un rectángulo blanco).
    private static RectTransform Capa(Transform padre, string nombre, Sprite sprite, Color color)
    {
        RectTransform rt = Hijo(padre, nombre, typeof(Image));
        Estirar(rt);

        Image img = rt.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (sprite == null) img.enabled = false;
        return rt;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
