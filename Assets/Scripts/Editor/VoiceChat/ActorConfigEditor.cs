using UnityEditor;
using UnityEngine;

namespace EchoTrio {
    [CustomEditor(typeof(ActorConfig))]
    public class ActorConfigEditor : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            ActorConfig actorConfig = (ActorConfig)target;

            EditorGUILayout.LabelField("Combined Instructions", EditorStyles.boldLabel);
            string combinedInstructions = actorConfig.GetInstructions();
            EditorGUILayout.LabelField(combinedInstructions, new UnityEngine.GUILayoutOption[] {
                GUILayout.MinWidth(GUI.skin.label.CalcSize(new GUIContent(combinedInstructions)).x),
                GUILayout.MinHeight(GUI.skin.label.CalcSize(new GUIContent(combinedInstructions)).y),
            });
        }
    }
}