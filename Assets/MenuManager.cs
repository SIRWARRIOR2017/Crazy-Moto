using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelJuego;
    public GameObject panelGameOver;

    void Start()
    {
        // Si venimos de apretar "Jugar", arrancamos jugando directo.
        // Si no (primera vez que abre, o volvió al menú), mostramos el menú.
        if (PlayerPrefs.GetInt("IrAJugar", 0) == 1)
        {
            PlayerPrefs.SetInt("IrAJugar", 0);
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
        PlayerPrefs.SetInt("IrAJugar", 1);
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
        PlayerPrefs.SetInt("IrAJugar", 1);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Botón MENÚ (recarga y muestra el menú)
    public void VolverAlMenu()
    {
        PlayerPrefs.SetInt("IrAJugar", 0);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}