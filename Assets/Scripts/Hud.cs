using UnityEngine;
using TMPro;

// HUD del juego. Se crea en tiempo de ejecución desde GameManager y arma sus
// textos por código (TextMeshPro) como hijos de los paneles de MenuManager, así
// aparecen y desaparecen junto con ellos.
public class Hud : MonoBehaviour
{
    private TMP_Text textoPuntaje;        // durante la partida (hijo de PanelJuego)
    private TMP_Text textoPuntajeFinal;   // en Game Over (hijo de PanelGameOver)

    void Start()
    {
        MenuManager menu = FindAnyObjectByType<MenuManager>();
        if (menu == null)
        {
            Debug.LogError("Hud: no encontré el MenuManager. Me desactivo.");
            enabled = false;
            return;
        }

        if (menu.panelJuego != null)
            textoPuntaje = CrearTexto(menu.panelJuego.transform, "TextoPuntaje",
                                      new Vector2(0f, -50f), 48f);

        if (menu.panelGameOver != null)
            textoPuntajeFinal = CrearTexto(menu.panelGameOver.transform, "TextoPuntajeFinal",
                                           new Vector2(0f, -110f), 40f);
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        if (textoPuntaje != null)
            textoPuntaje.text = "Puntaje: " + GameManager.Instance.puntaje;

        if (textoPuntajeFinal != null && GameManager.Instance.juegoTerminado)
            textoPuntajeFinal.text = "Puntaje final: " + GameManager.Instance.puntaje;
    }

    TMP_Text CrearTexto(Transform padre, string nombre, Vector2 posicion, float tamano)
    {
        GameObject go = new GameObject(nombre);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = new Vector2(600f, 80f);

        TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
        txt.alignment = TextAlignmentOptions.Top;
        txt.fontSize = tamano;
        txt.color = Color.white;
        txt.fontStyle = FontStyles.Bold;
        txt.raycastTarget = false;
        txt.text = "";
        return txt;
    }
}
