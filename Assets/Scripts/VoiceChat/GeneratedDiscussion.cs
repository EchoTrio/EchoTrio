using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EchoTrio {
    [CreateAssetMenu(fileName = "GeneratedDiscussion", menuName = "EchoTrio/GeneratedDiscussion")]
    public class GeneratedDiscussion : Discussion {
        [Header("Generated Discussion Settings")]
        [SerializeField, Range(1, 10)] private int minRounds = 1;
        [SerializeField, Range(1, 10)] private int maxRounds = 3;
        [SerializeField] private List<Persona> speakers = new List<Persona>();
        [SerializeField, Tooltip("The prompt to give to the model to generate a discussion. If left blank, it will default to the trigger topic.")] private string discussionPrompt = string.Empty;

        public List<Persona> GenerateRandomSpeakerOrder() {
            if (speakers.Count == 0) { return new List<Persona>(); }

            int numRounds = Random.Range(minRounds, maxRounds + 1);
            if (speakers.Count == 1) { return Enumerable.Repeat(speakers[0], numRounds).ToList(); }

            List<Persona> order = new List<Persona>();
            int prevIndex = -1;
            int currIndex = -1;
            for (int i = 0; i < numRounds; ++i) {
                currIndex = Random.Range(0, speakers.Count);
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
            maxRounds = Mathf.Max(maxRounds, minRounds);
        }
    }
}