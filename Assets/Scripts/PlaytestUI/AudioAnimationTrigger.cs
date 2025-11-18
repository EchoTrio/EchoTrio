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

    private bool prevCtrlState = false;

    void Update()
    {
        bool isCtrlHeld =
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        //=== AUDIO → TALKING BOOL ===/
        bool isTalkingA = audioA != null && audioA.isPlaying;
        bool isTalkingP = audioP != null && audioP.isPlaying;

        if (animatorA != null)
            animatorA.SetBool(AtalkingBool, isTalkingA);
            animatorA.SetBool(PtalkingBool, isTalkingP);

        if (animatorP != null)
            animatorP.SetBool(AtalkingBool, isTalkingA);
            animatorP.SetBool(PtalkingBool, isTalkingP);

        //=== CTRL → ANIMATION STATE ===//
        if (isCtrlHeld && !prevCtrlState)
        {
            // CTRL just pressed
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
        else if (!isCtrlHeld && prevCtrlState)
        {
            // CTRL just released
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
        }

        prevCtrlState = isCtrlHeld;
    }
}
