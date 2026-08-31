using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // Claves de PlayerPrefs para los 3 volúmenes que se ajustan en Opciones.
    public const string ClaveVolumenGeneral = "Volumen";
    public const string ClaveVolumenMusica = "VolumenMusica";
    public const string ClaveVolumenEfectos = "VolumenEfectos";

    [Header("Fuentes de audio")]
    public AudioSource musica;
    public AudioSource efectos;

    [Header("Clips (dejar vacío hasta tener los archivos definitivos)")]
    public AudioClip musicaFondo;
    public AudioClip sonidoChoque;
    public AudioClip sonidoClick;

    void Awake()
    {
        Instance = this;

        // Si no se asignaron a mano en el Inspector, se crean solas.
        if (musica == null)
        {
            musica = gameObject.AddComponent<AudioSource>();
            musica.loop = true;
        }

        if (efectos == null)
            efectos = gameObject.AddComponent<AudioSource>();

        // Aplicar los volúmenes guardados (el jugador los ajusta en Opciones).
        AudioListener.volume = PlayerPrefs.GetFloat(ClaveVolumenGeneral, 1f);
        musica.volume = PlayerPrefs.GetFloat(ClaveVolumenMusica, 1f);
        efectos.volume = PlayerPrefs.GetFloat(ClaveVolumenEfectos, 1f);
    }

    // ---- Volumen (lo llaman los sliders de MenuOpciones) ----

    // Volumen general: baja TODO el audio del juego a la vez.
    public void SetVolumenGeneral(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat(ClaveVolumenGeneral, valor);
    }

    public void SetVolumenMusica(float valor)
    {
        if (musica != null) musica.volume = valor;
        PlayerPrefs.SetFloat(ClaveVolumenMusica, valor);
    }

    public void SetVolumenEfectos(float valor)
    {
        if (efectos != null) efectos.volume = valor;
        PlayerPrefs.SetFloat(ClaveVolumenEfectos, valor);
    }

    // ---- Música (suena SOLO en el menú: la dispara MenuManager) ----

    public void ReproducirMusicaMenu()
    {
        ReproducirMusica(musicaFondo);
    }

    public void ReproducirMusica(AudioClip clip)
    {
        if (musica == null || clip == null) return;
        if (musica.isPlaying && musica.clip == clip) return;   // ya está sonando esta música

        musica.clip = clip;
        musica.loop = true;
        musica.Play();
    }

    public void DetenerMusica()
    {
        if (musica != null)
            musica.Stop();
    }

    // ---- Efectos ----

    public void ReproducirEfecto(AudioClip clip)
    {
        if (efectos == null || clip == null) return;

        efectos.PlayOneShot(clip);
    }

    // Choque contra un obstáculo / Game Over.
    public void ReproducirChoque()
    {
        ReproducirEfecto(sonidoChoque);
    }

    // Click de UI. Solo tiene sentido usarlo en paneles que NO recargan la
    // escena (por ejemplo, un futuro menú de pausa), porque los botones que
    // sí recargan cortan el sonido antes de que termine de sonar.
    public void ReproducirClick()
    {
        ReproducirEfecto(sonidoClick);
    }
}
