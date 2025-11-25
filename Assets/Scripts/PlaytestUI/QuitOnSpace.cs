using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartOnR : MonoBehaviour
{
    void Update()
    {
        //restart scene when F12 is pressed
        if (Input.GetKeyDown(KeyCode.F12))
        {
            //reload current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
