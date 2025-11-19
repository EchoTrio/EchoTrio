using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.Kinect.BodyTracking;

namespace Spelunx.Orbbec {
    /// <summary>
    /// BodyTracker represents the BodyData from the ORBBEC sensor as a skeleton.
    /// Important: BodyTracker prefab's child hierachy must be in the same order as JointId.
    /// </summary>
    public class BodyTracker : MonoBehaviour {
        [Header("References (Do NOT edit unless you know what you're doing!)")]
        [SerializeField] private Transform rootJoint = null;

        // Constants
        readonly Quaternion Y_180_FLIP = new Quaternion(0.0f, 1.0f, 0.0f, 0.0f);
        readonly Vector3 X_POSITIVE = Vector3.right; // Follow the Left-Hand Rule.
        readonly Vector3 Y_POSITIVE = Vector3.up; // Follow the Left-Hand Rule.
        readonly Vector3 Z_POSITIVE = Vector3.forward; // Follow the Left-Hand Rule.

        // Internal variables.
        [Header("Internal Variables (Exposed for debugging purposes.)")]
        [SerializeField, Tooltip("Joint Positions")] private Vector3[] jointPositions = new Vector3[(int)JointId.Count];
        [SerializeField, Tooltip("Absolute Joint Rotations")] private Quaternion[] absoluteJointRotations = new Quaternion[(int)JointId.Count];
        private Dictionary<JointId, JointId> parentJointMap;
        private Dictionary<JointId, Quaternion> basisJointMap;

        private void Awake() {
            InitParentJointMap();
            InitBasisJointMap();
        }

        /// Sets the parent of every joint.
        private void InitParentJointMap() {
            parentJointMap = new Dictionary<JointId, JointId>();

            parentJointMap[JointId.Pelvis] = JointId.Count; // Pelvis has no parent, so set it's parent to JointId.Count.
            parentJointMap[JointId.SpineNavel] = JointId.Pelvis;
            parentJointMap[JointId.SpineChest] = JointId.SpineNavel;
            parentJointMap[JointId.Neck] = JointId.SpineChest;
            parentJointMap[JointId.ClavicleLeft] = JointId.SpineChest;
            parentJointMap[JointId.ShoulderLeft] = JointId.ClavicleLeft;
            parentJointMap[JointId.ElbowLeft] = JointId.ShoulderLeft;
            parentJointMap[JointId.WristLeft] = JointId.ElbowLeft;
            parentJointMap[JointId.HandLeft] = JointId.WristLeft;
            parentJointMap[JointId.HandTipLeft] = JointId.HandLeft;
            parentJointMap[JointId.ThumbLeft] = JointId.HandLeft;
            parentJointMap[JointId.ClavicleRight] = JointId.SpineChest;
            parentJointMap[JointId.ShoulderRight] = JointId.ClavicleRight;
            parentJointMap[JointId.ElbowRight] = JointId.ShoulderRight;
            parentJointMap[JointId.WristRight] = JointId.ElbowRight;
            parentJointMap[JointId.HandRight] = JointId.WristRight;
            parentJointMap[JointId.HandTipRight] = JointId.HandRight;
            parentJointMap[JointId.ThumbRight] = JointId.HandRight;
            parentJointMap[JointId.HipLeft] = JointId.SpineNavel;
            parentJointMap[JointId.KneeLeft] = JointId.HipLeft;
            parentJointMap[JointId.AnkleLeft] = JointId.KneeLeft;
            parentJointMap[JointId.FootLeft] = JointId.AnkleLeft;
            parentJointMap[JointId.HipRight] = JointId.SpineNavel;
            parentJointMap[JointId.KneeRight] = JointId.HipRight;
            parentJointMap[JointId.AnkleRight] = JointId.KneeRight;
            parentJointMap[JointId.FootRight] = JointId.AnkleRight;
            parentJointMap[JointId.Head] = JointId.Pelvis;
            parentJointMap[JointId.Nose] = JointId.Head;
            parentJointMap[JointId.EyeLeft] = JointId.Head;
            parentJointMap[JointId.EarLeft] = JointId.Head;
            parentJointMap[JointId.EyeRight] = JointId.Head;
            parentJointMap[JointId.EarRight] = JointId.Head;
        }

