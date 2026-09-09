using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Panel de Opciones del menú, armado por código (mismo patrón que MenuRanking).
// Lo crea MenuManager en Start() y lo registra en MenuManager.panelOpciones.
// Contenido, por secciones:
//   AUDIO     -> 3 sliders de volumen (general / música / efectos)
//   PERFIL    -> campo "Tu nombre (para el ranking)" -> PlayerPrefs("NombreJugador")
//   DATOS     -> botón "Reiniciar tabla de ranking"
//   CÁMARA    -> botón "Probar cámara" -> pantalla de MenuCamara
//   CONTROLES -> texto informativo (no editable)
//
// Provisional: cuando haya arte, esto pasa a ser un panel real en la escena.
public class MenuOpciones : MonoBehaviour
{
    private const string ClaveNombre = "NombreJugador";

    private MenuManager menu;
    private TMP_InputField inputNombre;

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

        AgregarBotonOpcionesAlMenu();
    }

    // El botón "Opciones" del menú principal se agrega por código (como el botón
    // "Reiniciar" que antes armaba MenuRanking): se clona "BotonJugar" para que
    // tenga el mismo estilo, se le cambia el texto y el OnClick, y se lo ubica
    // entre "Jugar" y "Salir".
    void AgregarBotonOpcionesAlMenu()
    {
        Transform menuT = menu.panelMenu.transform;
        Transform jugar = menuT.Find("BotonJugar");
        Transform salir = menuT.Find("BotonSalir");

        if (jugar == null || salir == null)
        {
            Debug.LogWarning("MenuOpciones: no encontré BotonJugar/BotonSalir; no agregué el botón Opciones.");
            return;
        }

        if (menuT.Find("BotonOpciones") != null) return;   // ya existe

        GameObject opciones = Instantiate(jugar.gameObject, jugar.parent);
        opciones.name = "BotonOpciones";
        opciones.transform.SetSiblingIndex(salir.GetSiblingIndex());

        TMP_Text label = opciones.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = "Opciones";

        Button btn = opciones.GetComponent<Button>();
        if (btn != null)
        {
            // Evento nuevo: descarta el listener heredado de "Jugar".
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(menu.MostrarOpciones);
        }

        // Reacomodar los 3 botones para que entren parejos.
        ((RectTransform)jugar).anchoredPosition = new Vector2(0f, 120f);
        ((RectTransform)opciones.transform).anchoredPosition = new Vector2(0f, 40f);
        ((RectTransform)salir).anchoredPosition = new Vector2(0f, -40f);
    }

    GameObject ConstruirPanel(Transform canvas)
    {
        // --- Fondo, pantalla completa ---
        GameObject panel = new GameObject("PanelOpciones", typeof(RectTransform), typeof(Image));
        RectTransform prt = (RectTransform)panel.transform;
        prt.SetParent(canvas, false);
        Estirar(prt);
        panel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

        // --- Título ---
        TMP_Text titulo = CrearTexto(prt, "Titulo", "OPCIONES", 48f, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform trt = titulo.rectTransform;
        trt.anchorMin = new Vector2(0.5f, 1f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -36f);
        trt.sizeDelta = new Vector2(600f, 70f);

        // --- Contenedor central con layout vertical ---
        GameObject cont = new GameObject("Contenido", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        RectTransform crt = (RectTransform)cont.transform;
        crt.SetParent(prt, false);
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(0f, -10f);
        crt.sizeDelta = new Vector2(760f, 780f);   // crece con la sección CÁMARA
        cont.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

        VerticalLayoutGroup vlg = cont.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.padding = new RectOffset(28, 28, 22, 22);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        // --- Sección AUDIO ---
        CrearEncabezado(crt, "AUDIO");
        CrearFilaSlider(crt, "Volumen general", PlayerPrefs.GetFloat(AudioManager.ClaveVolumenGeneral, 1f),
            v => { if (AudioManager.Instance != null) AudioManager.Instance.SetVolumenGeneral(v); });
        CrearFilaSlider(crt, "Volumen música", PlayerPrefs.GetFloat(AudioManager.ClaveVolumenMusica, 1f),
            v => { if (AudioManager.Instance != null) AudioManager.Instance.SetVolumenMusica(v); });
        CrearFilaSlider(crt, "Volumen efectos", PlayerPrefs.GetFloat(AudioManager.ClaveVolumenEfectos, 1f),
            v => { if (AudioManager.Instance != null) AudioManager.Instance.SetVolumenEfectos(v); });

        // --- Sección PERFIL ---
        CrearEncabezado(crt, "PERFIL");
        CrearFilaNombre(crt);

        // --- Sección DATOS ---
        CrearEncabezado(crt, "DATOS");
        CrearBoton(crt, "Reiniciar tabla de ranking", new Color(0.72f, 0.15f, 0.15f, 0.95f), () =>
        {
            RankingData.Instance.BorrarTodo();
            MenuRanking ranking = FindAnyObjectByType<MenuRanking>();
            if (ranking != null) ranking.Refrescar();
        });

        // --- Sección CÁMARA ---
        CrearEncabezado(crt, "CÁMARA");
        CrearBoton(crt, "Probar cámara", new Color(0.15f, 0.42f, 0.68f, 0.95f),
            () => menu.MostrarPruebaCamara());

        // --- Sección CONTROLES (informativa) ---
        CrearEncabezado(crt, "CONTROLES");
        TMP_Text info = CrearTexto(crt, "InfoControles",
            "Cámara:  doblar = tirar un brazo y empujar el otro  ·  wheelie = tirar los dos\n" +
            "Teclado:  mover = A / D  ·  wheelie = mantener Shift", 20f,
            TextAlignmentOptions.Left, FontStyles.Normal);
        info.color = new Color(0.85f, 0.85f, 0.85f);
        AgregarLayout(info.gameObject, 56f);

        // --- Botón Volver ---
        CrearBoton(crt, "Volver", new Color(0.2f, 0.2f, 0.26f, 0.95f), () => menu.VolverDeOpciones());

        return panel;
    }

    // ---- Filas ----

    void CrearEncabezado(Transform padre, string texto)
    {
        TMP_Text t = CrearTexto(padre, "Encabezado " + texto, texto, 26f, TextAlignmentOptions.Left, FontStyles.Bold);
        t.color = new Color(0.62f, 0.80f, 1f);
        AgregarLayout(t.gameObject, 34f);
    }

    void CrearFilaSlider(Transform padre, string etiqueta, float valor, UnityEngine.Events.UnityAction<float> alCambiar)
    {
        GameObject fila = new GameObject("Fila " + etiqueta, typeof(RectTransform));
        RectTransform frt = (RectTransform)fila.transform;
        frt.SetParent(padre, false);
        AgregarLayout(fila, 40f);

        TMP_Text lbl = CrearTexto(frt, "Etiqueta", etiqueta, 24f, TextAlignmentOptions.Left, FontStyles.Normal);
        RectTransform lrt = lbl.rectTransform;
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(0.42f, 1f);
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        Slider slider = CrearSlider(frt, Mathf.Clamp01(valor));
        RectTransform srt = (RectTransform)slider.transform;
        srt.anchorMin = new Vector2(0.45f, 0.2f);
        srt.anchorMax = new Vector2(1f, 0.8f);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;

        // El listener se agrega DESPUÉS de fijar el valor, así no se dispara al armar.
        slider.onValueChanged.AddListener(alCambiar);
    }

    void CrearFilaNombre(Transform padre)
    {
        GameObject fila = new GameObject("Fila Nombre", typeof(RectTransform));
        RectTransform frt = (RectTransform)fila.transform;
        frt.SetParent(padre, false);
        AgregarLayout(fila, 46f);

        TMP_Text lbl = CrearTexto(frt, "Etiqueta", "Tu nombre (para el ranking)", 24f,
            TextAlignmentOptions.Left, FontStyles.Normal);
        RectTransform lrt = lbl.rectTransform;
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(0.55f, 1f);
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        GameObject campo = new GameObject("CampoNombre", typeof(RectTransform), typeof(Image));
        RectTransform crt = (RectTransform)campo.transform;
        crt.SetParent(frt, false);
        crt.anchorMin = new Vector2(0.58f, 0.1f);
        crt.anchorMax = new Vector2(1f, 0.9f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;
        campo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.92f);

        inputNombre = campo.AddComponent<TMP_InputField>();

        GameObject area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        RectTransform art = (RectTransform)area.transform;
        art.SetParent(crt, false);
        art.anchorMin = Vector2.zero;
        art.anchorMax = Vector2.one;
        art.offsetMin = new Vector2(10f, 4f);
        art.offsetMax = new Vector2(-10f, -4f);

        TMP_Text placeholder = CrearTexto(art, "Placeholder", "Escribí tu nombre...", 22f,
            TextAlignmentOptions.Left, FontStyles.Italic);
        placeholder.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
        Estirar(placeholder.rectTransform);

        TMP_Text texto = CrearTexto(art, "Text", "", 22f, TextAlignmentOptions.Left, FontStyles.Normal);
        texto.color = Color.black;
        Estirar(texto.rectTransform);

        inputNombre.textViewport = art;
        inputNombre.textComponent = texto;
        inputNombre.placeholder = placeholder;
        inputNombre.characterLimit = 12;
        inputNombre.text = PlayerPrefs.GetString(ClaveNombre, "");
        inputNombre.onEndEdit.AddListener(GuardarNombre);
    }

    void GuardarNombre(string valor)
    {
        PlayerPrefs.SetString(ClaveNombre, valor.Trim());
        MenuRanking ranking = FindAnyObjectByType<MenuRanking>();
        if (ranking != null) ranking.Refrescar();
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

    // ---- Slider armado a mano (jerarquía estándar de UnityEngine.UI.Slider) ----

    Slider CrearSlider(Transform padre, float valor)
    {
        GameObject go = new GameObject("Slider", typeof(RectTransform));
        ((RectTransform)go.transform).SetParent(padre, false);

        GameObject bg = NuevoHijo(go.transform, "Background", typeof(Image));
        RectTransform bgrt = (RectTransform)bg.transform;
        bgrt.anchorMin = new Vector2(0f, 0.25f);
        bgrt.anchorMax = new Vector2(1f, 0.75f);
        bgrt.offsetMin = Vector2.zero;
        bgrt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        GameObject fillArea = NuevoHijo(go.transform, "Fill Area");
        RectTransform fart = (RectTransform)fillArea.transform;
        fart.anchorMin = new Vector2(0f, 0.25f);
        fart.anchorMax = new Vector2(1f, 0.75f);
        fart.offsetMin = new Vector2(10f, 0f);
        fart.offsetMax = new Vector2(-10f, 0f);

        GameObject fill = NuevoHijo(fillArea.transform, "Fill", typeof(Image));
        RectTransform fillrt = (RectTransform)fill.transform;
        fillrt.anchorMin = new Vector2(0f, 0f);
        fillrt.anchorMax = new Vector2(1f, 1f);
        fillrt.offsetMin = Vector2.zero;
        fillrt.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.95f, 1f);

        GameObject hsa = NuevoHijo(go.transform, "Handle Slide Area");
        RectTransform hsart = (RectTransform)hsa.transform;
        hsart.anchorMin = new Vector2(0f, 0f);
        hsart.anchorMax = new Vector2(1f, 1f);
        hsart.offsetMin = new Vector2(10f, 0f);
        hsart.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = NuevoHijo(hsa.transform, "Handle", typeof(Image));
        RectTransform hrt = (RectTransform)handle.transform;
        hrt.anchorMin = new Vector2(0f, 0f);
        hrt.anchorMax = new Vector2(0f, 1f);
        hrt.sizeDelta = new Vector2(24f, 0f);
        handle.GetComponent<Image>().color = Color.white;

        Slider slider = go.AddComponent<Slider>();
        slider.fillRect = fillrt;
        slider.handleRect = hrt;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(valor);
        return slider;
    }

    // ---- Helpers ----

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
