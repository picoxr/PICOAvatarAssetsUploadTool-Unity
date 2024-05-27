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
using UnityEditor.Experimental.GraphView;
using Pico.Platform;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.InputSystem.XR;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        internal class SkeletonAnimationBoneSettingWidget : AssetImportSettingWidget
        {
            enum TransformProperty
            {
                Scale = 0,
                Rotation = 1,
                Position = 2
            }

            enum AvatarMaskEnabledBits
            {
                Scale = 1 << 0,
                Rotation = 1 << 1,
                Position = 1 << 2,
                All = AvatarMaskEnabledBits.Scale | AvatarMaskEnabledBits.Rotation | AvatarMaskEnabledBits.Position,
            };

            class JointTransformPropertySetting
            {
                public Toggle[] transformPropertyToggles = new Toggle[3];
                public bool[] defaultValue = new bool[]{false, false, false};
                public bool[] readOnly = new bool[]{false, false, false};
            }

            // asset import type.
            public virtual AssetImportSettingType settingType { get => AssetImportSettingType.Skeleton; }

            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetBuilder/Editors/Views/"
            public override string uxmlPathName { get => "UxmlWidget/SkeletonAnimationBoneSettingWidget.uxml";}

#region Public Methods

            /**
             * @brief Build ui element of the sub view.
             * @return root uxml element of the sub view.
             */
            public override VisualElement BuildUIDOM()
            {
                var root = base.BuildUIDOM();

                _jointTransformPropertySettingsBuiltIn.Clear();
                _jointTransformPropertySettingsCustom.Clear();

                // rotation limit settings
                _itemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetBuilderConfig.instance.uiDataAssetsPath + "UxmlWidget/SkeletonAnimationBoneSettingItemWidget.uxml");
                _foldoutAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetBuilderConfig.instance.uiDataAssetsPath + "UxmlWidget/SkeletonAnimationBoneSettingFoldoutWidget.uxml");

                var infoLabel = mainElement.Q<Label>("AnimationBoneConfig_Info_Label");
                infoLabel.text = _infoText;
                var allBonesGroup = mainElement.Q<VisualElement>("AnimationBoneConfig_All_Bones");

                var group = allBonesGroup.Q<VisualElement>("Group_Body");
                BuildSubGroups(SkeletonPanel.JointGroup.Body,group);

                group = allBonesGroup.Q<VisualElement>("Group_Head");
                BuildSubGroups(SkeletonPanel.JointGroup.Head,group);

                group = allBonesGroup.Q<VisualElement>("Group_LeftHand");
                BuildSubGroups(SkeletonPanel.JointGroup.LeftHand,group);

                group = allBonesGroup.Q<VisualElement>("Group_RightHand");
                BuildSubGroups(SkeletonPanel.JointGroup.RightHand,group);

                SetDefaultValues();

                return root;
            }


            public Color32 GetDefaultColor()
            {
                return new Color32(0xD2, 0xD2, 0xD2, 0xFF);
            }

            public Color32 GetEmptyObjectColor()
            {
                return new Color32(136, 136, 136, 255);
            }

            public Color32 GetErrorColor()
            {
                return new Color32(0xFF, 0x57, 0x52, 0xFF);
            }
            public Color32 GetToggleSelectedColor()
            {
                return new Color32(82, 157, 255, 255);
            }
    
            /**
             * @brief Bind ui events. Derived class SHOULD override the method.
             * Invoked from EditorWindowBase.ShowMe after build the ui elements.
             */
            public override bool BindUIActions()
            {
                if(!base.BindUIActions())
                {
                    return false;
                }

                return true;
            }

            /**
             * @brief Set new import settings to ui. Derived class SHOULD override the method.
             */
            public override void BindOrUpdateFromData(PaabAssetImportSettings importConfig)
            {
                if (importConfig != null)
                {
                    var skeletonSetting = importConfig.GetImportSetting<PaabSkeletonImportSetting>(true);
                    if (skeletonSetting != null)
                    {
                        //Built-in bones
                        var avatarJoints = skeletonSetting.avatarJoints;
                        foreach(var kvp in _jointTransformPropertySettingsBuiltIn)
                        {
                            JointType jointType = kvp.Key;
                            JointTransformPropertySetting propertySetting = kvp.Value;
                            if(avatarJoints.ContainsKey(jointType))
                            {
                                var avatarJoint = avatarJoints[jointType];
                                UpdateFromJointTransformProperty(propertySetting, avatarJoint.jointTransformProperty);
                            }
                        }
                        
                        //Custom bones
                        var avatarCustomJoints = skeletonSetting.avatarCustomJoints;
                        foreach(var kvp in _jointTransformPropertySettingsCustom)
                        {
                            string jointName = kvp.Key;
                            JointTransformPropertySetting propertySetting = kvp.Value;
                            if(avatarCustomJoints.ContainsKey(jointName))
                            {
                                var avatarJoint = avatarCustomJoints[jointName];
                                UpdateFromJointTransformProperty(propertySetting, avatarJoint.jointMaskBits);
                            }
                        }
                    }

                }
            }

            /**
             * @brief Set new import settings from panel ui. Derived class SHOULD override the method.
             */
            public override void UpdateToData(PaabAssetImportSettings importConfig)
            {
                var skeletonSetting = importConfig.GetImportSetting<PaabSkeletonImportSetting>(true);
                // TODO:
                if (skeletonSetting != null)
                {
                    //Built-in bones
                    var avatarJoints = skeletonSetting.avatarJoints;
                    foreach(var kvp in _jointTransformPropertySettingsBuiltIn)
                    {
                        JointType jointType = kvp.Key;
                        JointTransformPropertySetting propertySetting = kvp.Value;
                        if(avatarJoints.ContainsKey(jointType))
                        {
                            var avatarJoint = avatarJoints[jointType];
                            UpdateToJointTransformProperty(propertySetting, ref avatarJoint.jointTransformProperty);
                        }
                    }
                    
                    //Custom bones
                    var avatarCustomJoints = skeletonSetting.avatarCustomJoints;
                    foreach(var kvp in _jointTransformPropertySettingsCustom)
                    {
                        string jointName = kvp.Key;
                        JointTransformPropertySetting propertySetting = kvp.Value;
                        if(avatarCustomJoints.ContainsKey(jointName))
                        {
                            var avatarJoint = avatarCustomJoints[jointName];
                            UpdateToJointTransformProperty(propertySetting, ref avatarJoint.jointMaskBits);
                        }
                    }
                }
            }

            public void SetSkeletonSetting(PaabSkeletonImportSetting setting)
            {
                _skeletonSetting = setting;
            }

            public void RefreshCustomBoneSettingItems()
            {
                if(_skeletonSetting == null)
                {
                    return;
                }
                var avatarCustomJoints = _skeletonSetting.avatarCustomJoints;

                //delete bones removed form mapping widgets
                var currentCustomJointNames = new List<string>(_jointTransformPropertySettingsCustom.Keys);
                foreach(string customJointName in currentCustomJointNames)
                {
                    if(!avatarCustomJoints.ContainsKey(customJointName))
                    {
                        DeleteCustomBoneItem(customJointName);
                    }
                }

                //add new bones
                foreach(var kvp in avatarCustomJoints)
                {
                    string newCustomJointName = kvp.Key;
                    var newAvatarCustomJoint = kvp.Value;
                    if(!_jointTransformPropertySettingsCustom.ContainsKey(newCustomJointName))
                    {
                        AddCustomBoneSettingItem(GetCustomFoldout(newAvatarCustomJoint.jointGroup), newCustomJointName);
                    }
                }
            }
            
