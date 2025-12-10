using UnityEngine;

namespace EchoTrio {
    /// Discussions are a way for the designers to create a way for the actors to interact beyond the standard way of getting a basic response from the AI models.
    public abstract class Discussion : ScriptableObject {
        /// Ways that a discussion can be triggered.
        [System.Flags] public enum TriggerMode : uint {
            /// Trigger a discussion if the user mentions a specific topic.
            Topic = 1 << 0,
            /// Trigger a discussion if the round number reaches a certain number.
            Round = 1 << 1,
            /// Trigger a discussion if the user does not provide any input for a certain amount of time during a round.
            IdleTime = 1 << 2,
        }

        [Header("Discussion Settings")]
        [SerializeField] protected TriggerMode triggerMode = TriggerMode.Topic | TriggerMode.Round;
        [SerializeField] protected string triggerTopic = string.Empty;
        [SerializeField, Min(1)] protected int triggerRound = 1;
        [SerializeField, Range(10.0f, 300.0f)] protected float triggerIdleTime = 60.0f;

        public bool HasAllTriggerModes(TriggerMode modes) { return (triggerMode & modes) == modes; }
        public bool HasAnyTriggerMode(TriggerMode modes) { return (triggerMode & modes) != 0; }
        public TriggerMode GetTriggerMode() { return triggerMode; }
        public string GetTriggerTopic() { return triggerTopic; }
        public int GetTriggerRound() { return triggerRound; }
        public float GetTriggerIdleTime() { return triggerIdleTime; }
    }
}