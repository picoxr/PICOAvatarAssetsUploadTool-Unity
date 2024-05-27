#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Pico.Avatar;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        internal class SkeletonMappingSettingBodyWidget : SkeletonMappingSettingWidget
        {
            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetBuilder/Editors/Views/"
            public override string uxmlPathName { get => "UxmlWidget/SkeletonMappingSettingBodyWidget.uxml";}
            public override JointType rootJointType { get => JointType.Root; }
            public override JointType[] leafJointTypes { get => new JointType[]{JointType.Neck, JointType.LeftHandWrist, JointType.RightHandWrist}; }
            public override SkeletonPanel.JointGroup jointGroup {get => SkeletonPanel.JointGroup.Body;}
#region Public Methods
            public override void AutoMappingJoints()
            {
                base.AutoMappingJoints(); 
                //TODO
            }

            /**
             * @brief Build ui element of the sub view.
             * @return root uxml element of the sub view.
             */
            public override VisualElement BuildUIDOM()
            {
                var root = base.BuildUIDOM();
               
                // mapping settings
                var group = mainElement.Q<Foldout>("Foldout_Body");
                if(group != null)
                {
                    foreach(JointType type in _bodyJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    group.value = true;
                    _groupJointTypeDic["Body"] = _bodyJointList;
                }

                group = mainElement.Q<Foldout>("Foldout_LeftArm");
                if(group != null)
                {
                    foreach(JointType type in _leftArmJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    _groupJointTypeDic["LeftArm"] = _leftArmJointList;
                }

                group = mainElement.Q<Foldout>("Foldout_RightArm");
                if(group != null)
                {
                    foreach(JointType type in _rightArmJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    _groupJointTypeDic["RightArm"] = _rightArmJointList;
                }

                group = mainElement.Q<Foldout>("Foldout_LeftLeg");
                if(group != null)
                {
                    foreach(JointType type in _leftLegJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    _groupJointTypeDic["LeftLeg"] = _leftLegJointList;
                }

                group = mainElement.Q<Foldout>("Foldout_RightLeg");
                if(group != null)
                {
                    foreach(JointType type in _rightLegJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    _groupJointTypeDic["RightLeg"] = _rightLegJointList;
                }

                _foldoutExtra = AddCustomSettingFoldout();
                // GetOrAddFoldout("Foldout_Extra", "Custom Bones");
                return root;
            }

            
            public override List<JointType> GetJointTypesList()
            {
                return _bodyJointList.Concat(_leftArmJointList).Concat(_rightArmJointList).Concat(_leftLegJointList).Concat(_rightLegJointList).ToList<JointType>();
            }

#endregion


#region Private Fields
            
            private Dictionary<string, string> _defaultNameMap = new Dictionary<string, string>()
            {
                { "Root", "Root" },
               // { "RootScale", "GlobalScale" },
                { "Hips", "Hips" },
                { "SpineLower", "Spine1" },
                { "SpineUpper", "Spine2" },
                { "Chest", "Chest" },

                //limbs
                { "LeftShoulder", "Shoulder_L" },
                { "LeftArmUpper", "Arm_L" },
               // { LeftArmUpperTwist, "Arm_L_twist" },
                { "LeftArmLower", "ForeArm_L" },
                // { "LeftHandTwist", "ForeArm_L_twist" },
                // { "LeftHandTwist2", "ForeArm1_L_twist" },
                { "LeftHandWrist", "Hand_L" },
                { "LeftLegUpper", "UpLeg_L" },
                { "LeftLegLower", "Leg_L" },
                { "LeftFootAnkle", "Foot_L" },
                { "LeftToe", "Toes_L" },
                { "LeftToeEnd", "ToesEnd_L" },

                { "RightShoulder", "Shoulder_R" },
                { "RightArmUpper", "Arm_R" },
                //{ RightArmUpperTwist, "Arm_R_twist" },
                { "RightArmLower", "ForeArm_R" },
                // { "RightHandTwist", "ForeArm_R_twist" },
                // { "RightHandTwist2", "ForeArm1_R_twist" },
                { "RightHandWrist", "Hand_R" },
                { "RightLegUpper", "UpLeg_R" },
                { "RightLegLower", "Leg_R" },
                { "RightFootAnkle", "Foot_R" },
                { "RightToe", "Toes_R" },
                { "RightToeEnd", "ToesEnd_R" },

                //extra
                {"LeftArmUpperTwist1", "Arm_L_twist01"},
                {"LeftArmUpperTwist2", "Arm_L_twist02"},

                {"LeftArmLowerTwist1", "ForeArm_L_twist01"},
                {"LeftArmLowerTwist2", "ForeArm_L_twist02"},
                {"LeftArmLowerTwist3", "ForeArm_L_twist03"},

                {"LeftLegUpperTwist1", "UpLeg_L_twist01"},
                {"LeftLegUpperTwist2", "UpLeg_L_twist02"},
                
                {"RightArmUpperTwist1", "Arm_R_twist01"},
                {"RightArmUpperTwist2", "Arm_R_twist02"},

                {"RightArmLowerTwist1", "ForeArm_R_twist01"},
                {"RightArmLowerTwist2", "ForeArm_R_twist02"},
                {"RightArmLowerTwist3", "ForeArm_R_twist03"},

                {"RightLegUpperTwist1", "UpLeg_R_twist01"},
                {"RightLegUpperTwist2", "UpLeg_R_twist02"},
            };

            private List<JointType> _bodyJointList = new List<JointType>
            {
                JointType.Root, JointType.Hips, JointType.SpineLower, JointType.SpineMiddle, JointType.SpineUpper, JointType.Chest
            };

            private List<JointType> _leftArmJointList = new List<JointType>
            {
                JointType.LeftShoulder, JointType.LeftArmUpper, JointType.LeftArmUpperTwist1, JointType.LeftArmUpperTwist2,
                JointType.LeftArmLower, JointType.LeftArmLowerTwist1, JointType.LeftArmLowerTwist2, JointType.LeftArmLowerTwist3, JointType.LeftHandWrist
            };

            private List<JointType> _rightArmJointList = new List<JointType>
            {
                JointType.RightShoulder, JointType.RightArmUpper, JointType.RightArmUpperTwist1, JointType.RightArmUpperTwist2,
                JointType.RightArmLower, JointType.RightArmLowerTwist1, JointType.RightArmLowerTwist2, JointType.RightArmLowerTwist3, JointType.RightHandWrist
            };

            private List<JointType> _leftLegJointList = new List<JointType>
            {
                JointType.LeftLegUpper, JointType.LeftLegUpperTwist1, JointType.LeftLegUpperTwist2,
                JointType.LeftLegLower, JointType.LeftFootAnkle, JointType.LeftToe
            };

            private List<JointType> _rightLegJointList = new List<JointType>
            {
                JointType.RightLegUpper, JointType.RightLegUpperTwist1, JointType.RightLegUpperTwist2,
                JointType.RightLegLower, JointType.RightFootAnkle, JointType.RightToe
            };
#endregion
        }
    }
}
#endif