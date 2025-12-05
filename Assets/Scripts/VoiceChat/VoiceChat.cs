// By Terri Lim, CMU ETC Class of 2026. Last updated by me in December 2025. Feel free to judge any code up till then.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameEvent;
using UnityEngine;

namespace EchoTrio {
    /// Voice chat system that acts as an intermediary between the human user and the AI models.
    /// The system works using the concept of rounds. During every round, a few things may happen.
    ///     - A scripted discussion is triggered at the start of the round specified by the designer where the actors speak scripted lines. No user input is allowed in this round. OR
    ///     - A generated discussion is triggered at the start of the round specified by the designer where the actors speak AI generated lines. No user input is allowed in this round. OR
    ///     - User input is accepted and the actors generates a reply. OR
    ///     - User input is accepted and it triggers a scripted or generated discussion as the reply from the actors if the user mentions certain topics. OR
    ///     - User input is allowed by the user does not provide any input. A scripted or generated dicussion is triggered after some time as specified by the designer.
    public class VoiceChat : MonoBehaviour {
        /// All the possible states of the system.
        private enum State {
            Invalid = -1,
            /// Idle while waiting for player to start the game.
            Idle,
            /// Before we start each round, we enter the Prepare stage, and make a decision of which state to enter for the round.
            Prepare,
            /// If user input is allowed this round, enter the Wait stage to wait for the actors to finish speaking, and ensure that the director has connected to OpenAI's server.
            Wait,
            /// Listen for user input.
            Listen,
            /// Actors speaking.
            Speak,
            /// Actors playing a scripted discussion, or generating a discussion on a topic.
            Discuss,
            /// The chat has ended.
            Finish,
            Num
        }

        /// Reference to an actor configuration, and the AudioSource it should play it's output audio from.
        [System.Serializable] public class ActorReferences {
            /// The configuration of the actor.
            public ActorConfig actorConfig;
            /// The audio source to output the speech of the actor.
            public AudioSource audioSource;
            /// The listening icon of the actor.
            public EchoTrio.UI.SpriteSwitcher listeningIcon = null;
        }

        /// Output of actors to be queued and played by the audio thread.
        private class ActorOutput {
            public string persona;
            public string message;
            public Emotion emotion = Emotion.Neutral;
            public List<string> reasonings;
            public AudioClip audioClip;
            public AudioSource audioSource;

            public ActorOutput(string persona, string message, Emotion emotion, List<string> reasonings, AudioClip audioClip, AudioSource audioSource) {
                this.persona = persona;
                this.message = message;
                this.emotion = emotion;
                this.reasonings = reasonings;
                this.audioClip = audioClip;
                this.audioSource = audioSource;
            }
        }

        [Header("GUI References")]
        [SerializeField] private EchoTrio.UI.Chatbox chatbox = null;
        [SerializeField] private TMPro.TextMeshProUGUI roundCounterText = null;
        [SerializeField] private TMPro.TextMeshProUGUI idleTimerText = null;

        [Header("Configurations")]
        [SerializeField] private DirectorConfig directorConfig = null;
        [SerializeField] private ActorReferences[] actorReferences = new ActorReferences[0]; // Designed as an array to be somewhat scalable so that in theory we could easily support 1 human user, multiple AI actors. But that's beyond the scope of this project.

        [Header("Discussions")]
        [SerializeField] private Discussion[] discussions = new Discussion[0];

        [Header("Voice Chat Settings")]
        [SerializeField, Range(1, 100), Tooltip("How many rounds to play before the chat ends.")] private int finishRound = 10;

        [Header("Debug")]
        [SerializeField] private bool enableDebug = true;
        [SerializeField] private bool showReasoning = true;

        // Internal Variables
        private GameInputActions gameInputActions = null;
        private FSM.FiniteStateMachine fsm = new FSM.FiniteStateMachine((int)State.Num);
        private Dictionary<string, (Actor, AudioSource)> actors = new Dictionary<string, (Actor, AudioSource)>();
        private Director director = null;
        private int roundCounter = 0;
        private float idleTimer = 0.0f;
        private bool continueChat = false;

