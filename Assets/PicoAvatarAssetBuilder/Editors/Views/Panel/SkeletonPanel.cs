#if UNITY_EDITOR
using Pico.Avatar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.Rendering.Universal;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class SkeletonPanel : AssetImportSettingsPanel
        {
            public enum JointGroup
            {
                Invalid,
                Body,
                Head,
                LeftHand,
                RightHand,
            }

            public enum JointErrorStatus
            {
                None,
                Warning,
                Error,
            }

            enum JointLevel
            {
                Required,
                Optional,
            }

            enum SkeletonMappingTab
            {
                Invalid,
                Mapping,
                AnimationBone,
                Muscle,
            }

            uint REQUIRED = 1 << 0;
            uint FILLED = 1 << 1;
            uint SELECTED = 1 << 2;
            class JointShowProperty
            {
                public JointShowProperty(JointLevel jointLevel_, JointGroup jointGroup_, uint top_, uint left_)
                {
                    jointLevel = jointLevel_;
                    jointGroup = jointGroup_;
                    top = top_;
                    left = left_;
                }
                public JointLevel jointLevel;

                public JointGroup jointGroup;
                public uint top;
                public uint left;
            }

            class JointStatus
            {   
                public JointStatus(uint fillStatus_, JointErrorStatus errorStatus_)
                {
                    fillStatus = fillStatus_;
                    errorStatus = errorStatus_;
                }

                public uint fillStatus;
                public JointErrorStatus errorStatus;
            }

            public override string displayName { get => "Configure Skeleton"; }
            public override string panelName { get => "SkeletonPanel"; }
            public override string uxmlPathName { get => "Uxml/SkeletonPanel.uxml"; }
            public Transform avatarRootTransform{
                get 
                {
                    return _avatarRootTransform;
                }
                set
                {
                    _avatarRootTransform = value;
                    if(_bodyWidget != null)
                    {
                        _bodyWidget.avatarRootTransform = value;
                    }
                    if(_leftHandWidget != null)
                    {
                        _leftHandWidget.avatarRootTransform = value;
                    }
                    if(_rightHandWidget != null)
                    {
                        _rightHandWidget.avatarRootTransform = value;
                    }
                    if(_headWidget != null)
                    {
                        _headWidget.avatarRootTransform = value;
                    }
                }
            }

            public Dictionary<JointType, JointType> jointParentMap{
                get
                {
                    if(_jointParentMap == null)
                    {
                        _jointParentMap = new Dictionary<JointType, JointType>();
                        foreach (KeyValuePair<JointType, List<JointType>> kvp in jointChildrenMap)
                        {
                            var startJoints = kvp.Value;
                            JointType endJoint = kvp.Key;
                            foreach(JointType startName in startJoints)
                            {
                                _jointParentMap.Add(startName, endJoint);
                            }
                        }
                    }
                    return _jointParentMap;
                }
            }
            public Dictionary<JointType, VisualElement> _jointVisualElements = null;

            [SerializeField] private PaabSkeletonImportSetting _skeletonImportSetting;

            [SerializeField] public string _skeletonImportSettingString = "";

            public bool customJointObjectsDirty = false;
            
            // menu button (bottom)
            const string k_SkeletonNextButton = "Skeleton_NextButton";
            

            private List<JointType> keyJointList = new List<JointType>() { 
                JointType.Root, JointType.Hips, JointType.Chest, JointType.Neck, JointType.Head, 
                JointType.LeftShoulder, JointType.LeftArmUpper, JointType.LeftArmLower, JointType.LeftHandWrist,
                JointType.RightShoulder, JointType.RightArmUpper, JointType.RightArmLower, JointType.RightHandWrist,
                JointType.LeftLegUpper, JointType.LeftLegLower, JointType.LeftFootAnkle, 
                JointType.RightLegUpper, JointType.RightLegLower, JointType.RightFootAnkle
            };

            private JointType[] _spineJoints = {JointType.Hips, JointType.SpineLower, JointType.SpineMiddle, JointType.SpineUpper, JointType.Chest, JointType.Neck, JointType.Head};
            private JointType[] _leftArmJoints = { JointType.LeftShoulder, JointType.LeftArmUpper, JointType.LeftArmUpperTwist,  JointType.LeftArmLower, JointType.LeftHandTwist, JointType.LeftHandTwist2, JointType.LeftHandWrist};
            private JointType[] _rightArmJoints = { JointType.RightShoulder, JointType.RightArmUpper, JointType.RightArmUpperTwist,  JointType.RightArmLower, JointType.RightHandTwist, JointType.RightHandTwist2, JointType.RightHandWrist};
            private JointType[] _leftLegJoints = { JointType.LeftLegUpper, JointType.LeftLegLower, JointType.LeftFootAnkle };
            private JointType[] _rightLegJoints = { JointType.RightLegUpper, JointType.RightLegLower, JointType.RightFootAnkle };


            private Dictionary<JointType, JointType> _jointParentMap = null;
            
            static public Dictionary<JointType, List<JointType>> jointChildrenMap = new Dictionary<JointType, List<JointType>>()
            {
                //spine
                { JointType.Root, new List<JointType>(){JointType.Hips} },
                //{ "RootScale" , new List<string>(){"Hips"} },
                { JointType.Hips, new List<JointType>(){JointType.SpineLower, JointType.LeftLegUpper,JointType.RightLegUpper}},
                { JointType.SpineLower, new List<JointType>(){JointType.SpineMiddle}},
                { JointType.SpineMiddle, new List<JointType>(){JointType.SpineUpper}},
                { JointType.SpineUpper, new List<JointType>(){JointType.Chest}},
                { JointType.Chest, new List<JointType>(){JointType.Neck, JointType.LeftShoulder, JointType.RightShoulder}},
                { JointType.Neck, new List<JointType>(){JointType.Head}},
                { JointType.Head, new List<JointType>(){JointType.Hair}},

                //left arm
                {JointType.LeftShoulder, new List<JointType>(){JointType.LeftArmUpper}},
                {JointType.LeftArmUpper, new List<JointType>(){JointType.LeftArmLower, JointType.LeftArmUpperTwist1, JointType.LeftArmUpperTwist2}},
                {JointType.LeftArmLower, new List<JointType>(){JointType.LeftHandWrist, JointType.LeftArmLowerTwist1, JointType.LeftArmLowerTwist2, JointType.LeftArmLowerTwist3}},
                {JointType.LeftHandWrist, new List<JointType>(){JointType.LeftHandThumbTrapezium, JointType.LeftHandIndexMetacarpal, JointType.LeftHandMiddleMetacarpal, JointType.LeftHandRingMetacarpal, JointType.LeftHandPinkyMetacarpal}},

                //right arm
                {JointType.RightShoulder, new List<JointType>(){JointType.RightArmUpper}},
                {JointType.RightArmUpper, new List<JointType>(){JointType.RightArmLower, JointType.RightArmUpperTwist1, JointType.RightArmUpperTwist2}},
                {JointType.RightArmLower, new List<JointType>(){JointType.RightHandWrist, JointType.RightArmLowerTwist1, JointType.RightArmLowerTwist2, JointType.RightArmLowerTwist3}},
                {JointType.RightHandWrist, new List<JointType>(){JointType.RightHandThumbTrapezium, JointType.RightHandIndexMetacarpal, JointType.RightHandMiddleMetacarpal, JointType.RightHandRingMetacarpal, JointType.RightHandPinkyMetacarpal}},

                //left leg
                {JointType.LeftLegUpper, new List<JointType>(){JointType.LeftLegLower, JointType.LeftLegUpperTwist1, JointType.LeftLegUpperTwist2 }},
                {JointType.LeftLegLower, new List<JointType>(){JointType.LeftFootAnkle}},
                {JointType.LeftFootAnkle, new List<JointType>(){JointType.LeftToe}},
                {JointType.LeftToe, new List<JointType>(){JointType.LeftToeEnd}},

                {JointType.RightLegUpper, new List<JointType>(){JointType.RightLegLower, JointType.RightLegUpperTwist1, JointType.RightLegUpperTwist2 }},
                {JointType.RightLegLower, new List<JointType>(){JointType.RightFootAnkle}},
                {JointType.RightFootAnkle, new List<JointType>(){JointType.RightToe}},
                {JointType.RightToe, new List<JointType>(){JointType.RightToeEnd}},

                //left hand
                {JointType.LeftHandThumbTrapezium, new List<JointType>(){JointType.LeftHandThumbMetacarpal}},
                {JointType.LeftHandThumbMetacarpal, new List<JointType>(){JointType.LeftHandThumbProximal}},
                {JointType.LeftHandThumbProximal, new List<JointType>(){JointType.LeftHandThumbDistal}},
                {JointType.LeftHandThumbDistal, new List<JointType>(){JointType.LeftHandThumbTip}},

                {JointType.LeftHandIndexMetacarpal, new List<JointType>(){JointType.LeftHandIndexProximal}},
                {JointType.LeftHandIndexProximal, new List<JointType>(){JointType.LeftHandIndexIntermediate}},
                {JointType.LeftHandIndexIntermediate, new List<JointType>(){JointType.LeftHandIndexDistal}},
                {JointType.LeftHandIndexDistal, new List<JointType>(){JointType.LeftHandIndexTip}},

                {JointType.LeftHandMiddleMetacarpal, new List<JointType>(){JointType.LeftHandMiddleProximal}},
                {JointType.LeftHandMiddleProximal, new List<JointType>(){JointType.LeftHandMiddleIntermediate}},
                {JointType.LeftHandMiddleIntermediate, new List<JointType>(){JointType.LeftHandMiddleDistal}},
                {JointType.LeftHandMiddleDistal, new List<JointType>(){JointType.LeftHandMiddleTip}},

                {JointType.LeftHandRingMetacarpal, new List<JointType>(){JointType.LeftHandRingProximal}},
                {JointType.LeftHandRingProximal, new List<JointType>(){JointType.LeftHandRingIntermediate}},
                {JointType.LeftHandRingIntermediate, new List<JointType>(){JointType.LeftHandRingDistal}},
                {JointType.LeftHandRingDistal, new List<JointType>(){JointType.LeftHandRingTip}},

                {JointType.LeftHandPinkyMetacarpal, new List<JointType>(){JointType.LeftHandPinkyProximal}},
                {JointType.LeftHandPinkyProximal, new List<JointType>(){JointType.LeftHandPinkyIntermediate}},
                {JointType.LeftHandPinkyIntermediate, new List<JointType>(){JointType.LeftHandPinkyDistal}},
                {JointType.LeftHandPinkyDistal, new List<JointType>(){JointType.LeftHandPinkyTip}},

                //right hand
                {JointType.RightHandThumbTrapezium, new List<JointType>(){JointType.RightHandThumbMetacarpal}},
                {JointType.RightHandThumbMetacarpal, new List<JointType>(){JointType.RightHandThumbProximal}},
                {JointType.RightHandThumbProximal, new List<JointType>(){JointType.RightHandThumbDistal}},
                {JointType.RightHandThumbDistal, new List<JointType>(){JointType.RightHandThumbTip}},

                {JointType.RightHandIndexMetacarpal, new List<JointType>(){JointType.RightHandIndexProximal}},
                {JointType.RightHandIndexProximal, new List<JointType>(){JointType.RightHandIndexIntermediate}},
                {JointType.RightHandIndexIntermediate, new List<JointType>(){JointType.RightHandIndexDistal}},
                {JointType.RightHandIndexDistal, new List<JointType>(){JointType.RightHandIndexTip}},

                {JointType.RightHandMiddleMetacarpal, new List<JointType>(){JointType.RightHandMiddleProximal}},
                {JointType.RightHandMiddleProximal, new List<JointType>(){JointType.RightHandMiddleIntermediate}},
                {JointType.RightHandMiddleIntermediate, new List<JointType>(){JointType.RightHandMiddleDistal}},
                {JointType.RightHandMiddleDistal, new List<JointType>(){JointType.RightHandMiddleTip}},

                {JointType.RightHandRingMetacarpal, new List<JointType>(){JointType.RightHandRingProximal}},
                {JointType.RightHandRingProximal, new List<JointType>(){JointType.RightHandRingIntermediate}},
                {JointType.RightHandRingIntermediate, new List<JointType>(){JointType.RightHandRingDistal}},
                {JointType.RightHandRingDistal, new List<JointType>(){JointType.RightHandRingTip}},

                {JointType.RightHandPinkyMetacarpal, new List<JointType>(){JointType.RightHandPinkyProximal}},
                {JointType.RightHandPinkyProximal, new List<JointType>(){JointType.RightHandPinkyIntermediate}},
                {JointType.RightHandPinkyIntermediate, new List<JointType>(){JointType.RightHandPinkyDistal}},
                {JointType.RightHandPinkyDistal, new List<JointType>(){JointType.RightHandPinkyTip}},
            };

            private Dictionary<JointType, JointShowProperty> _jointShowMap = new Dictionary<JointType, JointShowProperty>
            {
                { JointType.Root, new JointShowProperty(JointLevel.Required, JointGroup.Body, 463, 149)},
                //{ "RootScale" , new List<string>(){"Hips"} },
                { JointType.Hips, new JointShowProperty(JointLevel.Required, JointGroup.Body, 238, 149)},
                { JointType.SpineLower, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 213, 149)},
                { JointType.SpineMiddle, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 192, 149)},
                { JointType.SpineUpper, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 171, 149)},
                { JointType.Chest, new JointShowProperty(JointLevel.Required, JointGroup.Body, 146, 149)},


                { JointType.Neck, new JointShowProperty(JointLevel.Required, JointGroup.Head, 436, 153)},
                { JointType.Head, new JointShowProperty(JointLevel.Required, JointGroup.Head, 351, 153)},

                //left arm
                {JointType.LeftShoulder, new JointShowProperty(JointLevel.Required, JointGroup.Body, 117, 171)},
                {JointType.LeftArmUpper, new JointShowProperty(JointLevel.Required, JointGroup.Body, 124, 189)},
                {JointType.LeftArmUpperTwist1, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 144, 195)},
                {JointType.LeftArmUpperTwist2, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 161, 201)},
                {JointType.LeftArmLower, new JointShowProperty(JointLevel.Required, JointGroup.Body, 178, 205)},
                {JointType.LeftArmLowerTwist1, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 195, 210)},
                {JointType.LeftArmLowerTwist2, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 212, 214)},
                {JointType.LeftArmLowerTwist3, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 230, 218)},
                {JointType.LeftHandWrist, new JointShowProperty(JointLevel.Required, JointGroup.Body, 247, 222)},

                //right arm
                {JointType.RightShoulder, new JointShowProperty(JointLevel.Required, JointGroup.Body, 117, 125)},
                {JointType.RightArmUpper, new JointShowProperty(JointLevel.Required, JointGroup.Body, 124, 106)},
                {JointType.RightArmUpperTwist1, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 144, 100)},
                {JointType.RightArmUpperTwist2, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 161, 95)},
                {JointType.RightArmLower, new JointShowProperty(JointLevel.Required, JointGroup.Body, 178, 91)},
                {JointType.RightArmLowerTwist1, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 195, 87)},
                {JointType.RightArmLowerTwist2, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 212, 83)},
                {JointType.RightArmLowerTwist3, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 230, 79)},
                {JointType.RightHandWrist, new JointShowProperty(JointLevel.Required, JointGroup.Body, 247, 76)},

                //left leg
                {JointType.LeftLegUpper, new JointShowProperty(JointLevel.Required, JointGroup.Body, 249, 171)},
                {JointType.LeftLegUpperTwist1, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 282, 174)},
                {JointType.LeftLegUpperTwist2, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 308, 176)},
                {JointType.LeftLegLower, new JointShowProperty(JointLevel.Required, JointGroup.Body, 334, 178)},
                {JointType.LeftFootAnkle, new JointShowProperty(JointLevel.Required, JointGroup.Body, 452, 188)},
                {JointType.LeftToe, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 467, 202)},

                //right leg
                {JointType.RightLegUpper, new JointShowProperty(JointLevel.Required, JointGroup.Body, 249, 127)},
                {JointType.RightLegUpperTwist1, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 282, 123)},
                {JointType.RightLegUpperTwist2, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 308, 121)},
                {JointType.RightLegLower, new JointShowProperty(JointLevel.Required, JointGroup.Body, 334, 118)},
                {JointType.RightFootAnkle, new JointShowProperty(JointLevel.Required, JointGroup.Body, 452, 108)},
                {JointType.RightToe, new JointShowProperty(JointLevel.Optional, JointGroup.Body, 467, 94)},

                //left hand
                {JointType.LeftHandThumbTrapezium, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 237, 56)},
                {JointType.LeftHandThumbMetacarpal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 256, 74)},
                {JointType.LeftHandThumbProximal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 304, 69)},
                {JointType.LeftHandThumbDistal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 341, 72)},
                {JointType.LeftHandThumbTip, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 369, 70)},

                {JointType.LeftHandIndexMetacarpal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 283, 117)},
                {JointType.LeftHandIndexProximal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 318, 148)},
                {JointType.LeftHandIndexIntermediate, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 349, 181)},
                {JointType.LeftHandIndexDistal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 370, 210)},
                {JointType.LeftHandIndexTip, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 389, 235)},

                {JointType.LeftHandMiddleMetacarpal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 281, 133)},
                {JointType.LeftHandMiddleProximal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 307, 165)},
                {JointType.LeftHandMiddleIntermediate, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 331, 195)},
                {JointType.LeftHandMiddleDistal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 353, 223)},
                {JointType.LeftHandMiddleTip, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 369, 242)},

                {JointType.LeftHandRingMetacarpal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 267, 137)},
                {JointType.LeftHandRingProximal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 288, 170)},
                {JointType.LeftHandRingIntermediate, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 304, 202)},
                {JointType.LeftHandRingDistal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 321, 229)},
                {JointType.LeftHandRingTip, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 334, 246)},

                {JointType.LeftHandPinkyMetacarpal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 250, 133)},
                {JointType.LeftHandPinkyProximal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 268, 165)},
                {JointType.LeftHandPinkyIntermediate, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 277, 194)},
                {JointType.LeftHandPinkyDistal, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 286, 215)},
                {JointType.LeftHandPinkyTip, new JointShowProperty(JointLevel.Optional, JointGroup.LeftHand, 292, 232)},

                //right hand
                {JointType.RightHandThumbTrapezium, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 237, 240)},
                {JointType.RightHandThumbMetacarpal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 256, 222)},
                {JointType.RightHandThumbProximal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 304, 227)},
                {JointType.RightHandThumbDistal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 341, 224)},
                {JointType.RightHandThumbTip, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 369, 226)},

                {JointType.RightHandIndexMetacarpal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 283, 179)},
                {JointType.RightHandIndexProximal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 318, 148)},
                {JointType.RightHandIndexIntermediate, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 349, 115)},
                {JointType.RightHandIndexDistal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 370, 86)},
                {JointType.RightHandIndexTip, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 389, 61)},

                {JointType.RightHandMiddleMetacarpal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 281, 163)},
                {JointType.RightHandMiddleProximal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 307, 131)},
                {JointType.RightHandMiddleIntermediate, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 331, 101)},
                {JointType.RightHandMiddleDistal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 353, 73)},
                {JointType.RightHandMiddleTip, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 369, 54)},

                {JointType.RightHandRingMetacarpal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 267, 159)},
                {JointType.RightHandRingProximal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 288, 126)},
                {JointType.RightHandRingIntermediate, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 304, 94)},
                {JointType.RightHandRingDistal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 321, 67)},
                {JointType.RightHandRingTip, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 334, 50)},

                {JointType.RightHandPinkyMetacarpal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 250, 163)},
                {JointType.RightHandPinkyProximal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 268, 131)},
                {JointType.RightHandPinkyIntermediate, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 277, 102)},
                {JointType.RightHandPinkyDistal, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 286, 81)},
                {JointType.RightHandPinkyTip, new JointShowProperty(JointLevel.Optional, JointGroup.RightHand, 292, 64)},
            };

            // UI Buttons
            Button m_SkeletonNextButton;
            
