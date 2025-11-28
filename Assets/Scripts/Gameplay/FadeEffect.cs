using UnityEngine;

namespace EchoTrio.Gameplay {
    public class FadeEffect : MonoBehaviour {
        private enum State { Idle, FadeIn, FadeOut, Num }

        [Header("References")]
        [SerializeField] private Renderer targetRenderer = null;
        
        [Header("Fade Settings")]
        [SerializeField, Range(0.1f, 60.0f)] private float fadeDuration = 5.0f;

        // Private Variables
        private Color initialColour = Color.white;
        private FSM.FiniteStateMachine fsm = new FSM.FiniteStateMachine((int)State.Num);
        private float timer = 0.0f;

        // Public Interfaces
        public float GetFadeDuration() { return fadeDuration; }

        public void FadeIn() { fsm.ChangeState((int)State.FadeIn); }

        public void FadeOut() { fsm.ChangeState((int)State.FadeOut); }

        // Internal Functions
        private void Awake() {
            // Get initial colour.
            if (targetRenderer != null) {
                initialColour = targetRenderer.material.color;
            }

            // Initialise FSM.
            fsm.SetStateEntry((int)State.FadeIn, OnEnterFadeIn);
            fsm.SetStateUpdate((int)State.FadeIn, OnUpdateFadeIn);
            fsm.SetStateEntry((int)State.FadeOut, OnEnterFadeOut);
            fsm.SetStateUpdate((int)State.FadeOut, OnUpdateFadeOut);
        }

        private void Start() {
            // Set default state.
            fsm.ChangeState((int)State.Idle);
        }

        private void Update() { fsm.Update(); }

        private void LateUpdate() { fsm.LateUpdate(); }

        // Fade In State
        private void OnEnterFadeIn() { timer = 0.0f; }

        private void OnUpdateFadeIn() {
            // Update timer.
            timer = Mathf.Min(timer + Time.deltaTime, fadeDuration);

            // Change colour.
            Material material = targetRenderer.material;
            Color colour = material.color;
            colour.a = Mathf.Lerp(0.0f, initialColour.a, timer / fadeDuration);
            material.color = colour;

            // Return to idle once done.
            if (timer == fadeDuration) { fsm.ChangeState((int)State.Idle); }
        }

        private void OnEnterFadeOut() { timer = 0.0f; }

        private void OnUpdateFadeOut() {
            // Update timer.
            timer = Mathf.Min(timer + Time.deltaTime, fadeDuration);

            // Change colour.
            Material material = targetRenderer.material;
            Color colour = material.color;
            colour.a = Mathf.Lerp(initialColour.a, 0.0f, timer / fadeDuration);
            material.color = colour;

            // Return to idle once done.
            if (timer == fadeDuration) { fsm.ChangeState((int)State.Idle); }
        }
    }
}