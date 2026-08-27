using UnityEngine;
using UnityEngine.UI;
using TMPro;

// UI del menú para los datos persistentes: un campo "Tu nombre" (se guarda en
// PlayerPrefs y lo usa cada partida) y la tabla de ranking (Top 10 + tu puesto
// real si estás más abajo). Se crea en tiempo de ejecución desde MenuManager y
// arma todo por código como hijo de PanelMenu.
//
// Provisional: cuando haya arte, esto pasa a ser objetos de UI en la escena.
public class MenuRanking : MonoBehaviour
{
    private const string ClaveNombre = "NombreJugador";
    private const int PuestosVisibles = 10;

    private TMP_InputField inputNombre;
    private TMP_Text[] filas;
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

        Transform padre = menu.panelMenu.transform;
        ConstruirCampoNombre(padre);
        ConstruirTabla(padre);
        Refrescar();
    }

    // ---- Campo de nombre ----

    void ConstruirCampoNombre(Transform padre)
    {
        GameObject root = new GameObject("CampoNombre", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)root.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(40f, -60f);
        rt.sizeDelta = new Vector2(360f, 56f);
        root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);

        // Etiqueta arriba del campo
        TMP_Text etiqueta = CrearTexto(rt, "Etiqueta", "Tu nombre:", 22f, TextAlignmentOptions.BottomLeft);
        etiqueta.color = Color.white;
        RectTransform ert = etiqueta.rectTransform;
        ert.anchorMin = new Vector2(0f, 1f);
        ert.anchorMax = new Vector2(1f, 1f);
        ert.pivot = new Vector2(0f, 0f);
        ert.anchoredPosition = new Vector2(0f, 4f);
        ert.sizeDelta = new Vector2(0f, 28f);

        inputNombre = root.AddComponent<TMP_InputField>();

        RectTransform area = CrearHijoEstirado(rt, "Text Area");
        area.offsetMin = new Vector2(12f, 6f);
        area.offsetMax = new Vector2(-12f, -6f);
        area.gameObject.AddComponent<RectMask2D>();

        TMP_Text placeholder = CrearTexto(area, "Placeholder", "Escribí tu nombre...", 24f, TextAlignmentOptions.Left);
        placeholder.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        placeholder.fontStyle = FontStyles.Italic;
        EstirarEnPadre(placeholder.rectTransform);

        TMP_Text texto = CrearTexto(area, "Text", "", 24f, TextAlignmentOptions.Left);
        texto.color = Color.black;
        EstirarEnPadre(texto.rectTransform);

        inputNombre.textViewport = area;
        inputNombre.textComponent = texto;
        inputNombre.placeholder = placeholder;
        inputNombre.characterLimit = 12;
        inputNombre.text = PlayerPrefs.GetString(ClaveNombre, "");
        inputNombre.onEndEdit.AddListener(GuardarNombre);
    }

    void GuardarNombre(string valor)
    {
        PlayerPrefs.SetString(ClaveNombre, valor.Trim());
        Refrescar();
    }

    // ---- Tabla de ranking ----

    void ConstruirTabla(Transform padre)
    {
        GameObject cont = new GameObject("PanelRanking", typeof(RectTransform), typeof(Image));
        RectTransform crt = (RectTransform)cont.transform;
        crt.SetParent(padre, false);
        crt.anchorMin = new Vector2(1f, 0.5f);
        crt.anchorMax = new Vector2(1f, 0.5f);
        crt.pivot = new Vector2(1f, 0.5f);
        crt.anchoredPosition = new Vector2(-40f, 0f);
        crt.sizeDelta = new Vector2(440f, 640f);
        cont.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        TMP_Text titulo = CrearFila(crt, "Titulo", 16f, 32f, FontStyles.Bold);
        titulo.text = "RANKING";
        titulo.alignment = TextAlignmentOptions.Center;

        lineaTuPuesto = CrearFila(crt, "TuPuesto", 58f, 22f, FontStyles.Italic);
        lineaTuPuesto.alignment = TextAlignmentOptions.Center;

        filas = new TMP_Text[PuestosVisibles];
        for (int i = 0; i < PuestosVisibles; i++)
            filas[i] = CrearFila(crt, "Fila" + (i + 1), 100f + i * 50f, 26f, FontStyles.Normal);

        CrearBotonReiniciar(crt);
    }

    void CrearBotonReiniciar(RectTransform contenedor)
    {
        GameObject go = new GameObject("BotonReiniciar", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(contenedor, false);
        rt.anchorMin = new Vector2(0.5f, 0f);   // centro-abajo del panel de ranking
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 1f);        // cuelga por debajo del panel
        rt.anchoredPosition = new Vector2(0f, -10f);
        rt.sizeDelta = new Vector2(300f, 46f);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.72f, 0.15f, 0.15f, 0.95f);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(ReiniciarTabla);

        TMP_Text label = CrearTexto(rt, "Texto", "Reiniciar tabla", 22f, TextAlignmentOptions.Center);
        label.color = Color.white;
        label.fontStyle = FontStyles.Bold;
        EstirarEnPadre(label.rectTransform);
    }

    void ReiniciarTabla()
    {
        RankingData.Instance.BorrarTodo();
        Refrescar();
    }

    TMP_Text CrearFila(RectTransform padre, string nombre, float offsetSuperior, float tamano, FontStyles estilo)
    {
        TMP_Text t = CrearTexto(padre, nombre, "", tamano, TextAlignmentOptions.Left);
        t.fontStyle = estilo;
        t.color = Color.white;

        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-40f, tamano + 12f);         // 20 px de margen a cada lado
        rt.anchoredPosition = new Vector2(0f, -offsetSuperior);
        return t;
    }

    public void Refrescar()
    {
        RankingData data = RankingData.Instance;
        var entradas = data.Entradas;

        for (int i = 0; i < filas.Length; i++)
        {
            if (i < entradas.Count)
                filas[i].text = (i + 1) + ".  " + entradas[i].nombre + "   " + entradas[i].puntaje;
            else
                filas[i].text = (i + 1) + ".  —";
        }

        string nombre = PlayerPrefs.GetString(ClaveNombre, "");
        int puesto = data.PuestoDe(nombre);
        lineaTuPuesto.text = (puesto > PuestosVisibles) ? "Estás en el puesto " + puesto : "";
    }

    // ---- Helpers de UI ----

    TMP_Text CrearTexto(Transform padre, string nombre, string contenido, float tamano, TextAlignmentOptions alineacion)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);

        TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = contenido;
        txt.fontSize = tamano;
        txt.alignment = alineacion;
        txt.raycastTarget = false;
        return txt;
    }

    RectTransform CrearHijoEstirado(Transform padre, string nombre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        EstirarEnPadre(rt);
        return rt;
    }

    void EstirarEnPadre(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