#region Private Fields
            private static SkeletonPanel _instance;
            private BaseInfoWidget _basicInfoWidget = null;
            private VisualElement _avatarRootWidget = null;
            private SkeletonMappingSettingBodyWidget _bodyWidget = null;
            private SkeletonMappingSettingLeftHandWidget _leftHandWidget = null;
            private SkeletonMappingSettingRightHandWidget _rightHandWidget = null;
            private SkeletonMappingSettingHeadWidget _headWidget = null;
            private SkeletonMappingMuscleSettingWidget _muscleWidget = null;
            private SkeletonAnimationBoneSettingWidget _animationBoneWidget = null;
            private Transform _avatarRootTransform = null;
            private ObjectField _avatarRootObject = null;
            private JointErrorStatus _avatarRootErrorStatus = JointErrorStatus.None;
            private VisualElement  _leftVis = null; //"skeletonConfig_show_ve";


            private Dictionary<JointGroup, GroupBox> _jointVisGroups = null;
            private Dictionary<uint, StyleBackground> _jointVisIcons = null;
            private Dictionary<JointType, JointStatus> _jointStatusMap = null;
            private Dictionary<JointGroup, SkeletonMappingSettingWidget> _jointMappingWidgets = null;
            private Dictionary<JointType, ObjectField> _jointObjects = null;

            private List<ObjectField> _customJointObjects = null;

            private JointGroup _currentGroup = JointGroup.Body;
        
            private Button _currentSelectedBodyPart = null;

            private SkeletonMappingTab _currentSelectedTab = SkeletonMappingTab.Invalid;
            private Button _currentSelectedTabButton = null;

            private JointType _currentSelectedJointType = JointType.Invalid;

            private bool _waitingForNextStep = false;

            private bool _readyForNextStep = false;

            private bool _hasWarning = false;

            private bool _warningChecked = false;

            private StyleBackground _jointMappingCompleteIcon = null;

            private StyleBackground _jointMappingIncompleteIcon = null;

            private VisualElement _jointMappingProgressSign = null;