#endregion

#region Private Fields

            private Foldout GetOrAddFoldoutElement(VisualElement parent, string name, string showName = "", bool isOpen = false)
            {
                var foldoutElement = parent.Q<Foldout>(name);
                if(foldoutElement == null)
                {
                    var item = _foldoutAsset.Instantiate();
                    foldoutElement = item.Q<Foldout>();
                    foldoutElement.name = name;
                    foldoutElement.text = showName;
                    foldoutElement.value = isOpen;

                    var toggle = item.Q<Toggle>();
                    var label = item.Q<VisualElement>("Labels");
                    toggle.Add(label);
                    parent.Add(item);
                }
                return foldoutElement;
            }

            private void BuildSubGroups(SkeletonPanel.JointGroup jointGroup, VisualElement group)
            {
                if(group != null)
                {
                    foreach(var kvp in SkeletonPanel.instance.GetWidgetByJointGroup(jointGroup)?.GetGroupJointTypeDic())
                    {
                        string groupName = kvp.Key;
                        var jointTypes = kvp.Value;
                        Foldout subGroup = GetOrAddFoldoutElement(group, "Anim_Foldout_" + groupName, groupName, false);
                        foreach(JointType type in jointTypes)
                        {
                            AddBuiltInBoneSettingItem(subGroup,type);
                        }
                    }
                    GetOrAddFoldoutElement(group, "Anim_Foldout_Custom", "CustomBones", true);
                    //var jointTypes = SkeletonPanel.instance.GetWidgetByJointGroup(SkeletonPanel.JointGroup.Body)?.GetJointTypesList();
                    //group.value = false;
                }
            }
            private Foldout GetCustomFoldout(SkeletonPanel.JointGroup jointGroup)
            {
                string groupName = "Group_" + jointGroup.ToString();
                VisualElement group = mainElement.Q<VisualElement>(groupName);
                return group.Q<Foldout>("Anim_Foldout_Custom");
            }

            private void AddBuiltInBoneSettingItem(Foldout group, JointType jointType)
            {
                string name = jointType.ToString();
                var item = _itemAsset.Instantiate();
                var settingGroup = item.Q<GroupBox>(_settingGroupPrefix);
                settingGroup.name = _settingGroupPrefix + "_" + name;
                var label = item.Q<Label>("SettingItemName");
                label.text = name;

                if(!_jointTransformPropertySettingsBuiltIn.ContainsKey(jointType))
                {
                    var jointTransformPropertySetting = new JointTransformPropertySetting();
                    jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Position] = item.Q<Toggle>("PositionToggle");
                    jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Rotation] = item.Q<Toggle>("RotationToggle");
                    jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Scale] = item.Q<Toggle>("ScaleToggle");


                    //set default values
                    jointTransformPropertySetting.defaultValue[(uint)TransformProperty.Rotation] = true;
                    if(jointType < JointType.BasicJointCount || jointType >= JointType.LeftArmUpperTwist1)
                    {
                        jointTransformPropertySetting.readOnly[(uint)TransformProperty.Rotation] = true;
                        jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Rotation].RegisterValueChangedCallback(OnReadOnlyToggleValueChanged);
                        jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Rotation].name = "RotationReadOnlyToggle";
                    }

                    if(_defaultPosBoneTypes.Contains(jointType))
                    {
                        jointTransformPropertySetting.defaultValue[(uint)TransformProperty.Position] = true;
                        jointTransformPropertySetting.readOnly[(uint)TransformProperty.Position] = true;
                        jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Position].RegisterValueChangedCallback(OnReadOnlyToggleValueChanged);
                        jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Position].name = "PositionReadOnlyToggle";
                    }
                    if(_defaultScaleBoneTypes.Contains(jointType))
                    {
                        jointTransformPropertySetting.defaultValue[(uint)TransformProperty.Scale] = true;
                        jointTransformPropertySetting.readOnly[(uint)TransformProperty.Scale] = true;
                        jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Scale].RegisterValueChangedCallback(OnReadOnlyToggleValueChanged);
                        jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Scale].name = "ScaleReadOnlyToggle";
                    }

                    _jointTransformPropertySettingsBuiltIn[jointType] = jointTransformPropertySetting;
                }
              
                group.Add(item);
            }

            private void AddCustomBoneSettingItem(Foldout group, string name)
            {
                var item = _itemAsset.Instantiate();
                var settingGroup = item.Q<GroupBox>(_settingGroupPrefix);
                settingGroup.name = _settingGroupPrefix + "_" + name;
                var label = item.Q<Label>("SettingItemName");
                label.text = name;

                 if(!_jointTransformPropertySettingsCustom.ContainsKey(name))
                {
                    var jointTransformPropertySetting = new JointTransformPropertySetting();
                    jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Position] = item.Q<Toggle>("PositionToggle");
                    jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Rotation] = item.Q<Toggle>("RotationToggle");
                    jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Scale] = item.Q<Toggle>("ScaleToggle");
                    
                    jointTransformPropertySetting.defaultValue[(uint)TransformProperty.Rotation] = true;
                    jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Rotation].value = true;
                    _jointTransformPropertySettingsCustom[name] = jointTransformPropertySetting;
                }
              
                group.Add(item);
                _customJointItems.Add(name,item);
            }

            private void DeleteCustomBoneItem(string name)
            {
                if(!_customJointItems.ContainsKey(name))
                {
                    return;
                }
                var item = _customJointItems[name];
                item.RemoveFromHierarchy();
                _customJointItems.Remove(name);
                _jointTransformPropertySettingsCustom.Remove(name);
            }

            private void SetDefaultValues()
            {
                foreach(var item in _jointTransformPropertySettingsBuiltIn)
                {
                    var jointTransformPropertySetting = item.Value;
                    jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Position].value = jointTransformPropertySetting.defaultValue[(uint)TransformProperty.Position];
                    jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Rotation].value = jointTransformPropertySetting.defaultValue[(uint)TransformProperty.Rotation];
                    jointTransformPropertySetting.transformPropertyToggles[(uint)TransformProperty.Scale].value = jointTransformPropertySetting.defaultValue[(uint)TransformProperty.Scale];
                }
            }

            private void OnReadOnlyToggleValueChanged(ChangeEvent<bool> evt)
            {
                var toggle = evt.currentTarget as Toggle;
                toggle.SetValueWithoutNotify(evt.previousValue);
            }

            private void UpdateToJointTransformProperty(JointTransformPropertySetting setting, ref byte jointTransformProperty)
            {
                jointTransformProperty = 0;
                if(setting.transformPropertyToggles[(uint)TransformProperty.Scale].value == true)
                {
                    jointTransformProperty |= (byte)AvatarMaskEnabledBits.Scale;
                }
                if(setting.transformPropertyToggles[(uint)TransformProperty.Rotation].value == true)
                {
                    jointTransformProperty |= (byte)AvatarMaskEnabledBits.Rotation;
                }
                if(setting.transformPropertyToggles[(uint)TransformProperty.Position].value == true)
                {
                    jointTransformProperty |= (byte)AvatarMaskEnabledBits.Position;
                }
            }

            private void UpdateFromJointTransformProperty(JointTransformPropertySetting setting, byte jointTransformProperty)
            {
                if((jointTransformProperty & (byte)AvatarMaskEnabledBits.Scale) != 0)
                {
                    setting.transformPropertyToggles[(uint)TransformProperty.Scale].value = true;
                }

                if((jointTransformProperty & (byte)AvatarMaskEnabledBits.Rotation) != 0)
                {
                    setting.transformPropertyToggles[(uint)TransformProperty.Rotation].value = true;
                }

                if((jointTransformProperty & (byte)AvatarMaskEnabledBits.Position) != 0)
                {
                    setting.transformPropertyToggles[(uint)TransformProperty.Position].value = true;
                }
            }

            private PaabSkeletonImportSetting _skeletonSetting = null;
            private VisualTreeAsset _itemAsset = null;
            private VisualTreeAsset _foldoutAsset = null;
            private string _settingGroupPrefix = "AnimationBoneConfig";
            private Dictionary<JointType, JointTransformPropertySetting> _jointTransformPropertySettingsBuiltIn = new Dictionary<JointType, JointTransformPropertySetting>();
            private Dictionary<string, JointTransformPropertySetting> _jointTransformPropertySettingsCustom = new Dictionary<string, JointTransformPropertySetting>();
            protected Dictionary<string,VisualElement> _customJointItems = new Dictionary<string,VisualElement>();
            private HashSet<JointType> _defaultPosBoneTypes = new HashSet<JointType>{JointType.Root, JointType.Hips, 
                JointType.LeftArmUpper, JointType.LeftArmLower, JointType.LeftHandWrist,
                JointType.RightArmUpper, JointType.RightArmLower, JointType.RightHandWrist,
                JointType.LeftArmUpperTwist1, JointType.LeftArmUpperTwist2, JointType.LeftArmLowerTwist1,JointType.LeftArmLowerTwist2,JointType.LeftArmLowerTwist3,
                JointType.RightArmUpperTwist1, JointType.RightArmUpperTwist2, JointType.RightArmLowerTwist1,JointType.RightArmLowerTwist2,JointType.RightArmLowerTwist3,
            };
            private HashSet<JointType> _defaultScaleBoneTypes = new HashSet<JointType>{JointType.Root, JointType.LeftHandWrist, JointType.RightHandWrist};
            private string _infoText = "If there are too many selected bones, it will lead to an excessively large size of the animation's motion data packet.";
#endregion
        }
    }
}
#endif