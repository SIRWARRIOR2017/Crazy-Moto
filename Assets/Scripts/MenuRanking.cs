using UnityEngine;
using UnityEngine.UI;
using TMPro;

// UI del menú para el ranking: la tabla de Top 10 (más una línea "Estás en el
// puesto N" si el jugador quedó afuera). Se crea en tiempo de ejecución desde
// MenuManager y arma todo por código como hijo de PanelMenu.
//
// El campo "Tu nombre" y el botón "Reiniciar tabla" viven ahora en el panel de
// Opciones (ver MenuOpciones), que llama a Refrescar() cuando cambian.
// Provisional: cuando haya arte, esto pasa a ser objetos de UI en la escena.
public class MenuRanking : MonoBehaviour
{
    private const string ClaveNombre = "NombreJugador";
    private const int PuestosVisibles = 10;

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

        ConstruirTabla(menu.panelMenu.transform);
        Refrescar();
    }

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

    // La llaman MenuOpciones (al cambiar el nombre o reiniciar la tabla) y el
    // propio Start.
    public void Refrescar()
    {
        if (filas == null) return;

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
}
