using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Panel de Opciones, armado por código con las piezas de EstiloUI. Lo crea
// MenuManager en Start() y lo registra en MenuManager.panelOpciones. Se abre desde
// el menú principal y desde la pausa (su "Volver" regresa a donde estabas).
//
// Dos tarjetas lado a lado:
//   izquierda -> AUDIO (3 volúmenes) y PERFIL (tu nombre para el ranking)
//   derecha   -> CÁMARA (Probar cámara), DATOS (reiniciar el ranking) y
//                CONTROLES (texto informativo, no editable)
public class MenuOpciones : MonoBehaviour
{
    // "Reiniciar tabla" pide un segundo clic antes de borrar: en la feria el
    // ranking tiene los puntajes de todos, y un clic sin querer los perdía.
    private const float SegundosParaConfirmar = 3f;

    private MenuManager menu;
    private TMP_InputField inputNombre;

    private const string TextoReiniciar = "REINICIAR TABLA DE RANKING";

    private TMP_Text textoReiniciar;
    private float confirmarHasta = -1f;   // mientras no venza, el próximo clic borra
    private float avisoHasta = -1f;       // hasta cuándo se muestra "TABLA REINICIADA"

    void Start()
    {
        menu = FindAnyObjectByType<MenuManager>();
        if (menu == null || menu.panelMenu == null)
        {
            Debug.LogError("MenuOpciones: no encontré el MenuManager o su panelMenu. Me desactivo.");
            enabled = false;
            return;
        }

        Transform canvas = menu.panelMenu.transform.parent;
        GameObject panel = ConstruirPanel(canvas);
        panel.SetActive(false);
        menu.panelOpciones = panel;

        VerificarBotonOpciones();
    }

    void Update()
    {
        // unscaledTime: Opciones se usa con el juego en timeScale 0 (menú o pausa).
        float ahora = Time.unscaledTime;

        if (confirmarHasta > 0f && ahora > confirmarHasta)
        {
            confirmarHasta = -1f;
            textoReiniciar.text = TextoReiniciar;
        }

        if (avisoHasta > 0f && ahora > avisoHasta)
        {
            avisoHasta = -1f;
            textoReiniciar.text = TextoReiniciar;
        }
    }

    // El botón "Opciones" del menú principal ES un objeto de la escena
    // (PanelMenu/BotonOpciones), igual que "Jugar" y "Salir", con su estilo puesto
    // y su OnClick cableado en el Inspector. Sólo queda el aviso por si alguien lo
    // borra de la escena sin querer.
    void VerificarBotonOpciones()
    {
        if (menu.panelMenu.transform.Find("BotonOpciones") == null)
            Debug.LogWarning("MenuOpciones: no está 'BotonOpciones' en PanelMenu. " +
                             "Sin él no se puede abrir Opciones desde el menú.");
    }

    GameObject ConstruirPanel(Transform canvas)
    {
        RectTransform pantalla = EstiloUI.CrearPantalla(canvas, "PanelOpciones", true);
        EstiloUI.CrearTitulo(pantalla, "OPCIONES");

        // --- Izquierda: lo que se ajusta ---
        RectTransform izq = EstiloUI.CrearTarjeta(pantalla, "TarjetaAjustes", new Vector2(700f, 450f), new Vector2(-370f, 40f));

        EstiloUI.CrearRotulo(izq, "AUDIO");
        FilaSlider(izq, "Volumen general", PlayerPrefs.GetFloat(AudioManager.ClaveVolumenGeneral, 1f),
            v => { if (AudioManager.Instance != null) AudioManager.Instance.SetVolumenGeneral(v); });
        FilaSlider(izq, "Música", PlayerPrefs.GetFloat(AudioManager.ClaveVolumenMusica, 1f),
            v => { if (AudioManager.Instance != null) AudioManager.Instance.SetVolumenMusica(v); });
        FilaSlider(izq, "Efectos", PlayerPrefs.GetFloat(AudioManager.ClaveVolumenEfectos, 1f),
            v => { if (AudioManager.Instance != null) AudioManager.Instance.SetVolumenEfectos(v); });

        EstiloUI.CrearRotulo(izq, "PERFIL");
        FilaNombre(izq);

        // --- Derecha: acciones e información ---
        RectTransform der = EstiloUI.CrearTarjeta(pantalla, "TarjetaMas", new Vector2(700f, 450f), new Vector2(370f, 40f));

        EstiloUI.CrearRotulo(der, "CÁMARA");
        EstiloUI.CrearBoton(der, "Probar cámara", EstiloUI.TipoBoton.Secundario,
            () => menu.MostrarPruebaCamara(), 60f, 22f);

        EstiloUI.CrearRotulo(der, "DATOS");
        Button reiniciar = EstiloUI.CrearBoton(der, "Reiniciar tabla de ranking", EstiloUI.TipoBoton.Peligro,
            PedirReinicio, 60f, 22f);
        textoReiniciar = reiniciar.GetComponentInChildren<TMP_Text>();

        EstiloUI.CrearRotulo(der, "CONTROLES");
        TMP_Text info = EstiloUI.CrearTexto(der, "InfoControles",
            "<color=#BDEFFF>Teclado:</color> A / D para moverte, Shift para el wheelie, Esc para pausar.\n" +
            "<color=#BDEFFF>Cámara:</color> tirá un brazo y empujá el otro para doblar; tirá los dos hacia el cuerpo para el wheelie.",
            EstiloUI.Media, 20f, EstiloUI.TintaSuave, TextAlignmentOptions.TopLeft);
        info.lineSpacing = 12f;
        EstiloUI.Alto(info.gameObject, 100f);

        // --- Volver, abajo al centro ---
        Button volver = EstiloUI.CrearBoton(pantalla, "Volver", EstiloUI.TipoBoton.Secundario,
            () => menu.VolverDeOpciones(), 72f, 26f);
        RectTransform vrt = (RectTransform)volver.transform;
        vrt.anchorMin = new Vector2(0.5f, 0f);
        vrt.anchorMax = new Vector2(0.5f, 0f);
        vrt.pivot = new Vector2(0.5f, 0f);
        vrt.anchoredPosition = new Vector2(0f, 90f);
        vrt.sizeDelta = new Vector2(420f, 72f);

        return pantalla.gameObject;
    }

