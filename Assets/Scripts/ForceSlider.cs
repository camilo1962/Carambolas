using UnityEngine;
using UnityEngine.UI;

public class ForceSlider : MonoBehaviour
{
    public StandardInputManager inputManager; // Asegúrate de asignar esto desde el Editor de Unity

    private Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        inputManager.SetShotPower(value); // Este método debe ser implementado en tu script de MobileInputManager
    }
}

