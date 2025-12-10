using System.Collections.Generic;
using UnityEngine;

namespace EchoTrio {
    /// ScriptedDiscussions allow the designer to make the actors speak scripted lines.
    [CreateAssetMenu(fileName = "ScriptedDiscussion", menuName = "EchoTrio/ScriptedDiscussion")]
    public class ScriptedDiscussion : Discussion {
        [System.Serializable] public class Dialogue {
            public Persona speaker = Persona.Athena;
            [TextArea(minLines: 4, maxLines: 16)] public string message = string.Empty;
            public Emotion emotion = Emotion.Neutral; // For now, this isn't being used.
        }

        [Header("Scripted Discussion Settings")]
        [SerializeField] private List<Dialogue> dialogues = new List<Dialogue>();

        public List<Dialogue> GetDialogues() { return dialogues; }
    }
}