using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using GameEvent;

namespace EchoTrio.Gameplay {
    /// GameManager controls the gameplay animation and audio.
    public class GameManager : MonoBehaviour {
        /// GameManager states.
        private enum State {
            /// Wait for the player to start the game.
            Idle,
            /// Game is in progress.
            Play,
            /// Game has finished.
            Finish,
            Num,
        }

        /// Helper class to group references needed for animation.
        [System.Serializable] public class AnimationReferences {
            [Header("References")]
            public Animator animator = null;
            public AudioSource audioSource = null;
            public AudioSource[] sfxVariants = new AudioSource[0];

            [Header("Animator Parameters")]
            public string pushToTalkStartedBool = "IsCtrlPressed";
            public string pushToTalkCancelledBool = "IsCtrlReleased";
            public string actorIsTalkingBool = "<Insert Animation Parameter Here>";

            // Public Interface
            public AudioSource GetRandomSFXVariant() { return sfxVariants[UnityEngine.Random.Range(0, sfxVariants.Length)]; }
        }

        [Header("Prefabs & References")]
        [SerializeField] private EchoTrio.UI.FadeEffect[] clouds = new EchoTrio.UI.FadeEffect[0];
        [SerializeField] private Spelunx.Orbbec.BodyTrackerManager bodyTrackerManager = null;
        [SerializeField] private AudioSource waitBGM = null;
        [SerializeField] private AudioSource playBGM = null;
        [SerializeField] private AnimationReferences[] animationReferences = new AnimationReferences[0];

        [Header("Settings")]
        [SerializeField, Min(1)] private int numDisplays = 3;

        // Internal Variables
        GameInputActions gameInputActions = null;
        FSM.FiniteStateMachine fsm = new FSM.FiniteStateMachine((int)State.Num);

        // Finish State Variables
        private float fadeInDelay = 3.0f;
        private float fadeInTimer = 0.0f;
        private bool hasFadedIn = false;

        // Internal Functions
        private void Awake() {
            gameInputActions = new GameInputActions();

            // Initialise FSM.
            fsm.SetStateEntry((int)State.Idle, OnEnterIdle);
            fsm.SetStateUpdate((int)State.Idle, OnUpdateIdle);
            fsm.SetStateExit((int)State.Idle, OnExitIdle);
            
            fsm.SetStateEntry((int)State.Play, OnEnterPlay);
            fsm.SetStateUpdate((int)State.Play, OnUpdatePlay);
            fsm.SetStateExit((int)State.Play, OnExitPlay);

            fsm.SetStateEntry((int)State.Finish, OnEnterFinish);
            fsm.SetStateUpdate((int)State.Finish, OnUpdateFinish);
            fsm.SetStateExit((int)State.Finish, OnExitFinish);
        }

        private void OnEnable() {
            // Enable inputs.
            gameInputActions.Enable();
            gameInputActions.Game.Start.performed += OnStart;
            gameInputActions.Game.Restart.performed += OnRestart;
            gameInputActions.Game.Continue.performed += OnContinue;
            gameInputActions.VoiceChat.PushToTalk.started += OnPushToTalkStarted;
            gameInputActions.VoiceChat.PushToTalk.canceled += OnPushToTalkCancelled;

            // Subscribe to game events.
            GameEvent.GameEventSystem.GetInstance().SubscribeToEvent(nameof(GameEventName.GameFinish), OnGameFinish);
        }

        private void OnDisable() {
            // Disable inputs.
            gameInputActions.Disable();
            gameInputActions.Game.Start.performed -= OnStart;
            gameInputActions.Game.Restart.performed -= OnRestart;
            gameInputActions.Game.Continue.performed -= OnContinue;
            gameInputActions.VoiceChat.PushToTalk.started -= OnPushToTalkStarted;
            gameInputActions.VoiceChat.PushToTalk.canceled -= OnPushToTalkCancelled;

            // Unsubscribe from game events.
            GameEvent.GameEventSystem.GetInstance().UnsubscribeFromEvent(nameof(GameEventName.GameFinish), OnGameFinish);
        }

        private void Start() {
            gameInputActions.Game.Start.Disable();
            gameInputActions.Game.Restart.Enable(); // Panic button should always be enabled.
            gameInputActions.Game.Continue.Disable();
            gameInputActions.VoiceChat.PushToTalk.Disable();

            // Set the default state.
            fsm.ChangeState((int)State.Idle);
        }