    // ---- Filas ----

    void FilaSlider(Transform padre, string etiqueta, float valor, UnityEngine.Events.UnityAction<float> alCambiar)
    {
        RectTransform fila = Fila(padre, "Fila " + etiqueta, 52f);

        TMP_Text lbl = EstiloUI.CrearTexto(fila, "Etiqueta", etiqueta, EstiloUI.Media, 22f, EstiloUI.Tinta,
            TextAlignmentOptions.Left);
        Ocupar(lbl.rectTransform, 0f, 0.4f);

        Slider slider = EstiloUI.CrearSlider(fila, valor);
        Ocupar((RectTransform)slider.transform, 0.43f, 1f);

        // El listener se agrega DESPUÉS de fijar el valor, así no se dispara al armar.
        slider.onValueChanged.AddListener(alCambiar);
    }

    void FilaNombre(Transform padre)
    {
        RectTransform fila = Fila(padre, "Fila Nombre", 58f);

        TMP_Text lbl = EstiloUI.CrearTexto(fila, "Etiqueta", "Tu nombre", EstiloUI.Media, 22f, EstiloUI.Tinta,
            TextAlignmentOptions.Left);
        Ocupar(lbl.rectTransform, 0f, 0.4f);

        inputNombre = EstiloUI.CrearCampoTexto(fila, "Escribí tu nombre...", 12);
        Ocupar((RectTransform)inputNombre.transform, 0.43f, 1f);

        inputNombre.text = PlayerPrefs.GetString(RankingData.ClaveNombre, "");
        inputNombre.onEndEdit.AddListener(GuardarNombre);
    }

    void GuardarNombre(string valor)
    {
        PlayerPrefs.SetString(RankingData.ClaveNombre, valor.Trim());
        RefrescarRanking();
    }

    // Primer clic: pide confirmación. Segundo clic dentro de 3 s: borra.
    void PedirReinicio()
    {
        if (confirmarHasta < 0f)
        {
            confirmarHasta = Time.unscaledTime + SegundosParaConfirmar;
            avisoHasta = -1f;
            textoReiniciar.text = "¿SEGURO? TOCÁ DE NUEVO";
            return;
        }

        confirmarHasta = -1f;
        RankingData.Instance.BorrarTodo();
        RefrescarRanking();

        avisoHasta = Time.unscaledTime + 1.5f;   // el aviso se ve un rato y vuelve solo
        textoReiniciar.text = "TABLA REINICIADA";
    }

    void RefrescarRanking()
    {
        MenuRanking ranking = FindAnyObjectByType<MenuRanking>();
        if (ranking != null) ranking.Refrescar();
    }

    // ---- Ayudas de maquetado ----

    RectTransform Fila(Transform padre, string nombre, float alto)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        EstiloUI.Alto(go, alto);
        return rt;
    }

    // Ocupa la franja horizontal [desde, hasta] (0..1) de su fila, a lo alto.
    void Ocupar(RectTransform rt, float desde, float hasta)
    {
        rt.anchorMin = new Vector2(desde, 0f);
        rt.anchorMax = new Vector2(hasta, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
