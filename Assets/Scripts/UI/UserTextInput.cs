using UnityEngine;

namespace EchoTrio.UI {
    [RequireComponent(typeof(TMPro.TMP_InputField))]
    public class UserTextInput : MonoBehaviour {
        [SerializeField] VoiceChat voiceChat = null;

        private TMPro.TMP_InputField inputField = null;

        public async void SubmitUserTextInput() {
            string message = inputField.text.Trim();
            if (0 < message.Length && await voiceChat.SubmitUserTextInput(message)) {
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