using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Pantalla "Probar cámara", armada por código (mismo patrón que MenuOpciones).
// La crea MenuManager en Start() y la registra en MenuManager.panelCamara; se
// abre desde el botón "Probar cámara" de la sección CÁMARA del panel de Opciones.
//
// Sirve para que el jugador se acomode antes de jugar: muestra en vivo si el
// detector está mandando datos, si lo está viendo, hacia dónde está doblando y si
// el wheelie se está activando. Todo sale de EntradaCamara (UDP), así que no abre
// la webcam por su cuenta: la webcam la tiene el script de Python.
//
// Provisional: cuando haya arte, esto pasa a ser un panel real en la escena.
public class MenuCamara : MonoBehaviour
{
    // Media anchura de la barra de dirección, en píxeles de UI.
    private const float MitadBarra = 285f;

    private MenuManager menu;
    private GameObject panel;

    private TMP_Text estado;
    private RectTransform marcador;
    private Image luzWheelie;
    private TMP_Text textoWheelie;

    void Start()
    {
        menu = FindAnyObjectByType<MenuManager>();
        if (menu == null || menu.panelMenu == null)
        {
            Debug.LogError("MenuCamara: no encontré el MenuManager o su panelMenu. Me desactivo.");
            enabled = false;
            return;
        }

        Transform canvas = menu.panelMenu.transform.parent;
        panel = ConstruirPanel(canvas);
        panel.SetActive(false);
        menu.panelCamara = panel;
    }

    void Update()
    {
        // Solo trabajamos si la pantalla está abierta.
        if (panel == null || !panel.activeInHierarchy) return;

        EntradaCamara cam = EntradaCamara.Instance;

        if (cam == null || !cam.HaySenal)
        {
            estado.text = "SIN SEÑAL\nEl detector no está corriendo. Abrí vision/deteccion.py";
            estado.color = new Color(1f, 0.45f, 0.45f);
            MoverMarcador(0f);
            PintarWheelie(false);
            return;
        }

        if (!cam.PersonaDetectada)
        {
            estado.text = "CONECTADA, PERO NO TE VEO\nPonete frente a la cámara, con los hombros en cuadro";
            estado.color = new Color(1f, 0.85f, 0.4f);
            MoverMarcador(0f);
            PintarWheelie(false);
            return;
        }

        estado.text = "LISTO — TE ESTOY VIENDO\nProbá los gestos: ya podés jugar así";
        estado.color = new Color(0.5f, 1f, 0.6f);
        MoverMarcador(cam.Lateral);
        PintarWheelie(cam.Wheelie);
    }

    void MoverMarcador(float lateral)
    {
        if (marcador == null) return;
        marcador.anchoredPosition = new Vector2(Mathf.Clamp(lateral, -1f, 1f) * MitadBarra, 0f);
    }

    void PintarWheelie(bool activo)
    {
        if (luzWheelie == null) return;
        luzWheelie.color = activo
            ? new Color(0.2f, 0.75f, 0.35f, 0.95f)
            : new Color(0.18f, 0.18f, 0.22f, 0.95f);
        textoWheelie.text = activo ? "WHEELIE  ¡SÍ!" : "WHEELIE";
    }

    GameObject ConstruirPanel(Transform canvas)
    {
        // --- Fondo, pantalla completa ---
        GameObject p = new GameObject("PanelCamara", typeof(RectTransform), typeof(Image));
        RectTransform prt = (RectTransform)p.transform;
        prt.SetParent(canvas, false);
        Estirar(prt);
        p.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.95f);

