using UnityEngine;

public class ToggleOnP : MonoBehaviour
{
    public GameObject objectToEnable;
    public GameObject objectToDisable;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (objectToEnable != null)
                objectToEnable.SetActive(true);

            if (objectToDisable != null)
                objectToDisable.SetActive(false);
        }
    }
}
