using UnityEngine;
using UnityEngine.UI;
using TMPro;

// UI del menú para el ranking: la tabla de Top 10 (más una línea "Estás en el
// puesto N" si el jugador quedó afuera). Se crea en tiempo de ejecución desde
// MenuManager y arma todo por código como hijo de PanelMenu.
//
// El campo "Tu nombre" y el botón "Reiniciar tabla" viven en el panel de
// Opciones (ver MenuOpciones), que llama a Refrescar() cuando cambian.
//
// Los colores y las tipografías salen de EstiloUI, así la tabla combina con el
// resto del menú, que está armado a mano en la escena.
public class MenuRanking : MonoBehaviour
{
    private const string ClaveNombre = "NombreJugador";
    private const int PuestosVisibles = 10;

    private TMP_Text[] puestos;
    private TMP_Text[] nombres;
    private TMP_Text[] puntajes;
    private Image[] fondosFila;
    private TMP_Text lineaTuPuesto;

    void Start()
    {
        MenuManager menu = FindAnyObjectByType<MenuManager>();
        if (menu == null || menu.panelMenu == null)
        {
            Debug.LogError("MenuRanking: no encontré el MenuManager o su panelMenu. Me desactivo.");
            enabled = false;
            return;
        }

        ConstruirTabla(menu.panelMenu.transform);
        Refrescar();
    }