        /// Sets the rotation basis of every joint. The rotation basis are according to (https://learn.microsoft.com/en-us/previous-versions/azure/kinect-dk/body-joints).
        private void InitBasisJointMap() {
            // Spine and left hip share the same basis.
            Quaternion leftHipBasis = Quaternion.LookRotation(X_POSITIVE, -Z_POSITIVE);
            Quaternion spineHipBasis = Quaternion.LookRotation(X_POSITIVE, -Z_POSITIVE);
            Quaternion rightHipBasis = Quaternion.LookRotation(X_POSITIVE, Z_POSITIVE);

            // Arms and thumbs share the same basis.
            Quaternion leftArmBasis = Quaternion.LookRotation(Y_POSITIVE, -Z_POSITIVE);
            Quaternion rightArmBasis = Quaternion.LookRotation(-Y_POSITIVE, Z_POSITIVE);
            Quaternion leftHandBasis = Quaternion.LookRotation(-Z_POSITIVE, -Y_POSITIVE);
            Quaternion rightHandBasis = Quaternion.identity;
            Quaternion leftFootBasis = Quaternion.LookRotation(X_POSITIVE, Y_POSITIVE);
            Quaternion rightFootBasis = Quaternion.LookRotation(X_POSITIVE, -Y_POSITIVE);

            basisJointMap = new Dictionary<JointId, Quaternion>();

            // pelvis has no parent so set to count
            basisJointMap[JointId.Pelvis] = spineHipBasis;
            basisJointMap[JointId.SpineNavel] = spineHipBasis;
            basisJointMap[JointId.SpineChest] = spineHipBasis;
            basisJointMap[JointId.Neck] = spineHipBasis;
            basisJointMap[JointId.ClavicleLeft] = leftArmBasis;
            basisJointMap[JointId.ShoulderLeft] = leftArmBasis;
            basisJointMap[JointId.ElbowLeft] = leftArmBasis;
            basisJointMap[JointId.WristLeft] = leftHandBasis;
            basisJointMap[JointId.HandLeft] = leftHandBasis;
            basisJointMap[JointId.HandTipLeft] = leftHandBasis;
            basisJointMap[JointId.ThumbLeft] = leftArmBasis;
            basisJointMap[JointId.ClavicleRight] = rightArmBasis;
            basisJointMap[JointId.ShoulderRight] = rightArmBasis;
            basisJointMap[JointId.ElbowRight] = rightArmBasis;
            basisJointMap[JointId.WristRight] = rightHandBasis;
            basisJointMap[JointId.HandRight] = rightHandBasis;
            basisJointMap[JointId.HandTipRight] = rightHandBasis;
            basisJointMap[JointId.ThumbRight] = rightArmBasis;
            basisJointMap[JointId.HipLeft] = leftHipBasis;
            basisJointMap[JointId.KneeLeft] = leftHipBasis;
            basisJointMap[JointId.AnkleLeft] = leftHipBasis;
            basisJointMap[JointId.FootLeft] = leftFootBasis;
            basisJointMap[JointId.HipRight] = rightHipBasis;
            basisJointMap[JointId.KneeRight] = rightHipBasis;
            basisJointMap[JointId.AnkleRight] = rightHipBasis;
            basisJointMap[JointId.FootRight] = rightFootBasis;
            basisJointMap[JointId.Head] = spineHipBasis;
            basisJointMap[JointId.Nose] = spineHipBasis;
            basisJointMap[JointId.EyeLeft] = spineHipBasis;
            basisJointMap[JointId.EarLeft] = spineHipBasis;
            basisJointMap[JointId.EyeRight] = spineHipBasis;
            basisJointMap[JointId.EarRight] = spineHipBasis;
        }

