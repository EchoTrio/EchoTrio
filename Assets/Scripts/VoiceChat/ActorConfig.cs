using IniParser;
using IniParser.Model;
using ElevenLabs;
using UnityEngine;

namespace EchoTrio {
    [CreateAssetMenu(fileName = "ActorConfig", menuName = "EchoTrio/ActorConfig")]
    public class ActorConfig : ScriptableObject {
        private const string OverrideFileName = "ActorOverrides.ini";

        [System.Serializable]
        public class InstructionSnippet {
            public bool enabled = true;
            [TextArea(minLines: 4, maxLines: 8)] public string instruction = string.Empty;
        }

        [System.Flags]
        public enum Feature {
            WebSearch = 1 << 0,
            FileSearch = 1 << 1,
            Reasoning = 1 << 2,
            AudioTags = 1 << 3,
        }

        [Header("Settings")]
        [SerializeField] private Persona persona = PersonaExtensions.DefaultValue;
        [SerializeField] private Feature enabledFeatures = Feature.WebSearch | Feature.FileSearch | Feature.Reasoning | Feature.AudioTags;
        [SerializeField] private string elevenLabsVoiceId = string.Empty;
        [SerializeField] private string openAIVectorStoreId = string.Empty;
        
        [Header("Instructions")]
        [SerializeField, TextArea(minLines: 16, maxLines: 32)] private string generalInstructions = string.Empty;
        [SerializeField] private InstructionSnippet[] contextInfos = new InstructionSnippet[0];
        [SerializeField] private InstructionSnippet[] backgroundInfos = new InstructionSnippet[0];
        [SerializeField] private InstructionSnippet[] personalityInfos = new InstructionSnippet[0];
        [SerializeField] private InstructionSnippet[] exampleResponses = new InstructionSnippet[0];

        // Public Interface
        public Persona GetPersona() { return persona; }

        public bool AreAllFeaturesEnabled(Feature features) { return (enabledFeatures & features) == features; }

        public bool IsAnyFeatureEnabled(Feature features) { return (enabledFeatures & features) != 0; }

        public string GetElevenLabsVoiceID() { return elevenLabsVoiceId; }

        public string GetOpenAIVectorStoreID() { return openAIVectorStoreId; }

        public string GetInstructions() {
            string instructions = $"You will be playing a character named {this.persona}.\n\n";
            instructions += generalInstructions + "\n\n";
            instructions += GetSnippetsInstructions("Here are some context information about your character:", contextInfos);
            instructions += GetSnippetsInstructions("Here are some background information about your character:", backgroundInfos);
            instructions += GetSnippetsInstructions("Here are some personality information about your character:", personalityInfos);
            instructions += GetSnippetsInstructions("Here are some example responses that you should style your own responses after:", exampleResponses);
            return instructions;
        }

        /// <summary>
        /// Overrides any actor config if value is set in ActorOverrides.ini.
        /// </summary>
        /// <returns>Returns a copy of this ActorConfig with values overridden.</returns>
        public ActorConfig Override() {
            FileIniDataParser parser = new FileIniDataParser();
            IniData data = parser.ReadFile($"{Application.streamingAssetsPath}/Configs/{OverrideFileName}");

            ActorConfig overriddenConfig = Instantiate(this);
            string section = persona.ToString();
            string value = string.Empty;

            // Override OpenAI Vector Store ID
            if (data.TryGetKey($"{section}.openai_vector_store_id", out value) && !string.IsNullOrEmpty(value.Trim())) {
                overriddenConfig.openAIVectorStoreId = value.Trim();
            }

            // Override ElevenLabs Voice ID
            if (data.TryGetKey($"{section}.elevenlabs_voice_id", out value) && !string.IsNullOrEmpty(value.Trim())) {
                overriddenConfig.elevenLabsVoiceId = value.Trim();
            }

            // Override Features
            if (data.TryGetKey($"{section}.feature_web_search", out value) && !string.IsNullOrEmpty(value.Trim()) && value.Trim().ToUpper() == "TRUE") {
                overriddenConfig.enabledFeatures |= Feature.WebSearch;
            }
            if (data.TryGetKey($"{section}.feature_file_search", out value) && !string.IsNullOrEmpty(value.Trim()) && value.Trim().ToUpper() == "TRUE") {
                overriddenConfig.enabledFeatures |= Feature.FileSearch;
            }
            if (data.TryGetKey($"{section}.feature_reasoning", out value) && !string.IsNullOrEmpty(value.Trim()) && value.Trim().ToUpper() == "TRUE") {
                overriddenConfig.enabledFeatures |= Feature.Reasoning;
            }
            if (data.TryGetKey($"{section}.feature_audio_tags", out value) && !string.IsNullOrEmpty(value.Trim()) && value.Trim().ToUpper() == "TRUE") {
                overriddenConfig.enabledFeatures |= Feature.AudioTags;
            }

            return overriddenConfig;
        }

        // Internal Variables
        private string GetSnippetsInstructions(string prefix, InstructionSnippet[] snippets) {
            // Check if there are any valid snippets.
            bool hasValidSnippets = false;
            foreach (InstructionSnippet snippet in snippets) {
                if (snippet == null) { continue; }
                hasValidSnippets = hasValidSnippets || snippet.enabled;
            }
            if (!hasValidSnippets) { return string.Empty; }

            // Combine all the snippets into a single string.
            string instructions = $"{prefix}\n";
            foreach (InstructionSnippet snippet in snippets) {
                if (snippet == null) { continue; }
                if (!snippet.enabled) { continue; }
                instructions += $"{snippet.instruction}\n";
            }
            return instructions + "\n";
        }
    }
}