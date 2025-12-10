using System.Collections;
using UnityEngine;

namespace EchoTrio.UI {
    public class Chatbox : MonoBehaviour {
        [SerializeField] private UnityEngine.UI.ScrollRect scrollView = null;
        [SerializeField] private TMPro.TMP_Text textArea = null;

        // Public Interfaces.
        public void AddMessage(string speaker, string message) {
            textArea.text += speaker + ": " + message + "\n\n";
            ScrollToBottom();
        }

        public void AddMessage(string message) {
            textArea.text += message + "\n\n";
            ScrollToBottom();
        }

        public void ScrollToBottom() {
            if (gameObject.activeSelf) {
                StartCoroutine(ScrollToBottomCoroutine());
            }
        }

        // Internal Functions
        private void Start() { }

        private void Update() { }

        private IEnumerator ScrollToBottomCoroutine() {
            yield return null; // Wait for 1 frame so that the ScrollRect can resize and stuff.
            scrollView.verticalNormalizedPosition = 0.0f; // Just scroll it all the way to the very bottom for now. Do this 1 frame later once the ScrollRect has resized.
        }
    }
}