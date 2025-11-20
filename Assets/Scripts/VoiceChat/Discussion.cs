// By Terri Lim, CMU ETC Class of 2026. Last updated by me in November 2025. Feel free to judge any code up till then.
using UnityEngine;

namespace EchoTrio {
    public abstract class Discussion : ScriptableObject {
        // If the number of ways to trigger a discussion ever increase, perhaps turn this into a bitmask instead.
        [System.Flags] public enum TriggerMode : uint {
            Topic = 1 << 0,
            Round = 1 << 1,
            IdleTime = 1 << 2,
        }

        [Header("Discussion Settings")]
        [SerializeField] protected TriggerMode triggerMode = TriggerMode.Topic | TriggerMode.Round;
        [SerializeField] protected string triggerTopic = string.Empty;
        [SerializeField, Min(1)] protected int triggerRound = 1;
        [SerializeField, Range(1.0f, 300.0f)] protected float triggerIdleTime = 60.0f;

        public bool HasTriggerModes(TriggerMode modes) { return (triggerMode & modes) == modes; }
        public TriggerMode GetTriggerMode() { return triggerMode; }
        public string GetTriggerTopic() { return triggerTopic; }
        public int GetTriggerRound() { return triggerRound; }
        public float GetTriggerIdleTime() { return triggerIdleTime; }
    }
}