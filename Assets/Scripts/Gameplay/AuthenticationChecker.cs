using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EchoTrio.Gameplay {
    /// MonoBehaviour to check if the authentication file exists before starting the experience.
    public class AuthenticationChecker : MonoBehaviour {
        [Header("References")]
        [SerializeField] private GameObject failureText = null;

        [Header("Settings")]
        [SerializeField] private string nextScene = "Playtest";

        // Internal Variable
        private GameInputActions gameInputActions = null;

        private void Awake() {
            gameInputActions = new GameInputActions();
            failureText.gameObject.SetActive(false);
        }

        private void OnEnable() {
            gameInputActions.Enable();
            gameInputActions.Game.Quit.performed += OnQuit;
        }

        private void OnDisable() {
            gameInputActions.Disable();
            gameInputActions.Game.Quit.performed -= OnQuit;
        }

        private void Start() {
            if (Authentication.AuthenticationFileExists()) {
                SceneManager.LoadScene(nextScene);
            } else {
                failureText.gameObject.SetActive(true);
            }
        }

        // Input Action Callbacks
        private void OnQuit(InputAction.CallbackContext context) {
            Application.Quit();
        }
    }
}