        private int FindClosestTrackedBody(FrameData trackerFrameData) {
            int closestBody = -1;
            float minDistanceFromKinect = float.MaxValue;
            for (int i = 0; i < (int)trackerFrameData.NumDetectedBodies; i++) {
                var pelvisPosition = trackerFrameData.Bodies[i].JointPositions3D[(int)JointId.Pelvis];
                Vector3 pelvisPos = new Vector3((float)pelvisPosition.X, (float)pelvisPosition.Y, (float)pelvisPosition.Z);
                if (pelvisPos.magnitude < minDistanceFromKinect) {
                    closestBody = i;
                    minDistanceFromKinect = pelvisPos.magnitude;
                }
            }
            return closestBody;
        }

        private Quaternion OrientateRotation(Quaternion rotation, SensorOrientation sensorOrientation) {
            switch (sensorOrientation) {
                case SensorOrientation.Clockwise90:
                    return Quaternion.AngleAxis(90.0f, Z_POSITIVE) * rotation;
                case SensorOrientation.CounterClockwise90:
                    return Quaternion.AngleAxis(-90.0f, Z_POSITIVE) * rotation;
                case SensorOrientation.Flip180:
                    return Quaternion.AngleAxis(180.0f, Z_POSITIVE) * rotation;
            }
            return rotation;
        }

        private Vector3 OrientatePosition(Vector3 position, SensorOrientation sensorOrientation) {
            float rotationAngle = 0.0f;
            switch (sensorOrientation) {
                case SensorOrientation.Clockwise90:
                    // Clockwise 90 degrees means that we face the camera, and then rotate the camera 90 degrees relative to us.
                    // Intuitively, I know that if the camera is rotated clockwise 90 degrees, we should be rotating it anti-clockwise 90 degrees instead to compensate for it.
                    // But why are we doing the opposite? Not a damn clue, I figured it out via trial and error.
                    // It works, my semester is ending and I'm burnt out, and I'm not sure I really care that much right now.
                    rotationAngle = 90.0f;
                    break;
                case SensorOrientation.CounterClockwise90:
                    rotationAngle = -90.0f;
                    break;
                case SensorOrientation.Flip180:
                    rotationAngle = 180.0f;
                    break;
            }

            // Left-Hand Rule!
            Matrix4x4 translationMatrix = Matrix4x4.Translate(position);
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.AngleAxis(rotationAngle, Z_POSITIVE));
            Matrix4x4 positionMatrix = rotationMatrix * translationMatrix;
            return new Vector3(positionMatrix.m03, positionMatrix.m13, positionMatrix.m23);
        }

        private void SetBonesTransform(BodyData body, SensorOrientation sensorOrientation) {
            for (int jointNum = 0; jointNum < (int)JointId.Count; jointNum++) {
                // Calculate joint position.
                Vector3 jointPos = OrientatePosition(
                    // Convert from System.Numerics.Vector3 to UnityEngine.Vector3.
                    new Vector3(body.JointPositions3D[jointNum].X,
                                -body.JointPositions3D[jointNum].Y,
                                body.JointPositions3D[jointNum].Z),
                    sensorOrientation);
                jointPositions[jointNum] = jointPos;

                // We have to convert from System.Numerics.Quaternion to UnityEngine.Quaternion.
                Quaternion bodyJointRotation = new Quaternion(
                    body.JointRotations[jointNum].X,
                    body.JointRotations[jointNum].Y,
                    body.JointRotations[jointNum].Z,
                    body.JointRotations[jointNum].W);

                // By rotating the inverse of a basis, we are bring a point from world space, into that basis' space.
                Quaternion jointRot = OrientateRotation(Y_180_FLIP * bodyJointRotation * Quaternion.Inverse(basisJointMap[(JointId)jointNum]), sensorOrientation);
                absoluteJointRotations[jointNum] = jointRot;

                // These are absolute body space because each joint has the body root for a parent in the scene graph.
                transform.GetChild(0).GetChild(jointNum).localPosition = jointPos;
                transform.GetChild(0).GetChild(jointNum).localRotation = jointRot;

                // Certain joints don't have a bone, so there's no need to render them.
                if (parentJointMap[(JointId)jointNum] == JointId.Head ||
                    parentJointMap[(JointId)jointNum] == JointId.Count) {
                    transform.GetChild(0).GetChild(jointNum).GetChild(0).gameObject.SetActive(false);
                    continue;
                }

                // For the other joints, rotate and scale their bones so that they link up with the parent joint.
                Vector3 parentTrackerSpacePosition = OrientatePosition(
                    new Vector3(body.JointPositions3D[(int)parentJointMap[(JointId)jointNum]].X,
                                -body.JointPositions3D[(int)parentJointMap[(JointId)jointNum]].Y,
                                body.JointPositions3D[(int)parentJointMap[(JointId)jointNum]].Z),
                    sensorOrientation);
                Vector3 boneDirectionTrackerSpace = jointPos - parentTrackerSpacePosition;
                Vector3 boneDirectionWorldSpace = transform.rotation * boneDirectionTrackerSpace;
                Vector3 boneDirectionLocalSpace = Quaternion.Inverse(transform.GetChild(0).GetChild(jointNum).rotation) * Vector3.Normalize(boneDirectionWorldSpace);

                // If the order of children in the scene hierachy ever changes, this will all be messed up.
                transform.GetChild(0).GetChild(jointNum).GetChild(0).localScale = new Vector3(1, 20.0f * 0.5f * boneDirectionWorldSpace.magnitude, 1);
                transform.GetChild(0).GetChild(jointNum).GetChild(0).localRotation = Quaternion.FromToRotation(Vector3.up, boneDirectionLocalSpace);
                transform.GetChild(0).GetChild(jointNum).GetChild(0).position = transform.GetChild(0).GetChild(jointNum).position - 0.5f * boneDirectionWorldSpace;
            }
        }

