using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace EchoTrio.Gameplay {
    /// GameManager controls the gameplay animation and audio.
    public class GameManager : MonoBehaviour {
        /// GameManager states.
        private enum State {
            /// Wait for the player to start the game.
            Wait,
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
        [SerializeField] private GameObject voiceChat = null;
        [SerializeField] private AudioSource waitBGM = null;
        [SerializeField] private AudioSource playBGM = null;
        [SerializeField] private AnimationReferences[] animationReferences = new AnimationReferences[0];

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
            fsm.SetStateEntry((int)State.Wait, OnEnterWait);
            fsm.SetStateUpdate((int)State.Wait, OnUpdateWait);
            fsm.SetStateExit((int)State.Wait, OnExitWait);
            
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
            // Set the default state.
            fsm.ChangeState((int)State.Wait);
        }

        private void Update() { fsm.Update(); }

        private void LateUpdate() { fsm.LateUpdate(); }

        // Wait State (You may not have to use all of these functions. I am just creating a template for you.)
        private void OnEnterWait() {
            gameInputActions.Game.Start.Enable();
            // they shouldn't be allowed to input audio yet
            gameInputActions.VoiceChat.PushToTalk.Disable();

            // Activate the displays
            Debug.Log("Connected displays: " + Display.displays.Length);
            for (int i = 1; i < Display.displays.Length; i++)
            {
                Display.displays[i].Activate();
            }

            // Play wait BGM.
            waitBGM.Play();
        }

        private void OnUpdateWait() {
            if (bodyTrackerManager.HasDetectedBodies()) {
                fsm.ChangeState((int)State.Play);
            }
        }

        private void OnExitWait() {
            gameInputActions.Game.Start.Disable();

            // Enable push-to-talk input.
            gameInputActions.VoiceChat.PushToTalk.Enable();
        }

        // Play State (You may not have to use all of these functions. I am just creating a template for you.)
        private void OnEnterPlay() {
            foreach (var cloud in clouds)
            {
                cloud.FadeOut();
            }
            voiceChat.SetActive(true);

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

        private void OnExitPlay() { }

        // Finish State (You may not have to use all of these functions. I am just creating a template for you.)
        private void OnEnterFinish() { hasFadedIn = false; }

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

        private void OnExitFinish() { }

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