using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// HUD del juego. Se crea en tiempo de ejecución desde GameManager y arma por
// código, con las piezas de EstiloUI, dos cosas que cuelgan de los paneles de
// MenuManager (así aparecen y desaparecen junto con ellos):
//   - PanelJuego:    el marcador de puntaje, arriba al centro. El número se pone
//                    magenta mientras estás sumando (o sea, haciendo wheelie).
//   - PanelGameOver: la tarjeta con el puntaje final y tu puesto en el ranking.
//                    Si el puntaje entró al Top 10, la tarjeta crece y pide el
//                    nombre antes de guardarlo (ver GameManager.esperandoNombre).
public class Hud : MonoBehaviour
{
    // Tamaño y posición de la tarjeta del Game Over: normal, y cuando pide el
    // nombre (crece hacia arriba, así no tapa los botones de abajo).
    private static readonly Vector2 TamResultado = new Vector2(560f, 230f);
    private static readonly Vector2 PosResultado = new Vector2(0f, 80f);
    private static readonly Vector2 TamPidiendoNombre = new Vector2(560f, 280f);
    private static readonly Vector2 PosPidiendoNombre = new Vector2(0f, 105f);

    private TMP_Text textoPuntaje;        // durante la partida (hijo de PanelJuego)
    private RectTransform tarjetaResultado;
    private TMP_Text rotuloResultado;
    private TMP_Text textoPuntajeFinal;   // en Game Over (hijo de PanelGameOver)
    private TMP_Text textoPuesto;
    private GameObject filaNombre;
    private TMP_InputField inputNombre;
    private bool resultadoMostrado;

    private PlayerController jugador;

    void Start()
    {
        MenuManager menu = FindAnyObjectByType<MenuManager>();
        if (menu == null)
        {
            Debug.LogError("Hud: no encontré el MenuManager. Me desactivo.");
            enabled = false;
            return;
        }

        jugador = FindAnyObjectByType<PlayerController>();

        if (menu.panelJuego != null)
            ConstruirMarcador(menu.panelJuego.transform);

        if (menu.panelGameOver != null)
            ConstruirResultado(menu.panelGameOver.transform);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (textoPuntaje != null)
        {
            textoPuntaje.text = gm.puntaje.ToString("N0");

            bool sumando = !gm.juegoTerminado && jugador != null && jugador.haciendoWheelie;
            textoPuntaje.color = sumando ? EstiloUI.Magenta : EstiloUI.Tinta;
        }

        // El resultado se arma una sola vez: cuando termina la partida el ranking
        // ya se guardó (GameManager.GameOver lo hace antes de mostrar el panel),
        // salvo que el puntaje haya entrado al Top 10 y esté esperando el nombre.
        if (gm.juegoTerminado && !resultadoMostrado && textoPuntajeFinal != null)
        {
            resultadoMostrado = true;
            textoPuntajeFinal.text = gm.puntaje.ToString("N0");

            if (gm.esperandoNombre)
                PedirNombre();
            else
                MostrarPuesto(gm.puntaje, false);
        }
    }

    // El puesto de ESTE puntaje, no el del nombre guardado en Opciones (antes,
    // con 0 puntos, decía "Estás en el puesto 1" por el récord de otra partida).
    void MostrarPuesto(int puntaje, bool recienGuardado)
    {
        if (puntaje <= 0)
        {
            textoPuesto.text = "Hacé wheelie para sumar puntos";
            return;
        }

        int puesto = RankingData.Instance.PuestoDePuntaje(puntaje);
        textoPuesto.text = (recienGuardado ? "¡Guardado! " : "") + "Quedaste en el puesto " + puesto + " del ranking";
    }

    void PedirNombre()
    {
        tarjetaResultado.sizeDelta = TamPidiendoNombre;
        tarjetaResultado.anchoredPosition = PosPidiendoNombre;
        rotuloResultado.text = "¡ENTRASTE AL TOP 10!";
        rotuloResultado.color = EstiloUI.Magenta;
        textoPuesto.text = "";

        filaNombre.SetActive(true);
        inputNombre.text = "";
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(inputNombre.gameObject);
        inputNombre.ActivateInputField();   // se puede escribir sin hacer clic
    }

