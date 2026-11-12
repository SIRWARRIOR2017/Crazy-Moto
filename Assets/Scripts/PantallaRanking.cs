using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Ventana aparte con el ranking, para mostrarlo en el segundo monitor del stand
// mientras se juega en la notebook ("ranking de jugadores en pantalla paralela").
//
// Unity no puede abrir una segunda ventana desde el mismo juego, así que el botón
// de Opciones abre OTRA copia del ejecutable con el argumento "-ranking". Esa
// copia arranca en "modo ranking": no crea el tráfico, ni los sonidos, ni el
// receptor de la cámara (el puerto UDP es del juego), y sólo muestra el Top 10 en
// grande. Lee el mismo ranking.json y lo vuelve a leer cada vez que el juego lo
// guarda. F11 la pone en pantalla completa en el monitor donde esté.
public class PantallaRanking : MonoBehaviour
{
    public const string Argumento = "-ranking";

    private const int Puestos = 10;
    private const float SegundosEntreLecturas = 1f;
    private const int AnchoVentana = 960;
    private const int AltoVentana = 720;

    private static bool? modoRanking;
    private static System.Diagnostics.Process ventana;

    private TMP_Text[] puestos;
    private TMP_Text[] nombres;
    private TMP_Text[] puntajes;
    private Image[] fondosFila;

    private float proximaLectura;
    private System.DateTime ultimaEscritura;

