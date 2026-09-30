using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Pausa de la partida, armada por código con las piezas de EstiloUI. La crea
// MenuManager en Start() y registra el panel en MenuManager.panelPausa.
//
// Se activa con la tecla Escape o con el botón de pausa que este script agrega en
// la esquina superior derecha del PanelJuego. El panel es un velo oscuro sobre el
// juego congelado, con 3 botones como los del menú principal:
//   Continuar      -> MenuManager.Reanudar()
//   Opciones       -> MenuManager.MostrarOpciones()  (el mismo PanelOpciones que en
//                     el menú; su botón "Volver" regresa a la pausa)
//   Salir al menú  -> MenuManager.VolverAlMenu()
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
        // Time.timeScale 0 (Update sigue corriendo).
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // Escape es siempre "volver un paso": desde Probar cámara a Opciones, desde
        // Opciones a donde se abrió (menú o pausa), y en la partida alterna la pausa.
        if (menu.panelCamara != null && menu.panelCamara.activeSelf)
            menu.VolverDePruebaCamara();
        else if (menu.panelOpciones != null && menu.panelOpciones.activeSelf)
            menu.VolverDeOpciones();
        else
            menu.AlternarPausa();
    }

    // ---- Panel de pausa ----

    GameObject ConstruirPanel(Transform canvas)
    {
        // Velo translúcido: se sigue viendo la ruta congelada detrás.
        RectTransform pantalla = EstiloUI.CrearPantalla(canvas, "PanelPausa", false);

        TMP_Text titulo = EstiloUI.CrearTitulo(pantalla, "PAUSA", 110f);
        RectTransform trt = titulo.rectTransform;
        trt.anchorMin = new Vector2(0.5f, 0.5f);
        trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0f, 220f);

        // Los tres botones, del mismo tamaño y separación que los del menú.
        Boton(pantalla, "Continuar", EstiloUI.TipoBoton.Principal, () => menu.Reanudar(), 50f);
        Boton(pantalla, "Opciones", EstiloUI.TipoBoton.Secundario, () => menu.MostrarOpciones(), -60f);
        Boton(pantalla, "Salir al menú", EstiloUI.TipoBoton.Apagado, () => menu.VolverAlMenu(), -170f);

        return pantalla.gameObject;
    }

    void Boton(RectTransform padre, string texto, EstiloUI.TipoBoton tipo,
               UnityEngine.Events.UnityAction alClickear, float y)
    {
        Button b = EstiloUI.CrearBoton(padre, texto, tipo, alClickear, 88f, 30f);
        RectTransform rt = (RectTransform)b.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(480f, 88f);
    }

    // Botón de pausa en la esquina superior derecha del PanelJuego. Aparece y
    // desaparece con ese panel, igual que el puntaje del HUD. El ícono son dos
    // barras dibujadas (no el texto "II"), así se ve nítido a cualquier tamaño.
    void AgregarBotonPausaAlHud()
    {
        if (menu.panelJuego.transform.Find("BotonPausaHud") != null) return;   // ya existe

        Button b = EstiloUI.CrearBoton(menu.panelJuego.transform, "", EstiloUI.TipoBoton.Secundario,
            () => menu.Pausar(), 72f);
        b.gameObject.name = "BotonPausaHud";

        RectTransform rt = (RectTransform)b.transform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-28f, -28f);
        rt.sizeDelta = new Vector2(72f, 72f);

        Barra(rt, -9f);
        Barra(rt, 9f);
    }

    void Barra(RectTransform padre, float x)
    {
        GameObject go = new GameObject("Barra", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta = new Vector2(8f, 28f);

        Image img = go.GetComponent<Image>();
        img.color = EstiloUI.TintaClara;
        img.raycastTarget = false;
    }
}
