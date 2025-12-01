// By Terri Lim, CMU ETC Class of 2026. Last updated by me in December 2025. Feel free to judge any code up till then.
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI;
using OpenAI.Models;
using OpenAI.Realtime;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using Utilities.Audio;
using Utilities.Encoding.Wav;

namespace EchoTrio {
    /// The director is the OpenAI Realtime model whose main function is to listen to the user's speech and decides the order in which the actors reply.
    /// For example, Athena can speak first, or Poseidon can speak first, or only one of them replies.
    /// The director has a secondary function of triggering a Discussion if the user mentions certain topics.
    public class Director {
        /// Current status of the director.
        public enum Status {
            /// Idling and waiting for VoiceChat system.
            Waiting,
            /// Listening for user input.
            Listening,
            /// Replying to text input.
            TextInput,
            /// Replying to voice input.
            VoiceInput,
        }

        /// Helper class to act as a mutex for the status value, as it may be read by multiple threads.
        public class StatusMutex {
            public Status value = Status.Waiting;
        }

        public class Response {
            public string userTranscript = null;
            public List<string> speakerOrder = null;
            public string discussionTopic = null;

            public bool IsDone => userTranscript != null && (speakerOrder != null || discussionTopic != null);
        }

        // Public Properties
        public bool EnableDebug { get; set; } = false;
        public bool IsMicMuted { get; set; } = false;
        public bool IsConnected { get; private set; } = false;

        // Internal Variables & Properties
        private DirectorConfig config = null;
        private OpenAIClient api = null;
        private RealtimeSession session = null;
        private Director.Response response = null;
        private UnityAction<Director.Response> onDirectorResponse = null;
        private List<OpenAI.Tool> tools = new List<OpenAI.Tool>();

        // Concurrency/Async Variables
        private StatusMutex statusMutex = new StatusMutex();
        private int inputAudioBufferCommittedResponseCounter = 0;
        private int conversationItemInputAudioTranscriptionResponseCounter = 0;

        // Public Interface
        public Director() {
            // Initialise OpenAI
            api = new OpenAIClient(Authentication.GetOpenAIAuthentication()) { EnableDebug = this.EnableDebug };
        }

        /// Initialise the director and connect to OpenAI's server.
        /// <param name="onDirectorResponse">The callback to invoke when the director has a response ready.</param>
        /// <param name="cancellationToken">Cancellation token used to cancel any async actions when the program shuts down.</param>
        public void Initialise(UnityAction<Director.Response> onDirectorResponse, CancellationToken cancellationToken) {
            // Set the callback to inform the VoiceChat system whenever a director response is ready.
            this.onDirectorResponse = onDirectorResponse;

            // Run the director session in a separate thread.
            Func<CancellationToken, Task> run = async (CancellationToken cancellationToken) => {
                try {
                    // Create session.
                    session = await api.RealtimeEndpoint.CreateSessionAsync(GetSessionConfiguration(), cancellationToken);
                    // Start recording user audio.
                    RecordInputAudio(session, cancellationToken);
                    // Let the VoiceChat system know we are ready to receive input.
                    IsConnected = true;
                    // Receive server response in a loop.
                    await session.ReceiveUpdatesAsync<IServerEvent>(OnServerEvent, cancellationToken);
                } catch (Exception e) {
                    switch (e) {
                        case TaskCanceledException: break;
                        case OperationCanceledException: break;
                        default: Debug.LogException(e); break;
                    }
                } finally {
                    session?.Dispose();
                    session = null;
                    Debug.Log("Director's session disposed.");
                }
            };
            _ = run(cancellationToken);
        }

        public bool IsStatus(Status value) { lock (statusMutex) { return statusMutex.value == value; } }

        /// Listens for the next user input. This needs to be invoked at the start of every round, in order to let the director prepare for user input.
        /// Before invoking, you need to check that the status is Waiting.
        /// <param name="config">The director configuration.</param>
        /// <param name="speakers">The list of actors that are possibly speaking. The speaker order will be determined by choosing actors from this list.</param>
        /// <param name="topics">Possible discussion topics to be triggered by the user input.</param>
        /// <param name="cancellationToken">Cancellation token used to cancel any async actions when the program shuts down.</param>
        public async void ListenForNextUserInput(DirectorConfig config, List<string> speakers, List<string> topics, CancellationToken cancellationToken) {
            if (!IsStatus(Status.Waiting)) {
                throw new System.Exception("Director.ListenForNextInput can only be invoked if the status is Waiting!");
            }

            // Update director session configuration.
            this.config = config;
            this.tools = new List<OpenAI.Tool>() { BuildTriggerResponseTool(speakers), BuildTriggerDiscussionTool(topics) };
            this.response = new Director.Response();

            try {
                await session.SendAsync(new UpdateSessionRequest(GetSessionConfiguration()), cancellationToken);
            } catch (Exception e) {
                Debug.LogException(e);
            }

            // Starting listening to the human user.
            SetStatus(Status.Listening);
        }

