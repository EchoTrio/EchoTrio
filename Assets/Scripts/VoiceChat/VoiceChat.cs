using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace EchoTrio {
    public class VoiceChat : MonoBehaviour {
        private enum State { Invalid = -1, Prepare, Wait, Listen, Speak, Discuss, Num }

        [System.Serializable]
        public class ActorReferences {
            public ActorConfig actorConfig;
            public AudioSource audioSource;
        }

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

        [Header("GUI References (Do not change unless you know what you're doing!)")]
        [SerializeField] private Chatbox chatbox = null;
        [SerializeField] private SpriteSwitcher micMuteButton = null;
        [SerializeField] private SpriteSwitcher listeningIcon = null;
        [SerializeField] private SpriteSwitcher listeningIcon2 = null;
        [SerializeField] private TMPro.TextMeshProUGUI roundCounterText = null;
        [SerializeField] private TMPro.TextMeshProUGUI idleTimerText = null;

        [Header("Configurations")]
        [SerializeField] private DirectorConfig directorConfig = null;
        [SerializeField] private ActorReferences[] actorReferences = new ActorReferences[0];

        [Header("Discussions")]
        [SerializeField] private Discussion[] discussions = new Discussion[0];

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
        private float idleTimerBuffer = 10.0f; // The idle timer needs to be about 10s to reduce chances of a race condition. (This is not a real fix, but works for 99.99% of cases).

        // Audio
        private bool isAudioPlaying = false;
        private Queue<ActorOutput> actorOutputQueue = new Queue<ActorOutput>();
        private Dictionary<AudioClip, bool> audioClipGarbageCollector = new Dictionary<AudioClip, bool>();

        // Discuss Variables
        private List<Discussion> untriggeredDiscussions = null;
        private Queue<Discussion> discussionQueue = new Queue<Discussion>();

        // Speak Variables
        private Queue<string> speakerQueue = new Queue<string>();

        // Public Interfaces
        public void ToggleMicMute() {
            director.IsMicMuted = !director.IsMicMuted;
            if (director.IsMicMuted) { idleTimer = 0.0f; }
        }

        public void ToggleChatbox() {
            chatbox.gameObject.SetActive(!chatbox.gameObject.activeSelf);
            chatbox.ScrollToBottom();
        }

        public void ResetIdleTimer() { idleTimer = 0.0f; }

        public bool SubmitUserTextInput(string message) {
            if (fsm.GetCurrentState() != (int)State.Listen) return false;

            director.SubmitUserTextInput(message, destroyCancellationToken);
            return true;
        }

        public int GetRoundCounter() { return roundCounter; }

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
            fsm.SetStateEntry((int)State.Prepare, OnEnterPrepare);
            fsm.SetStateEntry((int)State.Wait, OnEnterWait);
            fsm.SetStateUpdate((int)State.Wait, OnUpdateWait);
            fsm.SetStateEntry((int)State.Listen, OnEnterListen);
            fsm.SetStateUpdate((int)State.Listen, OnUpdateListen);
            fsm.SetStateEntry((int)State.Speak, OnEnterSpeak);
            fsm.SetStateEntry((int)State.Discuss, OnEnterDiscuss);
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
            gameInputActions.Enable();
            gameInputActions.VoiceChat.PushToTalk.started += OnPushToTalkStarted;
            gameInputActions.VoiceChat.PushToTalk.canceled += OnPushToTalkCancelled;
            gameInputActions.VoiceChat.ToggleChatbox.performed += OnToggleChatbox;
        }

        private void OnDisable() {
            gameInputActions.Disable();
            gameInputActions.VoiceChat.PushToTalk.started -= OnPushToTalkStarted;
            gameInputActions.VoiceChat.PushToTalk.canceled -= OnPushToTalkCancelled;
            gameInputActions.VoiceChat.ToggleChatbox.performed -= OnToggleChatbox;
        }

        private void Start() {
            StartCoroutine(AudioThread()); // Launch a thread to play audio.
            director.Initialise(OnDirectorResponse, destroyCancellationToken);
            fsm.ChangeState((int)State.Prepare);
        }

        private void Update() {
            fsm.Update();

            // Update GUI.
            if (listeningIcon != null) { listeningIcon.SetSprite(director.IsListening ? 1 : 0); }
            if (listeningIcon2 != null) { listeningIcon2.SetSprite(director.IsListening ? 1 : 0); }
            if (micMuteButton != null) { micMuteButton.SetSprite(director.IsMicMuted ? 1 : 0); }

            // Cleanup finished audio clips.
            lock (audioClipGarbageCollector) {
                foreach (var kv in audioClipGarbageCollector) {
                    if (kv.Value && kv.Key != null) { Destroy(kv.Key); }
                }
                audioClipGarbageCollector = audioClipGarbageCollector.Where(kv => !kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
            }
        }

        private void LateUpdate() {
            fsm.LateUpdate();
        }

        private void QueueActorOutput(ActorOutput actorOutput) {
            // Flag that audio has started playing.
            isAudioPlaying = true;

            // Ensure that the audio clip is added to the garbage collector first.
            // This is so that it is impossible for the audio thread to mark a clip as finished before it is placed into the garbage collector.
            if (actorOutput.audioClip != null) {
                lock (audioClipGarbageCollector) {
                    audioClipGarbageCollector.Add(actorOutput.audioClip, false);
                }
            }
            lock (actorOutputQueue) {
                actorOutputQueue.Enqueue(actorOutput);
            }
        }

        private IEnumerator AudioThread() {
            Debug.Log("Starting audio thread...");

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
                } else {
                    isAudioPlaying = false;
                }

                // Wait for next frame.
                yield return null;
            }

            Debug.Log("Shutting down audio thread...");
        }

        private void PropogateActorMessage(Actor speaker, string message) {
            foreach (var item in actors) {
                Actor actor = item.Value.Item1;
                if (actor == speaker) continue;
                actor.AddUserMessage("@" + speaker.Persona + " " + message);
            }
        }

        private void PropogateUserMessage(string message) {
            foreach (var item in actors) {
                Actor actor = item.Value.Item1;
                actor.AddUserMessage("@User " + message);
            }
        }

        // Prepare State
        private void OnEnterPrepare() {
            Debug.Log("On Enter Prepare");

            // Increase round counter && update GUI.
            ++roundCounter;
            if (roundCounterText != null) { roundCounterText.text = $"Round {roundCounter}"; }

            // If there is a discussion to be triggered, trigger it instead of asking the director to listen.
            for (int i = 0; i < untriggeredDiscussions.Count; ++i) {
                Discussion discussion = untriggeredDiscussions[i];
                if (discussion.HasTriggerModes(Discussion.TriggerMode.Round) &&
                    discussion.GetTriggerRound() <= roundCounter) {
                    discussionQueue.Enqueue(discussion);
                    untriggeredDiscussions.RemoveAt(i);
                    fsm.ChangeState((int)State.Discuss);
                    return;
                }
            }

            fsm.ChangeState((int)State.Wait);
        }

        // Wait State
        private void OnEnterWait() { Debug.Log("On Enter Wait"); }

        private void OnUpdateWait() {
            if (director.IsConnected && !isAudioPlaying) {
                fsm.ChangeState((int)State.Listen);
            }
        }

        // Listen State
        private void OnEnterListen() {
            Debug.Log("On Enter Listen");

            idleTimer = 0.0f;
            discussionQueue.Clear();
            speakerQueue.Clear();

            // Get the direction to listen for user input.
            List<string> speakers = actors.Keys.ToList();
            List<string> topics = untriggeredDiscussions.
                Where(d => d.HasTriggerModes(Discussion.TriggerMode.Topic)).
                Select(d => d.GetTriggerTopic()).ToList();
            director.ListenForNextUserInput(directorConfig, speakers, topics, destroyCancellationToken);
        }

        private void OnUpdateListen() {
            // Update Idle Timer
            if (director.IsMicMuted) { idleTimer += Time.deltaTime; }
            if (idleTimerText != null) {
                if (idleTimer < idleTimerBuffer) {
                    idleTimerText.text = $"Idle Buffer Time: {(idleTimerBuffer - idleTimer).ToString("n2")}s";
                } else {
                    idleTimerText.text = $"Idle Time: {(idleTimer - idleTimerBuffer).ToString("n2")}s";
                }
            }

            // Check if we should trigger any idle discussions.
            for (int i = 0; i < untriggeredDiscussions.Count; ++i) {
                Discussion discussion = untriggeredDiscussions[i];
                if (discussion.HasTriggerModes(Discussion.TriggerMode.IdleTime) &&
                    discussion.GetTriggerIdleTime() + idleTimerBuffer <= idleTimer) {
                    director.StopListening();
                    discussionQueue.Enqueue(discussion);
                    untriggeredDiscussions.RemoveAt(i);
                    fsm.ChangeState((int)State.Discuss);
                    return;
                }
            }
        }

        // Speak State
        private async void RunSpeak() {
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

            fsm.ChangeState((int)State.Prepare);
        }

        private void OnEnterSpeak() {
            Debug.Log("On Enter Speak");
            RunSpeak();
        }

        // Discussion State
        private async void RunScriptedDiscussion(ScriptedDiscussion discussion) {
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

            fsm.ChangeState((int)State.Prepare);
        }

        private async void RunGeneratedDiscussion(GeneratedDiscussion discussion) {
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

            fsm.ChangeState((int)State.Prepare);
        }

        private void OnEnterDiscuss() {
            Discussion discussion = discussionQueue.Dequeue();
            switch (discussion) {
                case ScriptedDiscussion:
                    Debug.Log("On Enter Scripted Discussion");
                    RunScriptedDiscussion((ScriptedDiscussion)discussion);
                    break;
                case GeneratedDiscussion:
                    Debug.Log("On Enter Generated Discussion");
                    RunGeneratedDiscussion((GeneratedDiscussion)discussion);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        // Input Callbacks
        private void OnPushToTalkStarted(UnityEngine.InputSystem.InputAction.CallbackContext context) { ToggleMicMute(); }

        private void OnPushToTalkCancelled(UnityEngine.InputSystem.InputAction.CallbackContext context) { ToggleMicMute(); }

        private void OnToggleChatbox(UnityEngine.InputSystem.InputAction.CallbackContext context) { ToggleChatbox(); }

        private void OnDirectorResponse(Director.Response response) {
            // Add the message to the chatbox.
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