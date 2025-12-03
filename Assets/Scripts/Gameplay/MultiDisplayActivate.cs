using UnityEngine;

public class MultiDisplayActivate : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Connected displays: " + Display.displays.Length);

        //activates all displays except display 1
        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }
    }
}

