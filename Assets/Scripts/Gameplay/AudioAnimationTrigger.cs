using UnityEngine;

public class AudioAnimationTrigger : MonoBehaviour
{
    [Header("Animator / Audio Pairs")]
    public Animator animatorA;
    public AudioSource audioA;

    public Animator animatorP;
    public AudioSource audioP;

    [Header("Animator Parameters")]
    public string ctrlPressedBool = "IsCtrlPressed";
    public string ctrlReleasedBool = "IsCtrlReleased";
    public string AtalkingBool = "AIsTalking";
    public string PtalkingBool = "PIsTalking";

    [Header("Athena Audio Variants")]
    public AudioSource AthenaAudio1;
    public AudioSource AthenaAudio2;

    [Header("Poseidon Audio Variants")]
    public AudioSource PoseidonAudio1;
    public AudioSource PoseidonAudio2;

    void Update()
    {
        //=== AUDIO → TALKING BOOL ===//
        bool isTalkingA = audioA != null && audioA.isPlaying;
        bool isTalkingP = audioP != null && audioP.isPlaying;

        if (animatorA != null)
        {
            animatorA.SetBool(AtalkingBool, isTalkingA);
            animatorA.SetBool(PtalkingBool, isTalkingP);
        }

        if (animatorP != null)
        {
            animatorP.SetBool(AtalkingBool, isTalkingA);
            animatorP.SetBool(PtalkingBool, isTalkingP);
        }

        //=== CTRL → ANIMATOR TRIGGERS ===//

        // CTRL pressed
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            if (animatorA != null)
            {
                animatorA.SetBool(ctrlPressedBool, true);
                animatorA.SetBool(ctrlReleasedBool, false);
            }

            if (animatorP != null)
            {
                animatorP.SetBool(ctrlPressedBool, true);
                animatorP.SetBool(ctrlReleasedBool, false);
            }
        }

        // CTRL released
        if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl))
        {
            if (animatorA != null)
            {
                animatorA.SetBool(ctrlPressedBool, false);
                animatorA.SetBool(ctrlReleasedBool, true);
            }

            if (animatorP != null)
            {
                animatorP.SetBool(ctrlPressedBool, false);
                animatorP.SetBool(ctrlReleasedBool, true);
            }

            // Play random ATHENA audio
            int rA = Random.Range(0, 2);
            if (rA == 0 && AthenaAudio1 != null) AthenaAudio1.Play();
            if (rA == 1 && AthenaAudio2 != null) AthenaAudio2.Play();

            // Play random POSEIDON audio
            int rP = Random.Range(0, 2);
            if (rP == 0 && PoseidonAudio1 != null) PoseidonAudio1.Play();
            if (rP == 1 && PoseidonAudio2 != null) PoseidonAudio2.Play();
        }
    }
}
