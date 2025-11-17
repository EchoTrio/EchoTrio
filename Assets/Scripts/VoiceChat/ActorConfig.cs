using UnityEngine;

namespace EchoTrio {
    [CreateAssetMenu(fileName = "ActorConfig", menuName = "EchoTrio/ActorConfig")]
    public class ActorConfig : ScriptableObject {
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
        public Persona persona = PersonaExtensions.DefaultValue;
        public string elevenLabsVoiceId = string.Empty;
        public Feature enabledFeatures = Feature.WebSearch | Feature.FileSearch | Feature.Reasoning | Feature.AudioTags;
        public string openAIFileSearchVectorStoreId = string.Empty;
        
        [Header("Instructions")]
        [TextArea(minLines: 16, maxLines: 32)] public string generalInstructions = string.Empty;
        public InstructionSnippet[] contextInfos = new InstructionSnippet[0];
        public InstructionSnippet[] backgroundInfos = new InstructionSnippet[0];
        public InstructionSnippet[] personalityInfos = new InstructionSnippet[0];
        public InstructionSnippet[] exampleResponses = new InstructionSnippet[0];

        public bool AreFeaturesEnabled(Feature features) {
            return (enabledFeatures & features) == features;
        }

        public string GetInstructions() {
            string instructions = $"You will be playing a character named {this.persona}.\n\n";
            instructions += generalInstructions + "\n\n";
            instructions += GetSnippetsInstructions("Here are some context information about your character:", contextInfos);
            instructions += GetSnippetsInstructions("Here are some background information about your character:", backgroundInfos);
            instructions += GetSnippetsInstructions("Here are some personality information about your character:", personalityInfos);
            instructions += GetSnippetsInstructions("Here are some example responses that you should style your own responses after:", exampleResponses);
            return instructions;
        }

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