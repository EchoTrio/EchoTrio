using UnityEngine;

public class CtrlAnimationTrigger : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public AudioSource audioSource;

    [Header("Animator Parameters")]
    public string ctrlPressedBool = "IsCtrlPressed";
    public string ctrlReleasedBool = "IsCtrlReleased";
    public string talkingBool = "IsTalking";

    private bool prevCtrlState = false;

    void Update()
    {
        bool isAudioPlaying = audioSource.isPlaying;

        animator.SetBool(talkingBool, isAudioPlaying);

        bool ctrlDown =
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        //if CTRL is held → go to Animation A
        if (ctrlDown && !prevCtrlState)
        {
            animator.SetBool(ctrlPressedBool, true);
            animator.SetBool(ctrlReleasedBool, false);
        }
        //if CTRL was released → go back to Animation B
        // else if (!ctrlDown && prevCtrlState)
        // {
        //     animator.SetBool(ctrlPressedBool, false);
        //     animator.SetBool(ctrlReleasedBool, true);
        // }

        prevCtrlState = ctrlDown;
    }
}

