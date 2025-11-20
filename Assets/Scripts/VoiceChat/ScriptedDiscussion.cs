// By Terri Lim, CMU ETC Class of 2026. Last updated by me in November 2025. Feel free to judge any code up till then.
using System.Collections.Generic;
using UnityEngine;

namespace EchoTrio {
    [CreateAssetMenu(fileName = "ScriptedDiscussion", menuName = "EchoTrio/ScriptedDiscussion")]
    public class ScriptedDiscussion : Discussion {
        [System.Serializable]
        public class Dialogue {
            public Persona speaker = Persona.Athena;
            [TextArea(minLines: 4, maxLines: 16)] public string message = string.Empty;
            public Emotion emotion = Emotion.Neutral; // For now, this isn't being used.
        }

        [Header("Scripted Discussion Settings")]
        [SerializeField] private List<Dialogue> dialogues = new List<Dialogue>();

        public List<Dialogue> GetDialogues() { return dialogues; }
    }
}