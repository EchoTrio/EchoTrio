using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class FeedbackLogger : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI conversationText;

    void Update()
    {
        if (conversationText == null)
            return;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            conversationText.text += " [LIKED]";
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            conversationText.text += " [DISLIKED]";
        }
    }
}
