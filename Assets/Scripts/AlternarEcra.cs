using UnityEngine;
using UnityEngine.InputSystem; // Necessário para o novo sistema

public class AlternarEcra : MonoBehaviour
{
    void Start()
    {
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.fullScreen = true;
    }

    void Update()
    {
        if (Keyboard.current.f11Key.wasPressedThisFrame)
        {
            AlternarModoEcra();
        }
    }

    void AlternarModoEcra()
    {
        if (Screen.fullScreen)
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.fullScreen = false;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
        }
    }
}