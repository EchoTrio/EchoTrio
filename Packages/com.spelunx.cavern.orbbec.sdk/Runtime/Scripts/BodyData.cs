using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;
using System;
using System.Numerics;
using UnityEngine;
using System.Runtime.Serialization;

namespace Spelunx.Orbbec {
    /// Data about a body that is being tracked.
    /// Note that its properties uses System.Numerics classes instead of UnityEngine classes for Vector2, Vector3 and Quaternion.
    [Serializable]
    public struct BodyData : ISerializable {
        public const float Invalid2DCoordinate = -1;

        // Take note that theses are System.Numerics classes, not UnityEngine classes.
        public System.Numerics.Vector2[] JointPositions2D { get; private set; }
        public System.Numerics.Vector3[] JointPositions3D { get; private set; }
        public System.Numerics.Quaternion[] JointRotations { get; private set; }
        public JointConfidenceLevel[] JointConfidenceLevels { get; private set; }
        public int Length { get; private set; }
        public uint Id { get; private set; }

        public BodyData(int maxJointsLength) {
            JointPositions2D = new System.Numerics.Vector2[maxJointsLength];
            JointPositions3D = new System.Numerics.Vector3[maxJointsLength];
            JointRotations = new System.Numerics.Quaternion[maxJointsLength];
            JointConfidenceLevels = new JointConfidenceLevel[maxJointsLength];
            Length = 0;
            Id = 0;
        }

        public static BodyData DeepCopy(BodyData copyFromBody) {
            int maxJointsLength = copyFromBody.Length;
            BodyData copiedBody = new BodyData(maxJointsLength);

            for (int i = 0; i < maxJointsLength; i++) {
                copiedBody.JointPositions2D[i] = copyFromBody.JointPositions2D[i];
                copiedBody.JointPositions3D[i] = copyFromBody.JointPositions3D[i];
                copiedBody.JointRotations[i] = copyFromBody.JointRotations[i];
                copiedBody.JointConfidenceLevels[i] = copyFromBody.JointConfidenceLevels[i];
            }
            copiedBody.Id = copyFromBody.Id;
            copiedBody.Length = copyFromBody.Length;
            return copiedBody;
        }

        public void CopyFromBodyTrackingSdk(Microsoft.Azure.Kinect.BodyTracking.Body body, Calibration sensorCalibration) {
            Id = body.Id;
            Length = Microsoft.Azure.Kinect.BodyTracking.Skeleton.JointCount;

            for (int bodyPoint = 0; bodyPoint < Length; bodyPoint++) {
                // K4ABT joint position unit is in millimeter. We need to convert to meters before we use the values.
                JointPositions3D[bodyPoint] = body.Skeleton.GetJoint(bodyPoint).Position / 1000.0f;
                JointRotations[bodyPoint] = body.Skeleton.GetJoint(bodyPoint).Quaternion;
                JointConfidenceLevels[bodyPoint] = body.Skeleton.GetJoint(bodyPoint).ConfidenceLevel;

                var jointPosition = JointPositions3D[bodyPoint];
                var position2d = sensorCalibration.TransformTo2D(
                    jointPosition,
                    CalibrationDeviceType.Depth,
                    CalibrationDeviceType.Depth);

                if (position2d != null) {
                    JointPositions2D[bodyPoint] = position2d.Value;
                } else {
                    JointPositions2D[bodyPoint].X = Invalid2DCoordinate;
                    JointPositions2D[bodyPoint].Y = Invalid2DCoordinate;
                }
            }
        }

