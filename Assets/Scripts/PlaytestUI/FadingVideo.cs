using UnityEngine;

public class FadingVideo : MonoBehaviour
{
    public float duration = 5f; // how long the fade lasts

    private Material mat;
    private Color originalColor;

    void Start()
    {
        // get a unique material instance
        mat = GetComponent<Renderer>().material;
        originalColor = mat.color;

        StartCoroutine(FadeOut());
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

        // ensure alpha is exactly zero at the end
        Color final = mat.color;
        final.a = 0f;
        mat.color = final;
    }
}
