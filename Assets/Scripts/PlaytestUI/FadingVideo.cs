using UnityEngine;
using System.Collections;

public class FadingVideo : MonoBehaviour
{
    public float duration = 5f;

    // NEW: objects to fade instead of fading the attached object's renderer
    public GameObject[] objectsToFade; // assign in Inspector
    private bool hasFadedIn = false;


    public GameObject objectToEnable;
    public Spelunx.Orbbec.BodyTrackerManager orbbecScript;
    public EchoTrio.VoiceChat voicechatScript;

    public AudioSource audioA;
    public AudioSource audioB;

    public GameObject Audio1ToEnable;
    public GameObject Audio2ToDisable;

    public float silenceThreshold = 3f;
    private float silenceTimer = 0f;
    private bool hasStartedFade = false;

    // internal storage for materials & original colors
    private Material[] mats;
    private Color[] originalColors;

    void Start()
    {
        if (objectsToFade != null && objectsToFade.Length > 0)
        {
            mats = new Material[objectsToFade.Length];
            originalColors = new Color[objectsToFade.Length];

            for (int i = 0; i < objectsToFade.Length; i++)
            {
                Renderer r = objectsToFade[i].GetComponent<Renderer>();
                if (r != null)
                {
                    mats[i] = r.material;
                    originalColors[i] = r.material.color;
                }
            }
        }

        if (objectToEnable != null)
            objectToEnable.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            StartCoroutine(FadeOut());
        }
        if ((Input.GetKeyDown(KeyCode.F1) && !hasStartedFade)
            || (orbbecScript != null && orbbecScript.personInFrame && !hasStartedFade))
        {
            hasStartedFade = true;

            if (objectToEnable != null)
                objectToEnable.SetActive(true);
            if (Audio1ToEnable != null)
                Audio1ToEnable.SetActive(true);
            if (Audio2ToDisable != null)
                Audio2ToDisable.SetActive(false);

            StartCoroutine(FadeOut());
        }

        int roundCounter = voicechatScript.GetRoundCounter();
        bool aPlaying = audioA != null && audioA.isPlaying;
        bool bPlaying = audioB != null && audioB.isPlaying;

        if (!aPlaying && !bPlaying && roundCounter >= 10 && !hasFadedIn)
        {
            silenceTimer += Time.deltaTime;

            if (silenceTimer >= silenceThreshold)
            {
                StartCoroutine(FadeIn());
                hasFadedIn = true;
                silenceTimer = 0f;
            }
        }
        else
        {
            silenceTimer = 0f;
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                Color c = mats[i].color;
                c.a = Mathf.Lerp(originalColors[i].a, 0f, t);
                mats[i].color = c;
            }

            yield return null;
        }

        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) continue;
            Color c = mats[i].color;
            c.a = 0f;
            mats[i].color = c;
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                Color c = mats[i].color;
                c.a = Mathf.Lerp(0f, originalColors[i].a, t);
                mats[i].color = c;
            }

            yield return null;
        }

        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) continue;
            Color c = mats[i].color;
            c.a = originalColors[i].a;
            mats[i].color = c;
        }
    }
}