#endregion
            // 
            public static SkeletonPanel instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<SkeletonPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/SkeletonPanel.asset");
                    }
                    return _instance;
                }
            }
            
            public override void OnDestroy()
            {
                ResetJointOrientations();
                if(curImportSettings != null)
                {
                    this.UpdateToData(curImportSettings);
                    SaveContext();
                }
                
                base.OnDestroy();
                //
                if(_instance == this)
                {
                    _instance = null;
                }
            }
            
            public Label GetObjectFieldLabel(ObjectField item)
            {
                return item.ElementAt(0).ElementAt(0).ElementAt(1) as Label;
            }
            
            public static Color32 GetDefaultColor()
            {
                return new Color32(0xD2, 0xD2, 0xD2, 0xFF);
            }

            public static Color32 GetEmptyObjectColor()
            {
                return new Color32(255, 255, 255, 102);
            }

            public static Color32 GetEmptyOptionalObjectColor()
            {
                return new Color32(255, 255, 255, 51);
            }

            public static Color32 GetOptionalObjectTextColor()
            {
                return new Color32(255, 255, 255, 51);
            }


            public static Color32 GetRequiredObjectTextColor()
            {
                return new Color32(255, 255, 255, 204);
            }

            public static Color32 GetOptionalEmptyObjectColor()
            {
                return new Color32(136, 136, 136, 255);
            }

            public static Color32 GetErrorColor()
            {
                return new Color32(0xFF, 0x57, 0x52, 0xFF);
            }

            public static Color32 GetWarningColor()
            { 
                return new Color32(0xFF, 0xBA, 0x00, 0xFF);
            }

            internal SkeletonMappingSettingWidget GetWidgetByJointGroup(JointGroup jointGroup)
            {
                switch(jointGroup){
                    case JointGroup.Body:
                    return _bodyWidget;
                    case JointGroup.Head:
                    return _headWidget;
                    case JointGroup.LeftHand:
                    return _leftHandWidget;
                    case JointGroup.RightHand:
                    return _rightHandWidget;
                    default:
                    return null;
                }
            }
            
            protected override bool BuildUIDOM(VisualElement parent) //SetVisualElements and BuildWithUxml
            {
                curImportSettings = ScriptableObject.CreateInstance<PaabAssetImportSettings>();
                base.BuildUIDOM(parent);
                
                mainElement.style.flexGrow = 1;
                mainElement.style.flexShrink = 1;

                //_basicInfoWidget = new AssetBasicInfoSettingWidget();
                _basicInfoWidget = new BaseInfoWidget(null, BaseInfoType.AssetSkeleton);
                _bodyWidget = new SkeletonMappingSettingBodyWidget();
                _headWidget = new SkeletonMappingSettingHeadWidget();
                _leftHandWidget = new SkeletonMappingSettingLeftHandWidget();
                _rightHandWidget = new SkeletonMappingSettingRightHandWidget();
                _muscleWidget = new SkeletonMappingMuscleSettingWidget();
                _animationBoneWidget = new SkeletonAnimationBoneSettingWidget();

                _jointMappingWidgets = new Dictionary<JointGroup, SkeletonMappingSettingWidget>();
                _jointMappingWidgets.Add(JointGroup.Body, _bodyWidget);
                _jointMappingWidgets.Add(JointGroup.Head, _headWidget);
                _jointMappingWidgets.Add(JointGroup.LeftHand, _leftHandWidget);
                _jointMappingWidgets.Add(JointGroup.RightHand, _rightHandWidget);
               
                AddWidget(_basicInfoWidget);

                _bodyWidget.SetOtherSideMappingWidget(_bodyWidget);
                _leftHandWidget.SetOtherSideMappingWidget(_rightHandWidget);
                _rightHandWidget.SetOtherSideMappingWidget(_leftHandWidget);

                // add skeleton root item
                var itemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetBuilderConfig.instance.uiDataAssetsPath + "UxmlWidget/SkeletonMappingSettingItemWidget.uxml");
                var item = itemAsset.Instantiate();
                var settingGroup = item.Q<GroupBox>("SkeletonConfig");
                settingGroup.name = "SkeletonConfig_SkeletonRoot";
                var label = item.Q<Label>("JointTypeName");
                label.text = "Skeleton Root";
                label.name = "Label_" + "SkeletonRoot";
                _avatarRootObject = item.Q<ObjectField>();
                _avatarRootObject.name = "SkeletonRoot";
                contentElement.Add(item);
                _avatarRootWidget = item;
                item.style.marginLeft = 32;
                item.style.marginRight = 32;
                item.style.marginBottom = 16;
                UIUtils.AddVisualElementHoverMask(_avatarRootObject, _avatarRootObject, true);

                {
                    var gameObjectLabel = GetObjectFieldLabel(_avatarRootObject);
                    if (_avatarRootObject.value == null)
                    {
                        gameObjectLabel.text = "(Required)";
                        gameObjectLabel.style.color = (Color)GetEmptyObjectColor();
                    }
                }

                AddWidget(_bodyWidget);
                AddWidget(_headWidget);
                AddWidget(_leftHandWidget);
                AddWidget(_rightHandWidget);
                AddWidget(_muscleWidget);
                AddWidget(_animationBoneWidget);

                _basicInfoWidget.mainElement.style.marginLeft = 32;
                _basicInfoWidget.mainElement.style.marginRight = 32;
                _basicInfoWidget.mainElement.style.marginBottom = 4;
                _basicInfoWidget.ShowWidget();
                _bodyWidget.mainElement.style.marginLeft = 32;
                _bodyWidget.mainElement.style.marginRight = 32;
                _bodyWidget.ShowWidget();
                _headWidget.mainElement.style.marginLeft = 32;
                _headWidget.mainElement.style.marginRight = 32;
                _headWidget.ShowWidget();
                _leftHandWidget.mainElement.style.marginLeft = 32;
                _leftHandWidget.mainElement.style.marginRight = 32;
                _leftHandWidget.ShowWidget();
                _rightHandWidget.mainElement.style.marginLeft = 32;
                _rightHandWidget.mainElement.style.marginRight = 32;
                _rightHandWidget.ShowWidget();
                _muscleWidget.mainElement.style.marginRight = 32;
                _muscleWidget.ShowWidget();
                _animationBoneWidget.mainElement.style.marginRight = 32;
                _animationBoneWidget.ShowWidget();
                

                _headWidget.HideWidget();
                _leftHandWidget.HideWidget();
                _rightHandWidget.HideWidget();
                _muscleWidget.HideWidget();
                _animationBoneWidget.HideWidget();

                _currentGroup = JointGroup.Body;

                _leftVis = mainElement.Q<VisualElement>("skeletonConfig_show_ve");

                m_SkeletonNextButton = mainElement.Q<Button>(k_SkeletonNextButton);
                UIUtils.AddVisualElementHoverMask(m_SkeletonNextButton);

                this.curImportSettings.GetImportSetting<PaabBasicInfoImportSetting>(true);
                _skeletonImportSetting = this.curImportSettings.GetImportSetting<PaabSkeletonImportSetting>(true);
                _muscleWidget.SetSkeletonSetting(_skeletonImportSetting);
                _animationBoneWidget.SetSkeletonSetting(_skeletonImportSetting);

                _jointObjects = new Dictionary<JointType, ObjectField>();

                var jointTypes = GetJointTypesList();
                foreach(JointType jointType in jointTypes)
                {
                    string jointTypeName = (jointType).ToString();
                    var jointObject = mainElement.Q<ObjectField>(jointTypeName);
                    if (jointObject == null) continue;
                     _jointObjects.Add(jointType, jointObject);
                    var gameObjectLabel = GetObjectFieldLabel(jointObject);
                    //if (gameObjectLabel.text == "None (Transform)")
                    //
                    if (jointObject.value == null)
                    {
                        gameObjectLabel.text = "(Optional)";
                        gameObjectLabel.style.color = (Color)GetEmptyOptionalObjectColor();
                    }

                    var jointLabel = jointObject.parent.parent.parent.Q<Label>("Label_" + jointTypeName);
                    if(jointLabel != null)
                    {
                        jointLabel.style.color = (Color)GetOptionalObjectTextColor();
                    }
                }
                for (int i = 0; i < keyJointList.Count; ++i)
                {
                    string jointTypeName = keyJointList[i].ToString();
                    var jointObject = mainElement.Q<ObjectField>(jointTypeName);
                    if (jointObject == null) continue;
                    var gameObjectLabel = GetObjectFieldLabel(jointObject);
                    if (gameObjectLabel.text == "(Optional)")
                    {
                        gameObjectLabel.text = "(Required)";
                        gameObjectLabel.style.color = (Color)GetEmptyObjectColor();
                    }
                    var jointLabel = jointObject.parent.parent.parent.Q<Label>("Label_" + jointTypeName);
                    if(jointLabel != null)
                    {
                        jointLabel.style.color = (Color)GetRequiredObjectTextColor();
                    }
                }

                _avatarRootErrorStatus = JointErrorStatus.None;
                _waitingForNextStep = false;
                _readyForNextStep = false;
                _hasWarning = false;
                _warningChecked = false;

                BuildJointUI();
                return true;
            }

            protected override bool BindUIActions() //RegisterButtonCallbacks
            {
                // register action when each button is clicked
                m_SkeletonNextButton?.RegisterCallback<ClickEvent>(SkeletonNextButtonFunc);
                var bodyButton = this.mainElement.Q<Button>("Skeleton_BodyButton");
                var headButton = this.mainElement.Q<Button>("Skeleton_HeadButton");
                var leftHandButton = this.mainElement.Q<Button>("Skeleton_LeftHandButton");
                var rightHandButton = this.mainElement.Q<Button>("Skeleton_RightHandButton");
    			_currentSelectedBodyPart = bodyButton;

                bodyButton?.RegisterCallback<ClickEvent>(ShowMappingWidget);
                UIUtils.AddVisualElementHoverMask(bodyButton);
                headButton?.RegisterCallback<ClickEvent>(ShowMappingWidget);
                UIUtils.AddVisualElementHoverMask(headButton);
                leftHandButton?.RegisterCallback<ClickEvent>(ShowMappingWidget);
                UIUtils.AddVisualElementHoverMask(leftHandButton);
                rightHandButton?.RegisterCallback<ClickEvent>(ShowMappingWidget);
                UIUtils.AddVisualElementHoverMask(rightHandButton);
                
                _avatarRootObject.RegisterValueChangedCallback((eve) =>
                {
                    ResetWarningCheck();
                    avatarRootTransform = (Transform)eve.newValue;
                    GetObjectFieldLabel(_avatarRootObject).style.color =(Color)GetDefaultColor();
                    if(eve.newValue == null)
                    {
                        GetObjectFieldLabel(_avatarRootObject).text = "(Required)";
                    }
                    AutoMappingJoints();
                });

                var mappingButton = this.mainElement.Q<Button>("Skeleton_MappingButton");
                var animationBoneButton = this.mainElement.Q<Button>("Skeleton_AnimationBoneButton");
                var muscleButton = this.mainElement.Q<Button>("Skeleton_MuscleButton");

                _currentSelectedTab = SkeletonMappingTab.Mapping;
                _currentSelectedTabButton = mappingButton;

                mappingButton?.RegisterCallback<ClickEvent>(SwitchMappingTab);
                UIUtils.AddVisualElementHoverMask(mappingButton);
                muscleButton?.RegisterCallback<ClickEvent>(SwitchMappingTab);
                UIUtils.AddVisualElementHoverMask(muscleButton);
                animationBoneButton?.RegisterCallback<ClickEvent>(SwitchMappingTab);
                UIUtils.AddVisualElementHoverMask(animationBoneButton);

                var clearButton = this.mainElement.Q<Button>("SkeletonConfig_show_middle_clearButton");
                clearButton?.RegisterCallback<ClickEvent>(SkeletonClearButtonFunc);
                UIUtils.AddVisualElementHoverMask(clearButton);

                // var customBoneButton = this.mainElement.Q<Button>("SkeletonConfig_show_middle_genCustomBones");
                // customBoneButton?.RegisterCallback<ClickEvent>(GenerateAllCustomBoneButtonFunc);
                // UIUtils.AddVisualElementHoverMask(customBoneButton); 

                // var addCustomBoneButton = this.mainElement.Q<Button>("SkeletonConfig_show_middle_addCustomBone");
                // addCustomBoneButton?.RegisterCallback<ClickEvent>(AddCustomBoneButtonFunc);
                // UIUtils.AddVisualElementHoverMask(addCustomBoneButton); 

                return base.BindUIActions();
            }

            /**
             * @brief Set new import settings to ui. Derived class SHOULD override the method.
             */
            public override void BindOrUpdateFromData(PaabAssetImportSettings importConfig)
            {
                _skeletonImportSetting = importConfig.GetImportSetting<PaabSkeletonImportSetting>(true);

                base.BindOrUpdateFromData(importConfig);

                //_basicInfoWidget.NameReadOnly = importConfig.opType == OperationType.Update ? true : false; 
                _basicInfoWidget.SetInputValue(importConfig.basicInfoSetting.assetName, 
                    importConfig.basicInfoSetting.assetNickName, importConfig.basicInfoSetting.assetIconPath);
                _basicInfoWidget.NameReadOnly = importConfig.opType == OperationType.Update ||
                                                 importConfig.opType == OperationType.UpdateAsset;
                //_basicInfoWidget.NameReadOnly = true;
                _basicInfoWidget.AutoSetName(importConfig.basicInfoSetting.characterName);
                _basicInfoWidget.AutoSetIconIfNotSet();
            }

            /**
             * @brief Set new import settings from panel ui. Derived class SHOULD override the method.
             */
            public override void UpdateToData(PaabAssetImportSettings importConfig)
            {
                importConfig.basicInfoSetting.SetAssetName(_basicInfoWidget.Name);
                importConfig.basicInfoSetting.assetNickName = _basicInfoWidget.ShowName;
                importConfig.basicInfoSetting.assetIconPath = _basicInfoWidget.Icon;

                _skeletonImportSetting = importConfig.GetImportSetting<PaabSkeletonImportSetting>(true);
                _skeletonImportSetting.skeletonName = importConfig.basicInfoSetting.assetName;
                if(_avatarRootTransform != null)
                {
                     _skeletonImportSetting.skeletonRootName = _avatarRootTransform.name;
                     //_skeletonImportSetting.skeletonRootGO = _avatarRootTransform.gameObject;
                }
                _skeletonImportSetting.avatarCustomJoints.Clear();

                base.UpdateToData(importConfig);

                _muscleWidget?.SetSkeletonSetting(_skeletonImportSetting);
                _animationBoneWidget?.SetSkeletonSetting(_skeletonImportSetting);
                if (_skeletonImportSetting == null)
                {
                    return;
                }
                
                //calculate joint axis
                var avatarJoints = _skeletonImportSetting.avatarJoints;
                //spine joints, use default root forward as reference
               

                //store default state
                var jointTypes = new List<JointType>(avatarJoints.Keys);

                foreach(JointType jointType in jointTypes)
                {
                    var avatarJoint = avatarJoints[jointType];
                    if(avatarJoints[jointType].JointTransform != null)
                    {
                        avatarJoint.defaultLocalOrientation = avatarJoint.JointTransform.localRotation;
                    }
                    //avatarJoints[jointTypeName] = avatarJoint;
                }

                if(avatarJoints.ContainsKey(JointType.Root))
                {
                    CalculateSpineJointAxis(_spineJoints);
                    CalculateLimbJointAxis(_leftArmJoints, JointType.LeftArmUpper, JointType.LeftArmLower, JointType.LeftHandWrist);
                    CalculateLimbJointAxis(_rightArmJoints, JointType.RightArmUpper, JointType.RightArmLower, JointType.RightHandWrist);
                    CalculateLimbJointAxis(_leftLegJoints, JointType.LeftLegUpper,  JointType.LeftLegLower,  JointType.LeftFootAnkle);
                    CalculateLimbJointAxis(_rightLegJoints,  JointType.RightLegUpper,  JointType.RightLegLower,  JointType.RightFootAnkle);
                }
            }

           

            public override void SaveContext()
            {
                // this.BindOrUpdateFromData(this.curImportSettings);
                var jsonObj = new Dictionary<string, object>();
                _skeletonImportSetting.ToJsonObject(jsonObj);
                _skeletonImportSettingString =  JsonConvert.SerializeObject(jsonObj);
                
                //1. save the assetSettings to local CurrentPreviewAsset.asset
                if(_skeletonImportSetting != null)
                {
                    if (string.IsNullOrEmpty(AssetBuilderConfig.instance.uiDataStorePath + "PanelData/SkeletonPanel.asset"))
                    {
                        Utils.ReCreateAssetAt(_instance, AssetBuilderConfig.instance.assetViwerDataAssetsPath + "Data/CurrentPreviewAsset.asset");
                    }
                }
                //
                base.SaveContext();
            }

            public void OnJointSelected(JointType jointType)
            {
                if(_currentSelectedJointType == jointType)
                {
                    return;
                }
                if(!_jointVisualElements.ContainsKey(jointType) || !_jointShowMap.ContainsKey(jointType))
                {
                    return;
                }

                //select current
                var jointVisualElement = _jointVisualElements[jointType];
                uint jointStatus = _jointStatusMap[jointType].fillStatus;
                jointStatus |= SELECTED;
                jointVisualElement.style.backgroundImage = _jointVisIcons[jointStatus];
                _jointStatusMap[jointType].fillStatus = jointStatus;
                
                //unselect current
                if(!_jointVisualElements.ContainsKey(_currentSelectedJointType) || !_jointShowMap.ContainsKey(_currentSelectedJointType))
                {
                    _currentSelectedJointType = jointType;
                    return;
                }
                jointVisualElement = _jointVisualElements[_currentSelectedJointType];
                jointStatus = _jointStatusMap[_currentSelectedJointType].fillStatus;
                jointStatus &= ~SELECTED;
                jointVisualElement.style.backgroundImage = _jointVisIcons[jointStatus];
                _jointStatusMap[_currentSelectedJointType].fillStatus = jointStatus;

                _currentSelectedJointType = jointType;
                
            }

            public void OnJointValueChanged(JointType jointType, bool filled, ObjectField jointObjectField)
            {
                if(!_jointVisualElements.ContainsKey(jointType) || !_jointShowMap.ContainsKey(jointType))
                {
                    return;
                }

                //select current
                var jointVisualElement = _jointVisualElements[jointType];
                uint jointStatus = _jointStatusMap[jointType].fillStatus;
                if(filled)
                {
                    jointStatus |= FILLED;
                }
                else
                {
                    jointStatus &= ~FILLED;
                }
                jointVisualElement.style.backgroundImage = _jointVisIcons[jointStatus];
                _jointStatusMap[jointType].fillStatus = jointStatus;
            }
            

            private void BuildJointUI()
            {
                _jointVisGroups = new Dictionary<JointGroup, GroupBox>();
                _jointVisualElements = new Dictionary<JointType, VisualElement>();
                _jointStatusMap = new Dictionary<JointType,JointStatus>();
                _jointVisIcons = new Dictionary<uint, StyleBackground>();

                var bodyGroup = mainElement.Q<GroupBox>("skeletonConfig_show_middle_body");
                _jointVisGroups.Add(JointGroup.Body, bodyGroup);

                var headGroup = mainElement.Q<GroupBox>("skeletonConfig_show_middle_head");
                _jointVisGroups.Add(JointGroup.Head, headGroup);

                var leftHandGroup = mainElement.Q<GroupBox>("skeletonConfig_show_middle_leftHand");
                _jointVisGroups.Add(JointGroup.LeftHand, leftHandGroup);

                var rightHandGroup = mainElement.Q<GroupBox>("skeletonConfig_show_middle_rightHand");
                _jointVisGroups.Add(JointGroup.RightHand, rightHandGroup);

                _jointMappingProgressSign = mainElement.Q<VisualElement>("skeletonConfig_show_middle_progressSign");

                // load icons
                _jointVisIcons.Add(0, new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetBuilderConfig.instance.uiDataIconPath + "/Skeleton/Skeleton_Optional_Default.png")));
                _jointVisIcons.Add(REQUIRED, new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetBuilderConfig.instance.uiDataIconPath + "/Skeleton/Skeleton_Required_Default.png")));
                _jointVisIcons.Add(FILLED, new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetBuilderConfig.instance.uiDataIconPath + "/Skeleton/Skeleton_Optional_Filled.png")));
                _jointVisIcons.Add(REQUIRED | FILLED, new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetBuilderConfig.instance.uiDataIconPath + "/Skeleton/Skeleton_Required_Filled.png")));
                _jointVisIcons.Add(SELECTED, new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetBuilderConfig.instance.uiDataIconPath + "/Skeleton/Skeleton_Optional_Selected.png")));
                _jointVisIcons.Add(REQUIRED | SELECTED, new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetBuilderConfig.instance.uiDataIconPath + "/Skeleton/Skeleton_Required_Selected.png")));
                _jointVisIcons.Add(SELECTED | FILLED, new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetBuilderConfig.instance.uiDataIconPath + "/Skeleton/Skeleton_Optional_Filled_Selected.png")));
                _jointVisIcons.Add(REQUIRED | SELECTED | FILLED, new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetBuilderConfig.instance.uiDataIconPath + "/Skeleton/Skeleton_Required_Filled_Selected.png")));

                _jointMappingIncompleteIcon = new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetBuilderConfig.instance.uiDataIconPath + "/Skeleton/Skeleton_NotComplete.png"));
                _jointMappingCompleteIcon = new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetBuilderConfig.instance.uiDataIconPath + "/Skeleton/Skeleton_AllComplete.png"));

                foreach (KeyValuePair<JointType, JointShowProperty> kvp in _jointShowMap)
                {
                    var showProp = kvp.Value;
                    GroupBox jointGroupBox = null;
                    

                    var jointGroup = showProp.jointGroup;
                    if(_jointVisGroups.ContainsKey(jointGroup))
                    {
                        jointGroupBox = _jointVisGroups[jointGroup];
                    }
                    if(jointGroupBox == null)
                    {
                        continue;
                    }
                    int size = jointGroup == JointGroup.Head ? 64 : 32;
                    int iconOffset = jointGroup == JointGroup.Head ? 24 : 12;
                    
                    var element = new VisualElement();
                  
                    element.style.left = showProp.left - iconOffset;
                    element.style.top = showProp.top - iconOffset;
                    element.style.width = size;
                    element.style.height = size;
                    element.style.position = Position.Absolute;
                    element.name = "skeletonConfig_show_joint_" + kvp.Key;
                    if(showProp.jointLevel == JointLevel.Required)
                    {
                        element.style.backgroundImage = _jointVisIcons[REQUIRED];
                        _jointStatusMap.Add(kvp.Key, new JointStatus(REQUIRED, JointErrorStatus.None));
                    }
                    else
                    {
                        element.style.backgroundImage = _jointVisIcons[0];
                        _jointStatusMap.Add(kvp.Key, new JointStatus(0, JointErrorStatus.None));
                    }

                    jointGroupBox.Add(element);
                    _jointVisualElements.Add(kvp.Key, element);
                }
            }

            private List<JointType> GetJointTypesList()
            {
                return _bodyWidget?.GetJointTypesList().Concat(_headWidget?.GetJointTypesList()).Concat(_leftHandWidget?.GetJointTypesList()).Concat(_rightHandWidget?.GetJointTypesList()).ToList<JointType>();
            }

            private int CountChildren(Transform parent)
            {
                // 计算当前 Transform 的子节点数量
                int count = parent.childCount;

                // 递归计算所有子节点的数量
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    count += CountChildren(child);
                }

                return count;
            }
            
            private JointType GetJointParentType(JointType jointType)
            {
                if(_skeletonImportSetting == null)
                {
                    return JointType.Invalid;
                }
                if(jointParentMap.ContainsKey(jointType))
                {
                    JointType parentType = jointParentMap[jointType];
                    if (_skeletonImportSetting.avatarJoints.ContainsKey(parentType))
                    {
                        return parentType;
                    }
                    else
                    {
                        return GetJointParentType(parentType);
                    }
                }
                return JointType.Invalid;
            }
            
            private void CheckAll()
            {
                _basicInfoWidget.CheckValue(OnCheckFinish);

            }

            private string GetSkeletonDir(string assetName)
            {
                return $"{CharacterUtil.CharacterFolderPath}/{CharacterUtil.SkeletonFolderName}/{assetName}";
            }

            public GameObject[] OnPreEnterPreview()
            {
                if (_avatarRootTransform == null || _jointObjects == null || _jointMappingWidgets == null || _avatarRootObject == null || _avatarRootObject.value == null)
                {
                    return null;
                }

                List<GameObject> objs = new List<GameObject>();

                List<SkeletonMappingSettingWidget> jointMappingWidgets = new List<SkeletonMappingSettingWidget>();
                jointMappingWidgets.AddRange(_jointMappingWidgets.Values);
                List<ObjectField> jointObjects = new List<ObjectField>();
                jointObjects.AddRange(_jointObjects.Values);

                objs.Add(_avatarRootTransform.gameObject);
                objs.Add((_avatarRootObject.value as Transform).gameObject);
                for (int i = 0; i < jointMappingWidgets.Count; ++i)
                {
                    objs.Add(jointMappingWidgets[i]?.avatarRootTransform?.gameObject);
                }
                for (int i = 0; i < jointObjects.Count; ++i)
                {
                    objs.Add((jointObjects[i]?.value as Transform)?.gameObject);
                }

                return objs.ToArray();
            }

            public void OnPostExitPreview(GameObject[] objs)
            {
                if (_jointObjects == null || _jointMappingWidgets == null || _avatarRootObject == null)
                {
                    return;
                }

                List<SkeletonMappingSettingWidget> jointMappingWidgets = new List<SkeletonMappingSettingWidget>();
                jointMappingWidgets.AddRange(_jointMappingWidgets.Values);
                List<ObjectField> jointObjects = new List<ObjectField>();
                jointObjects.AddRange(_jointObjects.Values);

                int index = 0;
                _avatarRootTransform = objs[index++]?.transform;
                _avatarRootObject.SetValueWithoutNotify(objs[index++]?.transform);
                for (int i = 0; i < jointMappingWidgets.Count; ++i)
                {
                    var transform = objs[index++]?.transform;
                    if (jointMappingWidgets[i] != null)
                    {
                        jointMappingWidgets[i].avatarRootTransform = transform;
                    }
                }
                for (int i = 0; i < jointObjects.Count; ++i)
                {
                    var transform = objs[index++]?.transform;
                    if (jointObjects[i] != null)
                    {
                        jointObjects[i].SetValueWithoutNotify(transform);
                    }
                }
            }

            private void DoConvert()
            {
                // Todo: curImportSettings is not null, but cacheptr is 0x0.
                if (curImportSettings.settingItems.Length > 0)
                {
                    string assetName = curImportSettings.assetName;
                    string extrasJson = curImportSettings.ToJsonText();
                    var tempDir = Application.dataPath.Replace("/Assets", "") + "/SkeletonTemp/";
                    var tempZipPath = tempDir + assetName + ".zip";

                    var skeleton = _avatarRootTransform.gameObject;
                    {
                        AvatarConverter.ConvertSkeleton(assetName, skeleton, tempZipPath, extrasJson, () => {
                            //Debug.LogError("ConvertSkeleton complete " + tempZipPath);
                            if (!string.IsNullOrEmpty(CharacterUtil.CharacterFolderPath) && System.IO.Directory.Exists(CharacterUtil.CharacterFolderPath))
                            {
                                string skeletonDir = GetSkeletonDir(assetName);
                                if (!Directory.Exists(skeletonDir))
                                {
                                    Directory.CreateDirectory(skeletonDir);
                                }
                                var sourceDir = new System.IO.FileInfo(tempZipPath).DirectoryName;
                                System.IO.File.Copy(tempZipPath, skeletonDir + "/0.zip", true);
                                System.IO.File.Copy(sourceDir + "/config.json", skeletonDir + "/" + "0.config.json", true);
                                Directory.Delete(tempDir, true);

                                CharacterManager.instance.SetAssetLoadSource(PaabCharacterImportSetting.AssetState_Local, curImportSettings.assetName, CharacterBaseAssetType.Skeleton);
                                curImportSettings.assetStatus = AssetStatus.Ready;
                                _readyForNextStep = true;
                                ResetWarningCheck();
                            }
                        });
                    }
                }
            }

            private void OnCheckFinish(BaseInfoValueCheckResult result)
            {
                var errors = BaseInfoWidget.GetBaseInfoErrorMessage(result);
                bool showWarning = !_warningChecked;
                if(CheckSkeleton(showWarning, false, errors))
                {
                    if(_hasWarning && !_warningChecked)
                    {
                        _warningChecked = true;
                        _waitingForNextStep = false;
                        //reset preview pos
                        if(_currentSelectedTab == SkeletonMappingTab.Muscle)
                        {
                            _muscleWidget?.InitPreviewValues();
                        }
                        return;
                    }
                    _warningChecked = false;
                    //convert to zip
                    DoConvert();
                }
                else
                {
                    if(_currentSelectedTab == SkeletonMappingTab.Muscle)
                    {
                        _muscleWidget?.InitPreviewValues();
                    }
                    _waitingForNextStep = false;
                }
            }

            public override void OnUpdate()
            {
                base.OnUpdate();
                UpdateLabels();
                UpdateMappingProgress();
                // if(_muscleWidget != null)
                // {
                //     _muscleWidget.UpdateValueTextPositions();
                // }
                if(_waitingForNextStep && _readyForNextStep)
                {
                    _waitingForNextStep = false;
                    _readyForNextStep = false;
                    AssetTestPanel.ShowAssetTestPanel(this, curImportSettings);
                }
            }

            public override void OnNextStep()
            {
               SkeletonNextButtonFunc(null);
            }

            public override void OnShow()
            {
                base.OnShow();
                WarningPanel.instance.OnNextStepCallback += OnNextStep;
                if(_currentSelectedTab == SkeletonMappingTab.Muscle)
                {
                    _muscleWidget?.InitPreviewValues();
                }
                ResetWarningCheck();
            }

            public override void OnHide()
            {
                base.OnHide();
                WarningPanel.instance.OnNextStepCallback -= OnNextStep;
                this._muscleWidget?.ResetPreviewValues();
                ResetJointOrientations();
                ResetWarningCheck();
                this.UpdateToData(curImportSettings);   
            }

            public void ResetWarningCheck()
            {
                _warningChecked = false;
            }
            
            public bool UpdateToDataAndCheckSkeleton()
            {
                UpdateToData(curImportSettings);

                if(!CheckSkeleton(false, true))
                {
                    return false;
                }
                return true;
            } 
             

            private void UpdateLabels()
            {
                if(_avatarRootObject != null)
                {
                    var gameObjectLabel = GetObjectFieldLabel(_avatarRootObject);
                    if (_avatarRootObject.value == null)
                    {
                        gameObjectLabel.text = "(Required)";
                        if(_avatarRootErrorStatus == JointErrorStatus.Error)
                        {
                            gameObjectLabel.style.color = (Color)GetErrorColor();
                        }
                        else
                        {
                            gameObjectLabel.style.color = (Color)GetEmptyObjectColor();
                        }
                        
                    }
                }
                var jointTypes = GetJointTypesList();
                foreach(JointType jointType in jointTypes)
                {
                    if(!_jointObjects.ContainsKey(jointType))
                    {
                        continue;
                    }
                    var jointObject = _jointObjects[jointType];
                    if (jointObject == null) continue;
                    var gameObjectLabel = GetObjectFieldLabel(jointObject);
                    if (jointObject.value == null)
                    {
                        gameObjectLabel.text = "(Optional)";
                        if(_jointStatusMap[jointType].errorStatus == JointErrorStatus.Error)
                        {
                            gameObjectLabel.style.color = (Color)GetErrorColor();
                        }
                        else
                        {
                            gameObjectLabel.style.color = (Color)GetEmptyOptionalObjectColor();
                        }
                    }
                }
                for (int i = 0; i < keyJointList.Count; ++i)
                {
                    JointType jointType = keyJointList[i];
                    if(!_jointObjects.ContainsKey(jointType))
                    {
                        continue;
                    }
                    var jointObject = _jointObjects[jointType];
                    if (jointObject == null) continue;
                    var gameObjectLabel = GetObjectFieldLabel(jointObject);
                    if (gameObjectLabel.text == "(Optional)")
                    {
                        gameObjectLabel.text = "(Required)";
                        if(_jointStatusMap[jointType].errorStatus == JointErrorStatus.Error)
                        {
                            gameObjectLabel.style.color = (Color)GetErrorColor();
                        }
                        else
                        {
                            gameObjectLabel.style.color = (Color)GetEmptyOptionalObjectColor();
                        }
                    }
                }

                _bodyWidget.UpdateCustomBoneLabels();
                _headWidget.UpdateCustomBoneLabels();
                _leftHandWidget.UpdateCustomBoneLabels();
                _rightHandWidget.UpdateCustomBoneLabels();
            }

            private void UpdateMappingProgress()
            {
                bool allJointsMapped = true;
                foreach(JointType keyJointType in keyJointList)
                {
                    if(_jointStatusMap.ContainsKey(keyJointType))
                    {
                        uint jointStatus = _jointStatusMap[keyJointType].fillStatus;
                        if((jointStatus & FILLED) == 0)
                        {
                            allJointsMapped = false;
                            break;
                        }
                    }
                }
                if(allJointsMapped)
                {
                    if(_jointMappingProgressSign != null)
                    {
                        _jointMappingProgressSign.style.backgroundImage = _jointMappingCompleteIcon;
                    }
                }
                else
                {
                    if(_jointMappingProgressSign != null)
                    {
                        _jointMappingProgressSign.style.backgroundImage = _jointMappingIncompleteIcon;
                    }
                }
            }

            private bool IsApproximate(Quaternion q1, Quaternion q2, float precision = 1e-6f)
            {
                return Mathf.Abs(Quaternion.Dot(q1, q2)) > 1 - precision;
            }

            private bool IsApproximate(Vector3 v1, Vector3 v2, float precision = 1e-6f)
            {
                return Vector3.SqrMagnitude(v1-v2) < precision;
            }
            
            private void CollectCustomBones()
            {
                if(_customJointObjects == null)
                {
                    _customJointObjects = new List<ObjectField>();
                }
                _customJointObjects.Clear();
                _customJointObjects.AddRange(_bodyWidget.customJointObjects);
                _customJointObjects.AddRange(_headWidget.customJointObjects);
                _customJointObjects.AddRange(_leftHandWidget.customJointObjects);
                _customJointObjects.AddRange(_rightHandWidget.customJointObjects);
                customJointObjectsDirty = false;
            }

            private void CheckSkeletonPose(List<CommonDialogWindow.Message> messages, ref int errorCount, ref int warningCount)
            {
                Transform rootTransformInSkeleton = _avatarRootTransform;
                //1. check skeleton root orientation
                if(rootTransformInSkeleton!= null)
                {
                    var gameObjectLabel = GetObjectFieldLabel(_avatarRootObject);
                    var rootPosition = rootTransformInSkeleton.localPosition;
                    var rootOrientation = rootTransformInSkeleton.localRotation;

                    gameObjectLabel.style.color =(Color)GetDefaultColor();
                    if(!IsApproximate(rootPosition,Vector3.zero))
                    {
                        gameObjectLabel.style.color = (Color)GetErrorColor();
                        _avatarRootErrorStatus = JointErrorStatus.Error;
                        errorCount += 1;
                        messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error,"Avatar skeleton root local position is not zero"));
                    }  

                    if(!IsApproximate(rootOrientation,Quaternion.identity))
                    {
                        gameObjectLabel.style.color = (Color)GetErrorColor();
                        _avatarRootErrorStatus = JointErrorStatus.Error;
                        errorCount += 1;
                        messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error,"Avatar skeleton root local rotation is not zero"));
                    }  
                }

                if(_skeletonImportSetting.avatarJoints.ContainsKey(JointType.Root))
                {
                    var rootTransform = _skeletonImportSetting.avatarJoints[JointType.Root].JointTransform;
                    if(rootTransform != null)
                    {
                        var jointObject = mainElement.Q<ObjectField>("Root");
                    
                        var rootPosition = rootTransform.localPosition;
                        var rootOrientation = rootTransform.localRotation;
                        Label gameObjectLabel = null;
                        if (jointObject != null) 
                        {
                            gameObjectLabel = GetObjectFieldLabel(jointObject);
                            gameObjectLabel.style.color = (Color)GetDefaultColor();
                        }

                        if(!IsApproximate(rootPosition,Vector3.zero))
                        {
                            if (gameObjectLabel != null) 
                            {
                                gameObjectLabel.style.color = (Color)GetErrorColor();
                            }
                            _jointStatusMap[JointType.Root].errorStatus = JointErrorStatus.Error;
                            errorCount += 1;
                            messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error,"Avatar root bone local position is not zero"));
                        }  

                        if(!IsApproximate(rootOrientation,Quaternion.identity))
                        {
                            if (gameObjectLabel != null) 
                            {
                                gameObjectLabel.style.color = (Color)GetErrorColor();
                            }
                            _jointStatusMap[JointType.Root].errorStatus = JointErrorStatus.Error;
                            errorCount += 1;
                            messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error,"Avatar root bone local rotation is not zero"));
                        }  
                    }
                }

                
                // 2. check skeleton TPose.
                if (_skeletonImportSetting.avatarJoints.ContainsKey(JointType.LeftShoulder) &&
                    _skeletonImportSetting.avatarJoints.ContainsKey(JointType.LeftHandWrist) &&
                    _skeletonImportSetting.avatarJoints.ContainsKey(JointType.RightShoulder) &&
                    _skeletonImportSetting.avatarJoints.ContainsKey(JointType.RightHandWrist))
                {
                    Transform shoulderLTransformInSkeleton = _skeletonImportSetting.avatarJoints[JointType.LeftShoulder].JointTransform;
                    Transform handLTransformInSkeleton = _skeletonImportSetting.avatarJoints[JointType.LeftHandWrist].JointTransform;
                    Transform shoulderRTransformInSkeleton = _skeletonImportSetting.avatarJoints[JointType.RightShoulder].JointTransform;
                    Transform handRTransformInSkeleton = _skeletonImportSetting.avatarJoints[JointType.RightHandWrist].JointTransform;
                    float distanceL = Vector3.Distance(handLTransformInSkeleton.position, shoulderLTransformInSkeleton.position);
                    float distanceR = Vector3.Distance(handRTransformInSkeleton.position, shoulderRTransformInSkeleton.position);
                    float distanceLR = Vector3.Distance(handLTransformInSkeleton.position,handRTransformInSkeleton.position);
                    
                    if (distanceL + distanceR < distanceLR)
                    {
                      
                        // if(showWarning)
                        // {
                        //     warningCount += 1;
                        //     messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Warning,"the Skeleton is T Pose"));
                        // }
                    }
                    else
                    {
                        //if(showWarning)
                        {
                            errorCount += 1;
                            messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error,"the Skeleton is A Pose"));
                        }
                    }

                    if(_skeletonImportSetting.avatarJoints.ContainsKey(JointType.Head) && rootTransformInSkeleton != null)
                    {
                        Transform headTransform = _skeletonImportSetting.avatarJoints[JointType.Head].JointTransform;
                        Vector3 directionLeft = Vector3.Normalize(handLTransformInSkeleton.position - handRTransformInSkeleton.position);
                        Vector3 directionUp = Vector3.Normalize(headTransform.position - rootTransformInSkeleton.position);
                        Vector3 directionForward = Vector3.Normalize(Vector3.Cross(directionUp,directionLeft));

                        Quaternion rootOrientation = rootTransformInSkeleton.rotation;
                        Vector3 requiredUp = rootOrientation * Vector3.up;
                        Vector3 requiredForward = rootOrientation * Vector3.forward;

                        if(!IsApproximate(requiredUp,directionUp,0.1f) || !IsApproximate(requiredForward,directionForward,0.1f))
                        {
                            errorCount += 1;
                            messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error,"Avatar is not Y-Up and Z-forward"));
                        }
                    }
                }
                


                // 3. check skeleton Unit. 
                // if the distance between the root node and the head node is less than 1, then unit is cm.
                if (_skeletonImportSetting.avatarJoints.ContainsKey(JointType.Head) && rootTransformInSkeleton != null)
                {
                    Transform headTransformInSkeleton = _skeletonImportSetting.avatarJoints[JointType.Head].JointTransform;
                    float distance = Vector3.Distance(rootTransformInSkeleton.position, headTransformInSkeleton.position);
                    if (distance > 10.0f)
                    {
                        errorCount += 1;
                        messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error,"the Skeleton Unit is cm."));
                    }
                }
            }

            private void CheckSkeletonStructure(List<CommonDialogWindow.Message> messages, ref int errorCount, ref int warningCount)
            {
                Transform rootTransformInSkeleton = _avatarRootTransform;
                if(rootTransformInSkeleton)
                {
                    if(_skeletonImportSetting.avatarJoints.ContainsKey(JointType.Root))
                    {
                        var rootTransform = _skeletonImportSetting.avatarJoints[JointType.Root].JointTransform;
                        if(rootTransform != null)
                        {
                            if(!rootTransform.IsChildOf(rootTransformInSkeleton))
                            {
                                _jointStatusMap[JointType.Root].errorStatus = JointErrorStatus.Error;
                                errorCount += 1;
                                messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error,"Avatar Root bone is not child of avatar skeleton root"));
                            }
                        }
                    }
                }
                
                // JointParentMap 
                foreach (KeyValuePair<JointType, List<JointType>> kvp in jointChildrenMap)
                {
                    // string startName = kvp.Key;
                    // string endName = kvp.Value;

                    var startTypes = kvp.Value;
                    JointType endType = kvp.Key;

                    Transform startTrans = null; 
                    Transform endTrans = null;

                    foreach(JointType startType in startTypes)
                    {
                        //check parent - child relationship
                        if (_skeletonImportSetting.avatarJoints.ContainsKey(startType))
                        {
                            startTrans = _skeletonImportSetting.avatarJoints[startType].JointTransform;
                        }
                        else
                        {
                            continue;
                        }
                        
                        if (_skeletonImportSetting.avatarJoints.ContainsKey(endType))
                        {
                            endTrans = _skeletonImportSetting.avatarJoints[endType].JointTransform;
                        }
                        else
                        {
                            // loop find the end transForm
                            while (endTrans == null)
                            {
                                if(!jointParentMap.ContainsKey(endType))
                                {
                                    break;
                                }
                                JointType endParentType = jointParentMap[endType];
                                if (_skeletonImportSetting.avatarJoints.ContainsKey(endParentType))
                                {
                                    endTrans = _skeletonImportSetting.avatarJoints[endParentType].JointTransform;
                                    endType = endParentType;
                                }
                                else
                                {
                                    endType = endParentType;
                                }
                            }
                        }

                        if (startTrans != null && endTrans != null)
                        {
                            if (startTrans == endTrans || !startTrans.IsChildOf(endTrans))
                            {
                                errorCount += 1;
                                messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error, startType.ToString() + " is not child of " + endType.ToString()));
                                // set startName objectfield red.
                                var jointObject = mainElement.Q<ObjectField>(startType.ToString());
                                if (jointObject != null)
                                {
                                    var gameObjectLabel = GetObjectFieldLabel(jointObject);
                                    gameObjectLabel.style.color = (Color)GetErrorColor();
                                    _jointStatusMap[startType].errorStatus = JointErrorStatus.Error;
                                }
                            }
                        }

                        //check relationship between childrens
                        foreach(JointType siblingType in startTypes)
                        {
                            if (siblingType != startType && _skeletonImportSetting.avatarJoints.ContainsKey(siblingType))
                            {
                                var siblingTrans = _skeletonImportSetting.avatarJoints[siblingType].JointTransform;
                                if(startTrans.IsChildOf(siblingTrans))
                                {
                                    errorCount += 1;
                                    messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error, startType.ToString() + " should not be the child of " + siblingType.ToString()));
                                    // set startName objectfield red.
                                    var jointObject = mainElement.Q<ObjectField>(startType.ToString());
                                    if (jointObject != null)
                                    {
                                        var gameObjectLabel = GetObjectFieldLabel(jointObject);
                                        gameObjectLabel.style.color = (Color)GetErrorColor();
                                        _jointStatusMap[startType].errorStatus = JointErrorStatus.Error;
                                    }
                                }
                            }
                        }       
                    }         
                }
            }

            private void CheckDulicateAndCustomBones(List<CommonDialogWindow.Message> messages, ref int errorCount, ref int warningCount)
            {
                CollectCustomBones();
                
                HashSet<Transform> mappedJoints = new HashSet<Transform>();
                HashSet<Transform> duplicateTransforms = new HashSet<Transform>();
                foreach(var avatarJoint in _skeletonImportSetting.avatarJoints)
                {
                    var jointTransform = avatarJoint.Value.JointTransform;
                    if(jointTransform == null)
                    {
                        continue;
                    }
                    if(mappedJoints.Contains(jointTransform))
                    {
                        duplicateTransforms.Add(jointTransform);
                    }
                    else
                    {
                        mappedJoints.Add(jointTransform);
                    }
                }

                foreach(var customJointObject in _customJointObjects)
                {
                    var jointTransform = customJointObject.value as Transform;
                    if(jointTransform == null)
                    {
                        continue;
                    }
                    if(mappedJoints.Contains(jointTransform))
                    {
                        duplicateTransforms.Add(jointTransform);
                    }
                    else
                    {
                        mappedJoints.Add(jointTransform);
                    }
                }

                foreach(var kvp in _jointObjects)
                {
                    var jointObject = kvp.Value;
                    var jointType = kvp.Key;
                    var jointTransform = jointObject.value as Transform;
                    if(duplicateTransforms.Contains(jointTransform))
                    {
                        var gameObjectLabel = GetObjectFieldLabel(jointObject);
                        gameObjectLabel.style.color = (Color)GetErrorColor();
                        _jointStatusMap[jointType].errorStatus = SkeletonPanel.JointErrorStatus.Error;
                        errorCount += 1;
                    }
                }

                _bodyWidget.CheckCustomBones(duplicateTransforms, _avatarRootTransform, messages, ref errorCount, ref warningCount);
                _headWidget.CheckCustomBones(duplicateTransforms, _avatarRootTransform, messages, ref errorCount, ref warningCount);
                _leftHandWidget.CheckCustomBones(duplicateTransforms, _avatarRootTransform, messages, ref errorCount, ref warningCount);
                _rightHandWidget.CheckCustomBones(duplicateTransforms,  _avatarRootTransform, messages, ref errorCount, ref warningCount);

                foreach(var duplicateTransform in duplicateTransforms)
                {
                    messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error,"Transform " + duplicateTransform.name + " appears more than once."));
                }
            }

            private bool CheckSkeleton(bool showWarning = true,  bool mappingOnly = false, List<string> errorMessages = null)
            {
                int errorCount = 0;
                int warningCount = 0;
                var messages = new List<CommonDialogWindow.Message>();
                
                // 0. add existing error messages from base info widget to messages list.
                if(errorMessages != null)
                {
                    for (int i = 0; i < errorMessages.Count; ++i)
                    {
                        errorCount += 1;
                        messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error, errorMessages[i]));
                    }
                }

                // 1. check skeleton Number.
                Transform rootTransformInSkeleton = _avatarRootTransform;
                if (rootTransformInSkeleton != null)
                {
                    int skeletonNum = CountChildren(rootTransformInSkeleton);
                    if (skeletonNum > 120)
                    {
                        if (skeletonNum <= 250)
                        {
                            if (showWarning)
                            {
                                warningCount += 1;
                                messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Warning, "The number of skeleton is too many. The recomended number is 120."));
                            }
                        }
                        else
                        {
                            errorCount += 1;
                            messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error, "The number of skeleton is too many. The maximum number is 250."));
                        }
                    }
                }
                
                // 2. check key skeleton.
                bool MissingKeySkeleton = false;

                var jointTypes = GetJointTypesList();
                foreach(JointType jointType in jointTypes)
                {
                    string jointTypeName = jointType.ToString();
                    var jointObject = mainElement.Q<ObjectField>(jointTypeName);
                    if (jointObject == null) continue;
                    var gameObjectLabel = GetObjectFieldLabel(jointObject);
                    if (gameObjectLabel.text == "(Optional)")
                    {
                        gameObjectLabel.style.color = (Color)GetEmptyOptionalObjectColor();
                        _jointStatusMap[jointType].errorStatus = JointErrorStatus.None;
                        // warningCount += 1;
                        // messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Warning,"Missing Option Joint, named:  " + jointTypeName));
                    }
                    else if (gameObjectLabel.text == "(Required)")
                    {
                        MissingKeySkeleton = true;
                        gameObjectLabel.style.color = (Color)GetErrorColor();
                        _jointStatusMap[jointType].errorStatus = JointErrorStatus.Error;
                        errorCount += 1;
                        messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error,"Missing key Joint, named:  " + jointTypeName));
                    }
                    else
                    {
                        gameObjectLabel.style.color = (Color)GetDefaultColor();
                        _jointStatusMap[jointType].errorStatus = JointErrorStatus.None;
                    }
                }
                {
                    var gameObjectLabel = GetObjectFieldLabel(_avatarRootObject);
                    if (gameObjectLabel.text == "(Required)")
                    {
                        gameObjectLabel.style.color = (Color)GetErrorColor();
                        _avatarRootErrorStatus = JointErrorStatus.Error;
                        errorCount += 1;
                        messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error,"Missing Avatar Root"));
                    }
                    else{
                        _avatarRootErrorStatus = JointErrorStatus.None;
                    }
                }
                
                // 3. check skeleton Pose 
                CheckSkeletonPose(messages, ref errorCount, ref warningCount);
                
                // 4. Check the main skeletal subordinate relationship.
                if (MissingKeySkeleton == false)
                {
                    CheckSkeletonStructure(messages, ref errorCount, ref warningCount);
                }
                
                //5. check custom bones & duplicate bones
                CheckDulicateAndCustomBones(messages, ref errorCount, ref warningCount);
                

                //6. check twist settings
                if(!mappingOnly)
                {
                    _muscleWidget?.CheckTwistSettings(messages, ref errorCount, ref warningCount);
                }

                //Show error and warnings
                if (errorCount > 0 || (warningCount > 0 ))
                {
                    CommonDialogWindow.ShowCheckPopupDialog(messages);
                }

                if(errorCount == 0 && warningCount > 0)
                {
                    _hasWarning = true;
                }

                if(errorCount > 0)
                {
                    return false;
                }
                return true;
            }

            private void SkeletonNextButtonFunc(ClickEvent evt)
            {
                this._muscleWidget?.ResetPreviewValues();
                ResetJointOrientations();
                this.UpdateToData(curImportSettings);

                //force refresh animation bones and update to data
                this._animationBoneWidget?.RefreshCustomBoneSettingItems();
                this._animationBoneWidget?.UpdateToData(curImportSettings);

                _hasWarning = false;
                CheckAll();
                _waitingForNextStep = true;
                _readyForNextStep = false;
            }

            private void CalculateSpineJointAxis(JointType[] jointList)
            {
                 //for spine joints,  “up” is the dir pointing from parent to child ，"axis" is root forward
                var avatarJoints = _skeletonImportSetting.avatarJoints;
                var rootTransform = avatarJoints[JointType.Root].JointTransform;                   
                Vector3 rootForward = rootTransform.forward;
                Vector3 rootUp = rootTransform.up;
                foreach(JointType jointType in jointList)
                {
                    if(avatarJoints.ContainsKey(jointType))
                    {
                        var currentJointTransform = avatarJoints[jointType].JointTransform;
                        var parentName = GetJointParentType(jointType);
                        if(!avatarJoints.ContainsKey(parentName))
                        {
                            continue;
                        }
                        var parentJoint = avatarJoints[parentName];
                        var parentJointTransform = parentJoint.JointTransform;
                        if(jointList.Contains(parentName) && parentJointTransform != null)
                        {
                            Vector3 worldAxis = currentJointTransform.position - parentJointTransform.position;
                            Quaternion worldOrientationInv = Quaternion.Inverse(parentJointTransform.rotation);
                            parentJoint.axis = Vector3.Normalize(worldOrientationInv * worldAxis);
                            parentJoint.up = Vector3.Normalize(worldOrientationInv * rootForward);
                        }
                    }
                }

                //for end point "head" use default root up as reference.
                if(avatarJoints.ContainsKey(jointList[jointList.Length - 1]))
                {
                    var endJoint = avatarJoints[jointList[jointList.Length - 1]];
                    var endTransform = endJoint.JointTransform;
                    if(endTransform != null)
                    {
                        Quaternion worldOrientationInv = Quaternion.Inverse(endTransform.rotation);
                        endJoint.up = worldOrientationInv * rootUp;
                        endJoint.axis = worldOrientationInv * rootForward;
                        //avatarJoints[jointList[jointList.Length - 1]] = endJoint;
                    }
                   
                }
            }
            private void CalculateLimbJointAxis(JointType[] jointList, JointType upperJoint, JointType middleJoint, JointType LowerJoint, JointType shoulderJoint = JointType.LeftShoulder)
            {
                //for limb joints, "axis" is the direction pointing to child, "up" is limb bend normal
                var avatarJoints = _skeletonImportSetting.avatarJoints;
                if(!avatarJoints.ContainsKey(upperJoint) || !avatarJoints.ContainsKey(middleJoint) ||!avatarJoints.ContainsKey(LowerJoint)) 
                {
                    return;
                }

                var upperJointTransform = avatarJoints[upperJoint].JointTransform;
                var middleJointTransform =  avatarJoints[middleJoint].JointTransform;
                var lowerJointTransform =  avatarJoints[LowerJoint].JointTransform;

                Vector3 direction1 = middleJointTransform.position - upperJointTransform.position;
                Vector3 direction2 = lowerJointTransform.position - middleJointTransform.position;

                Vector3 bendNormal = Vector3.Cross(direction1, direction2);
                
                var rootTransform = avatarJoints[JointType.Root].JointTransform;
                Vector3 rootUp = rootTransform.up;

                foreach(JointType jointType in jointList)
                {
                    if(avatarJoints.ContainsKey(jointType))
                    {
                        var currentJointTransform = avatarJoints[jointType].JointTransform;
                        var parentType = GetJointParentType(jointType);
                        if(!avatarJoints.ContainsKey(parentType))
                        {
                            continue;
                        }
                        var parentJoint = avatarJoints[parentType];
                        var parentJointTransform = parentJoint.JointTransform;
                        if(jointList.Contains(parentType) && parentJointTransform != null)
                        {
                            Vector3 worldAxis = currentJointTransform.position - parentJointTransform.position;
                            Quaternion worldOrientationInv = Quaternion.Inverse(parentJointTransform.rotation);
                            parentJoint.axis = Vector3.Normalize(worldOrientationInv * worldAxis);
                            parentJoint.up = Vector3.Normalize(worldOrientationInv * bendNormal);
                            //avatarJoints[parentName] = parentJoint;
                        }
                    }
                }

                //for end point "hand" use elbow->head direction as reference
                if(avatarJoints.ContainsKey(jointList[jointList.Length - 1]))
                {
                    var endJoint = avatarJoints[jointList[jointList.Length - 1]];
                    var endTransform = endJoint.JointTransform;
                    if(endTransform != null)
                    {
                        Quaternion worldOrientationInv = Quaternion.Inverse(endTransform.rotation);
                        endJoint.axis = Vector3.Normalize(worldOrientationInv * direction2);
                        endJoint.up = Vector3.Normalize(worldOrientationInv *  bendNormal);
                        //avatarJoints[jointList[jointList.Length - 1]] = endJoint;
                    }
                }  
            }
            
            private void ResetJointOrientations()
            {
                if(_skeletonImportSetting == null)
                {
                    return;
                }
                //calculate joint axis
                var avatarJoints = _skeletonImportSetting.avatarJoints;
                var jointTypes = new List<JointType>( avatarJoints.Keys);
                foreach(JointType jointType in jointTypes)
                {
                    var avatarJoint = avatarJoints[jointType];
                    if(avatarJoint.JointTransform != null)
                    {
                        avatarJoint.JointTransform.localRotation = avatarJoint.defaultLocalOrientation;
                    }
                }
            }

            private void SwitchMappingTab(ClickEvent evt)
            {
                Button button = evt.currentTarget as Button;
                string name = button.name;

                //switch button
                if(_currentSelectedTabButton != button)
                {
                    if(!HideMappingTab(_currentSelectedTab))
                    {
                        return;
                    }
                    switch(name)
                    {
                        case "Skeleton_MappingButton":
                        _leftVis.SetActive(true);
                        _basicInfoWidget.ShowWidget();
                        _avatarRootWidget.SetActive(true);
                        if(_currentGroup != JointGroup.Invalid)
                        {
                            _jointMappingWidgets[_currentGroup].ShowWidget();
                        }
                        else
                        {
                            _jointMappingWidgets[JointGroup.Body].ShowWidget();
                            _currentGroup = JointGroup.Body;
                        }
                        
                        _currentSelectedTabButton.style.backgroundColor = (Color) new Color32(61, 61, 61, 0);
                        button.style.backgroundColor = (Color) new Color32(61, 61, 61, 255);
                        _currentSelectedTabButton = button;
                        _currentSelectedTab = SkeletonMappingTab.Mapping;
                        break;

                        case "Skeleton_AnimationBoneButton":
                        _animationBoneWidget.RefreshCustomBoneSettingItems();
                        _animationBoneWidget.ShowWidget();
                        _currentSelectedTabButton.style.backgroundColor = (Color) new Color32(61, 61, 61, 0);
                        button.style.backgroundColor = (Color) new Color32(61, 61, 61, 255);
                        _currentSelectedTabButton = button;
                        _currentSelectedTab = SkeletonMappingTab.AnimationBone;
                        break;

                        case "Skeleton_MuscleButton":
                        //TODO: optimize data updating 
                        _muscleWidget.InitPreviewValues();
                        _muscleWidget.ShowWidget();
                        _avatarRootWidget.SetActive(false);

                        _currentSelectedTabButton.style.backgroundColor = (Color) new Color32(61, 61, 61, 0);
                        button.style.backgroundColor = (Color) new Color32(61, 61, 61, 255);
                        _currentSelectedTabButton = button;
                        _currentSelectedTab = SkeletonMappingTab.Muscle;
                        break;
                    }
                }
            }

            private bool HideMappingTab(SkeletonMappingTab tab)
            {
                switch(tab)
                {
                    case SkeletonMappingTab.Mapping:
                        UpdateToData(curImportSettings);
                        //check skeleton before proceed to muscle preview
                        if(!CheckSkeleton(false, true))
                            return false;
                        _leftVis.SetActive(false);
                        _basicInfoWidget.HideWidget();
                        _avatarRootWidget.SetActive(false);
                        if(_currentGroup != JointGroup.Invalid)
                        {
                            _jointMappingWidgets[_currentGroup].HideWidget();
                        }
                        break;
                    case SkeletonMappingTab.AnimationBone:
                        _animationBoneWidget.HideWidget();
                        break;
                    case SkeletonMappingTab.Muscle:
                        _muscleWidget.ResetPreviewValues();
                        ResetJointOrientations();
                        _muscleWidget.HideWidget();
                        break;
                    default:
                        break;
                }
                return true;
            }

            private void ShowMappingWidget(ClickEvent evt)
            {
                Button button = evt.currentTarget as Button;
                string name = button.name;
                if(_currentSelectedBodyPart != button)
                {
                    _currentSelectedBodyPart.style.backgroundColor = (Color) new Color32(61, 61, 61, 0);
                    button.style.backgroundColor = (Color) new Color32(61, 61, 61, 255);
                    _currentSelectedBodyPart = button;
                    switch(name)
                    {
                        case "Skeleton_BodyButton":
                        _bodyWidget.ShowWidget();
                        _jointVisGroups[JointGroup.Body].visible = true;
                        if(_currentGroup != JointGroup.Invalid)
                        {
                            _jointMappingWidgets[_currentGroup].HideWidget();
                            _jointVisGroups[_currentGroup].visible = false;
                            _currentGroup = JointGroup.Body;
                        }
                        break;
                        case "Skeleton_HeadButton":
                        _headWidget.ShowWidget();
                        _jointVisGroups[JointGroup.Head].visible = true;
                        if(_currentGroup != JointGroup.Invalid)
                        {
                            _jointMappingWidgets[_currentGroup].HideWidget();
                            _jointVisGroups[_currentGroup].visible = false;
                            _currentGroup = JointGroup.Head;
                        }
                        break;
                        case "Skeleton_LeftHandButton":
                        _leftHandWidget.ShowWidget();
                        _jointVisGroups[JointGroup.LeftHand].visible = true;
                        if(_currentGroup != JointGroup.Invalid)
                        {
                            _jointMappingWidgets[_currentGroup].HideWidget();
                            _jointVisGroups[_currentGroup].visible = false;
                            _currentGroup = JointGroup.LeftHand;
                        }
                        break;
                        case "Skeleton_RightHandButton":
                        _rightHandWidget.ShowWidget();
                        _jointVisGroups[JointGroup.RightHand].visible = true;
                        if(_currentGroup != JointGroup.Invalid)
                        {
                            _jointMappingWidgets[_currentGroup].HideWidget();
                            _jointVisGroups[_currentGroup].visible = false;
                            _currentGroup = JointGroup.RightHand;
                        }
                        break;
                    } 
                }
            }

            private void SkeletonClearButtonFunc(ClickEvent evt)
            {
                CommonDialogWindow.ShowPopupConfirmDialog(() =>
                {
                    ClearMappingData();
                }, null, "Do you want to clear the mapping data?", default, "Clear", "Cancel");
            }
            
            private void GenerateCustomBones()
            {
                //need to fill required bones first
                UpdateToData(curImportSettings);

                //check skeleton before generate custom bones
                if(!CheckSkeleton(false, true))
                {
                    return;
                }

                //generate custom bones for each widget
                _bodyWidget?.GenerateCustomBones();
                _headWidget?.GenerateCustomBones();
                _leftHandWidget?.GenerateCustomBones();
                _rightHandWidget?.GenerateCustomBones();
            }

            private void AutoMappingJoints()
            {
                _bodyWidget?.AutoMappingJoints();
                _headWidget?.AutoMappingJoints();
                _leftHandWidget?.AutoMappingJoints();
                _rightHandWidget?.AutoMappingJoints();
            }

            private void ClearMappingData()
            {
                if(_avatarRootObject != null)
                {
                    _avatarRootObject.value = null;
                }
                _bodyWidget?.ClearMappingData();
                _headWidget?.ClearMappingData();
                _leftHandWidget?.ClearMappingData();
                _rightHandWidget?.ClearMappingData();
                _bodyWidget?.ClearCustomBones();
                _headWidget?.ClearCustomBones();
                _leftHandWidget?.ClearCustomBones();
                _rightHandWidget?.ClearCustomBones();
            }
        }
    }
}
#endif