        /// <summary>
        /// Show or hide the skeleton in the game scene.
        /// </summary>
        /// <param name="show">
        /// Show the skeleton if true, else hide the skeleton.
        /// </param>
        public void ShowSkeleton(bool show) {
            for (int jointNum = 0; jointNum < (int)JointId.Count; jointNum++) {
                transform.GetChild(0).GetChild(jointNum).gameObject.GetComponent<MeshRenderer>().enabled = show;
                transform.GetChild(0).GetChild(jointNum).GetChild(0).GetComponent<MeshRenderer>().enabled = show;
            }
        }

        public Vector3 GetJointPosition(JointId jointId) { return jointPositions[(int)jointId]; }

        /// <summary>
        /// Get the rotation of a joint, relative to the root.
        /// </summary>
        /// <param name="jointId">
        /// The ID of the joint which to get the rotation.
        /// </param>
        /// <returns>
        /// The rotation of the joint of ID jointId, relative to the root.
        /// </returns>
        public Quaternion GetAbsoluteJointRotation(JointId jointId) { return absoluteJointRotations[(int)jointId]; }

        /// <summary>
        /// Get the rotation of a joint, relative to its parent.
        /// </summary>
        /// <param name="jointId">
        /// The ID of the joint which to get the rotation.
        /// </param>
        /// <returns>
        /// The rotation of the joint of ID jointId, relative to its parent.
        /// </returns>
        public Quaternion GetRelativeJointRotation(JointId jointId) {
            JointId parent = parentJointMap[jointId];
            Quaternion parentJointRotationBodySpace = Quaternion.identity;
            if (parent == JointId.Count) {
                parentJointRotationBodySpace = Y_180_FLIP;
            } else {
                parentJointRotationBodySpace = absoluteJointRotations[(int)parent];
            }
            Quaternion jointRotationBodySpace = absoluteJointRotations[(int)jointId];
            Quaternion relativeRotation = Quaternion.Inverse(parentJointRotationBodySpace) * jointRotationBodySpace;

            return relativeRotation;
        }

        /// <summary>
        /// Update the skeleton based on frame data.
        /// </summary>
        /// <param name="frameData">
        /// The frame data to update the skeleton.
        /// </param>
        /// The sensor orientation, used to rotate the skeleton is upright.
        /// <param name="orientation"></param>
        public void UpdateSkeleton(FrameData frameData, SensorOrientation orientation) {
            //this is an array in case you want to get the n closest bodies
            int closestBody = FindClosestTrackedBody(frameData);

            // render the closest body
            BodyData skeleton = frameData.Bodies[closestBody];
            SetBonesTransform(skeleton, orientation);
        }

        public Transform GetRootJoint() { return rootJoint; }
    }
}