        public bool CancelListen() {
            lock (statusMutex) {
                if (statusMutex.value == Status.Listening) {
                    statusMutex.value = Status.Waiting;
                    return true;
                }
                return false;
            }
        }

        /// Submit the user text input. Used as an alternative to speaking into the microphone, usually for development & debugging purposes.
        /// <param name="message">The user text input.</param>
        /// <param name="cancellationToken">Cancellation token used to cancel any async actions when the program shuts down.</param>
        public async Awaitable<bool> SubmitUserTextInput(string message, CancellationToken cancellationToken) {
            // Check for status and update it.
            if (!TestAndSetStatus(Status.Listening, Status.TextInput)) { return false; }

            // Tell the director to clear everything it has heard.
            await session.SendAsync(new InputAudioBufferClearRequest(), cancellationToken);

            // Now tell it to reply to our text input.
            response.userTranscript = message;
            await session.SendAsync(new OpenAI.Realtime.ConversationItemCreateRequest(message), cancellationToken);
            await session.SendAsync(new OpenAI.Realtime.CreateResponseRequest(), cancellationToken);

            return true;
        }

        // Internal Functions
        private OpenAI.Realtime.SessionConfiguration GetSessionConfiguration() {
            return new OpenAI.Realtime.SessionConfiguration(
                Model.GPT4oRealtime,
                modalities: Modality.Text, // Text only since Director is not speaking to user directly.
                instructions: this.config ? this.config.instructions : null,
                inputAudioTranscriptionSettings: new OpenAI.Realtime.InputAudioTranscriptionSettings(Model.Transcribe_GPT_4o, language: "en"), // The settings we use to transcribe what the human says. Without this, the human's speech will not get transcibed. Apparently the language setting is fucking useless.
                turnDetectionSettings: new OpenAI.Realtime.ServerVAD(silenceDuration: 2000, createResponse: false), // We want Server VAD so that the AI automatically detects when speech starts or ends. But we don't want it to automatically trigger a response, because we have to make sure that a text input isn't already sent.
                tools: this.tools,
                toolChoice: "required"); // Set to auto or required to allow the AI to use tools.
        }

        private async void RecordInputAudio(OpenAI.Realtime.RealtimeSession session, CancellationToken cancellationToken) {
            var memoryStream = new MemoryStream();
            var semaphore = new SemaphoreSlim(1, 1);

            try {
                byte[] emptyBuffer = new byte[1024 * 16]; // 1 KB buffer.
                async Task BufferCallback(NativeArray<byte> buffer) {
                    try {
                        await semaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                        for (int i = 0; i < buffer.Length; ++i) { memoryStream.WriteByte(buffer[i]); }
                    } finally {
                        semaphore.Release();
                    }
                }

                // RecordingManager is from the com.utilities.audio package.
                // We don't await this so that we can implement buffer copy and send response to realtime API.
                RecordingManager.StartRecordingStream<WavEncoder>(BufferCallback, 24000, cancellationToken); // Sample rate has to be 24000 according to the InputAudioBufferAppendRequest API docs.

                do {
                    byte[] voiceBuffer = ArrayPool<byte>.Shared.Rent(1024 * 16); // 16 KB buffer.
                    try {
                        int bytesRead = 0;
                        try {
                            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            memoryStream.Position = 0;
                            bytesRead = await memoryStream.ReadAsync(voiceBuffer, 0, (int)Math.Min(voiceBuffer.Length, memoryStream.Length), cancellationToken).ConfigureAwait(false);
                            memoryStream.SetLength(0);
                        } finally {
                            semaphore.Release();
                        }

                        if (bytesRead > 0) {
                            // If we are recording, send what the microphone picks up.
                            if (!IsMicMuted && IsStatus(Status.Listening)) {
                                await session.SendAsync(new InputAudioBufferAppendRequest(voiceBuffer.AsMemory(0, bytesRead)), cancellationToken).ConfigureAwait(false);
                            }
                            // Otherwise, send silence. We want to continue sending data so that the model can trigger a response if it received silence.
                            else {
                                await session.SendAsync(new InputAudioBufferAppendRequest(emptyBuffer.AsMemory(0, bytesRead)), cancellationToken).ConfigureAwait(false);
                            }
                        } else {
                            await Task.Yield();
                        }
                    } catch (Exception e) {
                        switch (e) {
                            // Ignored
                            case TaskCanceledException: break;
                            case OperationCanceledException: break;
                            default: Debug.LogError(e); break;
                        }
                    } finally {
                        ArrayPool<byte>.Shared.Return(voiceBuffer);
                    }
                } while (!cancellationToken.IsCancellationRequested);

                RecordingManager.EndRecording();
            } catch (Exception e) {
                switch (e) {
                    // Ignored
                    case TaskCanceledException: break;
                    case OperationCanceledException: break;
                    default: Debug.LogError(e); break;
                }
            } finally {
                await memoryStream.DisposeAsync();
            }
        }