        // Audio
        private bool isAudioPlaying = false;
        private bool isQueueingAudio = false;
        private Queue<ActorOutput> actorOutputQueue = new Queue<ActorOutput>();
        private Dictionary<AudioClip, bool> audioClipGarbageCollector = new Dictionary<AudioClip, bool>();

        // Discuss Variables
        private List<Discussion> untriggeredDiscussions = null;
        private Queue<Discussion> discussionQueue = new Queue<Discussion>();

        // Speak Variables
        private Queue<string> speakerQueue = new Queue<string>();

        // Public Interfaces
        /// Toggle the director's microphone on or off.
        public void ToggleMicMute() {
            director.IsMicMuted = !director.IsMicMuted;
            if (director.IsMicMuted) { idleTimer = 0.0f; }
        }

        /// Toggle the chatbox to be active or inactive.
        public void ToggleChatbox() {
            chatbox.gameObject.SetActive(!chatbox.gameObject.activeSelf);
            chatbox.ScrollToBottom();
        }

        /// Reset the idle timer, usually invoked whenever there's any input by the user, such as typing something into the chatbox, or unmuting the microphone.
        public void ResetIdleTimer() { idleTimer = 0.0f; }

        /// <summary>
        /// Submit the user text input. Used as an alternative to speaking into the microphone, usually for development & debugging purposes.
        /// </summary>
        /// <param name="message">The user text input.</param>
        /// <returns>Returns true if the voice chat system is currently accepting user input. Else, returns false.</returns>
        public async Awaitable<bool> SubmitUserTextInput(string message) {
            return fsm.GetCurrentState() == (int)State.Listen && await director.SubmitUserTextInput(message, destroyCancellationToken);
        }

        public int GetRoundCounter() { return roundCounter; }

        public int GetFinishRound() { return finishRound; }

        // Internal Functions
        private void Awake() {
            // Initialise Input
            gameInputActions = new GameInputActions();

            // Copy discussions into a list.
            untriggeredDiscussions = new List<Discussion>(discussions);

            // Create actors.
            foreach (ActorReferences actorRef in actorReferences) {
                Actor actor = new Actor(actorRef.actorConfig) { EnableDebug = enableDebug };
                AudioSource audioSource = actorRef.audioSource;
                actors.Add(actor.Persona, (actor, audioSource));
            }

            // Create director. Has to be after actors.
            director = new Director() { IsMicMuted = true, EnableDebug = enableDebug };

            // Initialise Finite State Machine
            fsm.SetStateEntry((int)State.Idle, OnEnterIdle);
            fsm.SetStateEntry((int)State.Prepare, OnEnterPrepare);
            fsm.SetStateEntry((int)State.Wait, OnEnterWait);
            fsm.SetStateUpdate((int)State.Wait, OnUpdateWait);
            fsm.SetStateEntry((int)State.Listen, OnEnterListen);
            fsm.SetStateUpdate((int)State.Listen, OnUpdateListen);
            fsm.SetStateEntry((int)State.Speak, OnEnterSpeak);
            fsm.SetStateEntry((int)State.Discuss, OnEnterDiscuss);
            fsm.SetStateEntry((int)State.Finish, OnEnterFinish);
        }

        private void OnDestroy() {
            // Ensure that the all audio clips are destroyed.
            lock (audioClipGarbageCollector) {
                foreach (var kv in audioClipGarbageCollector) {
                    Destroy(kv.Key);
                }
                audioClipGarbageCollector.Clear();
            }
        }

        private void OnEnable() {
            // Enable input actions.
            gameInputActions.Enable();
            gameInputActions.VoiceChat.PushToTalk.started += OnPushToTalkStarted;
            gameInputActions.VoiceChat.PushToTalk.canceled += OnPushToTalkCancelled;

            // Subcribe to game events.
            GameEventSystem.GetInstance().SubscribeToEvent(nameof(GameEventName.GameStart), OnGameStart);
            GameEventSystem.GetInstance().SubscribeToEvent(nameof(GameEventName.GameContinue), OnGameContinue);
        }

