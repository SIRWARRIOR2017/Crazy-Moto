using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Pantalla "Probar cámara", armada por código con las piezas de EstiloUI. La crea
// MenuManager en Start() y la registra en MenuManager.panelCamara; se abre desde
// el botón "Probar cámara" del panel de Opciones.
//
// Sirve para que el jugador se acomode antes de jugar: muestra en vivo si el
// detector está mandando datos, si lo está viendo, hacia dónde está doblando y si
// el wheelie se está activando. Todo sale de EntradaCamara (UDP), así que no abre
// la webcam por su cuenta: la webcam la tiene el script de Python.
public class MenuCamara : MonoBehaviour
{
    // Media anchura de la barra de dirección, en píxeles de UI.
    private const float MitadBarra = 285f;

    private MenuManager menu;
    private GameObject panel;

    private TMP_Text estado;
    private RectTransform marcador;
    private Image luzWheelie;
    private Image marcoWheelie;
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
        // Sólo trabajamos si la pantalla está abierta.
        if (panel == null || !panel.activeInHierarchy) return;

        EntradaCamara cam = EntradaCamara.Instance;

        if (cam == null || !cam.HaySenal)
        {
            estado.text = "SIN SEÑAL\n<size=20><color=#8F93B5>El detector no está corriendo. Abrí vision/deteccion.py</color></size>";
            estado.color = EstiloUI.Alerta;
            MoverMarcador(0f);
            PintarWheelie(false);
            return;
        }

        if (!cam.PersonaDetectada)
        {
            estado.text = "CONECTADA, PERO NO TE VEO\n<size=20><color=#8F93B5>Ponete frente a la cámara, con los hombros en cuadro</color></size>";
            estado.color = EstiloUI.Ambar;
            MoverMarcador(0f);
            PintarWheelie(false);
            return;
        }

