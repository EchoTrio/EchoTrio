using UnityEngine;

public class UltraWide : MonoBehaviour
{
    void Awake()
    {
        //forces borderless mode
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        //forces ultrawide resolution
        Screen.SetResolution(3840, 1080, FullScreenMode.FullScreenWindow);
    }

}