        private void Update() { fsm.Update(); }

        private void LateUpdate() { fsm.LateUpdate(); }

        // Wait State (You may not have to use all of these functions. I am just creating a template for you.)
        private void OnEnterIdle() {
            gameInputActions.Game.Start.Enable();

            // Activate the displays
            Debug.Log("Connected displays: " + Display.displays.Length);
            for (int i = 1; i < Mathf.Min(Display.displays.Length, numDisplays); i++)
            {
                Display.displays[i].Activate();
            }

            // Play wait BGM.
            waitBGM.Play();
        }

        private void OnUpdateIdle() {
            if (bodyTrackerManager.HasDetectedBodies()) {
                fsm.ChangeState((int)State.Play);
            }
        }

        private void OnExitIdle() {
            gameInputActions.Game.Start.Disable();
        }

        // Play State (You may not have to use all of these functions. I am just creating a template for you.)
        private void OnEnterPlay() {
            gameInputActions.VoiceChat.PushToTalk.Enable();

            foreach (var cloud in clouds)
            {
                cloud.FadeOut();
            }

            // Tell the voice chat system to start.
            GameEventSystem.GetInstance().TriggerEvent(nameof(GameEventName.GameStart));

            // Switch BGMs.
            waitBGM.Stop();
            playBGM.Play();
        }

        private void OnUpdatePlay() {
            bool isATalking = animationReferences[0].audioSource != null && animationReferences[0].audioSource.isPlaying;
            bool isPTalking = animationReferences[1].audioSource != null && animationReferences[1].audioSource.isPlaying;

            if (animationReferences[0].animator != null)
            {
                animationReferences[0].animator.SetBool(animationReferences[0].actorIsTalkingBool, isATalking);
                animationReferences[0].animator.SetBool(animationReferences[1].actorIsTalkingBool, isPTalking);
            }
            if (animationReferences[1].animator != null)
            {
                animationReferences[1].animator.SetBool(animationReferences[1].actorIsTalkingBool, isPTalking);
                animationReferences[1].animator.SetBool(animationReferences[0].actorIsTalkingBool, isATalking);
            }
        }

        private void OnExitPlay() {
            gameInputActions.VoiceChat.PushToTalk.Disable();
        }

        // Finish State (You may not have to use all of these functions. I am just creating a template for you.)
        private void OnEnterFinish() {
            gameInputActions.Game.Continue.Enable();
            hasFadedIn = false;
        }

        private void OnUpdateFinish() {
            // Fade the clouds back in.
            fadeInTimer += Time.deltaTime;
            if (!hasFadedIn && fadeInTimer >= fadeInDelay)
            {
                foreach (var cloud in clouds)
                {
                    cloud.FadeIn();
                }
                hasFadedIn = true;
            }
        }

        private void OnExitFinish() {
            gameInputActions.Game.Continue.Disable();
        }

        // Input Callbacks
        private void OnStart(InputAction.CallbackContext context) {
            // Start the game by transiting to the Play state.
            fsm.ChangeState((int)State.Play);
        }

        private void OnRestart(InputAction.CallbackContext context) {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnContinue(InputAction.CallbackContext context) {
            // return to play state from finish state
            fsm.ChangeState((int)State.Play);

            // Trigger the Game Continue event.
            GameEventSystem.GetInstance().TriggerEvent(nameof(GameEventName.GameContinue));
        }

        private void OnPushToTalkStarted(InputAction.CallbackContext context) {
            foreach (var animRef in animationReferences)
            {
                if (animRef.animator != null)
                {
                    animRef.animator.SetBool(animRef.pushToTalkStartedBool, true);
                    animRef.animator.SetBool(animRef.pushToTalkCancelledBool, false);
                }
            }
        }

        private void OnPushToTalkCancelled(InputAction.CallbackContext context) {
            foreach (var animRef in animationReferences)
            {
                // play the thinking SFX
                AudioSource sfx = animRef.GetRandomSFXVariant();
                if (sfx != null)
                {
                    sfx.Play();
                }
                // update animator
                if (animRef.animator != null)
                {
                    animRef.animator.SetBool(animRef.pushToTalkStartedBool, false);
                    animRef.animator.SetBool(animRef.pushToTalkCancelledBool, true);
                }
            }
        }

        // Game Event Callbacks
        private void OnGameFinish() {
            // Transit to the finish state when we receive the GameFinish event.
            fsm.ChangeState((int)State.Finish);
        }
    }
}