        estado.text = "LISTO — TE ESTOY VIENDO\n<size=20><color=#8F93B5>Probá los gestos: ya podés jugar así</color></size>";
        estado.color = EstiloUI.Ok;
        MoverMarcador(cam.Lateral);
        PintarWheelie(cam.Wheelie);
    }

    void MoverMarcador(float lateral)
    {
        if (marcador == null) return;
        marcador.anchoredPosition = new Vector2(Mathf.Clamp(lateral, -1f, 1f) * MitadBarra, 0f);
    }

    // Apagada: marco tenue. Prendida: relleno magenta, como el botón Jugar.
    void PintarWheelie(bool activo)
    {
        if (luzWheelie == null) return;

        luzWheelie.color = activo ? EstiloUI.Magenta
                                  : new Color(EstiloUI.FondoBoton.r, EstiloUI.FondoBoton.g, EstiloUI.FondoBoton.b, 0.5f);
        marcoWheelie.enabled = !activo && marcoWheelie.sprite != null;
        textoWheelie.color = activo ? EstiloUI.TintaOscura : EstiloUI.TintaSuave;
        textoWheelie.text = activo ? "WHEELIE  ¡SÍ!" : "WHEELIE";
    }

    GameObject ConstruirPanel(Transform canvas)
    {
        RectTransform pantalla = EstiloUI.CrearPantalla(canvas, "PanelCamara", true);
        EstiloUI.CrearTitulo(pantalla, "PROBAR CÁMARA");

        RectTransform cont = EstiloUI.CrearTarjeta(pantalla, "Tarjeta", new Vector2(900f, 580f), new Vector2(0f, 20f));

        // --- Estado de la conexión ---
        EstiloUI.CrearRotulo(cont, "ESTADO");
        estado = EstiloUI.CrearTexto(cont, "Estado", "...", EstiloUI.Negrita, 28f, EstiloUI.Tinta,
            TextAlignmentOptions.Center);
        estado.lineSpacing = 8f;
        EstiloUI.Alto(estado.gameObject, 76f);

        // --- Barra de dirección ---
        EstiloUI.CrearRotulo(cont, "DIRECCIÓN");
        CrearBarraDireccion(cont);

        // --- Luz de wheelie ---
        EstiloUI.CrearRotulo(cont, "WHEELIE");
        CrearLuzWheelie(cont);

        // --- Cómo se juega ---
        EstiloUI.CrearRotulo(cont, "CÓMO SE JUEGA");
        TMP_Text ayuda = EstiloUI.CrearTexto(cont, "Ayuda",
            "Sentate frente a la cámara y subí los brazos como si agarraras un manubrio.\n" +
            "<color=#BDEFFF>Doblar:</color> tirá un brazo hacia el cuerpo y empujá el otro hacia adelante.\n" +
            "<color=#BDEFFF>Wheelie:</color> tirá los DOS brazos hacia el cuerpo al mismo tiempo.\n" +
            "Si la cámara falla, el teclado (A / D / Shift) sigue funcionando.",
            EstiloUI.Media, 19f, EstiloUI.TintaSuave, TextAlignmentOptions.TopLeft);
        ayuda.lineSpacing = 8f;
        EstiloUI.Alto(ayuda.gameObject, 118f);

        // --- Volver, abajo al centro ---
        Button volver = EstiloUI.CrearBoton(pantalla, "Volver", EstiloUI.TipoBoton.Secundario,
            () => menu.VolverDePruebaCamara(), 72f, 26f);
        RectTransform vrt = (RectTransform)volver.transform;
        vrt.anchorMin = new Vector2(0.5f, 0f);
        vrt.anchorMax = new Vector2(0.5f, 0f);
        vrt.pivot = new Vector2(0.5f, 0f);
        vrt.anchoredPosition = new Vector2(0f, 90f);
        vrt.sizeDelta = new Vector2(420f, 72f);

        return pantalla.gameObject;
    }

    // Pista horizontal con el centro marcado y un cursor que se mueve según la
    // dirección que manda la cámara (izquierda = negativo, derecha = positivo).
    void CrearBarraDireccion(Transform padre)
    {
        GameObject fila = new GameObject("BarraDireccion", typeof(RectTransform));
        RectTransform frt = (RectTransform)fila.transform;
        frt.SetParent(padre, false);
        EstiloUI.Alto(fila, 50f);

        GameObject pista = new GameObject("Pista", typeof(RectTransform), typeof(Image));
        RectTransform prt = (RectTransform)pista.transform;
        prt.SetParent(frt, false);
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(MitadBarra * 2f + 30f, 30f);
        Image imgPista = pista.GetComponent<Image>();
        imgPista.color = new Color(EstiloUI.FondoOpaco.r, EstiloUI.FondoOpaco.g, EstiloUI.FondoOpaco.b, 0.9f);
        imgPista.raycastTarget = false;
        EstiloUI.AgregarMarco(prt, EstiloUI.BordePanel);

        GameObject centro = new GameObject("Centro", typeof(RectTransform), typeof(Image));
        RectTransform cert = (RectTransform)centro.transform;
        cert.SetParent(prt, false);
        cert.anchorMin = new Vector2(0.5f, 0f);
        cert.anchorMax = new Vector2(0.5f, 1f);
        cert.pivot = new Vector2(0.5f, 0.5f);
        cert.sizeDelta = new Vector2(2f, 0f);
        cert.anchoredPosition = Vector2.zero;
        Image imgCentro = centro.GetComponent<Image>();
        imgCentro.color = new Color(EstiloUI.Cyan.r, EstiloUI.Cyan.g, EstiloUI.Cyan.b, 0.5f);
        imgCentro.raycastTarget = false;

        GameObject cursor = new GameObject("Marcador", typeof(RectTransform), typeof(Image));
        marcador = (RectTransform)cursor.transform;
        marcador.SetParent(prt, false);
        marcador.anchorMin = new Vector2(0.5f, 0.5f);
        marcador.anchorMax = new Vector2(0.5f, 0.5f);
        marcador.pivot = new Vector2(0.5f, 0.5f);
        marcador.sizeDelta = new Vector2(24f, 30f);
        marcador.anchoredPosition = Vector2.zero;
        Image imgCursor = cursor.GetComponent<Image>();
        imgCursor.color = EstiloUI.Magenta;
        imgCursor.raycastTarget = false;

        TMP_Text izq = EstiloUI.CrearTexto(frt, "Izq", "IZQ", EstiloUI.Negrita, 18f, EstiloUI.TintaApagada,
            TextAlignmentOptions.Left);
        izq.characterSpacing = 12f;
        Ocupar(izq.rectTransform, 0f, 0.15f);

        TMP_Text der = EstiloUI.CrearTexto(frt, "Der", "DER", EstiloUI.Negrita, 18f, EstiloUI.TintaApagada,
            TextAlignmentOptions.Right);
        der.characterSpacing = 12f;
        Ocupar(der.rectTransform, 0.85f, 1f);
    }

    void CrearLuzWheelie(Transform padre)
    {
        GameObject go = new GameObject("LuzWheelie", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        EstiloUI.Alto(go, 52f);

        luzWheelie = go.GetComponent<Image>();
        luzWheelie.raycastTarget = false;
        marcoWheelie = EstiloUI.AgregarMarco(rt, EstiloUI.BordeApagado);

        textoWheelie = EstiloUI.CrearTexto(rt, "Texto", "WHEELIE", EstiloUI.Negrita, 24f, EstiloUI.TintaSuave,
            TextAlignmentOptions.Center);
        textoWheelie.characterSpacing = 16f;
        EstiloUI.Estirar(textoWheelie.rectTransform);

        PintarWheelie(false);
    }

    void Ocupar(RectTransform rt, float desde, float hasta)
    {
        rt.anchorMin = new Vector2(desde, 0f);
        rt.anchorMax = new Vector2(hasta, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
