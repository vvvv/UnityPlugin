/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.HandsModule
{
    /// <summary>
    /// A data structure to define all the fingers in a hand, the wrist and elbow
    /// </summary>
    [System.Serializable]
    public class BoundHand
    {
        public BoundFinger[] fingers = new BoundFinger[5];
        public BoundBone wrist = new BoundBone();
        public BoundBone elbow = new BoundBone();
        public float baseScale;
        public Vector3 startScale;
        [Range(-1, 3)] public float scaleOffset = 1;
        [Range(-1, 3)] public float elbowOffset = 1;
    }

    /// <summary>
    /// A data structure to define a finger
    /// </summary>
    [System.Serializable]
    public class BoundFinger
    {
        public BoundBone[] boundBones = new BoundBone[4];
        public float fingerTipBaseLength;
        [Range(-1, 3)] public float fingerTipScaleOffset = 1;
    }

    /// <summary>
    /// A data structure to define starting position, an offset and the Transform reference found in the scene
    /// </summary>
    [System.Serializable]
    public class BoundBone
    {
        public Transform boundTransform;
        public TransformStore startTransform = new TransformStore();
        public TransformStore offset = new TransformStore();
    }

    /// <summary>
    /// A data structure to store a transforms position and rotation
    /// </summary>
    [System.Serializable]
    public class TransformStore
    {
        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
        public Vector3 scale = Vector3.zero;
    }

    /// <summary>
    /// A data structure to store information about a transform and a Gameobject
    /// </summary>
    [System.Serializable]
    public class SerializedTransform
    {
        public TransformStore transform;
        public GameObject reference;
    }

    /// <summary>
    /// ENUM types for bones of the hand the hand binder can attach to
    /// </summary>
    public enum BoundTypes
    {
        THUMB_METACARPAL,
        THUMB_PROXIMAL,
        THUMB_INTERMEDIATE,
        THUMB_DISTAL,

        INDEX_METACARPAL,
        INDEX_PROXIMAL,
        INDEX_INTERMEDIATE,
        INDEX_DISTAL,

        MIDDLE_METACARPAL,
        MIDDLE_PROXIMAL,
        MIDDLE_INTERMEDIATE,
        MIDDLE_DISTAL,

        RING_METACARPAL,
        RING_PROXIMAL,
        RING_INTERMEDIATE,
        RING_DISTAL,

        PINKY_METACARPAL,
        PINKY_PROXIMAL,
        PINKY_INTERMEDIATE,
        PINKY_DISTAL,

        WRIST,
        ELBOW,
    }

    public static class HandBinderUtilities
    {

        /// <summary>
        /// The mapping that allows a BoundType and Leap FingerType/BoneType to map back to the HandBinders Data structure
        /// </summary>
        public readonly static Dictionary<BoundTypes, (Finger.FingerType, Bone.BoneType)> boundTypeMapping = new Dictionary<BoundTypes, (Finger.FingerType, Bone.BoneType)>
            {
            {BoundTypes.THUMB_METACARPAL, (Finger.FingerType.TYPE_THUMB, Bone.BoneType.TYPE_METACARPAL)},
            {BoundTypes.THUMB_PROXIMAL, (Finger.FingerType.TYPE_THUMB, Bone.BoneType.TYPE_PROXIMAL)},
            {BoundTypes.THUMB_INTERMEDIATE, (Finger.FingerType.TYPE_THUMB, Bone.BoneType.TYPE_INTERMEDIATE)},
            {BoundTypes.THUMB_DISTAL, (Finger.FingerType.TYPE_THUMB, Bone.BoneType.TYPE_DISTAL)},
            {BoundTypes.INDEX_METACARPAL, (Finger.FingerType.TYPE_INDEX, Bone.BoneType.TYPE_METACARPAL)},
            {BoundTypes.INDEX_PROXIMAL, (Finger.FingerType.TYPE_INDEX, Bone.BoneType.TYPE_PROXIMAL)},
            {BoundTypes.INDEX_INTERMEDIATE, (Finger.FingerType.TYPE_INDEX, Bone.BoneType.TYPE_INTERMEDIATE)},
            {BoundTypes.INDEX_DISTAL, (Finger.FingerType.TYPE_INDEX, Bone.BoneType.TYPE_DISTAL)},
            {BoundTypes.MIDDLE_METACARPAL, (Finger.FingerType.TYPE_MIDDLE, Bone.BoneType.TYPE_METACARPAL)},
            {BoundTypes.MIDDLE_PROXIMAL, (Finger.FingerType.TYPE_MIDDLE, Bone.BoneType.TYPE_PROXIMAL)},
            {BoundTypes.MIDDLE_INTERMEDIATE, (Finger.FingerType.TYPE_MIDDLE, Bone.BoneType.TYPE_INTERMEDIATE)},
            {BoundTypes.MIDDLE_DISTAL, (Finger.FingerType.TYPE_MIDDLE, Bone.BoneType.TYPE_DISTAL)},
            {BoundTypes.RING_METACARPAL, (Finger.FingerType.TYPE_RING, Bone.BoneType.TYPE_METACARPAL)},
            {BoundTypes.RING_PROXIMAL, (Finger.FingerType.TYPE_RING, Bone.BoneType.TYPE_PROXIMAL)},
            {BoundTypes.RING_INTERMEDIATE, (Finger.FingerType.TYPE_RING, Bone.BoneType.TYPE_INTERMEDIATE)},
            {BoundTypes.RING_DISTAL, (Finger.FingerType.TYPE_RING, Bone.BoneType.TYPE_DISTAL)},
            {BoundTypes.PINKY_METACARPAL, (Finger.FingerType.TYPE_PINKY, Bone.BoneType.TYPE_METACARPAL)},
            {BoundTypes.PINKY_PROXIMAL, (Finger.FingerType.TYPE_PINKY, Bone.BoneType.TYPE_PROXIMAL)},
            {BoundTypes.PINKY_INTERMEDIATE, (Finger.FingerType.TYPE_PINKY, Bone.BoneType.TYPE_INTERMEDIATE)},
            {BoundTypes.PINKY_DISTAL, (Finger.FingerType.TYPE_PINKY, Bone.BoneType.TYPE_DISTAL)},
        };


        /// <summary>
        /// Calculate a Hand from a BoundHand
        /// </summary>
        public static Hand GenerateLeapHand(this BoundHand boundHand, Hand leapHand, float fingerTipScale = 0.8f)
        {
            if (leapHand == null)
            {
                return null;
            }

            //Loop through all the fingers of the hand to calculate where the leap data should be in relation to the Bound Hand
            for (int leapFingerID = 0; leapFingerID < leapHand.Fingers.Count; leapFingerID++)
            {
                //Get the leap Finger
                Finger leapFinger = leapHand.Fingers[leapFingerID];

                for (int leapBoneID = 0; leapBoneID < leapFinger.bones.Length; leapBoneID++)
                {
                    //Get the leapBone
                    Bone leapBone = leapFinger.bones[leapBoneID];

                    //If this bone is the distal bone, calculate a finger tip position
                    if (leapBoneID == (int)Bone.BoneType.TYPE_DISTAL)
                    {
                        BoundBone currentBoundBone = boundHand.fingers[leapFingerID].boundBones[leapBoneID];
                        BoundBone previousBoundBone = boundHand.fingers[leapFingerID].boundBones[leapBoneID - 1];

                        if (previousBoundBone.boundTransform && currentBoundBone.boundTransform)
                        {
                            //Get the positions of the rigged joints
                            Vector nextBoundBonePosition = currentBoundBone.boundTransform.position.ToVector();
                            Vector previousBoundBonePosition = previousBoundBone.boundTransform.position.ToVector();

                            //Get the direction of the finger
                            Vector direction = (previousBoundBonePosition - nextBoundBonePosition);
                            float length = direction.Magnitude;

                            //Calculate the finger tip position given an offset
                            previousBoundBonePosition += -direction.Normalized * (length * fingerTipScale);
                            nextBoundBonePosition += -direction.Normalized * (length * fingerTipScale);

                            //Calculate the center of the finger
                            Vector center = Vector.Lerp(previousBoundBonePosition, nextBoundBonePosition, 0.5f);

                            //Set the leap finger
                            leapFinger.bones[leapBoneID] = new Bone(previousBoundBonePosition, nextBoundBonePosition, center, direction, length, leapBone.Width, leapBone.Type, leapBone.Rotation);
                            //Set the finger tip position
                            leapFinger.TipPosition = nextBoundBonePosition;
                        }
                    }
                    else
                    {
                        BoundBone previousBoundBone = boundHand.fingers[leapFingerID].boundBones[leapBoneID];
                        BoundBone nextBoundBone = boundHand.fingers[leapFingerID].boundBones[leapBoneID + 1];

                        //If the bones are not null, calculate the data needed for this bone
                        if (previousBoundBone.boundTransform && nextBoundBone.boundTransform)
                        {
                            Vector previousBoundJointPosition = previousBoundBone.boundTransform.position.ToVector();
                            Vector nextBoundJointPosition = nextBoundBone.boundTransform.position.ToVector();
                            Vector direction = (previousBoundJointPosition - nextBoundJointPosition);
                            float length = direction.Magnitude;
                            Vector center = Vector.Lerp(previousBoundJointPosition, nextBoundJointPosition, 0.5f);

                            //Set the data for a new leap bone
                            leapFinger.bones[leapBoneID] = new Bone(previousBoundJointPosition, nextBoundJointPosition, center, direction, length, leapBone.Width, leapBone.Type, leapBone.Rotation);
                        }

                        //If the bone is a metacarpal, use the wrist bone as the previous joint
                        else if (leapBoneID == (int)Bone.BoneType.TYPE_METACARPAL)
                        {
                            BoundBone proximalBoundBone  = boundHand.fingers[leapFingerID].boundBones[(int)Bone.BoneType.TYPE_PROXIMAL];
                            BoundBone wristBoundBone = boundHand.wrist;

                            if (proximalBoundBone.boundTransform && wristBoundBone.boundTransform)
                            {
                                Vector nextBoundJointPosition = proximalBoundBone.boundTransform.position.ToVector();
                                Vector previousBoundJointPosition = Vector.Lerp(wristBoundBone.boundTransform.position.ToVector(), nextBoundJointPosition, 0.5f);
                                Vector direction = (previousBoundJointPosition - nextBoundJointPosition);
                                float length = direction.Magnitude;
                                Vector center = Vector.Lerp(previousBoundJointPosition, nextBoundJointPosition, 0.5f);

                                //Set the data for the leap finger
                                leapFinger.bones[leapBoneID] = new Bone(previousBoundJointPosition, nextBoundJointPosition, center, direction, length, leapBone.Width, leapBone.Type, leapBone.Rotation);
                            }
                        }
                    }
                }
            }

            //Calculate the data for the wrist
            if (boundHand.wrist.boundTransform != null)
            {
                leapHand.WristPosition = boundHand.wrist.boundTransform.position.ToVector();

                //Sor the elbow data
                if (boundHand.elbow.boundTransform != null)
                {
                    Vector elbowPos = boundHand.elbow.boundTransform.position.ToVector();
                    Vector wristPos = boundHand.wrist.boundTransform.position.ToVector();
                    Vector center = Vector.Lerp(elbowPos, wristPos, 0.5f);
                    Vector dir = (elbowPos - wristPos);
                    float length = dir.Magnitude;

                    //Set the data on the leap Arm
                    leapHand.Arm = new Arm(elbowPos, wristPos, center, dir, length, leapHand.Arm.Width, leapHand.Arm.Rotation);
                    leapHand.Arm.PrevJoint = elbowPos;
                }
            }

            //Calculate the palm position half way between the wrist and the middle proximal bone
            Vector palmPos = Vector.Lerp(leapHand.WristPosition, leapHand.GetMiddle().bones[(int)Bone.BoneType.TYPE_PROXIMAL].PrevJoint, 0.5f);

            //Set the data on the leap hand
            leapHand.PalmPosition = palmPos;
            leapHand.StabilizedPalmPosition = leapHand.PalmPosition;
            leapHand.PalmWidth = (leapHand.GetPinky().bones[1].PrevJoint - leapHand.GetIndex().bones[1].PrevJoint).Magnitude;
            leapHand.Arm.NextJoint = leapHand.WristPosition;

            return leapHand;
        }
    }
}