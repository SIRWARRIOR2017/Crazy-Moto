using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelJuego;
    public GameObject panelGameOver;

    // El panel de Opciones y el de Pausa se arman por código (ver MenuOpciones y
    // MenuPausa) y se registran acá al crearse, así que no aparecen en el Inspector.
    [HideInInspector] public GameObject panelOpciones;
    [HideInInspector] public GameObject panelPausa;

    // Flag en memoria (no en disco, a diferencia de PlayerPrefs): sobrevive a la
    // recarga de escena, pero nunca a un cierre del juego. Así, si la app se
    // cierra de golpe justo después de apretar "Jugar", el próximo arranque
    // siempre muestra el menú en vez de quedar pegado arrancando en partida.
    private static bool irAJugar = false;

    // Distingue "el juego está en Time.timeScale 0 porque el jugador puso pausa"
    // de los otros dos motivos (estamos en el menú, o el juego terminó). Sin esta
    // variable los tres estados se pisarían entre sí (ver punto 1.7 de
    // docs/AUDITORIA.md).
    public bool estaPausado { get; private set; }

    void Start()
    {
        if (panelMenu == null || panelJuego == null || panelGameOver == null)
        {
            Debug.LogError("MenuManager: falta asignar uno o más paneles en el Inspector.");
            return;
        }

        // La tabla de ranking, el panel de Opciones y el de Pausa se arman por
        // código; se crean acá para que existan en cada carga de escena.
        if (FindAnyObjectByType<MenuRanking>() == null)
            new GameObject("MenuRanking").AddComponent<MenuRanking>();

        if (FindAnyObjectByType<MenuOpciones>() == null)
            new GameObject("MenuOpciones").AddComponent<MenuOpciones>();

        if (FindAnyObjectByType<MenuPausa>() == null)
            new GameObject("MenuPausa").AddComponent<MenuPausa>();

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
        estaPausado = false;
        Time.timeScale = 0f;            // el juego queda pausado en el menú
        panelMenu.SetActive(true);
        panelJuego.SetActive(false);
        panelGameOver.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelPausa != null) panelPausa.SetActive(false);

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
        estaPausado = false;
        Time.timeScale = 1f;
        panelMenu.SetActive(false);
        panelJuego.SetActive(true);
        panelGameOver.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelPausa != null) panelPausa.SetActive(false);

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
    // Botón OPCIONES del menú (cableado por OnClick en la escena) y también botón
    // "Opciones" del panel de pausa (cableado por código en MenuPausa).
    public void MostrarOpciones()
    {
        if (panelOpciones == null) return;
        panelMenu.SetActive(false);
        if (panelPausa != null) panelPausa.SetActive(false);
        panelOpciones.SetActive(true);
    }

    // Botón VOLVER dentro del panel de Opciones (cableado por código en MenuOpciones).
    public void VolverDeOpciones()
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);

        // Si abrimos Opciones desde la pausa, volvemos a la pausa; si no (lo
        // abrimos desde el menú), volvemos al menú.
        if (estaPausado && panelPausa != null)
            panelPausa.SetActive(true);
        else
            MostrarMenu();
    }

    // ---- PAUSA ----
    // La disparan la tecla Escape y el botón "II" del HUD (ver MenuPausa).
    public void AlternarPausa()
    {
        if (estaPausado) Reanudar();
        else Pausar();
    }

    public void Pausar()
    {
        // Solo se puede pausar durante una partida en curso.
        if (!panelJuego.activeSelf) return;
        if (GameManager.Instance != null && GameManager.Instance.juegoTerminado) return;
        if (estaPausado) return;

        estaPausado = true;
        Time.timeScale = 0f;
        if (panelPausa != null) panelPausa.SetActive(true);
    }

    public void Reanudar()
    {
        if (!estaPausado) return;

        estaPausado = false;
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        Time.timeScale = 1f;

        // El botón "Continuar" no recarga la escena, así que este click sí llega
        // a escucharse (cuando haya un clip asignado en AudioManager.sonidoClick).
        if (AudioManager.Instance != null)
            AudioManager.Instance.ReproducirClick();
    }

    // ---- GAME OVER ----
    // La llama el GameManager cuando perdés
    public void MostrarGameOver()
    {
        estaPausado = false;
        panelMenu.SetActive(false);
        panelJuego.SetActive(false);
        panelGameOver.SetActive(true);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelPausa != null) panelPausa.SetActive(false);
    }

    // Botón VOLVER A JUGAR (recarga y arranca jugando directo)
    public void VolverAJugar()
    {
        irAJugar = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Botón MENÚ (recarga y muestra el menú). También lo usa el botón "Salir" del
    // panel de pausa.
    public void VolverAlMenu()
    {
        irAJugar = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
