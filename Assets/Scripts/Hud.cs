using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD del juego. Se crea en tiempo de ejecución desde GameManager y arma por
// código, con las piezas de EstiloUI, dos cosas que cuelgan de los paneles de
// MenuManager (así aparecen y desaparecen junto con ellos):
//   - PanelJuego:    el marcador de puntaje, arriba al centro. El número se pone
//                    magenta mientras estás sumando (o sea, haciendo wheelie).
//   - PanelGameOver: la tarjeta con el puntaje final y tu puesto en el ranking.
public class Hud : MonoBehaviour
{
    private TMP_Text textoPuntaje;        // durante la partida (hijo de PanelJuego)
    private TMP_Text textoPuntajeFinal;   // en Game Over (hijo de PanelGameOver)
    private TMP_Text textoPuesto;
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
        // ya se guardó (GameManager.GameOver lo hace antes de mostrar el panel).
        if (gm.juegoTerminado && !resultadoMostrado && textoPuntajeFinal != null)
        {
            resultadoMostrado = true;
            textoPuntajeFinal.text = gm.puntaje.ToString("N0");

            int puesto = RankingData.Instance.PuestoDe(RankingData.NombreActual());
            textoPuesto.text = puesto > 0 ? "Estás en el puesto " + puesto + " del ranking" : "";
        }
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
        RectTransform tarjeta = Tarjeta(panel, "Resultado", new Vector2(560f, 230f));
        tarjeta.anchorMin = new Vector2(0.5f, 0.5f);
        tarjeta.anchorMax = new Vector2(0.5f, 0.5f);
        tarjeta.pivot = new Vector2(0.5f, 0.5f);
        tarjeta.anchoredPosition = new Vector2(0f, 80f);

        TMP_Text rotulo = EstiloUI.CrearTexto(tarjeta, "Rotulo", "PUNTAJE FINAL", EstiloUI.Negrita, 21f, EstiloUI.Cyan,
            TextAlignmentOptions.Center);
        rotulo.characterSpacing = EstiloUI.EspaciadoRotulo;
        Arriba(rotulo.rectTransform, 22f, 30f);

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
