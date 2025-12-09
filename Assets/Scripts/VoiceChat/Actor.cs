// By Terri Lim, CMU ETC Class of 2026. Last updated by me in December 2025. Feel free to judge any code up till then.
using ElevenLabs;
using ElevenLabs.TextToSpeech;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EchoTrio {
    /// The actors are the OpenAI Response model which chats with the user.
    public class Actor {
        [System.Serializable]
        public class Response {
            public string message = null;
            public Emotion emotion = Emotion.Neutral;
            public AudioClip audioClip = null;
            public List<string> reasonings = new List<string>();
        }

        private class OpenAISettings {
            public OpenAI.Models.Model model = OpenAI.Models.Model.GPT5;
            public ReasoningEffort reasoningEffort = ReasoningEffort.Low; // Must be at least ReasoningEffort.Low in order to use file search or web search.
            public List<string> include = new List<string>();
            public List<OpenAI.Tool> tools = new List<Tool>();

            public OpenAISettings(ActorConfig config) {
                // Choose the minimum reasoning effort that is required to run the enabled features.
                reasoningEffort = config.IsAnyFeatureEnabled(ActorConfig.Feature.WebSearch | ActorConfig.Feature.FileSearch | ActorConfig.Feature.Reasoning) ? ReasoningEffort.Low : ReasoningEffort.Minimal;

                // TODO: Change from WebSearchPreviewTool to WebSearchTool once the com.openai.unity library supports it.
                if (config.AreAllFeaturesEnabled(ActorConfig.Feature.WebSearch)) {
                    tools.Add(new WebSearchPreviewTool(SearchContextSize.Low)); // User Location: Optional Free Text City, ISO 3166-1 Country Code, Free Text State/Region, IANA Time Zone
                    include.Add("web_search_call.action.sources");
                }
                
                if (config.AreAllFeaturesEnabled(ActorConfig.Feature.FileSearch)) {
                    if (config.GetOpenAIVectorStoreID() != string.Empty) {
                        tools.Add(new FileSearchTool(config.GetOpenAIVectorStoreID(), maxNumberOfResults: 2));
                        include.Add("file_search_call.results");
                    } else {
                        Debug.LogWarning($"Actor {config.GetPersona()} has file search enabled but an empty file search vector store ID was provided!");
                    }
                }
            }
        }

        private class ElevenLabsSettings {
            public ElevenLabs.Models.Model expressionModel = new("eleven_v3");
            public ElevenLabs.Models.Model fastModel = ElevenLabs.Models.Model.FlashV2_5;
            public string languageCode = "en"; // ISO 639 Language Code
            public string voiceId = string.Empty;

            public ElevenLabsSettings(ActorConfig config) {
                if (!config.AreAllFeaturesEnabled(ActorConfig.Feature.AudioTags)) { expressionModel = fastModel; }
                voiceId = config.GetElevenLabsVoiceID();
            }
        }

        // OpenAI Internal Variables
        private OpenAIClient openAIApi = null;
        private OpenAISettings openAISettings = null;

        // ElevenLabs Internal Variables
        private ElevenLabsClient elevenLabsApi = null;
        private ElevenLabsSettings elevenLabsSettings = null;

        // Internal Variables
        /// The conversation history from this actor's point of view.
        private List<IResponseItem> conversation = new List<IResponseItem>();

        // Public Properties
        public string Persona { private set; get; }
        public bool EnableDebug { get; set; } = false;

        public Actor(ActorConfig config) {
            // Check for config overrides.
            config = config.Override();
            Persona = config.GetPersona().ToString();

            // Initialise OpenAI
            openAISettings = new OpenAISettings(config);
            openAISettings.tools.Add(BuildSetEmotionTool());

            openAIApi = new OpenAIClient(Authentication.GetOpenAIAuthentication()) { EnableDebug = this.EnableDebug };
            AddSystemMesssage(config.GetInstructions());

            // Initialise ElevenLabs
            elevenLabsSettings = new ElevenLabsSettings(config);
            elevenLabsApi = new ElevenLabsClient(Authentication.GetElevenLabsAuthentication()) { EnableDebug = this.EnableDebug };
        }

        /// Append a system message to the actor's conversation history.
        /// <param name="message">The message to append.</param>
        public void AddSystemMesssage(string message) {
            conversation.Add(new Message(OpenAI.Role.System, message));
        }

        /// Append a user message to the actor's conversation history.
        /// <param name="message">The message to append.</param>
        public void AddUserMessage(string message) {
            conversation.Add(new Message(OpenAI.Role.User, message));
        }

        /// Append an assistant message to the actor's conversation history. This means that we can make the actor think it said something, even if it did not. 
        /// Used for scripted speech, where we force the actor to say something the designer wrote.
        /// <param name="message">The message to append.</param>
        /// <param name="emotion">The emotion of the message.</param>
        /// <param name="cancellationToken">Cancellation token used to cancel any async actions when the program shuts down.</param>
        /// <returns>The actor's response.</returns>
        public async Task<Actor.Response> InsertResponse(string message, Emotion emotion, CancellationToken cancellationToken) {
            // Bit of a hack because assistant messages must now be of type output_text. Had to make my own custom class because the package's creator rejected my pull request.
            conversation.Add(new Message(OpenAI.Role.Assistant, new OutputTextContent(message)));
            return new Actor.Response() {
                message = message,
                emotion = emotion,
                audioClip = await GetAudioClipAsync(message, cancellationToken)
            };
        }

        /// Request the actor to generate a response based on the conversation history.
        /// <param name="cancellationToken">Cancellation token used to cancel any async actions when the program shuts down.</param>
        /// <returns>The actor's response.</returns>
        public async Task<Actor.Response> GetResponse(CancellationToken cancellationToken) {
            try {
                // Request a response from OpenAI.
                CreateResponseRequest request = new CreateResponseRequest(
                    input: conversation,
                    model: openAISettings.model,
                    tools: openAISettings.tools,
                    reasoning: new Reasoning(openAISettings.reasoningEffort, OpenAI.ReasoningSummary.Auto),
                    maxToolCalls: 0 < openAISettings.tools.Count ? openAISettings.tools.Count : null,
                    include: openAISettings.include);
                OpenAI.Responses.Response response = await openAIApi.ResponsesEndpoint.CreateModelResponseAsync(request, cancellationToken: cancellationToken);

                // Get response from OpenAI.
                Actor.Response actorResponse = new Actor.Response();
                for (int i = 0; i < response.Output.Count; ++i) {
                    IResponseItem responseItem = response.Output[i];
                    switch (responseItem) {
                        case OpenAI.Responses.Message message:
                            conversation.Add(message);
                            actorResponse.message = message.ToString();
                            actorResponse.audioClip = await GetAudioClipAsync(message.ToString(), cancellationToken);
                            break;
                        case OpenAI.Responses.ReasoningItem reasoningItem:
                            conversation.Add(reasoningItem);
                            List<string> reasonings = new List<string>();
                            foreach (OpenAI.Responses.ReasoningSummary reasoningSummary in reasoningItem.Summary) {
                                actorResponse.reasonings.Add(reasoningSummary.Text);
                            }
                            break;
                        case OpenAI.Responses.WebSearchToolCall webSearchToolCall:
                            Debug.Log("Actor " + Persona + " Searched Web");
                            break;
                        case OpenAI.Responses.FileSearchToolCall fileSearchToolCall:
                            Debug.Log("Actor " + Persona + " Searched Files");
                            break;
                        case OpenAI.Responses.FunctionToolCall functionToolCall:
                            Debug.Log("Actor " + Persona + " Function Call: " + functionToolCall.Name + ", Arguments: " + functionToolCall.Arguments.ToString());

                            // Handle function calls.
                            string output = string.Empty;
                            if (functionToolCall.Name == "set_emotion") {
                                output = ParseEmotion(functionToolCall.Arguments.ToString());
                                actorResponse.emotion = output.ToEmotion();
                            }

                            // Return the function call output to the model.
                            conversation.Add(functionToolCall);
                            conversation.Add(new FunctionToolCallOutput(functionToolCall, output));
                            return await GetResponse(cancellationToken);
                        default:
                            Debug.LogWarning("Actor.GetResponse: Unhandled " + responseItem.GetType().Name + " Received");
                            break;
                    }
                }
                return actorResponse;
            } catch (Exception e) {
                Debug.Log(e);
                return null;
            }
        }

        // Internal Functions
        private async Task<AudioClip> GetAudioClipAsync(string text, CancellationToken cancellationToken) {
            try {
                ElevenLabs.Voices.Voice voice = new ElevenLabs.Voices.Voice(elevenLabsSettings.voiceId, this.Persona);
                TextToSpeechRequest request = new TextToSpeechRequest(
                    voice, text,
                    model: ContainsAudioTags(text) ? elevenLabsSettings.expressionModel : elevenLabsSettings.fastModel,
                    languageCode: elevenLabsSettings.languageCode,
                    outputFormat: OutputFormat.PCM_24000); // Output format must be PCM, because that's what the AudioClip convertor in VoiceClip expects.
                ElevenLabs.VoiceClip voiceClip = await elevenLabsApi.TextToSpeechEndpoint.TextToSpeechAsync(request, cancellationToken);
                return voiceClip.AudioClip;
            } catch (Exception e) {
                Debug.Log(e);
            }
            return null;
        }

        private bool ContainsAudioTags(string text) {
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"[\[\]]");
        }

        // Tools
        /// Create a function following OpenAI's JSON Schema for the actor to select its emotion for the current response.
        /// OpenAI API on function calling: https://platform.openai.com/docs/guides/function-calling
        /// <returns>The function's JSON Object.</returns>
        private OpenAI.Function BuildSetEmotionTool() {
            List<string> emotions = new List<string>();
            for (int i = 0; i < (int)Emotion.Num; ++i) {
                Emotion emotion = (Emotion)i;
                emotions.Add(emotion.ToString());
            }

            var args = new {
                type = "object",
                properties = new {
                    emotion = new {
                        type = "string",
                        description = "The emotion of your reply.",
                        @enum = emotions.ToArray() // Adding an enum means that the AI can only pick from this set of values. (Well, the AI is stupid and still sometimes hallucinates invalid values.)
                    }
                },
                required = new[] { "emotion" }
            };
            string parameters = JsonConvert.SerializeObject(args, Formatting.Indented);
            return new OpenAI.Function("set_emotion", "Set the emotion of your current reply. The selected emotion is returned.", JToken.Parse(parameters));
        }

        private string ParseEmotion(string args) {
            JToken parsedArgs = JToken.Parse(args);
            if (parsedArgs == null || parsedArgs["emotion"] == null) { return string.Empty; }
            string emotion = parsedArgs["emotion"].ToString();
            return emotion;
        }
    }
}