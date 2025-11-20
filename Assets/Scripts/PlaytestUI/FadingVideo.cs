using UnityEngine;

public class FadingVideo : MonoBehaviour
{
    public float duration = 5f; // how long the fade lasts
    public GameObject objectToEnable; // assign in Inspector
    // commit#1
    // commit#2
    // commit#3
    public Spelunx.Orbbec.BodyTrackerManager orbbecScript;

    public EchoTrio.VoiceChat voicechatScript;
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
        if (Input.GetKeyDown(KeyCode.P) && !hasStartedFade
            || (orbbecScript != null && orbbecScript.personInFrame && !hasStartedFade))
        {
            hasStartedFade = true;

            //enable the object
            if (objectToEnable != null)
                objectToEnable.SetActive(true);

            //start fading
            StartCoroutine(FadeOut());
        }

        int roundCounter = voicechatScript.GetRoundCounter();
        //print("Round Counter: " + roundCounter);
        if (roundCounter >= 10)
        {
            StartCoroutine(FadeIn());
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

    private System.Collections.IEnumerator FadeIn()
{
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;

        float alpha = Mathf.Lerp(0f, originalColor.a, elapsed / duration);

        Color c = mat.color;
        c.a = alpha;
        mat.color = c;

        yield return null;
    }

    // ensure full alpha at end
    Color final = mat.color;
    final.a = originalColor.a;
    mat.color = final;
}

}
