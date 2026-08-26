using UnityEngine;
using UnityEngine.UI;

public class VolumenSlider : MonoBehaviour
{
    private const string ClavePlayerPrefs = "Volumen";

    void Start()
    {
        Slider slider = GetComponent<Slider>();
        float volumenGuardado = PlayerPrefs.GetFloat(ClavePlayerPrefs, 1f);
        slider.SetValueWithoutNotify(volumenGuardado);
        AplicarVolumen(volumenGuardado);
    }

    // Enganchado al evento "On Value Changed" del Slider en el Inspector.
    public void CambiarVolumen(float valor)
    {
        AplicarVolumen(valor);
        PlayerPrefs.SetFloat(ClavePlayerPrefs, valor);
    }

    private void AplicarVolumen(float valor)
    {
        AudioListener.volume = valor;
    }
}
