// By Terri Lim, CMU ETC Class of 2026. Last updated by me in December 2025. Feel free to judge any code up till then.
using UnityEngine;

namespace EchoTrio.UI {
    [RequireComponent(typeof(TMPro.TMP_InputField))]
    public class UserTextInput : MonoBehaviour {
        [SerializeField] VoiceChat voiceChat = null;

        private TMPro.TMP_InputField inputField = null;

        public void SubmitUserTextInput() {
            string message = inputField.text.Trim();
            if (0 < message.Length && voiceChat.SubmitUserTextInput(message)) {
                inputField.text = string.Empty;
            }
        }

        private void Awake() {
            inputField = GetComponent<TMPro.TMP_InputField>();
        }

        private void Start() {

        }

        private void Update() {

        }
    }
}