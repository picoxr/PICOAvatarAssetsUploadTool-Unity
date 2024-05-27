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

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        internal class SkeletonMappingMuscleSettingWidget : AssetImportSettingWidget
        {
            class JointProperty
            {
                public JointProperty(List<JointType> _jointTypes, int _targetProp, string _showName, float _defaultMin, float _defaultMax)
                {
                    jointTypes = _jointTypes;
                    targetProp = _targetProp;
                    showName = _showName;
                    defaultMin = _defaultMin;
                    defaultMax = _defaultMax;
                }
                public List<JointType> jointTypes = null;
                public int targetProp = 0; //0, 1, 2 "x","y","z" 
                public string showName;

                public float defaultMin = 0;

                public float defaultMax = 0;
            }
            class RotationLimitSetting
            {
                //public MinMaxSlider slider;
                public Slider previewSlider = null;
                public FloatField minValue = null;
                public FloatField maxValue = null;
                public Label previewValue = null;
                public JointProperty jointProperty = null;
                public VisualElement dragger = null;
            }

            class TwistProperty
            {
                 public TwistProperty(List<JointType> _jointTypes, float _defaultValue)
                 {
                    jointTypes = _jointTypes;
                    defaultValue = _defaultValue;
                 }
                 public List<JointType> jointTypes = null;
                 public float defaultValue = 0;
            }

            // asset import type.
            public virtual AssetImportSettingType settingType { get => AssetImportSettingType.Skeleton; }

            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetBuilder/Editors/Views/"
            public override string uxmlPathName { get => "UxmlWidget/SkeletonMuscleSettingWidget.uxml";}

            public Transform avatarRootTransform{
                get 
                {
                    return _avatarRootTransform;
                }
                set
                {
                    _avatarRootTransform = value;
                }
            }

#region Public Methods

            /**
             * @brief Build ui element of the sub view.
             * @return root uxml element of the sub view.
             */
            public override VisualElement BuildUIDOM()
            {
                var root = base.BuildUIDOM();
               
                // rotation limit settings
                var itemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetBuilderConfig.instance.uiDataAssetsPath + "UxmlWidget/SkeletonMuscleSettingItemWidget.uxml");

                var group = mainElement.Q<Foldout>("Foldout_Body");
                if(group != null)
                {
                    foreach(string name in _bodySettingNames)
                    {
                        AddRotationLimitSettingItem(ref group, name, ref itemAsset);
                    }
                    group.value = true;
                }

                group = mainElement.Q<Foldout>("Foldout_Head");
                if(group != null)
                {
                    foreach(string name in _headSettingNames)
                    {
                        AddRotationLimitSettingItem(ref group, name, ref itemAsset);
                    }
                }

                group = mainElement.Q<Foldout>("Foldout_LeftArm");
                if(group != null)
                {
                    foreach(string name in _leftArmSettingNames)
                    {
                        AddRotationLimitSettingItem(ref group, name, ref itemAsset);
                    }
                }

                group = mainElement.Q<Foldout>("Foldout_RightArm");
                if(group != null)
                {
                    foreach(string name in _rightArmSettingNames)
                    {
                        AddRotationLimitSettingItem(ref group, name, ref itemAsset);
                    }
                }

        	    group = mainElement.Q<Foldout>("Foldout_LeftLeg");
                if(group != null)
                {
                    foreach(string name in _leftLegSettingNames)
                    {
                        AddRotationLimitSettingItem(ref group, name, ref itemAsset);
                    }
                }

                group = mainElement.Q<Foldout>("Foldout_RightLeg");
                if(group != null)
                {
                    foreach(string name in _rightLegSettingNames)
                    {
                        AddRotationLimitSettingItem(ref group, name, ref itemAsset);
                    }
                }

                //addditional seting items
                itemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetBuilderConfig.instance.uiDataAssetsPath + "UxmlWidget/SkeletonMuscleAdditionalSettingItemWidget.uxml");

                var additionalSettingElement = mainElement.Q<VisualElement>("SkeletonMuscleConfig_AdditionalSettings");
                // upper arm twist
                group = additionalSettingElement.Q<Foldout>("Foldout_UpperArmTwist");
                if(group != null)
                {
                    foreach(string name in _upperArmTwistNames)
                    {
                        AddTwistSettingItem(ref group, name, ref itemAsset);
                    }
                    group.value = true;
                }

                // lower arm twist
                group = additionalSettingElement.Q<Foldout>("Foldout_LowerArmTwist");
                if(group != null)
                {
                    foreach(string name in _lowerArmTwistNames)
                    {
                        AddTwistSettingItem(ref group, name, ref itemAsset);
                    }
                }

                // upper leg twist
                group = additionalSettingElement.Q<Foldout>("Foldout_UpperLegTwist");
                if(group != null)
                {
                    foreach(string name in _upperLegTwistNames)
                    {
                        AddTwistSettingItem(ref group, name, ref itemAsset);
                    }
                }

                // arm and leg stretch 
                AddAdditionalSettingItem(ref additionalSettingElement, "ArmStretch", ref itemAsset, "Arm Stretch");
                AddAdditionalSettingItem(ref additionalSettingElement, "LegStretch", ref itemAsset, "Leg Stretch");
                AddAdditionalSettingItem(ref additionalSettingElement, "MinHipsHeight", ref itemAsset, "Min Hips Height", 0.25f);

                VisualElement perMuscleSettingsTip = mainElement.Q<VisualElement>("SkeletonMuscleConfig_Per-MuscleSettings-Tip");
                VisualElement additionalSettingsTip = mainElement.Q<VisualElement>("SkeletonMuscleConfig_AdditionalSettings_Tip");
                
                UIUtils.SetTipText(perMuscleSettingsTip, StringTable.GetString("PerMuscleSettingsTip"));
                UIUtils.SetTipText(additionalSettingsTip, StringTable.GetString("AdditionalSettingsTip"));

                return root;
            }

            private void AddRotationLimitSettingItem(ref Foldout group, string name, ref VisualTreeAsset itemAsset)
            {
                var item = itemAsset.Instantiate();
                var settingGroup = item.Q<GroupBox>(_settingGroupPrefix);
                settingGroup.name = _settingGroupPrefix + "_" + name;
                var previewSlider = settingGroup.Q<Slider>(_previewPrefix);
                previewSlider.name = _previewPrefix + "_" + name;
              

                if(_jointTargetPropertyMap.ContainsKey(name))
                {
                    var jointProperty = _jointTargetPropertyMap[name];
                    var label = item.Q<Label>("SettingItemName");
                    label.text = jointProperty.showName;
                    var minValue = settingGroup.Q<FloatField>("MinValue");
                    var maxValue = settingGroup.Q<FloatField>("MaxValue");
                    minValue?.SetValueWithoutNotify(jointProperty.defaultMin);
                    maxValue?.SetValueWithoutNotify(jointProperty.defaultMax);
                }
                group.Add(item);
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

                //test code
                // Debug.Log("test");

                //rotation limitsetting
                var rotationSettingList = _jointTargetPropertyMap.Keys;
                foreach(string name in rotationSettingList)
                {
                    if(!_rotationLimitSettings.ContainsKey(name))
                    {
                        _rotationLimitSettings[name] = new RotationLimitSetting();
                    }

                    var rotationLimitSetting = _rotationLimitSettings[name];
                    rotationLimitSetting.jointProperty = _jointTargetPropertyMap[name];
                    //rotationLimitSetting.jointProperty.jointTypes = new List<JointType>();
                    var jointTypes = _rotationLimitSettings[name].jointProperty.jointTypes;
                    foreach(JointType jointType in jointTypes)
                    {
                        if(!_previewAngles.ContainsKey(jointType))
                        {
                            _previewAngles.Add(jointType, new Vector3(0,0,0));
                        }
                    }
                    var settingGroup = mainElement.Q<GroupBox>(_settingGroupPrefix + "_"  + name);
                    if(settingGroup != null)
                    {
                        //rotationLimitSetting.slider = settingGroup.Q<MinMaxSlider>("LimitRange");
                        rotationLimitSetting.previewSlider = settingGroup.Q<Slider>(_previewPrefix + "_"  + name);
                        rotationLimitSetting.minValue = settingGroup.Q<FloatField>("MinValue");
                        rotationLimitSetting.maxValue = settingGroup.Q<FloatField>("MaxValue");
                        rotationLimitSetting.previewValue = settingGroup.Q<Label>("PreviewValue");
                        rotationLimitSetting.dragger = rotationLimitSetting.previewSlider?.Q<VisualElement>("unity-dragger");

                        //rotationLimitSetting.slider?.RegisterValueChangedCallback(OnSliderValueChanged);
                        rotationLimitSetting.previewSlider?.RegisterValueChangedCallback(OnPreviewValueChanged);
                        rotationLimitSetting.minValue?.RegisterValueChangedCallback(OnMinMaxValueChanged);
                        rotationLimitSetting.maxValue?.RegisterValueChangedCallback(OnMinMaxValueChanged);

                        rotationLimitSetting.dragger.Add(rotationLimitSetting.previewValue.parent);
                        rotationLimitSetting.previewValue.parent.style.position = Position.Absolute;

                        //rotationLimitSetting.dragger.RegisterCallback<GeometryChangedEvent>(OnDraggerPositionChanged);
                    }
                    _rotationLimitSettings[name] = rotationLimitSetting;
                }

                //twist setting
                var twistSettingList = _twistPropertyMap.Keys;
                foreach(string name in twistSettingList)
                {
                    if(_twistSettingItems.ContainsKey(name))
                    {
                        var valueField = _twistSettingItems[name];
                        var group = valueField.parent;
                        if(group != null)
                        {
                            var slider = group.Q<Slider>();
                            valueField.RegisterValueChangedCallback(OnAdditionalValueChanged);
                            slider?.RegisterValueChangedCallback(OnSliderValueChanged); 
                        }
                    }
                }
                
                //strech setting
                _armStretchItem = mainElement.Q<FloatField>("CurrentValue_ArmStretch");
                if(_armStretchItem != null)
                {
                    var group = _armStretchItem.parent;
                    if(group != null)
                    {
                        var slider = group.Q<Slider>();
                        _armStretchItem.RegisterValueChangedCallback(OnAdditionalValueChanged);
                        slider?.RegisterValueChangedCallback(OnSliderValueChanged); 
                    }
                }

                _legStretchItem = mainElement.Q<FloatField>("CurrentValue_LegStretch");
                if(_legStretchItem != null)
                {
                    var group = _legStretchItem.parent;
                    if(group != null)
                    {
                        var slider = group.Q<Slider>();
                        _legStretchItem.RegisterValueChangedCallback(OnAdditionalValueChanged);
                        slider?.RegisterValueChangedCallback(OnSliderValueChanged); 
                    }
                }

                //hips height setting
                _minHipsHeightItem = mainElement.Q<FloatField>("CurrentValue_MinHipsHeight");
                if(_minHipsHeightItem != null)
                {
                    var group =_minHipsHeightItem.parent;
                    if(group != null)
                    {
                        var slider = group.Q<Slider>();
                        _minHipsHeightItem.RegisterValueChangedCallback(OnAdditionalValueChanged);
                        slider?.RegisterValueChangedCallback(OnSliderValueChanged); 
                    }
                }

                ResetPreviewValues();
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
                    var avatarJoints = skeletonSetting.avatarJoints;
                    var jointTypeNames = avatarJoints.Keys;
                    var rotationSettingList = _jointTargetPropertyMap.Keys;
                    foreach(string name in rotationSettingList)
                    {
                        if(_rotationLimitSettings.ContainsKey(name))
                        {
                            var rotationLimitSetting = _rotationLimitSettings[name];
                            if (rotationLimitSetting.jointProperty.jointTypes == null)
                                continue;
                            if(rotationLimitSetting.minValue == null || rotationLimitSetting.maxValue == null)
                                continue;
                            
                            if(rotationLimitSetting.jointProperty.jointTypes.Count > 0)
                            {
                                var jointType = rotationLimitSetting.jointProperty.jointTypes[0];
                                if (avatarJoints.ContainsKey(jointType))
                                {
                                     var avatarJoint = avatarJoints[jointType];
                                     rotationLimitSetting.minValue.SetValueWithoutNotify(avatarJoint.minAngle[rotationLimitSetting.jointProperty.targetProp]);
                                     rotationLimitSetting.maxValue.SetValueWithoutNotify(avatarJoint.maxAngle[rotationLimitSetting.jointProperty.targetProp]);
                                }
                            }
                        }
                    }

                    //twist setting
                    foreach(var entry in _twistPropertyMap)
                    {
                        var name = entry.Key;
                        var twistProperty = entry.Value;
      
                        if(_twistSettingItems.ContainsKey(name))
                        {
                            var valueField = _twistSettingItems[name];
                            var jointTypes = twistProperty.jointTypes;
                            if(jointTypes == null)
                                continue;
                            if(jointTypes.Count > 0)
                            {
                                var jointType = jointTypes[0];
                               
                                if (avatarJoints.ContainsKey(jointType))
                                {
                                    var avatarJoint = avatarJoints[jointType];
                                    valueField.value = avatarJoint.twistWeight;
                                }
                            }
                        }
                    }

                    //stretch setting
                    if(_armStretchItem != null)
                    {
                        _armStretchItem.value = skeletonSetting.armStretch;
                    }

                    if(_legStretchItem != null)
                    {
                        _legStretchItem.value = skeletonSetting.legStretch;
                    }

                    if(_minHipsHeightItem != null)
                    {
                        _minHipsHeightItem.value = skeletonSetting.minHipsHeight;
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
                    var avatarJoints = skeletonSetting.avatarJoints;

                    //rotation limit
                    var rotationSettingList = _jointTargetPropertyMap.Keys;
                    foreach(string name in rotationSettingList)
                    {
                        if(_rotationLimitSettings.ContainsKey(name))
                        {
                            var rotationLimitSetting = _rotationLimitSettings[name];
                            if (rotationLimitSetting.jointProperty.jointTypes == null)
                                continue;
                            if(rotationLimitSetting.minValue == null || rotationLimitSetting.maxValue == null)
                                continue;
                            foreach(JointType jointType in rotationLimitSetting.jointProperty.jointTypes)
                            {    
                                if (avatarJoints.ContainsKey(jointType))
                                {
                                    var avatarJoint = avatarJoints[jointType];
                                    avatarJoint.minAngle[rotationLimitSetting.jointProperty.targetProp] = rotationLimitSetting.minValue.value;
                                    avatarJoint.maxAngle[rotationLimitSetting.jointProperty.targetProp] = rotationLimitSetting.maxValue.value;
                                }
                            }
                        }
                    }

                    //twist setting
                    foreach(var entry in _twistPropertyMap)
                    {
                        var name = entry.Key;
                        var twistProperty = entry.Value;
      
                        if(_twistSettingItems.ContainsKey(name))
                        {
                            var valueField = _twistSettingItems[name];
                            var jointTypes = twistProperty.jointTypes;
                            if(jointTypes == null)
                                continue;
                            foreach(JointType jointType in jointTypes)
                            {
                                if (avatarJoints.ContainsKey(jointType))
                                {
                                    var avatarJoint = avatarJoints[jointType];
                                    avatarJoint.twistWeight = valueField.value;
                                }
                            }
                        }
                    }

                    //stretch setting
                    if(_armStretchItem != null)
                    {
                        skeletonSetting.armStretch = _armStretchItem.value;
                    }

                    if(_legStretchItem != null)
                    {
                        skeletonSetting.legStretch = _legStretchItem.value;
                    }
                    
                    if(_minHipsHeightItem != null)
                    {
                        skeletonSetting.minHipsHeight = _minHipsHeightItem.value;
                    }
                }
            }

            public void ResetPreviewValues()
            {
                foreach (var entry in _rotationLimitSettings)
                {
                    var rotationLimit = entry.Value;
                    rotationLimit.previewSlider?.SetValueWithoutNotify(0.5f); 
                    UpdateSliderTextValue(entry.Key,rotationLimit.previewSlider);
                    //rotationLimit.lastPreviewValue = 0;
                }
                foreach(var kvp in _previewAngles)
                {
                    kvp.Value.Set(0,0,0);
                }
            }

            public void InitPreviewValues()
            {
                foreach (var entry in _rotationLimitSettings)
                {
                    var name = entry.Key;
                    var rotationLimit = entry.Value;
                    if(rotationLimit.previewSlider != null)
                    {
                       UpdateJointRotation(name, rotationLimit.previewSlider.value);
                    }
                }
            }

            public void UpdateValueTextPositions()
            {
                foreach (var entry in _rotationLimitSettings)
                {
                    var name = entry.Key;
                    var rotationLimit = entry.Value;
                    if(rotationLimit.previewSlider != null)
                    {
                       UpdateSliderTextPosition(name, entry.Value.dragger);
                    }
                }
            }

            public void SetSkeletonSetting(PaabSkeletonImportSetting setting)
            {
                _skeletonSetting = setting;
            }
            

            public void CheckTwistSettings(List<CommonDialogWindow.Message> messages, ref int errorCount, ref int warningCount)
            {
                float weightSum = 0;
                foreach(string name in _upperArmTwistNames)
                {
                    if(_twistSettingItems.ContainsKey(name))
                    {
                        var valueField = _twistSettingItems[name];
                        weightSum += valueField.value;
                    }
                }
                if(weightSum > 1)
                {
                    errorCount += 1;
                    messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error, "The sum of upper arm twist weight should be smaller than 1"));
                }
                foreach(string name in _upperArmTwistNames)
                {
                    if(_twistSettingItems.ContainsKey(name))
                    {
                        var valueField = _twistSettingItems[name];
                        valueField.ElementAt(0).style.color = weightSum > 1 ? (Color)GetErrorColor() : (Color)GetDefaultColor();
                    }
                }

                weightSum = 0;
                foreach(string name in _lowerArmTwistNames)
                {
                    if(_twistSettingItems.ContainsKey(name))
                    {
                        var valueField = _twistSettingItems[name];
                        weightSum += valueField.value;
                    }
                    
                }
                if(weightSum > 1)
                {
                    errorCount += 1;
                    messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error, "The sum of lower arm twist weight should be smaller than 1"));
                }
                foreach(string name in _lowerArmTwistNames)
                {
                    if(_twistSettingItems.ContainsKey(name))
                    {
                        var valueField = _twistSettingItems[name];
                        valueField.ElementAt(0).style.color = weightSum > 1 ? (Color)GetErrorColor() : (Color)GetDefaultColor();
                    }
                }
                
                weightSum = 0;
                foreach(string name in _upperLegTwistNames)
                {
                    if(_twistSettingItems.ContainsKey(name))
                    {
                        var valueField = _twistSettingItems[name];
                        weightSum += valueField.value;
                    }
                }
                if(weightSum > 1)
                {
                    errorCount += 1;
                    messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error, "The sum of upper leg twist weight should be smaller than 1"));
                }
                foreach(string name in _upperLegTwistNames)
                {
                    if(_twistSettingItems.ContainsKey(name))
                    {
                        var valueField = _twistSettingItems[name];
                        valueField.ElementAt(0).style.color = weightSum > 1 ? (Color)GetErrorColor() : (Color)GetDefaultColor();
                    }
                }
            }
