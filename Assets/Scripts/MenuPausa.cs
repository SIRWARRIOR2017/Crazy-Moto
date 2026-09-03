using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Pausa de la partida, armada por código (mismo patrón que MenuOpciones y
// MenuRanking). La crea MenuManager en Start() y registra el panel en
// MenuManager.panelPausa.
//
// Se activa con la tecla Escape o con el botón "II" que este script agrega en la
// esquina superior derecha del PanelJuego. El panel muestra 3 botones centrados:
//   Continuar -> MenuManager.Reanudar()
//   Opciones  -> MenuManager.MostrarOpciones()  (el mismo PanelOpciones que en el
//                menú; su botón "Volver" regresa a la pausa)
//   Salir     -> MenuManager.VolverAlMenu()     (vuelve al menú principal)
//
// Provisional: cuando haya arte, esto pasa a ser un panel real en la escena.
public class MenuPausa : MonoBehaviour
{
    private MenuManager menu;

    void Start()
    {
        menu = FindAnyObjectByType<MenuManager>();
        if (menu == null || menu.panelMenu == null || menu.panelJuego == null)
        {
            Debug.LogError("MenuPausa: no encontré el MenuManager o sus paneles. Me desactivo.");
            enabled = false;
            return;
        }

        Transform canvas = menu.panelMenu.transform.parent;
        GameObject panel = ConstruirPanel(canvas);
        panel.SetActive(false);
        menu.panelPausa = panel;

        AgregarBotonPausaAlHud();
    }

    void Update()
    {
        if (menu == null) return;

        // Único lugar del proyecto donde se lee Escape. Funciona con
        // Time.timeScale 0 (Update sigue corriendo). El día del manubrio de
        // Arduino, la pausa puede pasar a uno de sus botones cambiando solo esto.
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // Si estamos dentro de Opciones, Escape hace de "Volver"; si no, alterna
        // la pausa de la partida.
        if (menu.panelOpciones != null && menu.panelOpciones.activeSelf)
            menu.VolverDeOpciones();
        else
            menu.AlternarPausa();
    }

    // ---- Panel de pausa ----

    GameObject ConstruirPanel(Transform canvas)
    {
        // Fondo a pantalla completa.
        GameObject panel = new GameObject("PanelPausa", typeof(RectTransform), typeof(Image));
        RectTransform prt = (RectTransform)panel.transform;
        prt.SetParent(canvas, false);
        Estirar(prt);
        panel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

        // Título.
        TMP_Text titulo = CrearTexto(prt, "Titulo", "PAUSA", 56f, FontStyles.Bold);
        RectTransform trt = titulo.rectTransform;
        trt.anchorMin = new Vector2(0.5f, 0.5f);
        trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0f, 150f);
        trt.sizeDelta = new Vector2(600f, 90f);
        titulo.alignment = TextAlignmentOptions.Center;

        // Contenedor central con los 3 botones apilados.
        GameObject cont = new GameObject("Botones", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform crt = (RectTransform)cont.transform;
        crt.SetParent(prt, false);
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(0f, -20f);
        crt.sizeDelta = new Vector2(360f, 240f);

        VerticalLayoutGroup vlg = cont.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 16f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        CrearBoton(crt, "Continuar", new Color(0.20f, 0.45f, 0.25f, 0.95f), () => menu.Reanudar());
        CrearBoton(crt, "Opciones", new Color(0.20f, 0.20f, 0.26f, 0.95f), () => menu.MostrarOpciones());
        CrearBoton(crt, "Salir", new Color(0.55f, 0.16f, 0.16f, 0.95f), () => menu.VolverAlMenu());

        return panel;
    }

    // Botón "II" en la esquina superior derecha del PanelJuego. Aparece y
    // desaparece con ese panel, igual que el texto de puntaje del HUD.
    void AgregarBotonPausaAlHud()
    {
        if (menu.panelJuego.transform.Find("BotonPausaHud") != null) return;   // ya existe

        GameObject go = new GameObject("BotonPausaHud", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(menu.panelJuego.transform, false);
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-24f, -24f);
        rt.sizeDelta = new Vector2(64f, 64f);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.45f);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => menu.Pausar());

        TMP_Text lbl = CrearTexto(rt, "Texto", "II", 30f, FontStyles.Bold);
        lbl.alignment = TextAlignmentOptions.Center;
        Estirar(lbl.rectTransform);
    }

    // ---- Helpers (mismo estilo que MenuOpciones) ----

    void CrearBoton(Transform padre, string texto, Color color, UnityEngine.Events.UnityAction alClickear)
    {
        GameObject go = new GameObject("Boton " + texto, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 60f;
        le.minHeight = 60f;

        Image img = go.GetComponent<Image>();
        img.color = color;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(alClickear);

        TMP_Text lbl = CrearTexto(rt, "Texto", texto, 26f, FontStyles.Bold);
        lbl.alignment = TextAlignmentOptions.Center;
        Estirar(lbl.rectTransform);
    }

    TMP_Text CrearTexto(Transform padre, string nombre, string contenido, float tamano, FontStyles estilo)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);

        TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = contenido;
        txt.fontSize = tamano;
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
