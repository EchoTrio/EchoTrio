using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI;
using OpenAI.Models;
using OpenAI.Realtime;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using Utilities.Audio;
using Utilities.Encoding.Wav;

namespace EchoTrio {
    /// <summary>
    /// AI Model that decides the order which the two actors reply, or to trigger a discussion based on the topic raised by the human user.
    /// </summary>
    public class Director {
        public class Response {
            public string userTranscript = null;
            public List<string> speakerOrder = null;
            public string discussionTopic = null;

            public bool Done => userTranscript != null && (speakerOrder != null || discussionTopic != null);
        }

        private enum InputMode { TextInput, VoiceInput }

        // Public Properties
        public bool EnableDebug { get; set; } = false;
        public bool IsMicMuted { get; set; } = false;
        public bool IsListening { get; private set; } = false;
        public bool IsConnected { get; private set; } = false;

        // Internal Variables & Properties
        private DirectorConfig config = null;
        private OpenAIClient api = null;
        private RealtimeSession session = null;
        private Director.Response response = null;
        private UnityAction<Director.Response> onDirectorResponse = null;
        private List<OpenAI.Tool> tools = new List<OpenAI.Tool>();
        private InputMode inputMode = InputMode.VoiceInput;

        public Director() {
            // Initialise OpenAI
            api = new OpenAIClient(Authentication.GetOpenAIAuthentication()) { EnableDebug = this.EnableDebug };
        }

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

        public async void ListenForNextUserInput(DirectorConfig config, List<string> speakers, List<string> topics, CancellationToken cancellationToken) {
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
            IsListening = true;
            inputMode = InputMode.VoiceInput;
        }

        public void StopListening() { IsListening = false; }

        public async void SubmitUserTextInput(string message, CancellationToken cancellationToken) {
            if (!IsListening) return;

            // Stop listening.
            IsListening = false;
            inputMode = InputMode.TextInput;

            // Tell the director to clear everything it has heard.
            await session.SendAsync(new InputAudioBufferClearRequest(), cancellationToken);

            // Now tell it to reply to our text input.
            response.userTranscript = message;
            await session.SendAsync(new OpenAI.Realtime.ConversationItemCreateRequest(message), cancellationToken);
            await session.SendAsync(new OpenAI.Realtime.CreateResponseRequest(), cancellationToken);
        }

        // Internal Functions
        private OpenAI.Realtime.SessionConfiguration GetSessionConfiguration() {
            return new OpenAI.Realtime.SessionConfiguration(
                Model.GPT4oRealtime,
                modalities: Modality.Text, // Text only since Director is not speaking to user directly.
                instructions: this.config ? this.config.instructions : null,
                inputAudioTranscriptionSettings: new OpenAI.Realtime.InputAudioTranscriptionSettings(Model.Transcribe_GPT_4o, language: "en"), // The settings we use to transcribe what the human says. Without this, the human's speech will not get transcibed. Apparently the language setting is fucking useless.
                turnDetectionSettings: new OpenAI.Realtime.ServerVAD(silenceDuration: 2000), // We want Server VAD so that the AI automatically detects when speech starts or ends.
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
                            if (!IsMicMuted && IsListening) {
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

        private void InvokeOnDirectorResponse() {
            if (response != null && response.Done) {
                onDirectorResponse?.Invoke(response);
                response = null;
            }
        }

        private void OnServerEvent(IServerEvent @event) {
            switch (@event) {
                case RealtimeEventError error: throw error;
                case SessionResponse sessionResponse: break;
                case RealtimeConversationResponse conversationResponse: break;
                case ConversationItemCreatedResponse conversationItemCreated: break;
                case ConversationItemInputAudioTranscriptionResponse conversationItemTranscription:
                    if (inputMode == InputMode.VoiceInput && conversationItemTranscription.IsCompleted) {
                        Debug.Log("User: " + conversationItemTranscription.Transcript.Trim());
                        response.userTranscript = conversationItemTranscription.Transcript.Trim();
                        InvokeOnDirectorResponse();
                    }
                    break;
                case ConversationItemTruncatedResponse conversationItemTruncated: break;
                case ConversationItemDeletedResponse conversationItemDeleted: break;
                case InputAudioBufferCommittedResponse committedResponse:
                    // User has stopped speaking for this intercourse.
                    if (inputMode == InputMode.VoiceInput) {
                        IsListening = false;
                    }
                    break;
                case InputAudioBufferClearedResponse clearedResponse: break;
                case InputAudioBufferStartedResponse startedResponse: break;
                case InputAudioBufferStoppedResponse stoppedResponse: break;
                case RealtimeResponse realtimeResponse: break;
                case ResponseOutputItemResponse outputItemResponse: break;
                case ResponseContentPartResponse contentPartResponse: break;
                case ResponseTextResponse textResponse: break; // Used if modality is Modality.Text only.
                case ResponseAudioResponse audioResponse: break;
                case ResponseAudioTranscriptResponse transcriptResponse: break; // Used if modality has Modality.Audio.
                case ResponseFunctionCallArgumentsResponse functionCallArgumentsResponse:
                    if (!functionCallArgumentsResponse.IsDone) return;

                    Debug.Log("Director Function Call: " + functionCallArgumentsResponse.Name + ", Arguments: " + functionCallArgumentsResponse.Arguments.ToString());

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
                     
                    InvokeOnDirectorResponse();
                    break;
                case RateLimitsResponse rateLimitsResponse: break;
                default: break;
            }
        }

        // Director Tools
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
                        maxItems = speakers.Count * 2,
                        uniqueItems = false
                    }
                },
                required = new[] { "speaker_order" }
            };
            string parameters = JsonConvert.SerializeObject(args, Formatting.Indented);
            return new OpenAI.Function("trigger_response", "Triggers the AI models to respond to the user. No output is given.", JToken.Parse(parameters));
        }

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