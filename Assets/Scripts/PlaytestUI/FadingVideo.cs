using UnityEngine;

public class FadingVideo : MonoBehaviour
{
    public float duration = 5f; // how long the fade lasts
    public GameObject objectToEnable; // assign in Inspector
    // commit#1
    // commit#2
    // commit#3

    private Material mat;
    private Color originalColor;
    private bool hasStartedFade = false;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        originalColor = mat.color;

        //optional: start with object disabled
        if (objectToEnable != null)
            objectToEnable.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && !hasStartedFade)
        {
            hasStartedFade = true;

            //enable the object
            if (objectToEnable != null)
                objectToEnable.SetActive(true);

            //start fading
            StartCoroutine(FadeOut());
        }
    }

    private System.Collections.IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float alpha = Mathf.Lerp(originalColor.a, 0f, elapsed / duration);

            Color c = mat.color;
            c.a = alpha;
            mat.color = c;

            yield return null;
        }

        // ensure exactly zero alpha at end
        Color final = mat.color;
        final.a = 0f;
        mat.color = final;
    }
}