#endregion

#region Private Fields
            private void OnSliderValueChanged(ChangeEvent<float> evt)
            {
                var slider = evt.currentTarget as Slider;
                var group = slider.parent;
                if(group != null)
                {
                    var currentValue = group.Q<FloatField>();
                    currentValue.SetValueWithoutNotify(evt.newValue);
                    //currentValue.ElementAt(0).style.color = (Color)GetDefaultColor();
                }
            }

            private void OnAdditionalValueChanged(ChangeEvent<float> evt)
            {
                var valueField = evt.currentTarget as FloatField;
                float val = evt.newValue;
                if(val < 0){
                    valueField.SetValueWithoutNotify(0);
                }
                if(val > 1){
                    valueField.SetValueWithoutNotify(1);
                }
                var group = valueField.parent;
                if(group != null)
                {
                    var slider = group.Q<Slider>();
                    slider.SetValueWithoutNotify(valueField.value);
                }
                //valueField.ElementAt(0).style.color = (Color)GetDefaultColor();
            }

            private void AddTwistSettingItem(ref Foldout group, String name, ref VisualTreeAsset itemAsset)
            {
                var item = itemAsset.Instantiate();
                var additionalGroup = item.Q<GroupBox>(_additionalGroupPrefix);

                additionalGroup.name = _additionalGroupPrefix + "_" + name;
                var slider = additionalGroup.Q<Slider>(_additionalValuePrefix);
                slider.name = _additionalValuePrefix + "_" + name;
                var label = item.Q<Label>("SettingItemName");
                label.text = name;

                if(_twistPropertyMap.ContainsKey(name))
                {
                    var twistProperty = _twistPropertyMap[name];
                   
                    var currentValue = additionalGroup.Q<FloatField>("CurrentValue");
                    _twistSettingItems.Add(name, currentValue);
                    currentValue?.SetValueWithoutNotify(twistProperty.defaultValue);
                    slider?.SetValueWithoutNotify(twistProperty.defaultValue);
                }
                group.Add(item);
            }

            private void AddAdditionalSettingItem(ref VisualElement element, String name, ref VisualTreeAsset itemAsset, string showName, float defaultValue = 0.0f)
            {
                var item = itemAsset.Instantiate();
                var additionalGroup = item.Q<GroupBox>(_additionalGroupPrefix);

                additionalGroup.name = _additionalGroupPrefix + "_" + name;
                var slider = additionalGroup.Q<Slider>(_additionalValuePrefix);
                slider.name = _additionalValuePrefix + "_" + name;
                var label = item.Q<Label>("SettingItemName");
                label.text = showName;
                var currentValue = additionalGroup.Q<FloatField>("CurrentValue");
                currentValue.name = "CurrentValue_" + name;
                element.Add(item);
                 currentValue.SetValueWithoutNotify(defaultValue);
                slider.SetValueWithoutNotify(defaultValue);
            }


            private void OnMinMaxValueChanged(ChangeEvent<float> evt)
            {
                var valueField = evt.currentTarget as FloatField; 
                var group = valueField.parent;
                var slider = group.Q<Slider>();
                var minValue = group.Q<FloatField>("MinValue");
                var maxValue = group.Q<FloatField>("MaxValue");
            
                float val = evt.newValue;
                float oldVal = evt.previousValue;

                //limit within valid range
                if(val < -180){
                    valueField.SetValueWithoutNotify(-180);
                }
                if(val > 180){
                    valueField.SetValueWithoutNotify(180);
                }

                //error, use previous value
                if(maxValue.value < minValue.value)
                {
                    valueField.SetValueWithoutNotify(oldVal);
                }

                if(slider == null)
                {
                    return;
                }

                var previewName = slider.name;
                string prefix = _previewPrefix + "_";
                var name = previewName.Substring(prefix.Length);
                UpdateJointRotation(name, slider.value);
                UpdateSliderTextValue(name,slider);
            }

            private void OnPreviewValueChanged(ChangeEvent<float> evt)
            {
                // TODO
                var valueField = evt.currentTarget as Slider;
                var group = valueField.parent.parent;
                var previewName = valueField.name;
                string prefix = _previewPrefix + "_";
                var name = previewName.Substring(prefix.Length);
                var dragger = valueField?.Q<VisualElement>("unity-dragger");
                UpdateJointRotation(name, evt.newValue);
                UpdateSliderTextValue(name,valueField);
                //UpdateSliderTextPosition(name,dragger);
            }

            private void OnDraggerPositionChanged(GeometryChangedEvent evt)
            {
                VisualElement dragger = evt.currentTarget as VisualElement;
                Slider slider = dragger.parent.parent.parent as Slider;
                var previewName = slider.name;
                string prefix = _previewPrefix + "_";
                var name = previewName.Substring(prefix.Length);
                UpdateSliderTextPosition(name,dragger);
            }
            private void UpdateSliderTextValue(string name, Slider slider)
            {
                var minValue = _rotationLimitSettings[name].minValue;
                var maxValue = _rotationLimitSettings[name].maxValue;
                var previewValue = _rotationLimitSettings[name].previewValue;
                
                if(previewValue != null && minValue != null && maxValue != null)
                {
                    float angle = minValue.value * (1 - slider.value) + maxValue.value * slider.value; 
                    previewValue.text = angle.ToString("F0");
                }         
            }

            private void UpdateSliderTextPosition(string name, VisualElement dragger)
            {
                //TODO: 更新
                var previewValueContainer = _rotationLimitSettings[name].previewValue.parent;
                if(dragger != null && previewValueContainer != null)
                {
                    // Vector2 draggerPosition = dragger.localBound.position;
                    // previewValueContainer.style.left = draggerPosition.x - previewValueContainer.resolvedStyle.width/2.0f + dragger.resolvedStyle.width/2.0f;

                    Vector3 previewValueContainerPos = previewValueContainer.transform.position;
                    float positionX = dragger.transform.position.x - previewValueContainer.resolvedStyle.width/2.0f + dragger.resolvedStyle.width/2.0f;
                    previewValueContainerPos.x  = positionX;
                    previewValueContainer.transform.position = previewValueContainerPos;
                }
            }

            private void UpdateJointRotation(string name, float previewSliderValue)
            {
                if(_skeletonSetting == null)
                {
                    return;
                }
                var avatarJoints = _skeletonSetting.avatarJoints;
                if(_rotationLimitSettings.ContainsKey(name))
                {
                    var minValue = _rotationLimitSettings[name].minValue;
                    var maxValue = _rotationLimitSettings[name].maxValue;
                    var previewValue = _rotationLimitSettings[name].previewValue;

                    var jointTypes = _rotationLimitSettings[name].jointProperty.jointTypes;
                    foreach(JointType jointType in jointTypes)
                    {
                        //find target transform selected from mapping widget
                        if (avatarJoints.ContainsKey(jointType))
                        {
                            var avatarJoint = avatarJoints[jointType];
                            var targetTransform = avatarJoint.JointTransform;
                            var previewAngle = _previewAngles[jointType];
                            
                            int jointProp = _rotationLimitSettings[name].jointProperty.targetProp;

                            //var jointAxis = new Vector3();
                            //bool isSpine = false;
                            //case spine 
                            // if(_bodySettingNames.Contains(name) || _headSettingNames.Contains(name))
                            // {
                            //     isSpine = true;
                            // }
                          
                            float angle = minValue.value * (1 - previewSliderValue) + maxValue.value * previewSliderValue; 
                            // if(previewValue != null)
                            // {
                            //     previewValue.text = angle.ToString("F0");
                            // }
                            
                            previewAngle[jointProp] = angle;
                            _previewAngles[jointType] = previewAngle;
                            // if(isSpine)
                            // {
                            //     Vector3 jointForward = Vector3.Cross(avatarJoint.axis, avatarJoint.up);
                            //     // var deltaRot = Quaternion.AngleAxis(previewAngle.x, Vector3.Cross(avatarJoint.axis,avatarJoint.up)) *
                            //     //                Quaternion.AngleAxis(previewAngle.y, avatarJoint.up) *
                            //     //                Quaternion.AngleAxis(previewAngle.z, avatarJoint.axis);

                            //     var deltaRot = Quaternion.AngleAxis(previewAngle.x, avatarJoint.up) *
                            //                    Quaternion.AngleAxis(previewAngle.y, avatarJoint.axis) *
                            //                    Quaternion.AngleAxis(previewAngle.z, Vector3.Cross(avatarJoint.up,avatarJoint.axis));

                            //     //deltaRot.w *= -1;
                            //     //deltaRot.z *= -1;
                            //     targetTransform.localRotation = avatarJoint.defaultLocalOrientation * deltaRot;
                            // }
                            // else
                            {
                                Vector3 jointForward = Vector3.Cross(avatarJoint.axis, avatarJoint.up);
                                var deltaRot = Quaternion.AngleAxis(previewAngle.x, avatarJoint.axis) *
                                               Quaternion.AngleAxis(previewAngle.y, avatarJoint.up) *
                                               Quaternion.AngleAxis(previewAngle.z, jointForward);
                                targetTransform.localRotation = avatarJoint.defaultLocalOrientation * deltaRot;
                            }
                        }
                    }
                }
            }

            private PaabSkeletonImportSetting _skeletonSetting = null;
            public Transform _avatarRootTransform = null;
          
            private Dictionary<string, RotationLimitSetting> _rotationLimitSettings = new Dictionary<string, RotationLimitSetting>();

            private Dictionary<string, FloatField> _twistSettingItems = new Dictionary<string, FloatField>();

            private FloatField _armStretchItem = null;

            private FloatField _legStretchItem = null;

            private FloatField _minHipsHeightItem = null;

            private string _previewPrefix = "Preview";

            private string _settingGroupPrefix = "SkeletonMuscleConfig";

            private string _additionalValuePrefix = "AdditionalValue";

            private string _additionalGroupPrefix = "SkeletonAdditionalConfig";

            // rotation limit setting
            private string[] _bodySettingNames = {"HipsFB", "HipsLR", "HipsTLR", "SpineFB", "SpineLR", "SpineTLR","ChestFB", "ChestLR", "ChestTLR"};
            private string[] _headSettingNames = {"NeckFB", "NeckLR", "NeckTLR","HeadFB", "HeadLR", "HeadTLR"};
            private string[] _leftArmSettingNames = {"LeftShoulderDU", "LeftShoulderFB", "LeftArmUpperDU", "LeftArmUpperFB", "LeftArmUpperTIO","LeftArmUpperTwist1TIO", "LeftArmUpperTwist2TIO","LeftArmLowerFB", "LeftArmLowerTIO","LeftArmLowerTwist1TIO", "LeftArmLowerTwist2TIO","LeftArmLowerTwist3TIO","LeftHandWristDU","LeftHandWristFB","LeftHandWristTIO"};
            private string[] _rightArmSettingNames = {"RightShoulderDU", "RightShoulderFB", "RightArmUpperDU", "RightArmUpperFB", "RightArmUpperTIO","RightArmUpperTwist1TIO", "RightArmUpperTwist2TIO","RightArmLowerFB", "RightArmLowerTIO","RightArmLowerTwist1TIO", "RightArmLowerTwist2TIO","RightArmLowerTwist3TIO","RightHandWristDU","RightHandWristFB", "RightHandWristTIO"};
            private string[] _leftLegSettingNames = {"LeftLegUpperIO", "LeftLegUpperFB", "LeftLegUpperTIO", "LeftLegUpperTwist1TIO", "LeftLegUpperTwist2TIO", "LeftLegLowerIO", "LeftLegLowerFB", "LeftLegLowerTIO", "LeftFootAnkleIO", "LeftFootAnkleUD", "LeftFootAnkleTIO"};
            private string[] _rightLegSettingNames = {"RightLegUpperIO", "RightLegUpperFB", "RightLegUpperTIO", "RightLegUpperTwist1TIO", "RightLegUpperTwist2TIO", "RightLegLowerIO", "RightLegLowerFB", "RightLegLowerTIO", "RightFootAnkleIO", "RightFootAnkleUD", "RightFootAnkleTIO"};
            
            private Dictionary<string, JointProperty> _jointTargetPropertyMap = new Dictionary<string, JointProperty>
            {
                //body
                { "HipsFB", new JointProperty(new List<JointType>{JointType.Hips}, 2, "Hips Back-Front", -45, 45) },
                { "HipsLR", new JointProperty(new List<JointType>{JointType.Hips}, 1, "Hips Right-Left", -45, 45) },   
                { "HipsTLR", new JointProperty(new List<JointType>{JointType.Hips}, 0, "Hips Twist In-Out", -45, 45)}, 
                { "SpineFB", new JointProperty(new List<JointType>{JointType.SpineLower, JointType.SpineMiddle, JointType.SpineUpper}, 2, "Spine Back-Front", -45, 45) },
                { "SpineLR", new JointProperty(new List<JointType>{JointType.SpineLower, JointType.SpineMiddle, JointType.SpineUpper}, 1, "Spine Right-Left", -30, 30) },   
                { "SpineTLR", new JointProperty(new List<JointType>{JointType.SpineLower, JointType.SpineMiddle, JointType.SpineUpper}, 0, "Spine Twist In-Out", -30, 30)}, 
                { "ChestFB", new JointProperty(new List<JointType>{JointType.Chest}, 2, "Chest Back-Front", -10, 10) },
                { "ChestLR", new JointProperty(new List<JointType>{JointType.Chest}, 1, "Chest Right-Left", -30,30) },   
                { "ChestTLR", new JointProperty(new List<JointType>{JointType.Chest}, 0, "Chest Twist In-Out",-30,30) }, 

                //head
                { "NeckFB", new JointProperty(new List<JointType>{JointType.Neck}, 2,"Neck Back-Front", -20, 20) },
                { "NeckLR", new JointProperty(new List<JointType>{JointType.Neck}, 1, "Neck Right-Left", -15, 15) },   
                { "NeckTLR", new JointProperty(new List<JointType>{JointType.Neck}, 0, "Neck Twist Left-Right", -35, 35) }, 
                { "HeadFB", new JointProperty(new List<JointType>{JointType.Head}, 2,"Head Back-Front", -30, 30) },
                { "HeadLR", new JointProperty(new List<JointType>{JointType.Head}, 1,"Head Right-Left", -15, 15) },   
                { "HeadTLR", new JointProperty(new List<JointType>{JointType.Head}, 0, "Head Twist In-Out",-35,35) }, 

                //left arm
                { "LeftShoulderDU", new JointProperty(new List<JointType>{JointType.LeftShoulder}, 2, "Shoulder Up-Down", -40, 0) },
                { "LeftShoulderFB", new JointProperty(new List<JointType>{JointType.LeftShoulder}, 1, "Shoulder Back-Front", -15,30) },   

                { "LeftArmUpperDU", new JointProperty(new List<JointType>{JointType.LeftArmUpper}, 2, "ArmUpper Down-Up", -180, 180) },
                { "LeftArmUpperFB", new JointProperty(new List<JointType>{JointType.LeftArmUpper}, 1, "ArmUpper Back-Front", -180,180)},   
                { "LeftArmUpperTIO", new JointProperty(new List<JointType>{JointType.LeftArmUpper}, 0, "ArmUpper Twist In-Out", -180, 180) },   

                { "LeftArmUpperTwist1TIO", new JointProperty(new List<JointType>{JointType.LeftArmUpperTwist1}, 0, "ArmUpperTwist1 Twist In-Out", -180, 180) },   
                { "LeftArmUpperTwist2TIO", new JointProperty(new List<JointType>{JointType.LeftArmUpperTwist2}, 0, "ArmUpperTwist2 Twist In-Out", -180, 180) },   
                
                { "LeftArmLowerFB", new JointProperty(new List<JointType>{JointType.LeftArmLower}, 1, "ArmLower Back-Front", -25, 120) },   
                { "LeftArmLowerTIO", new JointProperty(new List<JointType>{JointType.LeftArmLower}, 0, "ArmLower Twist In-Out", -90, 90) },  

                { "LeftArmLowerTwist1TIO", new JointProperty(new List<JointType>{JointType.LeftArmLowerTwist1}, 0, "ArmLowerTwist1 Twist In-Out", -180, 180) },   
                { "LeftArmLowerTwist2TIO", new JointProperty(new List<JointType>{JointType.LeftArmLowerTwist2}, 0, "ArmLowerTwist2 Twist In-Out", -180, 180) },  
                { "LeftArmLowerTwist3TIO", new JointProperty(new List<JointType>{JointType.LeftArmLowerTwist3}, 0, "ArmLowerTwist3 Twist In-Out", -180, 180) },   

                { "LeftHandWristDU", new JointProperty(new List<JointType>{JointType.LeftHandWrist}, 2, "Wrist Up-Down", -70, 80) },
                { "LeftHandWristFB", new JointProperty(new List<JointType>{JointType.LeftHandWrist}, 1,"Wrist Back-Front", -25, 20) },   
                { "LeftHandWristTIO", new JointProperty(new List<JointType>{JointType.LeftHandWrist}, 0, "Wrist Twist In-Out",-180, 180) },   

                //right arm
                { "RightShoulderDU", new JointProperty(new List<JointType>{JointType.RightShoulder}, 2, "Shoulder Down-Up", 0,40) },
                { "RightShoulderFB", new JointProperty(new List<JointType>{JointType.RightShoulder}, 1, "Shoulder Back-Front", -15,30) },   

                { "RightArmUpperDU", new JointProperty(new List<JointType>{JointType.RightArmUpper}, 2, "ArmUpper Down-Up", -180, 180) },
                { "RightArmUpperFB", new JointProperty(new List<JointType>{JointType.RightArmUpper}, 1, "ArmUpper Back-Front", -180,180)},   
                { "RightArmUpperTIO", new JointProperty(new List<JointType>{JointType.RightArmUpper}, 0, "ArmUpper Twist In-Out", -180, 180) },   

                { "RightArmUpperTwist1TIO", new JointProperty(new List<JointType>{JointType.RightArmUpperTwist1}, 0, "ArmUpperTwist1 Twist In-Out", -180, 180) },   
                { "RightArmUpperTwist2TIO", new JointProperty(new List<JointType>{JointType.RightArmUpperTwist2}, 0, "ArmUpperTwist2 Twist In-Out", -180, 180) },   
                
                { "RightArmLowerFB", new JointProperty(new List<JointType>{JointType.RightArmLower}, 1, "ArmLower Back-Front", -25, 120) },   
                { "RightArmLowerTIO", new JointProperty(new List<JointType>{JointType.RightArmLower}, 0, "ArmLower Twist In-Out", -90, 90) },  

                { "RightArmLowerTwist1TIO", new JointProperty(new List<JointType>{JointType.RightArmLowerTwist1}, 0, "ArmLowerTwist1 Twist In-Out", -180, 180) },   
                { "RightArmLowerTwist2TIO", new JointProperty(new List<JointType>{JointType.RightArmLowerTwist2}, 0, "ArmLowerTwist2 Twist In-Out", -180, 180) },  
                { "RightArmLowerTwist3TIO", new JointProperty(new List<JointType>{JointType.RightArmLowerTwist3}, 0, "ArmLowerTwist3 Twist In-Out", -180, 180) },   

                { "RightHandWristDU", new JointProperty(new List<JointType>{JointType.RightHandWrist}, 2, "Wrist Down-Up", -80, 70) },
                { "RightHandWristFB", new JointProperty(new List<JointType>{JointType.RightHandWrist}, 1,"Wrist Back-Front", -25, 20) },   
                { "RightHandWristTIO", new JointProperty(new List<JointType>{JointType.RightHandWrist}, 0, "Wrist Twist In-Out",-180, 180) },   

                //left leg
                { "LeftLegUpperIO", new JointProperty(new List<JointType>{JointType.LeftLegUpper}, 2, "LegUpper In-Out", -180, 180) },
                { "LeftLegUpperFB", new JointProperty(new List<JointType>{JointType.LeftLegUpper}, 1, "LegUpper Front-Back", -180, 180) },
                { "LeftLegUpperTIO", new  JointProperty(new List<JointType>{JointType.LeftLegUpper}, 0, "LegUpper Twist In-Out", -180, 180) },

                { "LeftLegUpperTwist1TIO", new JointProperty(new List<JointType>{JointType.LeftLegUpperTwist1}, 0, "LegUpperTwist1 Twist In-Out", -180, 180) },   
                { "LeftLegUpperTwist2TIO", new JointProperty(new List<JointType>{JointType.LeftLegUpperTwist2}, 0, "LegUpperTwist2 Twist In-Out", -180, 180) },   

                { "LeftLegLowerIO", new JointProperty(new List<JointType>{JointType.LeftLegLower}, 2, "LegLower In-Out", -20, 20) },
                { "LeftLegLowerFB", new JointProperty(new List<JointType>{JointType.LeftLegLower}, 1, "LegLower Front-Back", -20, 160)},
                { "LeftLegLowerTIO", new  JointProperty(new List<JointType>{JointType.LeftLegLower}, 0, "LegLower Twist In-Out", -20, 20) },

                { "LeftFootAnkleIO", new JointProperty(new List<JointType>{JointType.LeftFootAnkle}, 2, "Ankle In-Out", -20, 20) },
                { "LeftFootAnkleUD", new JointProperty(new List<JointType>{JointType.LeftFootAnkle}, 1, "Ankle Up-Down", -90,90) },
                { "LeftFootAnkleTIO", new  JointProperty(new List<JointType>{JointType.LeftFootAnkle}, 0,"Ankle Twist In-Out", -20, 20) },


                //right leg
                { "RightLegUpperIO", new JointProperty(new List<JointType>{JointType.RightLegUpper}, 2, "LegUpper In-Out", -180, 180) },
                { "RightLegUpperFB", new JointProperty(new List<JointType>{JointType.RightLegUpper}, 1, "LegUpper Front-Back", -180, 180) },
                { "RightLegUpperTIO", new  JointProperty(new List<JointType>{JointType.RightLegUpper}, 0, "LegUpper Twist In-Out", -180, 180) },

                
                { "RightLegUpperTwist1TIO", new JointProperty(new List<JointType>{JointType.RightLegUpperTwist1}, 0, "LegUpperTwist1 Twist In-Out", -180, 180) },   
                { "RightLegUpperTwist2TIO", new JointProperty(new List<JointType>{JointType.RightLegUpperTwist2}, 0, "LegUpperTwist2 Twist In-Out", -180, 180) },   

                { "RightLegLowerIO", new JointProperty(new List<JointType>{JointType.RightLegLower}, 2, "LegLower Out-In", -20, 20) },
                { "RightLegLowerFB", new JointProperty(new List<JointType>{JointType.RightLegLower}, 1, "LegLower Front-Back", -20, 160)},
                { "RightLegLowerTIO", new  JointProperty(new List<JointType>{JointType.RightLegLower}, 0, "LegLower Twist In-Out", -20, 20) },

                { "RightFootAnkleIO", new JointProperty(new List<JointType>{JointType.RightFootAnkle}, 2, "Ankle Out-In", -20, 20) },
                { "RightFootAnkleUD", new JointProperty(new List<JointType>{JointType.RightFootAnkle}, 1, "Ankle Up-Down", -90,90) },
                { "RightFootAnkleTIO", new  JointProperty(new List<JointType>{JointType.RightFootAnkle}, 0,"Ankle Twist In-Out", -20, 20) },
            };

            // additional setting 

            //TODO: need add more twist bones
            private string[] _upperArmTwistNames = {"UpperArmTwist1", "UpperArmTwist2"};
            private string[] _lowerArmTwistNames = {"LowerArm", "LowerArmTwist1", "LowerArmTwist2", "LowerArmTwist3"};
            private string[] _upperLegTwistNames = {"UpperLegTwist1", "UpperLegTwist2"};
            

            private Dictionary<string, TwistProperty> _twistPropertyMap = new Dictionary<string, TwistProperty>
            {
                {"UpperArmTwist1", new TwistProperty(new List<JointType>{JointType.LeftArmUpperTwist1, JointType.RightArmUpperTwist1}, 0.3f)},
                {"UpperArmTwist2", new TwistProperty(new List<JointType>{JointType.LeftArmUpperTwist2, JointType.RightArmUpperTwist2}, 0.3f)},

                {"LowerArm", new TwistProperty(new List<JointType>{JointType.LeftArmLower, JointType.RightArmLower}, 0.0f)},
                {"LowerArmTwist1", new TwistProperty(new List<JointType>{JointType.LeftArmLowerTwist1, JointType.RightArmLowerTwist1}, 0.25f)},
                {"LowerArmTwist2", new TwistProperty(new List<JointType>{JointType.LeftArmLowerTwist2, JointType.RightArmLowerTwist2}, 0.25f)},
                {"LowerArmTwist3", new TwistProperty(new List<JointType>{JointType.LeftArmLowerTwist3, JointType.RightArmLowerTwist3}, 0.25f)},

                {"UpperLegTwist1", new TwistProperty(new List<JointType>{JointType.LeftLegUpperTwist1, JointType.RightLegUpperTwist1}, 0.3f)},
                {"UpperLegTwist2", new TwistProperty(new List<JointType>{JointType.LeftLegUpperTwist2, JointType.RightLegUpperTwist2}, 0.3f)},

            };

            private Dictionary<JointType, Vector3> _previewAngles = new Dictionary<JointType, Vector3>();

#endregion
        }
    }
}
#endif