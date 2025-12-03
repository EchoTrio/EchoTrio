using UnityEditor;
using UnityEngine;

namespace Spelunx.Orbbec {
    [CustomEditor(typeof(BodyTracker))]
    public class BodyTrackerInspector : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            BodyTracker bodyTracker = (BodyTracker)target;
            if (GUILayout.Button("Turn On Skeleton")) {
                bodyTracker.ShowSkeleton(true);
            }

            if (GUILayout.Button("Turn Off Skeleton")) {
                bodyTracker.ShowSkeleton(false);
            }
        }
    }
}
