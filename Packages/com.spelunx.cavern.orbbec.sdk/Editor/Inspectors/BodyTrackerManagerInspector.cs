using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Spelunx.Orbbec {
    [CustomEditor(typeof(BodyTrackerManager))]
    public class BodyTrackerManagerInspector : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            BodyTrackerManager bodyTrackerManager = (BodyTrackerManager)target;
            List<string> foundSerials = bodyTrackerManager.GetAvailableSerials();

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontStyle = FontStyle.Bold; // Bold the header.
            headerStyle.fontSize += 4; // Slightly increase the font size.
            GUILayout.Space(8); // Leave some padding.
            if (0 == foundSerials.Count) {
                GUILayout.Label("No Devices Found (Enter Play Mode to scan for devices.)", headerStyle);
            } else {
                GUILayout.Label("Devices Found (Selecting also copies serial number to clipboard.)", headerStyle);
            }

            for (int i = 0; i < foundSerials.Count; ++i) {
                if (GUILayout.Button("Select Device " + foundSerials[i])) {
                    EditorGUIUtility.systemCopyBuffer = foundSerials[i];
                    bodyTrackerManager.SetDeviceSerial(foundSerials[i]);
                }
            }
        }
    }
}