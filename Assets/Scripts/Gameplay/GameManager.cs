using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoTrio.Gameplay {
    /// GameManager controls the gameplay animation and audio.
    public class GameManager : MonoBehaviour {
        // TODO: This class should replace AudioAnimationTrigger, FadingVideo, and MultiDisplayActivate.

        private enum State {
            /// Wait for the player to start the game.
            Wait,
            /// Game is in progress.
            Play,
            /// Game has finished.
            Finish,
            // Optional: Add more states as necessary.
            Num,
        }

        // Let's wrap all the references we need for an animation into a nice little class.
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
        [SerializeField] private VoiceChat voiceChat = null;
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
            gameInputActions.Game.Restart.performed += OnRestart;
            gameInputActions.VoiceChat.PushToTalk.started += OnPushToTalkStarted;
            gameInputActions.VoiceChat.PushToTalk.canceled += OnPushToTalkCancelled;

            // Subscribe to game events.
            GameEvent.GameEventSystem.GetInstance().SubscribeToEvent(nameof(GameEventName.GameFinish), OnGameFinish);
        }

        private void OnDisable() {
            // Disable inputs.
            gameInputActions.Disable();
            gameInputActions.Game.Restart.performed -= OnRestart;
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
            Debug.Log("GameManager: OnEnterWait");

            // TODO: We want the player to be able to start the game via the keyboard override. (Hint: input)

            // TODO: Is there anything we want to do just once when we loaded the scene? (Hint: displays/audio)
        }

        private void OnUpdateWait() {
            Debug.Log("GameManager: OnUpdateWait");

            if (bodyTrackerManager.HasDetectedBodies()) {
                // TODO: We want to start the game if the body tracker has detected something. (Hint: FSM)
            }
        }

        private void OnExitWait() {
            Debug.Log("GameManager: OnExitWait");

            // TODO: Now that the game has started, do not let the player start it again. (Hint: input)
        }

        // Play State (You may not have to use all of these functions. I am just creating a template for you.)
        private void OnEnterPlay() {
            Debug.Log("GameManager: OnEnterPlay");

            // TODO: Now that the game has started, we want to do something with the clouds. (Hint: check out the FadeEffect class)
            // Is there also anything else we should activate or deactivate?
        }

        private void OnUpdatePlay() {
            Debug.Log("GameManager: OnUpdatePlay");

            // TODO: Are there things we need to update during gameplay? (Hint: animation)
        }

        private void OnExitPlay() {
            Debug.Log("GameManager: OnExitPlay");
        }

        // Finish State (You may not have to use all of these functions. I am just creating a template for you.)
        private void OnEnterFinish() {
            Debug.Log("GameManager: OnEnterFinish");
        }

        private void OnUpdateFinish() {
            Debug.Log("GameManager: OnUpdateFinish");

            // TODO: Now that the game has finished, what should happen? Maybe we can consider restarting after a few seconds?
        }

        private void OnExitFinish() {
            Debug.Log("GameManager: OnExitFinish");
        }

        // Input Callbacks
        private void OnStart(UnityEngine.InputSystem.InputAction.CallbackContext context) {
            // Start the game by transiting to the Play state.
            fsm.ChangeState((int)State.Play);
        }

        private void OnRestart(UnityEngine.InputSystem.InputAction.CallbackContext context) {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnPushToTalkStarted(UnityEngine.InputSystem.InputAction.CallbackContext context) {
            Debug.Log("GameManager: OnPushToTalkStarted");

            // TODO: What do we want to do when the user has pressed the CTRL key?
        }

        private void OnPushToTalkCancelled(UnityEngine.InputSystem.InputAction.CallbackContext context) {
            Debug.Log("GameManager: OnPushToTalkCancelled");

            // TODO: What do we want to do when the user has released the CTRL key?
        }

        // Game Event Callbacks
        private void OnGameFinish() {
            // Transit to the finish state when we receive the GameFinish event.
            fsm.ChangeState((int)State.Finish);
        }
    }
}