        /// If the director's response is ready, invoke the response callback.
        /// <remarks>
        /// Note that user transcript can come before or after the Response event!Note that user transcript can come before or after the Response event!
        /// OpenAI API Link: https://platform.openai.com/docs/api-reference/realtime-server-events/conversation/item/input_audio_transcription?utm_source=chatgpt.com
        /// </remarks>
        private void InvokeOnDirectorResponse() {
            if (response != null && response.IsDone) {
                onDirectorResponse?.Invoke(response);
                response = null;
                SetStatus(Status.Waiting);
            }
        }

        /// Callback function to receive server events from OpenAI.
        /// <param name="event">The event received from OpenAI.</param>
        private void OnServerEvent(IServerEvent @event) {
            switch (@event) {
                case RealtimeEventError error: throw error;
                case SessionResponse sessionResponse: break;
                case RealtimeConversationResponse conversationResponse: break;
                case ConversationItemCreatedResponse conversationItemCreated: break;
                case ConversationItemInputAudioTranscriptionResponse conversationItemTranscription:
                    if (!conversationItemTranscription.IsCompleted) { return; }
                    Debug.Log($"Director's User Transcription: " + conversationItemTranscription.Transcript.Trim());

                    Interlocked.Increment(ref conversationItemInputAudioTranscriptionResponseCounter);

                    // We only want to update the user transcript if this is the latest transcript.
                    // This is to handle the insane case where:
                    // 1. User sends text and voice input at the same time.
                    // 2. Voice input is ignored, but a InputAudioBufferCommittedResponse was received.
                    // 3. ConversationItemInputAudioTranscriptionResponse was not received.
                    // 4. User sends voice input.
                    // 5. Second InputAudioBufferCommittedResponse is received.
                    // 6. First outdated ConversationItemInputAudioTranscriptionResponse is received. (This needs to be ignored.)
                    // 7. Second correct ConversationItemInputAudioTranscriptionResponse is received.
                    if (conversationItemInputAudioTranscriptionResponseCounter != inputAudioBufferCommittedResponseCounter) {
                        Debug.Log("Director.OnServerEvent ConversationItemInputAudioTranscriptionResponse ignored as it is outdated.");
                        return;
                    }

                    if (IsStatus(Status.VoiceInput)) {
                        response.userTranscript = conversationItemTranscription.Transcript.Trim();
                        InvokeOnDirectorResponse();
                    } else {
                        Debug.Log("Director.OnServerEvent ConversationItemInputAudioTranscriptionResponse ignored as the status is not ReplyToVoice.");
                    }
                    break;
                case ConversationItemTruncatedResponse conversationItemTruncated: break;
                case ConversationItemDeletedResponse conversationItemDeleted: break;
                case InputAudioBufferCommittedResponse committedResponse:
                    Debug.Log($"Director.OnServerEvent InputAudioBufferCommittedResponse");

                    // This should ALWAYS increment before conversationItemInputAudioTranscriptionResponseCounter.
                    Interlocked.Increment(ref inputAudioBufferCommittedResponseCounter);

                    if (TestAndSetStatus(Status.Listening, Status.VoiceInput)) {
                        session.Send(new OpenAI.Realtime.CreateResponseRequest()); // Now tell it to reply to our audio input.
                    } else {
                        Debug.Log("Director.OnServerEvent InputAudioBufferCommittedResponse ignored.");
                    }
                    break;
                case InputAudioBufferClearedResponse clearedResponse:
                    Debug.Log($"Director.OnServerEvent InputAudioBufferClearedResponse");
                    break;
                case InputAudioBufferStartedResponse startedResponse:
                    Debug.Log($"Director.OnServerEvent InputAudioBufferStartedResponse");
                    break;
                case InputAudioBufferStoppedResponse stoppedResponse: break;
                case RealtimeResponse realtimeResponse:
                    switch (realtimeResponse.Response.Status) {
                        case RealtimeResponseStatus.InProgress:
                            Debug.Log("Director Realtime Response InProgress.");
                            break;
                        case RealtimeResponseStatus.Completed:
                            Debug.Log("Director Realtime Response Completed.");
                            InvokeOnDirectorResponse();
                            break;
                        case RealtimeResponseStatus.Cancelled:
                            Debug.Log("Director Realtime Response Cancelled.");
                            break;
                        case RealtimeResponseStatus.Failed:
                            Debug.Log("Director Realtime Response Failed.");
                            break;
                        case RealtimeResponseStatus.Incomplete:
                            Debug.Log("Director Realtime Response Incomplete.");
                            break;
                    }
                    break;
                case ResponseOutputItemResponse outputItemResponse: break;
                case ResponseContentPartResponse contentPartResponse: break;
                case ResponseTextResponse textResponse: break; // Used if modality is Modality.Text only.
                case ResponseAudioResponse audioResponse: break;
                case ResponseAudioTranscriptResponse transcriptResponse: break; // Used if modality has Modality.Audio.
                case ResponseFunctionCallArgumentsResponse functionCallArgumentsResponse:
                    if (!functionCallArgumentsResponse.IsDone) { return; }
                    Debug.Log($"Director's Function Call: " + functionCallArgumentsResponse.Name + ", Arguments: " + functionCallArgumentsResponse.Arguments.ToString());

                    // Handle function calls.
                    string output = string.Empty;
                    if (functionCallArgumentsResponse.Name == "trigger_discussion") {
                        response.discussionTopic = ParseDiscussionTopic(functionCallArgumentsResponse.Arguments.ToString());
                    } else if (functionCallArgumentsResponse.Name == "trigger_response") {
                        response.speakerOrder = ParseSpeakerOrder(functionCallArgumentsResponse.Arguments.ToString());
                    }

                    // Return the function call output to the model.
                    ConversationItem functionCallOutput = new ConversationItem((ToolCall)functionCallArgumentsResponse, output);
                    session.Send(new OpenAI.Realtime.ConversationItemCreateRequest(functionCallOutput));
                    break;
                case RateLimitsResponse rateLimitsResponse: break;
                default: break;
            }
        }

