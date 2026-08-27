using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

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
    }

    void Start()
    {
        ReproducirMusica(musicaFondo);
    }

    public void ReproducirMusica(AudioClip clip)
    {
        if (musica == null || clip == null) return;

        musica.clip = clip;
        musica.loop = true;
        musica.Play();
    }

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
