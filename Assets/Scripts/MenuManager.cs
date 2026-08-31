using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelJuego;
    public GameObject panelGameOver;

    // El panel de Opciones se arma por código (ver MenuOpciones) y se registra
    // acá al crearse, así que no aparece en el Inspector.
    [HideInInspector] public GameObject panelOpciones;

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

        // La tabla de ranking y el panel de Opciones se arman por código; se
        // crean acá para que existan en cada carga de escena.
        if (FindAnyObjectByType<MenuRanking>() == null)
            new GameObject("MenuRanking").AddComponent<MenuRanking>();

        if (FindAnyObjectByType<MenuOpciones>() == null)
            new GameObject("MenuOpciones").AddComponent<MenuOpciones>();

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
        if (panelOpciones != null) panelOpciones.SetActive(false);

        // La música suena solo acá, en el menú.
        if (AudioManager.Instance != null)
            AudioManager.Instance.ReproducirMusicaMenu();
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
        if (panelOpciones != null) panelOpciones.SetActive(false);

        // Durante la partida no hay música (por las dudas la cortamos).
        if (AudioManager.Instance != null)
            AudioManager.Instance.DetenerMusica();
    }

    // Botón SALIR
    public void Salir()
    {
        Application.Quit();
        Debug.Log("Salir del juego"); // en el editor no cierra, pero verás este mensaje
    }

    // ---- OPCIONES ----
    // Botón OPCIONES del menú (cableado por OnClick en la escena).
    public void MostrarOpciones()
    {
        if (panelOpciones == null) return;
        panelMenu.SetActive(false);
        panelOpciones.SetActive(true);
    }

    // Botón VOLVER dentro del panel de Opciones (cableado por código en MenuOpciones).
    public void VolverDeOpciones()
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);
        MostrarMenu();
    }

    // ---- GAME OVER ----
    // La llama el GameManager cuando perdés
    public void MostrarGameOver()
    {
        panelMenu.SetActive(false);
        panelJuego.SetActive(false);
        panelGameOver.SetActive(true);
        if (panelOpciones != null) panelOpciones.SetActive(false);
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