        private void OnDisable() {
            // Disable input actions.
            gameInputActions.Disable();
            gameInputActions.VoiceChat.PushToTalk.started -= OnPushToTalkStarted;
            gameInputActions.VoiceChat.PushToTalk.canceled -= OnPushToTalkCancelled;

            // Unsubcribe from game events.
            GameEventSystem.GetInstance().UnsubscribeFromEvent(nameof(GameEventName.GameStart), OnGameStart);
            GameEventSystem.GetInstance().SubscribeToEvent(nameof(GameEventName.GameContinue), OnGameContinue);
        }

        private void Start() {
            StartCoroutine(AudioThread()); // Launch a thread to play queued audio.
            director.Initialise(OnDirectorResponse, destroyCancellationToken); // Tell the director to connect to OpenAI's server.
            fsm.ChangeState((int)State.Idle); // Start off the voice chat system in the "Idle" state.
        }

        private void Update() {
            // Update the finite state machine.
            fsm.Update();

            // Update GUI.
            for (int i = 0; i < actorReferences.Length; ++i) {
                if (actorReferences[i] != null && actorReferences[i].listeningIcon != null) {
                    actorReferences[i].listeningIcon.SetSprite(director.IsStatus(Director.Status.Listening) ? 1 : 0);
                }
            }
            
            // Cleanup finished audio clips.
            lock (audioClipGarbageCollector) {
                foreach (var kv in audioClipGarbageCollector) {
                    if (kv.Value && kv.Key != null) { Destroy(kv.Key); }
                }
                audioClipGarbageCollector = audioClipGarbageCollector.Where(kv => !kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
            }
        }

        private void LateUpdate() {
            // Late update the finite state machine.
            fsm.LateUpdate();
        }

        /// <summary>
        /// Queue up an actor's output to be played by the audio thread.
        /// </summary>
        /// <param name="actorOutput">The actor output to queue.</param>
        private void QueueActorOutput(ActorOutput actorOutput) {
            // Flag that audio has started playing.
            // Even though it doesn't actually play until the audio thread plays it, this flag has to be set here and not in the audio thread to ensure that the main thread doesn't start listening to user input again.
            // Because in theory it can be a few frames before the audio thread gets to it and we don't want to accidentally trigger listening for user input in the mean time.
            isAudioPlaying = true;

            // Ensure that the audio clip is added to the garbage collector before being queued.
            // This is so that it is impossible for the audio thread to mark a clip as finished and putting it into the garbage collector before we do it here, causing a double insert.
            // Because the audio thread and the main thread runs concurrently.
            if (actorOutput.audioClip != null) {
                lock (audioClipGarbageCollector) {
                    audioClipGarbageCollector.Add(actorOutput.audioClip, false);
                }
            }

            // Actually queue the actor output to be played.
            lock (actorOutputQueue) {
                actorOutputQueue.Enqueue(actorOutput);
            }
        }

        /// <summary>
        /// Launch an audio thread via Coroutine to play any queued audio from the actors.
        /// </summary>
        /// <returns>An IEnumerator for Coroutine.</returns>
        private IEnumerator AudioThread() {
            // We do not play the audio in the main thread because it is an infinite loop and we don't want to hang the main thread.
            Debug.Log("Starting audio thread...");

            // Run this loop until the MonoBehaviour is destroyed.
            AudioSource playingAudioSource = null; 
            while (!destroyCancellationToken.IsCancellationRequested) {
                // If there is already an audio clip playing, wait for it to be done.
                if (playingAudioSource != null && playingAudioSource.isPlaying) {
                    yield return null;
                    continue;
                }

                // Mark this audio clip as finished and ready to be deleted.
                if (playingAudioSource != null && playingAudioSource.clip != null) {
                    lock (audioClipGarbageCollector) {
                        audioClipGarbageCollector[playingAudioSource.clip] = true;
                    }
                }

                playingAudioSource = null;

                // If no audio clip is currently playing, try to see if there's any audio clip in the queue.
                bool hasOutput = false;
                ActorOutput actorOutput = null;
                lock (actorOutputQueue) {
                    hasOutput = actorOutputQueue.TryDequeue(out actorOutput);
                }

                // If there is an audio clip, play it.
                if (hasOutput) {
                    playingAudioSource = actorOutput.audioSource;
                    playingAudioSource.clip = actorOutput.audioClip;
                    if (actorOutput.audioSource != null && actorOutput.audioClip != null) {
                        playingAudioSource.Play();
                    }

                    Debug.Log($"{actorOutput.persona} is {actorOutput.emotion.ToString()}");
                    chatbox.AddMessage(actorOutput.persona, actorOutput.message);
                    if (showReasoning) {
                        for (int i = 0; i < actorOutput.reasonings.Count; ++i) {
                            chatbox.AddMessage(actorOutput.persona + $"'s Reasoning {i + 1}", actorOutput.reasonings[i]);
                        }
                    }
                }
                // Otherwise, if there's no more audio clips in the queue, and the main thread isn't queuing any more audio clips, reset the isAudioPlaying flag.
                else if (!isQueueingAudio) {
                    isAudioPlaying = false;
                }

                // Wait for next frame to save some CPU cycles.
                // It's unlikely for an audio clip to finish playing in 1 frame so there's no point checking the if the audio source is done playing.
                // No point running this loop a bajillion times per frame.
                yield return null;
            }

            Debug.Log("Shutting down audio thread...");
        }

        /// <summary>
        /// Send the message received from one actor, to all the other actors.
        /// </summary>
        /// <param name="speaker">The actor that the message was received from.</param>
        /// <param name="message">The actor's message.</param>
        private void PropogateActorMessage(Actor speaker, string message) {
            foreach (var item in actors) {
                Actor actor = item.Value.Item1;
                if (actor == speaker) continue;
                actor.AddUserMessage("@" + speaker.Persona + " " + message);
            }
        }

        /// <summary>
        /// Send the message received from the user to all the actors.
        /// </summary>
        /// <param name="message">The user's message.</param>
        private void PropogateUserMessage(string message) {
            foreach (var item in actors) {
                Actor actor = item.Value.Item1;
                actor.AddUserMessage("@User " + message);
            }
        }

        /// Flag that audio is being queued.
        private void BeginQueuingAudio() {
            // We can use a boolean because audio is only being queued by the main thread.
            // If there ever comes a time where it is possible for multiple threads to queue the audio concurrently, change this to a mutex protected integer increment everything a thread begins queuing audio.
            isQueueingAudio = true;
        }

        /// Flag that audio is not queued.
        private void EndQueuingAudio() {
            // We can use a boolean because audio is only being queued by the main thread.
            // If there ever comes a time where it is possible for multiple threads to queue the audio concurrently, change this to a mutex protected integer decrement everything a thread begins queuing audio.
            isQueueingAudio = false;
        }

        // Idle State
        private void OnEnterIdle() { Debug.Log("VoiceChat: OnEnterIdle"); }

        // Prepare State
        private void OnEnterPrepare() {
            Debug.Log("VoiceChat: OnEnterPrepare");

            // Increase round counter && update GUI.
            ++roundCounter;
            if (roundCounterText != null) { roundCounterText.text = $"Round {roundCounter}"; }

            // If there is a discussion to be triggered, trigger it instead of asking the director to listen.
            for (int i = 0; i < untriggeredDiscussions.Count; ++i) {
                Discussion discussion = untriggeredDiscussions[i];
                if (discussion.HasAllTriggerModes(Discussion.TriggerMode.Round) &&
                    discussion.GetTriggerRound() <= roundCounter) {
                    discussionQueue.Enqueue(discussion);
                    untriggeredDiscussions.RemoveAt(i);
                    fsm.ChangeState((int)State.Discuss);
                    return;
                }
            }

            // Otherwise, wait for the director to be ready to listen for user input.
            fsm.ChangeState((int)State.Wait);
        }

        // Wait State
        private void OnEnterWait() { Debug.Log("VoiceChat: OnEnterWait"); }

        private void OnUpdateWait() {
            if (!isAudioPlaying) {
                // If no audio is playing and the game should finish, and we did not trigger continue chat, go to the finish state.
                if (!continueChat && roundCounter >= finishRound) {
                    fsm.ChangeState((int)State.Finish);
                }
                // If the director is connected to OpenAI's server and no audio is playing, listen for user input.
                else if (director.IsConnected && director.IsStatus(Director.Status.Waiting)) {
                    fsm.ChangeState((int)State.Listen);
                }
            }
        }

        // Listen State
        private void OnEnterListen() {
            Debug.Log("VoiceChat: OnEnterListen");

            idleTimer = 0.0f;
            discussionQueue.Clear();
            speakerQueue.Clear();

            // Let the director know the name of the actors so that it can determine the speaking order when replying to the user.
            List<string> speakers = actors.Keys.ToList();

            // Let the director know which discussions it can trigger based on topic.
            List<string> topics = untriggeredDiscussions.
                Where(d => d.HasAllTriggerModes(Discussion.TriggerMode.Topic)).
                Select(d => d.GetTriggerTopic()).ToList();

            // Get the direction to listen for user input.
            director.ListenForNextUserInput(directorConfig, speakers, topics, destroyCancellationToken);
        }

        private void OnUpdateListen() {
            // Update Idle Timer
            if (director.IsMicMuted) { idleTimer += Time.deltaTime; }
            if (idleTimerText != null) { idleTimerText.text = $"Idle Time: {idleTimer.ToString("n2")}s"; }

            // Check if we should trigger any idle discussions if the user has not given any input after a while. This ends the round.
            for (int i = 0; i < untriggeredDiscussions.Count; ++i) {
                Discussion discussion = untriggeredDiscussions[i];
                // Check if we should trigger this idle discussion.
                if (discussion.HasAllTriggerModes(Discussion.TriggerMode.IdleTime) && discussion.GetTriggerIdleTime() <= idleTimer) {
                    // If the director has already stopped listening, abort.
                    if (!director.CancelListen()) {
                        Debug.Log("Aborting idle discussion as director has already started responding.");
                        break;
                    }

                    // Else, trigger idle discussion.
                    discussionQueue.Enqueue(discussion);
                    untriggeredDiscussions.RemoveAt(i);
                    fsm.ChangeState((int)State.Discuss);
                    return;
                }
            }
        }

        // Speak State
        private async void RunSpeak() {
            BeginQueuingAudio();

            // Get a response from every actor.
            while (0 < speakerQueue.Count) {
                var (actor, audioSource) = actors.GetValueOrDefault(speakerQueue.Dequeue(), (null, null));
                if (actor == null || audioSource == null) {
                    Debug.LogWarning("Actor or AudioSource is null!");
                    continue;
                }

                Actor.Response actorResponse = await actor.GetResponse(destroyCancellationToken);
                QueueActorOutput(new ActorOutput(actor.Persona, actorResponse.message, actorResponse.emotion, actorResponse.reasonings, actorResponse.audioClip, audioSource));
                PropogateActorMessage(actor, actorResponse.message);
            }

            EndQueuingAudio();
            fsm.ChangeState((int)State.Prepare);
        }

        private void OnEnterSpeak() {
            Debug.Log("VoiceChat: OnEnterSpeak");
            RunSpeak(); // Run the logic asynchronously so that it does not hang the main thread.
        }

        // Discussion State
        private async void RunScriptedDiscussion(ScriptedDiscussion discussion) {
            BeginQueuingAudio();
            
            foreach (ScriptedDiscussion.Dialogue dialogue in discussion.GetDialogues()) {
                var (actor, audioSource) = actors.GetValueOrDefault(dialogue.speaker.ToString(), (null, null));
                if (actor == null || audioSource == null) {
                    Debug.LogWarning("Actor or AudioSource is null!");
                    continue;
                }

                Actor.Response actorResponse = await actor.InsertResponse(dialogue.message, dialogue.emotion, destroyCancellationToken);
                QueueActorOutput(new ActorOutput(actor.Persona, actorResponse.message, actorResponse.emotion, actorResponse.reasonings, actorResponse.audioClip, audioSource));
                PropogateActorMessage(actor, actorResponse.message);
            }

            EndQueuingAudio();
            fsm.ChangeState((int)State.Prepare);
        }

        private async void RunGeneratedDiscussion(GeneratedDiscussion discussion) {
            BeginQueuingAudio();

            List<Persona> speakers = discussion.GenerateRandomSpeakerOrder();
            foreach (Persona speaker in speakers) {
                var (actor, audioSource) = actors.GetValueOrDefault(speaker.ToString(), (null, null));
                if (actor == null || audioSource == null) {
                    Debug.LogWarning("Actor or AudioSource is null!");
                    continue;
                }

                actor.AddSystemMesssage(discussion.GetDiscussionPrompt());
                Actor.Response actorResponse = await actor.GetResponse(destroyCancellationToken);
                QueueActorOutput(new ActorOutput(actor.Persona, actorResponse.message, actorResponse.emotion, actorResponse.reasonings, actorResponse.audioClip, audioSource));
                PropogateActorMessage(actor, actorResponse.message);
            }

            EndQueuingAudio();
            fsm.ChangeState((int)State.Prepare);
        }

        private void OnEnterDiscuss() {
            Debug.Log("VoiceChat: OnEnterDiscussion");
            Discussion discussion = discussionQueue.Dequeue();

            // Run the logic asynchronously so that it does not hang the main thread.
            switch (discussion) {
                case ScriptedDiscussion:
                    Debug.Log("VoiceChat: Starting scripted discussion...");
                    RunScriptedDiscussion((ScriptedDiscussion)discussion);
                    break;
                case GeneratedDiscussion:
                    Debug.Log("VoiceChat: Starting generated discussion...");
                    RunGeneratedDiscussion((GeneratedDiscussion)discussion);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        // Finish State
        private void OnEnterFinish() {
            Debug.Log("VoiceChat: OnEnterFinish");

            // Let all listeners know that the game has finished.
            GameEvent.GameEventSystem.GetInstance().TriggerEvent(nameof(GameEventName.GameFinish));
        }

        // Game Event Callbacks
        private void OnGameStart() {
            if (fsm.GetCurrentState() == (int)State.Idle) {
                fsm.ChangeState((int)State.Prepare);
            }
        }

        private void OnGameContinue() {
            if (fsm.GetCurrentState() == (int)State.Finish) {
                continueChat = true;
                fsm.ChangeState((int)State.Wait);
            }
        }

        // Input Callbacks
        private void OnPushToTalkStarted(UnityEngine.InputSystem.InputAction.CallbackContext context) { ToggleMicMute(); }

        private void OnPushToTalkCancelled(UnityEngine.InputSystem.InputAction.CallbackContext context) { ToggleMicMute(); }

        /// Callback invoked by the director when it has a response ready.
        /// <param name="response">The director's response.</param>
        private void OnDirectorResponse(Director.Response response) {
            // We only care about a response when we are in the listening state.
            if (fsm.GetCurrentState() != (int)State.Listen) { return; }

            // Add the user transcript to the chatbox.
            chatbox.AddMessage("User", response.userTranscript);
            // Inform each actor of what the user said.
            PropogateUserMessage(response.userTranscript);

            // Try to trigger a discussion.
            if (response.discussionTopic != null) {
                for (int i = 0; i < untriggeredDiscussions.Count; ++i) {
                    Discussion discussion = untriggeredDiscussions[i];
                    if (discussion.GetTriggerTopic() == response.discussionTopic) {
                        discussionQueue.Enqueue(discussion);
                        untriggeredDiscussions.RemoveAt(i);
                        break;
                    }
                }
                
                fsm.ChangeState((int)State.Discuss);
            }
            // Otherwise, get the actors to respond as per usual.
            else if (response.speakerOrder != null) {
                foreach (string speaker in response.speakerOrder) {
                    speakerQueue.Enqueue(speaker);
                }
                
                fsm.ChangeState((int)State.Speak);
            }
        }
    }
}