    void ConstruirTabla(Transform padre)
    {
        // --- Panel: pegado al borde derecho, centrado en vertical ---
        GameObject panel = new GameObject("PanelRanking", typeof(RectTransform), typeof(Image));
        RectTransform prt = (RectTransform)panel.transform;
        prt.SetParent(padre, false);
        prt.anchorMin = new Vector2(1f, 0.5f);
        prt.anchorMax = new Vector2(1f, 0.5f);
        prt.pivot = new Vector2(1f, 0.5f);
        prt.anchoredPosition = new Vector2(-76f, 0f);
        prt.sizeDelta = new Vector2(500f, 664f);

        Image fondo = panel.GetComponent<Image>();
        fondo.sprite = null;
        fondo.color = EstiloUI.FondoPanel;
        fondo.raycastTarget = false;

        // Borde de neón: un marco hueco estirado sobre el panel.
        Image marco = CrearImagen(prt, "Marco", EstiloUI.Marco, EstiloUI.BordePanel);
        marco.type = Image.Type.Sliced;
        Estirar(marco.rectTransform);

        // --- Contenido apilado ---
        GameObject contenido = new GameObject("Contenido", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform crt = (RectTransform)contenido.transform;
        crt.SetParent(prt, false);
        Estirar(crt);

        VerticalLayoutGroup vlg = contenido.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(28, 28, 30, 26);
        vlg.spacing = 20f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        ConstruirEncabezado(crt);
        ConstruirFilas(crt);

        lineaTuPuesto = CrearTexto(crt, "TuPuesto", "");
        EstiloUI.Aplicar(lineaTuPuesto, EstiloUI.Media, EstiloUI.TamChico, EstiloUI.TintaApagada);
        lineaTuPuesto.fontStyle = FontStyles.Italic;
        lineaTuPuesto.alignment = TextAlignmentOptions.Left;
        AltoFijo(lineaTuPuesto.gameObject, 30f);
    }

    // "RANKING" a la izquierda, "TOP 10" a la derecha, con una línea abajo.
    void ConstruirEncabezado(Transform padre)
    {
        GameObject fila = new GameObject("Encabezado", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform rt = (RectTransform)fila.transform;
        rt.SetParent(padre, false);

        HorizontalLayoutGroup hlg = fila.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 0, 0, 14);
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.LowerLeft;

        TMP_Text titulo = CrearTexto(rt, "Titulo", "RANKING");
        EstiloUI.Aplicar(titulo, EstiloUI.Negrita, EstiloUI.TamEncabezado, EstiloUI.Cyan, EstiloUI.EspaciadoRotulo);
        titulo.alignment = TextAlignmentOptions.Left;
        Flexible(titulo.gameObject);

        TMP_Text top = CrearTexto(rt, "Top", "TOP 10");
        EstiloUI.Aplicar(top, EstiloUI.Negrita, 19f, EstiloUI.TintaApagada, 18f);
        top.alignment = TextAlignmentOptions.Right;

        AltoFijo(fila, 48f);

        // La línea de abajo del encabezado.
        Image linea = CrearImagen(rt.parent, "LineaEncabezado", null, new Color(EstiloUI.Cyan.r, EstiloUI.Cyan.g, EstiloUI.Cyan.b, 0.3f));
        AltoFijo(linea.gameObject, 2f);
        linea.transform.SetSiblingIndex(1);   // justo debajo del encabezado
    }

    void ConstruirFilas(Transform padre)
    {
        GameObject lista = new GameObject("Filas", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform rt = (RectTransform)lista.transform;
        rt.SetParent(padre, false);

        VerticalLayoutGroup vlg = lista.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        puestos = new TMP_Text[PuestosVisibles];
        nombres = new TMP_Text[PuestosVisibles];
        puntajes = new TMP_Text[PuestosVisibles];
        fondosFila = new Image[PuestosVisibles];

        for (int i = 0; i < PuestosVisibles; i++)
            CrearFila(rt, i);
    }

    void CrearFila(Transform padre, int i)
    {
        GameObject fila = new GameObject("Fila" + (i + 1), typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        RectTransform rt = (RectTransform)fila.transform;
        rt.SetParent(padre, false);

        Image fondo = fila.GetComponent<Image>();
        fondo.sprite = null;
        fondo.color = Color.clear;            // sólo la primera fila se pinta
        fondo.raycastTarget = false;
        fondosFila[i] = fondo;

        HorizontalLayoutGroup hlg = fila.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 16f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        puestos[i] = CrearTexto(rt, "Puesto", "");
        EstiloUI.Aplicar(puestos[i], EstiloUI.Negrita, 23f, EstiloUI.TintaApagada);
        puestos[i].alignment = TextAlignmentOptions.Left;
        AnchoFijo(puestos[i].gameObject, 44f);

        nombres[i] = CrearTexto(rt, "Nombre", "");
        EstiloUI.Aplicar(nombres[i], EstiloUI.Media, EstiloUI.TamTexto, EstiloUI.Tinta);
        nombres[i].alignment = TextAlignmentOptions.Left;
        // Un nombre largo se recorta con puntos suspensivos en vez de romper la fila.
        nombres[i].textWrappingMode = TextWrappingModes.NoWrap;
        nombres[i].overflowMode = TextOverflowModes.Ellipsis;
        Flexible(nombres[i].gameObject);

        puntajes[i] = CrearTexto(rt, "Puntaje", "");
        EstiloUI.Aplicar(puntajes[i], EstiloUI.Negrita, EstiloUI.TamTexto, EstiloUI.TintaClara);
        puntajes[i].alignment = TextAlignmentOptions.Right;

        AltoFijo(fila, 46f);
    }

    // La llaman MenuOpciones (al cambiar el nombre o reiniciar la tabla) y el
    // propio Start.
    public void Refrescar()
    {
        if (puestos == null) return;

        RankingData data = RankingData.Instance;
        var entradas = data.Entradas;

        for (int i = 0; i < PuestosVisibles; i++)
        {
            bool hay = i < entradas.Count;
            bool primero = hay && i == 0;

            puestos[i].text = (i + 1).ToString();
            nombres[i].text = hay ? entradas[i].nombre : "—";
            puntajes[i].text = hay ? entradas[i].puntaje.ToString("N0") : "";

            // El primer puesto va resaltado en magenta; el resto, apagado.
            fondosFila[i].color = primero ? EstiloUI.FilaDestacada : Color.clear;
            puestos[i].color = primero ? EstiloUI.Magenta : EstiloUI.TintaApagada;
            nombres[i].color = hay ? EstiloUI.Tinta : EstiloUI.TintaApagada;
            puntajes[i].color = primero ? EstiloUI.Magenta : EstiloUI.TintaClara;
        }

        string nombre = PlayerPrefs.GetString(ClaveNombre, "");
        int puesto = data.PuestoDe(nombre);
        lineaTuPuesto.text = (puesto > PuestosVisibles) ? "Estás en el puesto " + puesto : "";
    }

    // ---- Ayudas ----

    TMP_Text CrearTexto(Transform padre, string nombre, string contenido)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);

        TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = contenido;
        return txt;
    }

    Image CrearImagen(Transform padre, string nombre, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    void AltoFijo(GameObject go, float alto)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.minHeight = alto;
        le.preferredHeight = alto;
    }

    void AnchoFijo(GameObject go, float ancho)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.minWidth = ancho;
        le.preferredWidth = ancho;
    }

    void Flexible(GameObject go)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
    }

    void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