        private void SetStatus(Status value) { lock (statusMutex) { statusMutex.value = value; } }

        private bool TestAndSetStatus(Status condition, Status value) {
            lock (statusMutex) {
                if (statusMutex.value == condition) {
                    statusMutex.value = value;
                    return true;
                }
                return false;
            }
        }

        // Director Tools
        /// Create a function following OpenAI's JSON Schema for the director to decide upon the speaker order.
        /// OpenAI API on function calling: https://platform.openai.com/docs/guides/function-calling
        /// <param name="speakers">The names of the speakers.</param>
        /// <returns>The function's JSON Object.</returns>
        private OpenAI.Function BuildTriggerResponseTool(List<string> speakers) {
            var args = new {
                type = "object",
                properties = new {
                    speaker_order = new {
                        type = "array",
                        description = "The order which the AI models will respond to the user.",
                        items = new {
                            type = "string",
                            @enum = speakers.ToArray() // Adding an enum means that the AI can only pick from this set of values. (Well, the AI is stupid and still sometimes hallucinates invalid values.)
                        },
                        minItems = 1,
                        maxItems = speakers.Count,
                        uniqueItems = true
                    }
                },
                required = new[] { "speaker_order" }
            };
            string parameters = JsonConvert.SerializeObject(args, Formatting.Indented);
            return new OpenAI.Function("trigger_response", "Triggers the AI models to respond to the user. No output is given.", JToken.Parse(parameters));
        }

        /// Create a function following OpenAI's JSON Schema for the director to trigger a discussion based on a topic.
        /// OpenAI API on function calling: https://platform.openai.com/docs/guides/function-calling
        /// <param name="topics">The possible topics to trigger a discussion for.</param>
        /// <returns>The function's JSON Object.</returns>
        private OpenAI.Function BuildTriggerDiscussionTool(List<string> topics) {
            var args = new {
                type = "object",
                properties = new {
                    topic = new {
                        type = "string",
                        description = "The topic of the discussion to trigger.",
                        @enum = topics.ToArray() // Adding an enum means that the AI can only pick from this set of values. (Well, the AI is stupid and still sometimes hallucinates invalid values.)
                    }
                },
                required = new[] { "topic" }
            };
            string parameters = JsonConvert.SerializeObject(args, Formatting.Indented);
            return new OpenAI.Function("trigger_discussion", "Triggers a discussion based on a topic. No output is given.", JToken.Parse(parameters));
        }

        private List<string> ParseSpeakerOrder(string args) {
            JToken parsedArgs = JToken.Parse(args);
            if (parsedArgs == null || parsedArgs["speaker_order"] == null) {
                return new List<string>();
            }
            return new List<string>(parsedArgs["speaker_order"].ToObject<string[]>());
        }

        private string ParseDiscussionTopic(string args) {
            JToken parsedArgs = JToken.Parse(args);
            if (parsedArgs == null || parsedArgs["topic"] == null) {
                return string.Empty;
            }
            return parsedArgs["topic"].ToString();
        }
    }
}
