using UnityEngine;

namespace EchoTrio {
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