    // ¿Esta copia del juego se abrió para mostrar sólo el ranking?
    public static bool ModoRanking
    {
        get
        {
            if (modoRanking == null)
                modoRanking = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), Argumento) >= 0;
            return modoRanking.Value;
        }
    }

    // ---- Del lado del juego: el botón de Opciones ----

    // Abre la ventana del ranking. Devuelve el texto que muestra el botón.
    public static string AbrirVentana()
    {
        if (Application.isEditor)
            return "SÓLO EN EL JUEGO COMPILADO";

        if (ventana != null && !ventana.HasExited)
            return "LA VENTANA YA ESTÁ ABIERTA";

        // Application.dataPath es la carpeta "Crazy Moto_Data", al lado del .exe.
        string exe = Path.Combine(Path.GetDirectoryName(Application.dataPath), Application.productName + ".exe");
        if (!File.Exists(exe))
        {
            Debug.LogWarning("PantallaRanking: no encontré el ejecutable en " + exe);
            return "NO ENCONTRÉ EL JUEGO";
        }

        try
        {
            ventana = System.Diagnostics.Process.Start(exe, Argumento + " -screen-fullscreen 0" +
                " -screen-width " + AnchoVentana + " -screen-height " + AltoVentana);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("PantallaRanking: no pude abrir la ventana (" + e.Message + ").");
            return "NO SE PUDO ABRIR";
        }

        return "VENTANA DE RANKING ABIERTA";
    }

    // ---- Del lado de la ventana: el modo ranking ----

    // La llama MenuManager.Start() cuando el juego se abrió con "-ranking".
    public static void Iniciar(Transform canvas)
    {
        // Es sólo una tabla: no hace falta dibujar 60 veces por segundo ni el
        // mundo 3D que quedó atrás, ni que suene nada.
        Application.targetFrameRate = 20;
        QualitySettings.vSyncCount = 0;

        // El foco lo tiene siempre la ventana del juego: sin esto, el ranking se
        // congela (el proyecto tiene "Run In Background" apagado) y no se entera
        // de los puntajes nuevos.
        Application.runInBackground = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.cullingMask = 0;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = EstiloUI.FondoOpaco;
        }

        PantallaRanking p = new GameObject("PantallaRanking").AddComponent<PantallaRanking>();
        p.Construir(canvas);
        p.Refrescar();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
            AlternarPantallaCompleta();

        // El juego guarda el ranking en otro proceso: se mira la fecha del archivo
        // y, si cambió, se vuelve a leer.
        if (Time.unscaledTime < proximaLectura) return;
        proximaLectura = Time.unscaledTime + SegundosEntreLecturas;

        string ruta = RankingData.Instance.RutaArchivo;
        if (!File.Exists(ruta)) return;

        System.DateTime escritura = File.GetLastWriteTimeUtc(ruta);
        if (escritura == ultimaEscritura) return;

        ultimaEscritura = escritura;
        RankingData.Instance.Cargar();
        Refrescar();
    }

    // Pantalla completa en el monitor donde está la ventana (por eso primero se
    // arrastra al segundo monitor y después se aprieta F11).
    void AlternarPantallaCompleta()
    {
        if (Screen.fullScreen)
        {
            Screen.SetResolution(AnchoVentana, AltoVentana, FullScreenMode.Windowed);
        }
        else
        {
            DisplayInfo monitor = Screen.mainWindowDisplayInfo;
            Screen.SetResolution(monitor.width, monitor.height, FullScreenMode.FullScreenWindow);
        }
    }

    void Refrescar()
    {
        var entradas = RankingData.Instance.Entradas;

        for (int i = 0; i < Puestos; i++)
        {
            bool hay = i < entradas.Count;
            bool primero = hay && i == 0;

            puestos[i].text = (i + 1).ToString();
            nombres[i].text = hay ? entradas[i].nombre : "—";
            puntajes[i].text = hay ? entradas[i].puntaje.ToString("N0") : "";

            fondosFila[i].color = primero ? EstiloUI.FilaDestacada : Color.clear;
            puestos[i].color = primero ? EstiloUI.Magenta : EstiloUI.TintaApagada;
            nombres[i].color = hay ? EstiloUI.Tinta : EstiloUI.TintaApagada;
            puntajes[i].color = primero ? EstiloUI.Magenta : EstiloUI.TintaClara;
        }
    }

    // ---- Armado ----

    void Construir(Transform canvas)
    {
        RectTransform pantalla = EstiloUI.CrearPantalla(canvas, "PanelRanking", true);
        EstiloUI.CrearTitulo(pantalla, "RANKING", 104f);

        TMP_Text sub = EstiloUI.CrearTexto(pantalla, "Subtitulo", "CRAZY MOTO  ·  TOP 10", EstiloUI.Negrita, 28f,
            EstiloUI.Cyan, TextAlignmentOptions.Center);
        sub.characterSpacing = EstiloUI.EspaciadoRotulo;
        RectTransform srt = sub.rectTransform;
        srt.anchorMin = new Vector2(0.5f, 1f);
        srt.anchorMax = new Vector2(0.5f, 1f);
        srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -200f);
        srt.sizeDelta = new Vector2(1400f, 40f);

        RectTransform tabla = EstiloUI.CrearTarjeta(pantalla, "Tabla", new Vector2(1200f, 720f), new Vector2(0f, -70f));
        VerticalLayoutGroup vlg = tabla.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        puestos = new TMP_Text[Puestos];
        nombres = new TMP_Text[Puestos];
        puntajes = new TMP_Text[Puestos];
        fondosFila = new Image[Puestos];
        for (int i = 0; i < Puestos; i++)
            CrearFila(tabla, i);

        TMP_Text pie = EstiloUI.CrearTexto(pantalla, "Pie", "F11: pantalla completa", EstiloUI.Media, 20f,
            EstiloUI.TintaApagada, TextAlignmentOptions.Center);
        RectTransform prt = pie.rectTransform;
        prt.anchorMin = new Vector2(0.5f, 0f);
        prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0f, 24f);
        prt.sizeDelta = new Vector2(800f, 30f);
    }

    // Igual que una fila de MenuRanking, pero más grande para leerla de lejos.
    void CrearFila(Transform padre, int i)
    {
        GameObject fila = new GameObject("Fila" + (i + 1), typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        RectTransform rt = (RectTransform)fila.transform;
        rt.SetParent(padre, false);
        EstiloUI.Alto(fila, 58f);

        Image fondo = fila.GetComponent<Image>();
        fondo.sprite = null;
        fondo.color = Color.clear;
        fondo.raycastTarget = false;
        fondosFila[i] = fondo;

        HorizontalLayoutGroup hlg = fila.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(20, 20, 2, 2);
        hlg.spacing = 24f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        puestos[i] = EstiloUI.CrearTexto(rt, "Puesto", "", EstiloUI.Negrita, 36f, EstiloUI.TintaApagada,
            TextAlignmentOptions.Left);
        Ancho(puestos[i].gameObject, 70f, 0f);

        // Con Ellipsis, TMP no dibuja nada si el alto es menor que una línea (ver
        // EstiloUI): a 40 px una línea mide ~52, por eso el alto de la fila es 58.
        nombres[i] = EstiloUI.CrearTexto(rt, "Nombre", "", EstiloUI.Media, 40f, EstiloUI.Tinta,
            TextAlignmentOptions.Left);
        nombres[i].textWrappingMode = TextWrappingModes.NoWrap;
        nombres[i].overflowMode = TextOverflowModes.Ellipsis;
        Ancho(nombres[i].gameObject, 0f, 1f);

        puntajes[i] = EstiloUI.CrearTexto(rt, "Puntaje", "", EstiloUI.Negrita, 40f, EstiloUI.TintaClara,
            TextAlignmentOptions.Right);
        puntajes[i].textWrappingMode = TextWrappingModes.NoWrap;
        Ancho(puntajes[i].gameObject, 260f, 0f);
    }

    // Ancho fijo (flexible = 0) o el espacio que sobra (flexible = 1, ancho 0).
    void Ancho(GameObject go, float ancho, float flexible)
    {
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minWidth = ancho;
        le.preferredWidth = ancho;
        le.flexibleWidth = flexible;
    }
}