        // --- Título ---
        TMP_Text titulo = CrearTexto(prt, "Titulo", "PROBAR CÁMARA", 44f, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform trt = titulo.rectTransform;
        trt.anchorMin = new Vector2(0.5f, 1f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -30f);
        trt.sizeDelta = new Vector2(700f, 60f);

        // --- Contenedor central ---
        GameObject cont = new GameObject("Contenido", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        RectTransform crt = (RectTransform)cont.transform;
        crt.SetParent(prt, false);
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(0f, -10f);
        crt.sizeDelta = new Vector2(760f, 620f);
        cont.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

        VerticalLayoutGroup vlg = cont.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 14f;
        vlg.padding = new RectOffset(28, 28, 22, 22);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        // --- Estado de la conexión ---
        CrearEncabezado(crt, "ESTADO");
        estado = CrearTexto(crt, "Estado", "...", 24f, TextAlignmentOptions.Center, FontStyles.Bold);
        AgregarLayout(estado.gameObject, 70f);

        // --- Barra de dirección ---
        CrearEncabezado(crt, "DIRECCIÓN");
        CrearBarraDireccion(crt);

        // --- Luz de wheelie ---
        CrearEncabezado(crt, "WHEELIE");
        CrearLuzWheelie(crt);

        // --- Cómo se juega ---
        CrearEncabezado(crt, "CÓMO SE JUEGA");
        TMP_Text ayuda = CrearTexto(crt, "Ayuda",
            "Sentate frente a la cámara y subí los brazos como si agarraras un manubrio.\n" +
            "Doblar:  tirá un brazo hacia el cuerpo y empujá el otro hacia adelante.\n" +
            "Wheelie: tirá los DOS brazos hacia el cuerpo al mismo tiempo.\n" +
            "Si la cámara falla, el teclado (A / D / Shift) sigue funcionando.",
            20f, TextAlignmentOptions.Left, FontStyles.Normal);
        ayuda.color = new Color(0.85f, 0.85f, 0.85f);
        AgregarLayout(ayuda.gameObject, 110f);

        // --- Botón Volver ---
        CrearBoton(crt, "Volver", new Color(0.2f, 0.2f, 0.26f, 0.95f), () => menu.VolverDePruebaCamara());

        return p;
    }

    // Barra horizontal con el centro marcado y un cursor que se mueve según la
    // dirección que manda la cámara (izquierda = negativo, derecha = positivo).
    void CrearBarraDireccion(Transform padre)
    {
        GameObject fila = new GameObject("BarraDireccion", typeof(RectTransform));
        RectTransform frt = (RectTransform)fila.transform;
        frt.SetParent(padre, false);
        AgregarLayout(fila, 56f);

        GameObject pista = NuevoHijo(frt, "Pista", typeof(Image));
        RectTransform prt = (RectTransform)pista.transform;
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(MitadBarra * 2f + 30f, 34f);
        pista.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        GameObject centro = NuevoHijo(prt, "Centro", typeof(Image));
        RectTransform cert = (RectTransform)centro.transform;
        cert.anchorMin = new Vector2(0.5f, 0f);
        cert.anchorMax = new Vector2(0.5f, 1f);
        cert.pivot = new Vector2(0.5f, 0.5f);
        cert.sizeDelta = new Vector2(2f, 0f);
        cert.anchoredPosition = Vector2.zero;
        centro.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);

        GameObject cursor = NuevoHijo(prt, "Marcador", typeof(Image));
        marcador = (RectTransform)cursor.transform;
        marcador.anchorMin = new Vector2(0.5f, 0.5f);
        marcador.anchorMax = new Vector2(0.5f, 0.5f);
        marcador.pivot = new Vector2(0.5f, 0.5f);
        marcador.sizeDelta = new Vector2(28f, 34f);
        marcador.anchoredPosition = Vector2.zero;
        cursor.GetComponent<Image>().color = new Color(0.62f, 0.80f, 1f, 1f);

        TMP_Text izq = CrearTexto(frt, "Izq", "IZQ", 18f, TextAlignmentOptions.Left, FontStyles.Bold);
        RectTransform irt = izq.rectTransform;
        irt.anchorMin = new Vector2(0f, 0f);
        irt.anchorMax = new Vector2(0.2f, 1f);
        irt.offsetMin = Vector2.zero;
        irt.offsetMax = Vector2.zero;
        izq.color = new Color(0.7f, 0.7f, 0.7f);

        TMP_Text der = CrearTexto(frt, "Der", "DER", 18f, TextAlignmentOptions.Right, FontStyles.Bold);
        RectTransform drt = der.rectTransform;
        drt.anchorMin = new Vector2(0.8f, 0f);
        drt.anchorMax = new Vector2(1f, 1f);
        drt.offsetMin = Vector2.zero;
        drt.offsetMax = Vector2.zero;
        der.color = new Color(0.7f, 0.7f, 0.7f);
    }

    void CrearLuzWheelie(Transform padre)
    {
        GameObject go = new GameObject("LuzWheelie", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        AgregarLayout(go, 48f);

        luzWheelie = go.GetComponent<Image>();
        luzWheelie.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);

        textoWheelie = CrearTexto(rt, "Texto", "WHEELIE", 24f, TextAlignmentOptions.Center, FontStyles.Bold);
        textoWheelie.color = Color.white;
        Estirar(textoWheelie.rectTransform);
    }

    // ---- Helpers (mismos que MenuOpciones) ----

    void CrearEncabezado(Transform padre, string texto)
    {
        TMP_Text t = CrearTexto(padre, "Encabezado " + texto, texto, 24f, TextAlignmentOptions.Left, FontStyles.Bold);
        t.color = new Color(0.62f, 0.80f, 1f);
        AgregarLayout(t.gameObject, 30f);
    }

    void CrearBoton(Transform padre, string texto, Color color, UnityEngine.Events.UnityAction alClickear)
    {
        GameObject go = new GameObject("Boton " + texto, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        AgregarLayout(go, 48f);

        Image img = go.GetComponent<Image>();
        img.color = color;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(alClickear);

        TMP_Text lbl = CrearTexto(rt, "Texto", texto, 24f, TextAlignmentOptions.Center, FontStyles.Bold);
        lbl.color = Color.white;
        Estirar(lbl.rectTransform);
    }

    GameObject NuevoHijo(Transform padre, string nombre, params System.Type[] extra)
    {
        System.Type[] tipos = new System.Type[extra.Length + 1];
        tipos[0] = typeof(RectTransform);
        for (int i = 0; i < extra.Length; i++) tipos[i + 1] = extra[i];

        GameObject go = new GameObject(nombre, tipos);
        go.transform.SetParent(padre, false);
        return go;
    }

    void AgregarLayout(GameObject go, float altura)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredHeight = altura;
        le.minHeight = altura;
    }

    TMP_Text CrearTexto(Transform padre, string nombre, string contenido, float tamano,
        TextAlignmentOptions alineacion, FontStyles estilo)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);

        TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = contenido;
        txt.fontSize = tamano;
        txt.alignment = alineacion;
        txt.fontStyle = estilo;
        txt.color = Color.white;
        txt.raycastTarget = false;
        return txt;
    }

    void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