    // Botón "Guardar" o Enter en el campo del nombre.
    void GuardarNombre()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.esperandoNombre) return;

        gm.GuardarConNombre(inputNombre.text);

        filaNombre.SetActive(false);
        tarjetaResultado.sizeDelta = TamResultado;
        tarjetaResultado.anchoredPosition = PosResultado;
        rotuloResultado.text = "PUNTAJE FINAL";
        rotuloResultado.color = EstiloUI.Cyan;
        MostrarPuesto(gm.puntaje, true);
    }

    // Si se sale del Game Over (Continuar, Menú o cerrar el juego) sin apretar
    // Guardar, el puntaje igual se guarda con lo que se haya escrito. La llama
    // MenuManager antes de recargar la escena.
    public void GuardarNombreSiFalta()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.esperandoNombre)
            gm.GuardarConNombre(inputNombre != null ? inputNombre.text : "");
    }

    void OnApplicationQuit()
    {
        GuardarNombreSiFalta();
    }

    // Marcador compacto arriba al centro, sobre una tarjeta oscura para que se
    // lea contra el cielo claro del día.
    void ConstruirMarcador(Transform panel)
    {
        RectTransform tarjeta = Tarjeta(panel, "Marcador", new Vector2(300f, 104f));
        tarjeta.anchorMin = new Vector2(0.5f, 1f);
        tarjeta.anchorMax = new Vector2(0.5f, 1f);
        tarjeta.pivot = new Vector2(0.5f, 1f);
        tarjeta.anchoredPosition = new Vector2(0f, -28f);

        TMP_Text rotulo = EstiloUI.CrearTexto(tarjeta, "Rotulo", "PUNTAJE", EstiloUI.Negrita, 17f, EstiloUI.Cyan,
            TextAlignmentOptions.Center);
        rotulo.characterSpacing = EstiloUI.EspaciadoRotulo;
        Arriba(rotulo.rectTransform, 12f, 24f);

        textoPuntaje = EstiloUI.CrearTexto(tarjeta, "Puntaje", "0", EstiloUI.Negrita, 52f, EstiloUI.Tinta,
            TextAlignmentOptions.Center);
        Abajo(textoPuntaje.rectTransform, 8f, 62f);
    }

    // Tarjeta del Game Over, entre el título y los botones.
    void ConstruirResultado(Transform panel)
    {
        RectTransform tarjeta = Tarjeta(panel, "Resultado", TamResultado);
        tarjeta.anchorMin = new Vector2(0.5f, 0.5f);
        tarjeta.anchorMax = new Vector2(0.5f, 0.5f);
        tarjeta.pivot = new Vector2(0.5f, 0.5f);
        tarjeta.anchoredPosition = PosResultado;
        tarjetaResultado = tarjeta;

        TMP_Text rotulo = EstiloUI.CrearTexto(tarjeta, "Rotulo", "PUNTAJE FINAL", EstiloUI.Negrita, 21f, EstiloUI.Cyan,
            TextAlignmentOptions.Center);
        rotulo.characterSpacing = EstiloUI.EspaciadoRotulo;
        Arriba(rotulo.rectTransform, 22f, 30f);
        rotuloResultado = rotulo;

        textoPuntajeFinal = EstiloUI.CrearTexto(tarjeta, "Puntaje", "0", EstiloUI.Titular, 96f, Color.white,
            TextAlignmentOptions.Center);
        textoPuntajeFinal.enableVertexGradient = true;
        textoPuntajeFinal.colorGradient = new VertexGradient(EstiloUI.Crema, EstiloUI.Crema, EstiloUI.Magenta, EstiloUI.Magenta);
        RectTransform prt = textoPuntajeFinal.rectTransform;
        prt.anchorMin = new Vector2(0f, 0.5f);
        prt.anchorMax = new Vector2(1f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(0f, 110f);
        prt.anchoredPosition = new Vector2(0f, 4f);

        textoPuesto = EstiloUI.CrearTexto(tarjeta, "Puesto", "", EstiloUI.Media, 21f, EstiloUI.TintaSuave,
            TextAlignmentOptions.Center);
        Abajo(textoPuesto.rectTransform, 20f, 30f);

        // Campo + botón para el nombre. Sólo se ve si el puntaje entró al Top 10.
        filaNombre = new GameObject("FilaNombre", typeof(RectTransform));
        RectTransform frt = (RectTransform)filaNombre.transform;
        frt.SetParent(tarjeta, false);
        Abajo(frt, 18f, 52f);
        frt.sizeDelta = new Vector2(-48f, 52f);   // 24 px de margen a cada lado

        inputNombre = EstiloUI.CrearCampoTexto(frt, "Escribí tu nombre...", 12);
        RectTransform irt = (RectTransform)inputNombre.transform;
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = Vector2.zero;
        irt.offsetMax = new Vector2(-176f, 0f);
        inputNombre.onSubmit.AddListener(_ => GuardarNombre());

        Button guardar = EstiloUI.CrearBoton(frt, "Guardar", EstiloUI.TipoBoton.Principal, GuardarNombre, 52f, 22f);
        RectTransform brt = (RectTransform)guardar.transform;
        brt.anchorMin = new Vector2(1f, 0f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(1f, 0.5f);
        brt.sizeDelta = new Vector2(164f, 0f);
        brt.anchoredPosition = Vector2.zero;

        filaNombre.SetActive(false);
    }

    // ---- Ayudas ----

    RectTransform Tarjeta(Transform padre, string nombre, Vector2 tamano)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.sizeDelta = tamano;

        Image img = go.GetComponent<Image>();
        img.color = EstiloUI.FondoPanel;
        img.raycastTarget = false;
        EstiloUI.AgregarMarco(rt, EstiloUI.BordePanel);
        return rt;
    }

    // Franja de 'alto' px pegada arriba, a 'margen' px del borde.
    void Arriba(RectTransform rt, float margen, float alto)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, alto);
        rt.anchoredPosition = new Vector2(0f, -margen);
    }

    // Franja de 'alto' px pegada abajo, a 'margen' px del borde.
    void Abajo(RectTransform rt, float margen, float alto)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, alto);
        rt.anchoredPosition = new Vector2(0f, margen);
    }
}
