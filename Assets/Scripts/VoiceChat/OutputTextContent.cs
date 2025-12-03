// By Terri Lim, CMU ETC Class of 2026. Last updated by me in December 2025. Feel free to judge any code up till then.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

// Custom text content class that has the type output_text, so that we can create fake assistant messages.
// Because the com.openai.unity package's TextContent's type is fixed to input_text, and assistant messages need to be output_text.
namespace EchoTrio {
    [Preserve]
    public sealed class OutputTextContent : OpenAI.BaseResponse, OpenAI.Responses.IResponseContent {
        [Preserve]
        public static implicit operator OutputTextContent(string input) => new(input);

        [Preserve]
        [JsonConstructor]
        internal OutputTextContent(
            [JsonProperty("type")] OpenAI.Responses.ResponseContentType type,
            [JsonProperty("text")] string text,
            [JsonProperty("annotations")] List<OpenAI.IAnnotation> annotations,
            [JsonProperty("log_probs")] List<OpenAI.LogProbInfo> logProbs) {
            Type = type;
            Text = text;
            Annotations = annotations;
            LogProbs = logProbs;
        }

        [Preserve]
        public OutputTextContent(string text) {
            Type = OpenAI.Responses.ResponseContentType.OutputText;
            Text = text;
        }

        [Preserve]
        [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.Include)]
        public OpenAI.Responses.ResponseContentType Type { get; }

        [Preserve]
        [JsonProperty("text", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Text { get; internal set; }

        private List<OpenAI.IAnnotation> annotations;

        [Preserve]
        [JsonProperty("annotations", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IReadOnlyList<OpenAI.IAnnotation> Annotations {
            get => annotations;
            private set => annotations = value?.ToList();
        }

        [Preserve]
        [JsonProperty("logprobs", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IReadOnlyList<OpenAI.LogProbInfo> LogProbs { get; }

        private string delta;

        [Preserve]
        [JsonProperty("delta", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Delta {
            get => delta;
            internal set {
                if (value == null) {
                    delta = null;
                } else {
                    delta += value;
                }
            }
        }

        [JsonIgnore]
        public string Object => Type.ToString();

        [Preserve]
        internal void InsertAnnotation(OpenAI.IAnnotation item, int index) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            annotations ??= new();

            if (index > annotations.Count) {
                for (var i = annotations.Count; i < index; i++) {
                    annotations.Add(null);
                }
            }

            annotations.Insert(index, item);
        }

        [Preserve]
        public override string ToString()
            => Delta ?? Text ?? string.Empty;
    }
}
