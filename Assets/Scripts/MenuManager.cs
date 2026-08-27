using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelJuego;
    public GameObject panelGameOver;

    // Flag en memoria (no en disco, a diferencia de PlayerPrefs): sobrevive a la
    // recarga de escena, pero nunca a un cierre del juego. Así, si la app se
    // cierra de golpe justo después de apretar "Jugar", el próximo arranque
    // siempre muestra el menú en vez de quedar pegado arrancando en partida.
    private static bool irAJugar = false;

    void Start()
    {
        if (panelMenu == null || panelJuego == null || panelGameOver == null)
        {
            Debug.LogError("MenuManager: falta asignar uno o más paneles en el Inspector.");
            return;
        }

        // Si venimos de apretar "Jugar", arrancamos jugando directo.
        // Si no (primera vez que abre, o volvió al menú), mostramos el menú.
        if (irAJugar)
        {
            irAJugar = false;
            EmpezarJuego();
        }
        else
        {
            MostrarMenu();
        }
    }

    // ---- MENÚ ----
    public void MostrarMenu()
    {
        Time.timeScale = 0f;            // el juego queda pausado en el menú
        panelMenu.SetActive(true);
        panelJuego.SetActive(false);
        panelGameOver.SetActive(false);
    }

    // Botón JUGAR (recarga la escena para empezar limpio)
    public void Jugar()
    {
        irAJugar = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Arranca la partida (sin recargar, cuando ya venimos de "Jugar")
    void EmpezarJuego()
    {
        Time.timeScale = 1f;
        panelMenu.SetActive(false);
        panelJuego.SetActive(true);
        panelGameOver.SetActive(false);
    }

    // Botón SALIR
    public void Salir()
    {
        Application.Quit();
        Debug.Log("Salir del juego"); // en el editor no cierra, pero verás este mensaje
    }

    // ---- GAME OVER ----
    // La llama el GameManager cuando perdés
    public void MostrarGameOver()
    {
        panelMenu.SetActive(false);
        panelJuego.SetActive(false);
        panelGameOver.SetActive(true);
    }

    // Botón VOLVER A JUGAR (recarga y arranca jugando directo)
    public void VolverAJugar()
    {
        irAJugar = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Botón MENÚ (recarga y muestra el menú)
    public void VolverAlMenu()
    {
        irAJugar = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}