        public BodyData(SerializationInfo info, StreamingContext context) {
            float[] JointPositions3DX = (float[])info.GetValue("JointPositions3DX", typeof(float[]));
            float[] JointPositions3DY = (float[])info.GetValue("JointPositions3DY", typeof(float[]));
            float[] JointPositions3DZ = (float[])info.GetValue("JointPositions3DZ", typeof(float[]));
            JointPositions3D = new System.Numerics.Vector3[JointPositions3DX.Length];
            for (int i = 0; i < JointPositions3DX.Length; i++) {
                JointPositions3D[i].X = JointPositions3DX[i];
                JointPositions3D[i].Y = JointPositions3DY[i];
                JointPositions3D[i].Z = JointPositions3DZ[i];
            }

            float[] JointPositions2DX = (float[])info.GetValue("JointPositions2DX", typeof(float[]));
            float[] JointPositions2DY = (float[])info.GetValue("JointPositions2DY", typeof(float[]));
            JointPositions2D = new System.Numerics.Vector2[JointPositions2DX.Length];
            for (int i = 0; i < JointPositions2DX.Length; i++) {
                JointPositions2D[i].X = JointPositions2DX[i];
                JointPositions2D[i].Y = JointPositions2DY[i];
            }

            float[] JointRotationsX = (float[])info.GetValue("JointRotationsX", typeof(float[]));
            float[] JointRotationsY = (float[])info.GetValue("JointRotationsY", typeof(float[]));
            float[] JointRotationsZ = (float[])info.GetValue("JointRotationsZ", typeof(float[]));
            float[] JointRotationsW = (float[])info.GetValue("JointRotationsW", typeof(float[]));
            JointRotations = new System.Numerics.Quaternion[JointRotationsX.Length];
            for (int i = 0; i < JointRotationsX.Length; i++) {
                JointRotations[i].X = JointRotationsX[i];
                JointRotations[i].Y = JointRotationsY[i];
                JointRotations[i].Z = JointRotationsZ[i];
                JointRotations[i].W = JointRotationsW[i];
            }

            uint[] ConfidenceLevel = (uint[])info.GetValue("ConfidenceLevel", typeof(uint[]));
            JointConfidenceLevels = new JointConfidenceLevel[ConfidenceLevel.Length];
            for (int i = 0; i < ConfidenceLevel.Length; i++) {
                JointConfidenceLevels[i] = (JointConfidenceLevel)ConfidenceLevel[i];
            }

            Length = (int)info.GetValue("Length", typeof(int));
            Id = (uint)info.GetValue("Id", typeof(uint));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            float[] JointPositions3DX = new float[Length];
            float[] JointPositions3DY = new float[Length];
            float[] JointPositions3DZ = new float[Length];
            for (int i = 0; i < Length; i++) {
                JointPositions3DX[i] = JointPositions3D[i].X;
                JointPositions3DY[i] = JointPositions3D[i].Y;
                JointPositions3DZ[i] = JointPositions3D[i].Z;
            }
            info.AddValue("JointPositions3DX", JointPositions3DX, typeof(float[]));
            info.AddValue("JointPositions3DY", JointPositions3DY, typeof(float[]));
            info.AddValue("JointPositions3DZ", JointPositions3DZ, typeof(float[]));

            float[] JointPositions2DX = new float[Length];
            float[] JointPositions2DY = new float[Length];
            for (int i = 0; i < Length; i++) {
                JointPositions2DX[i] = JointPositions2D[i].X;
                JointPositions2DY[i] = JointPositions2D[i].Y;
            }
            info.AddValue("JointPositions2DX", JointPositions2DX, typeof(float[]));
            info.AddValue("JointPositions2DY", JointPositions2DY, typeof(float[]));

            float[] JointRotationsX = new float[Length];
            float[] JointRotationsY = new float[Length];
            float[] JointRotationsZ = new float[Length];
            float[] JointRotationsW = new float[Length];
            for (int i = 0; i < Length; i++) {
                JointRotationsX[i] = JointRotations[i].X;
                JointRotationsY[i] = JointRotations[i].Y;
                JointRotationsZ[i] = JointRotations[i].Z;
                JointRotationsW[i] = JointRotations[i].W;

            }
            info.AddValue("JointRotationsX", JointRotationsX, typeof(float[]));
            info.AddValue("JointRotationsY", JointRotationsY, typeof(float[]));
            info.AddValue("JointRotationsZ", JointRotationsZ, typeof(float[]));
            info.AddValue("JointRotationsW", JointRotationsW, typeof(float[]));

            uint[] ConfidenceLevels = new uint[Length];
            for (int i = 0; i < Length; i++) {
                ConfidenceLevels[i] = (uint)JointConfidenceLevels[i];
            }
            info.AddValue("ConfidenceLevels", ConfidenceLevels, typeof(uint[]));

            info.AddValue("Length", Length, typeof(int));
            info.AddValue("Id", Id, typeof(uint));
        }
    }
}