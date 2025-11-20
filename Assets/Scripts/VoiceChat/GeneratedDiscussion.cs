// By Terri Lim, CMU ETC Class of 2026. Last updated by me in December 2025. Feel free to judge any code up till then.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EchoTrio {
    /// GeneratedDiscussions allow the designer to request the actors to generate N responses based on a prompt.
    [CreateAssetMenu(fileName = "GeneratedDiscussion", menuName = "EchoTrio/GeneratedDiscussion")]
    public class GeneratedDiscussion : Discussion {
        [Header("Generated Discussion Settings")]
        [SerializeField, Range(1, 10)] private int minTurns = 1;
        [SerializeField, Range(1, 10)] private int maxTurns = 3;
        [SerializeField] private List<Persona> speakers = new List<Persona>();
        [SerializeField, Tooltip("The prompt to give to the model to generate a discussion. If left blank, it will default to the trigger topic.")] private string discussionPrompt = string.Empty;

        public List<Persona> GenerateRandomSpeakerOrder() {
            if (speakers.Count == 0) { return new List<Persona>(); }

            int numTurns = UnityEngine.Random.Range(minTurns, maxTurns + 1);
            if (speakers.Count == 1) { return Enumerable.Repeat(speakers[0], numTurns).ToList(); }

            List<Persona> order = new List<Persona>();
            int prevIndex = -1;
            int currIndex = -1;
            for (int i = 0; i < numTurns; ++i) {
                currIndex = UnityEngine.Random.Range(0, speakers.Count);
                // If the current speaker is same as the previous, get the next speaker in line instead.
                currIndex = (currIndex == prevIndex) ? (currIndex + 1) % speakers.Count : currIndex;
                prevIndex = currIndex;
                order.Add(speakers[currIndex]);
            }
            return order;
        }

        public string GetDiscussionPrompt() {
            return discussionPrompt == string.Empty ? $"Talk about the topic {triggerTopic} in your next response." : discussionPrompt;
        }

        private void OnValidate() {
            maxTurns = Mathf.Max(maxTurns, minTurns);
        }
    }
}