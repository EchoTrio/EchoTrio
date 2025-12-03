using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.Kinect.BodyTracking;
using System.Text;

namespace Spelunx.Orbbec {
    /// Maps a 3D avatar's movements to a BodyTracker skeleton.
    /// Ensure that the Avatar's GameObjects in the scene matches what is defined in the FBX file, or it will break.
    public class BodyTrackerAvatar : MonoBehaviour {
        [Header("References")]
        [SerializeField] private Animator avatarAnimator;
        [SerializeField] private Transform avatarRoot; 
        [SerializeField] private Transform skeletonRootJoint;
        [SerializeField] private BodyTracker bodyTracker;

        // Internal variables.
        private Dictionary<JointId, Quaternion> absoluteOffsetMap;
        private Dictionary<JointId, Quaternion> relativeOffsetMap;

        public Animator GetAvatarAnimator() { return avatarAnimator; }
        public Transform GetAvatarRoot() { return avatarRoot; }

        public void SetSkeletonRootJoint(Transform skeletonRoot) { this.skeletonRootJoint = skeletonRoot; }
        public Transform GetSkeletonRootJoint() { return skeletonRootJoint; }

        public void SetBodyTracker(BodyTracker bodyTracker) { this.bodyTracker = bodyTracker; }
        public BodyTracker GetBodyTracker() { return bodyTracker; }

        /// Map Microsoft Kinect's joints to Unity's joints. (https://docs.microsoft.com/en-us/azure/Kinect-dk/body-joints)
        private static HumanBodyBones MapKinectJoint(JointId joint) {
            switch (joint) {
                case JointId.Pelvis: return HumanBodyBones.Hips;
                case JointId.SpineNavel: return HumanBodyBones.Spine;
                case JointId.SpineChest: return HumanBodyBones.Chest;
                case JointId.Neck: return HumanBodyBones.Neck;
                case JointId.Head: return HumanBodyBones.Head;
                case JointId.HipLeft: return HumanBodyBones.LeftUpperLeg;
                case JointId.KneeLeft: return HumanBodyBones.LeftLowerLeg;
                case JointId.AnkleLeft: return HumanBodyBones.LeftFoot;
                case JointId.FootLeft: return HumanBodyBones.LeftToes;
                case JointId.HipRight: return HumanBodyBones.RightUpperLeg;
                case JointId.KneeRight: return HumanBodyBones.RightLowerLeg;
                case JointId.AnkleRight: return HumanBodyBones.RightFoot;
                case JointId.FootRight: return HumanBodyBones.RightToes;
                case JointId.ClavicleLeft: return HumanBodyBones.LeftShoulder;
                case JointId.ShoulderLeft: return HumanBodyBones.LeftUpperArm;
                case JointId.ElbowLeft: return HumanBodyBones.LeftLowerArm;
                case JointId.WristLeft: return HumanBodyBones.LeftHand;
                case JointId.ClavicleRight: return HumanBodyBones.RightShoulder;
                case JointId.ShoulderRight: return HumanBodyBones.RightUpperArm;
                case JointId.ElbowRight: return HumanBodyBones.RightLowerArm;
                case JointId.WristRight: return HumanBodyBones.RightHand;
                default: return HumanBodyBones.LastBone;
            }
        }

        // WARNING: DO NOT RENAME YOUR MODEL'S GAMEOBJECT IN UNITY!
        // What this function does is that it gets the skeleton names from the FBX file, and tries to map it to a GameObject in Unity.
        // If the GameObject in Unity does not match the name in the FBX file, it breaks!
        private static SkeletonBone GetSkeletonBone(Animator animator, string boneName) {
            int count = 0;
            string cloneName = boneName + "(Clone)";
            foreach (SkeletonBone sb in animator.avatar.humanDescription.skeleton) {
                if (sb.name == boneName || sb.name == cloneName) {
                    return animator.avatar.humanDescription.skeleton[count];
                }
                count++;
            }
            return new SkeletonBone();
        }

        private void InitRelativeOffsetMap() {
            relativeOffsetMap = new Dictionary<JointId, Quaternion>();
            for (int i = 0; i < (int)JointId.Count; i++) {
                HumanBodyBones hbb = MapKinectJoint((JointId)i);
                if (hbb != HumanBodyBones.LastBone) {
                    Transform boneTransform = avatarAnimator.GetBoneTransform(hbb);
                    relativeOffsetMap[(JointId)i] = GetSkeletonBone(avatarAnimator, boneTransform.name).rotation;
                }
            }
        }

        private void InitAbsoluteOffsetMap() {
            // For every bone in the avatar, map it to a joint in the skeleton and find its absolute rotation.
            absoluteOffsetMap = new Dictionary<JointId, Quaternion>();
            for (int i = 0; i < (int)JointId.Count; i++) {
                HumanBodyBones hbb = MapKinectJoint((JointId)i);
                if (hbb != HumanBodyBones.LastBone) {
                    Transform boneTransform = avatarAnimator.GetBoneTransform(hbb);
                    Quaternion absOffset = GetSkeletonBone(avatarAnimator, boneTransform.name).rotation;
                    // find the absolute offset for the tpose
                    while (!ReferenceEquals(boneTransform, avatarRoot)) {
                        boneTransform = boneTransform.parent;
                        absOffset = GetSkeletonBone(avatarAnimator, boneTransform.name).rotation * absOffset;
                    }
                    absoluteOffsetMap[(JointId)i] = absOffset;
                }
            }
        }

        private void Start() {
            InitRelativeOffsetMap();
            InitAbsoluteOffsetMap();
        }

        private void LateUpdate() {
            for (int j = 0; j < (int)JointId.Count; j++) {
                if (MapKinectJoint((JointId)j) != HumanBodyBones.LastBone && absoluteOffsetMap.ContainsKey((JointId)j)) {
                    Quaternion absOffset = absoluteOffsetMap[(JointId)j];
                    Transform finalJoint = avatarAnimator.GetBoneTransform(MapKinectJoint((JointId)j));
                    if (j == (int)JointId.Pelvis) {
                        finalJoint.localRotation = bodyTracker.GetAbsoluteJointRotation((JointId)j) * relativeOffsetMap[(JointId)j]; 
                        finalJoint.localPosition = bodyTracker.GetJointPosition(JointId.Pelvis);
                    } else {
                        finalJoint.localRotation = bodyTracker.GetRelativeJointRotation((JointId)j) * relativeOffsetMap[(JointId)j];
                    }
                }
            }
        }
    }
}