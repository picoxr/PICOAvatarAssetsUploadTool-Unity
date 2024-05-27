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
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using TMPro;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        internal class SkeletonMappingSettingWidget : AssetImportSettingWidget
        {
            // asset import type.
            public virtual AssetImportSettingType settingType { get => AssetImportSettingType.Skeleton; }

            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetPreview/Editors/Views/"
            public override string uxmlPathName { get => "UxmlWidget/SkeletonMappingSettingBodyWidget.uxml";}

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

            public Dictionary<JointType, ObjectField> jointObjects{
                get
                {
                    return _jointObjects;
                }
            }

            public List<ObjectField> customJointObjects{
                get
                {
                    return _customJointObjects;
                }
            }

            public SkeletonMappingSettingWidget otherSideMappingWidget = null;

            public virtual JointType rootJointType { get => JointType.Root; }

            public virtual JointType[] leafJointTypes { get => new JointType[]{}; }

            public virtual SkeletonPanel.JointGroup jointGroup {get => SkeletonPanel.JointGroup.Invalid;}


#region Public Methods
            
            /**
             * @brief Build ui element of the sub view.
             * @return root uxml element of the sub view.
             */
            public override VisualElement BuildUIDOM()
            {
                var root = base.BuildUIDOM();
                _mappingJointItemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetBuilderConfig.instance.uiDataAssetsPath + "UxmlWidget/SkeletonMappingSettingItemWidget.uxml");
                _customJointItemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetBuilderConfig.instance.uiDataAssetsPath + "UxmlWidget/SkeletonMappingSettingCustomItemWidget.uxml");
                _customJointListItemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetBuilderConfig.instance.uiDataAssetsPath + "UxmlWidget/SkeletonMappingSettingCustomBoneListWidget.uxml");
                return root;
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
                
                var jointList = GetJointTypesList();
                foreach(JointType jointType in jointList)
                {
                    if(!_jointObjects.ContainsKey(jointType))
                    {
                        continue;
                    }
                    var jointObject = _jointObjects[jointType];
                    //var jointObject = mainElement.Q<ObjectField>(jointTypeName);
                    if (jointObject != null)
                    {
                        //_jointObjects[jointTypeName] = jointObject;
                        jointObject.RegisterValueChangedCallback((eve) =>
                        {
                            SkeletonPanel.instance.ResetWarningCheck();
                            //AutoMappingChildren(jointTypeName, (Transform)eve.newValue);
                            AutoMappingOtherSide(jointType.ToString(), (Transform)eve.newValue);
                            OnJointValueChanged(jointType, (Transform)eve.newValue, jointObject);
                        });
                        jointObject.RegisterCallback<ClickEvent>((eve) =>
                        {
                            SkeletonPanel.instance.OnJointSelected(jointType);
                        });

                        var jointLabel = jointObject.parent.parent.parent.Q<Label>("Label_" + jointType.ToString());
                        if(jointLabel != null)
                        {
                            jointLabel.RegisterCallback<ClickEvent>((eve) =>
                            {
                                SkeletonPanel.instance.OnJointSelected(jointType);
                            });
                        }
                    }
                }
                var customBoneButton = this.mainElement.Q<Button>("SkeletonConfig_Auto_Add_CustomBone");
                if(customBoneButton != null)
                {
                    customBoneButton?.RegisterCallback<ClickEvent>(GenerateAllCustomBoneButtonFunc);
                    UIUtils.AddVisualElementHoverMask(customBoneButton); 
                }

                var addCustomBoneButton = this.mainElement.Q<Button>("SkeletonConfig_Add_New_CustomBone");
                addCustomBoneButton?.RegisterCallback<ClickEvent>(AddCustomBoneButtonFunc);
                UIUtils.AddVisualElementHoverMask(addCustomBoneButton); 
                
                _foldoutExtra?.RegisterValueChangedCallback((eve) =>
                {
                    addCustomBoneButton.visible = eve.newValue;
                });
                
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
                    var avatarCustomJoints = skeletonSetting.avatarCustomJoints;
                    //var jointTypeNames = avatarJoints.Keys;
                    var jointTypes = GetJointTypesList();
                    foreach(JointType jointType in jointTypes)
                    {
                        if(avatarJoints.ContainsKey(jointType) && _jointObjects.ContainsKey(jointType))
                        {
                            var avatarJoint = avatarJoints[jointType];
                            var jointObject = _jointObjects[jointType];
                            var jointName = avatarJoint.jointName;
                           
                            if(_avatarRootTransform != null)
                            {
                                //jointObject.value = FindInChildren(skeletonRoot.transform, jointName);
                                var jointTransform = FindInChildren(_avatarRootTransform, jointName);
                                jointObject.SetValueWithoutNotify(jointTransform);
                                OnJointValueChanged(jointType, jointTransform, _jointObjects[jointType]);
                            }
                        }
                    }

                    ClearCustomBones();
                    if(_avatarRootTransform != null)
                    {
                        foreach(var kvp in avatarCustomJoints)
                        {
                            var customJointName = kvp.Key;
                            var avatarCustomJoint = kvp.Value;
                            if(avatarCustomJoint.jointGroup == jointGroup)
                            {
                                var jointTransform = FindInChildren(_avatarRootTransform, customJointName);
                                if(jointTransform != null)
                                {
                                    AddCustomSettingItem(jointTransform);
                                }
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
                    _mappedJoints.Clear();

                    //built-in 
                    var jointTypes =  _jointObjects.Keys;
                    var avatarJoints = skeletonSetting.avatarJoints;
                    foreach(JointType jointType in jointTypes)
                    {
                        var jointObject = _jointObjects[jointType];
                        Transform jointTransform = (Transform)jointObject.value;
                        if (jointTransform != null)
                        {
                            _mappedJoints.Add(jointTransform);
                            if (!avatarJoints.ContainsKey(jointType))
                            {
                                 avatarJoints.Add(jointType, new PaabSkeletonImportSetting.PaabAvatarJoint());
                            }
                            var avatarJoint = avatarJoints[jointType];
                            avatarJoint.jointType = jointType;

                            //set basic info
                            avatarJoint.jointName = jointTransform.name;
                            avatarJoint.JointTransform = jointTransform;
                        }
                        else
                        {
                            if (avatarJoints.ContainsKey(jointType))
                            {
                                avatarJoints.Remove(jointType);
                            }
                        }
                    }

                    //custom 

                    var avatarCustomJoints = skeletonSetting.avatarCustomJoints;
                    
                    //add new custom joints
                    foreach(var customJointObject in _customJointObjects)
                    {
                        Transform jointTransform = (Transform)customJointObject.value;
                        if(jointTransform != null)
                        {
                            string jointName = jointTransform.name;
                            if(!avatarCustomJoints.ContainsKey(jointName))
                            {
                                avatarCustomJoints.Add(jointName, new PaabSkeletonImportSetting.PaabAvatarCustomJoint());
                            }
                            var avatarCustomJoint = avatarCustomJoints[jointName];
                            //set basic info
                            avatarCustomJoint.jointName = jointName;
                            avatarCustomJoint.jointTransform = jointTransform;
                            avatarCustomJoint.jointGroup = jointGroup;
                            
                        }
                    }
                }
            }

            public virtual void AutoMappingChildren(JointType jointType, Transform newValue)
            {
                if(newValue == null)
                {
                    return;
                }
                var jointChildrenMap = SkeletonPanel.jointChildrenMap;
                if(jointChildrenMap.ContainsKey(jointType))
                {
                    var childrens = jointChildrenMap[jointType];
                    for(int i = 0; i < childrens.Count;++i)
                    {
                        var childType = childrens[i];
                        var childObject = mainElement.Q<ObjectField>(childType.ToString());
                        if(childObject != null)
                        {
                            if(childObject.value == null)
                            {
                                if(newValue.childCount > i)
                                {
                                    childObject.value = newValue.GetChild(i);
                                }
                            }
                        }
                    }
                }
            }

            public virtual void AutoMappingOtherSide(string jointTypeName, Transform newValue)
            {
                if(newValue == null || otherSideMappingWidget == null)
                {
                    return;
                }
                string jointTypeNameOtherSide = GetJointTypeNameOtherSide(jointTypeName);
                JointType jointTypeOtherSide;
                Enum.TryParse<JointType>(jointTypeNameOtherSide, out jointTypeOtherSide);
 
                var jointObjectsOtherside = otherSideMappingWidget.jointObjects;
                if(jointObjectsOtherside.ContainsKey(jointTypeOtherSide))
                {
                    var jointObject = jointObjectsOtherside[jointTypeOtherSide];
                    if( jointObject != null && jointObject.value == null)
                    {
                        string side = "";
                        if(jointTypeName.Contains("Left"))
                        {
                            side = "Left";
                        }
                        if(jointTypeName.Contains("Right"))
                        {
                            side = "Right";
                        }
                        List<string> jointNamesOtherSide = GetjointNamesOtherSideStrict(newValue.name, side);
                        if(jointNamesOtherSide.Count > 0)
                        {
                            foreach(string jointNameOtherSide in jointNamesOtherSide)
                            {
                                var jointTransformOtherSide = FindInChildren(_avatarRootTransform, jointNameOtherSide);
                                if(jointTransformOtherSide != null)
                                {
                                    jointObject.value = jointTransformOtherSide;
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            public virtual void AutoMappingJoints()
            {
                if (_avatarRootTransform != null)
                {
                    var jointTypes =  _jointObjects.Keys;
                    foreach(JointType jointType in jointTypes)
                    {
                        //auto mapping by exact name match
                        if(_jointObjects[jointType].value != null)
                        {
                            continue;
                        }

                        List<string> names = new List<string>();
                        names.Add(jointType.ToString());
                        if (_defaultNameMap.ContainsKey(jointType))
                        {
                            string defaultJointName = _defaultNameMap[jointType];
                            names.Add(defaultJointName);
                        }
            
                        var jointTransform = FindInChildrenMatchOne(_avatarRootTransform, names);

                        //TODO: auto mapping by naming rule and skeleton hierarchy
                        if (jointTransform != null)
                        {
                           // _jointObjects[jointTypeName].value = jointTransform;
                            _jointObjects[jointType].SetValueWithoutNotify(jointTransform);
                            OnJointValueChanged(jointType, jointTransform, _jointObjects[jointType]);
                            // SkeletonPanel.instance.GetObjectFieldLabel(_jointObjects[jointTypeName]).style.color = (Color)SkeletonPanel.instance.GetDefaultColor();
                            // SkeletonPanel.instance.OnJointValueChanged(jointTypeName, jointTransform != null, _jointObjects[jointTypeName]);
                        }
                    }
                }
            }

            protected void OnJointValueChanged(JointType jointType, Transform jointTransform, ObjectField jointObjectField)
            {
                SkeletonPanel.instance.GetObjectFieldLabel(jointObjectField).style.color = (Color)SkeletonPanel.GetDefaultColor();
                SkeletonPanel.instance.OnJointValueChanged(jointType, jointTransform != null, jointObjectField);
            }

            protected void AddMappingSettingItem(ref Foldout group, JointType jointType)
            {
                string name = jointType.ToString();
                var item = _mappingJointItemAsset.Instantiate();
                var settingGroup = item.Q<GroupBox>(_settingGroupPrefix);
                settingGroup.name = _settingGroupPrefix + "_" + name;
                var label = item.Q<Label>("JointTypeName");
                label.text = name;
                label.name = "Label_" + name;
                var objectfield = item.Q<ObjectField>();
                objectfield.name = name;
                _jointObjects[jointType] = objectfield;
                _allJointObjects[jointType] = objectfield;
                group.Add(item);
                UIUtils.AddVisualElementHoverMask(objectfield, objectfield, true);
            }

            public Foldout AddCustomSettingFoldout()
            {
                var item = _customJointListItemAsset.Instantiate();
                mainElement.Add(item);
                var foldoutElement = item.Q<Foldout>();
                Toggle toggle = foldoutElement.Q<Toggle>();
                toggle.style.paddingBottom = 6;
                return foldoutElement;
            }
            public void AddCustomSettingItem(Transform jointTransform)
            {
                if(_foldoutExtra == null)
                {
                    _foldoutExtra = AddCustomSettingFoldout();
                }
                if(_foldoutExtra.value == false)
                {
                    _foldoutExtra.value = true;
                }
                _customJointCount++;
                string name = "" + _customJointCount.ToString("D3");
                var item = _customJointItemAsset.Instantiate();

                var settingGroup = item.Q<GroupBox>(_settingGroupPrefix);
                var label = item.Q<Label>("JointTypeName");
                label.text = name;
                var objectfield = item.Q<ObjectField>();
                objectfield.value = jointTransform;

                // bind click event on the minus button
                var deleteBoneBtn = item.Q<Button>("DeleteCustomBoneButton");
                deleteBoneBtn.clicked += () =>
                {
                    DeleteCustomBoneItem(item);
                };

                _customJointObjects.Add(objectfield);
                _customJointItems.Add(item);
                _customJointStatus.Add(SkeletonPanel.JointErrorStatus.None);
                _foldoutExtra.Add(item);
                UIUtils.AddVisualElementHoverMask(objectfield, objectfield, true);
                UIUtils.AddVisualElementHoverMask(deleteBoneBtn, deleteBoneBtn);
                SkeletonPanel.instance.customJointObjectsDirty = true;
            }

            public void CheckCustomBones(HashSet<Transform> duplicateTransforms, Transform skeletonRoot,List<CommonDialogWindow.Message> messages, ref int errorCount, ref int warningCount)
            {
                updateRootAndLeaves();

                for(int i = 0; i < _customJointObjects.Count; ++i)
                {
                    var customJointObject = _customJointObjects[i]; 
                    var gameObjectLabel = GetObjectFieldLabel(customJointObject);
                    _customJointStatus[i] = SkeletonPanel.JointErrorStatus.None;
                    if (gameObjectLabel.text == "(Optional)")
                    {
                        gameObjectLabel.style.color = (Color)SkeletonPanel.GetEmptyOptionalObjectColor();
                    }
                    else
                    {
                        gameObjectLabel.style.color = (Color)SkeletonPanel.GetDefaultColor();
                    }
                    
                    var customJointTransform = customJointObject.value as Transform;
                    if(customJointTransform != null)
                    {
                        if(!customJointTransform.IsChildOf(skeletonRoot))
                        {
                            gameObjectLabel.style.color = (Color)SkeletonPanel.GetErrorColor();
                            _customJointStatus[i] = SkeletonPanel.JointErrorStatus.Error;
                            errorCount += 1;
                            messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error, customJointTransform.name.ToString() + " is not child of " + _rootJointTrans.name));
                        }

                        if(duplicateTransforms.Contains(customJointTransform))
                        {
                            gameObjectLabel.style.color = (Color)SkeletonPanel.GetErrorColor();
                            _customJointStatus[i] = SkeletonPanel.JointErrorStatus.Error;
                            errorCount += 1;
                        }
                    }
                }
            }

            public void UpdateCustomBoneLabels()
            {
                for(int i = 0; i < _customJointObjects.Count; ++i)
                {
                    var jointObject = _customJointObjects[i];
                    if (jointObject == null) continue;
                    var gameObjectLabel = GetObjectFieldLabel(jointObject);
                    if (jointObject.value == null)
                    {
                        gameObjectLabel.text = "(Optional)";
                        gameObjectLabel.style.color = (Color)SkeletonPanel.GetEmptyOptionalObjectColor();
                    }
                    else
                    {
                        gameObjectLabel.style.color = (Color)SkeletonPanel.GetDefaultColor();
                    }
                    if(_customJointStatus[i] == SkeletonPanel.JointErrorStatus.Error)
                    {
                        gameObjectLabel.style.color = (Color)SkeletonPanel.GetErrorColor();
                    }
                }
            }

            private void UpdateCustomBoneLabelTexts(int startIdx)
            {
                for(int i = startIdx; i < _customJointItems.Count; ++i)
                {
                    var label = _customJointItems[i].Q<Label>("JointTypeName");
                    label.text = "" + (i + 1).ToString("D3");
                }
            }

            private void DeleteCustomBoneItem(VisualElement item)
            {
                int itemIndex = _foldoutExtra.IndexOf(item);
                _customJointObjects.RemoveAt(itemIndex);
                _customJointItems.RemoveAt(itemIndex);
                _customJointStatus.RemoveAt(itemIndex);
                item.RemoveFromHierarchy();
                UpdateCustomBoneLabelTexts(itemIndex);
                _customJointCount--;
                SkeletonPanel.instance.customJointObjectsDirty = true;
            }

            virtual public List<JointType> GetJointTypesList()
            {
                return new List<JointType>();
            }

            public Dictionary<string,List<JointType>> GetGroupJointTypeDic()
            {
                return _groupJointTypeDic;
            }

            public void ClearCustomBones()
            {
                //Clear existing custom bones
                _customJointCount = 0;
                _foldoutExtra.Clear();
                _customJointObjects.Clear();
                _customJointItems.Clear();
            }

            public void updateRootAndLeaves()
            {
                _rootJointTrans = null;
                _leafJointsTrans.Clear();
        
                if(_allJointObjects.ContainsKey(rootJointType))
                {
                    _rootJointTrans = _allJointObjects[rootJointType].value as Transform;
                }

                if(_rootJointTrans == null)
                {
                    return;
                }

                foreach(JointType leafJointType in leafJointTypes)
                {
                    if(_allJointObjects.ContainsKey(leafJointType))
                    {
                        Transform leafjointTrans = _allJointObjects[leafJointType].value as Transform;
                        if(_leafJointsTrans != null)
                        {
                            _leafJointsTrans.Add(leafjointTrans);
                        }
                    }
                }
            }

            public void GenerateCustomBones()
            {
                //need to fill required bones first, check skeleton before generate custom bones
                if(SkeletonPanel.instance.UpdateToDataAndCheckSkeleton())
                {
                    //Traverse all the bones from root to leaves,
                    //add all the bones that do not have a bone mapping to the custom bone list
                    ClearCustomBones();
                    updateRootAndLeaves();
                    
                    FindAndAddCustomBoneObject(_rootJointTrans, _rootJointTrans, _leafJointsTrans);
                }
            }

            private void FindAndAddCustomBoneObject(Transform jointTrans, Transform rootJointTrans,  HashSet<Transform> leafJointsTrans)
            {
                if(jointTrans == null || leafJointsTrans.Contains(jointTrans))
                {
                    return;
                }
                if(!_mappedJoints.Contains(jointTrans) && jointTrans != rootJointTrans)
                {
                    //add to custom bone list
                    AddCustomSettingItem(jointTrans);
                }
                foreach(Transform childTrans in jointTrans)
                {
                    FindAndAddCustomBoneObject(childTrans, rootJointTrans, leafJointsTrans);
                }
            }

            public void SetOtherSideMappingWidget(SkeletonMappingSettingWidget widget)
            {
                otherSideMappingWidget = widget;
            }

            public void ClearMappingData()
            {
                var jointTypes =  _jointObjects.Keys;
                foreach(JointType jointType in jointTypes)
                {
                    var jointObject = _jointObjects[jointType];
                    if (jointObject != null)
                    {
                        jointObject.value = null;
                    }
                }
            }

            private Transform FindInChildren(Transform trans, string name)
            {
                if (trans == null)
                {
                    return null;
                }
                if (trans.name.ToLower() == name.ToLower())
                {
                    return trans;
                }
                foreach (Transform child in trans)
                {
                    var res = FindInChildren(child, name);
                    if (res != null)
                    {
                        return res;
                    }
                }
                return null;
            }

            private Transform FindInChildrenContainAll(Transform trans, List<string> names)
            {
                if (trans == null)
                {
                    return null;
                }

                bool nameMatch = true;
               
                foreach (string name in names)
                {
     
                    if (!trans.name.Contains(name))
                    {
                        nameMatch = false;
                        break;
                    }
                }

                if(nameMatch == true)
                {
                    return trans;
                }

                foreach (Transform child in trans)
                {
                    var res = FindInChildrenContainAll(child, names);
                    if (res != null)
                    {
                        return res;
                    }
                }
                return null;
            }

            private Transform FindInChildrenMatchOne(Transform trans, List<string> names)
            {
                if (trans == null)
                {
                    return null;
                }
               
                foreach (string name in names)
                {
                    if (trans.name.ToLower() == name.ToLower())
                    {
                        return trans;
                    }
                }

                foreach (Transform child in trans)
                {
                    var res = FindInChildrenMatchOne(child, names);
                    if (res != null)
                    {
                        return res;
                    }
                }
                return null;
            }

            private string GetJointTypeNameOtherSide(string jointTypeName)
            {
                string[] sides = {"Left", "Right"};
                for(int i=0; i < sides.Length; ++i)
                {
                    string side = sides[i];
                    if(jointTypeName.Contains(side))
                    {
                        string jointType = jointTypeName.Replace(side,"");
                        string otherSide = sides[(i+1)%2];
                        return otherSide + jointType;
                    }   
                }
                return "";
            }

            private List<string> GetjointNamesOtherSideStrict(string jointName, string side)
            {
                string[] sides = {"Left","Right"};
                List<string> jointNames = new List<string>();
                if(!sides.Contains(side))
                {
                    return jointNames;
                }
                jointName = jointName.ToLower();
                for(int i = 0; i < sides.Length; ++i)
                {
                    if(side == sides[i])
                    {
                        string otherSide = sides[(i+1)%2];
                        var sideKey = _sideKeys[side];
                        var otherSideKey = _sideKeys[otherSide];
                        for(int j = 0; j < sideKey.Length; ++j)
                        {
                            string key = sideKey[j];
                            string otherKey = otherSideKey[j];
                            if(jointName.Contains(key))
                            {
                                string newJointName = jointName.Replace(key, otherKey);
                                jointNames.Add(newJointName);
                                //return newJointName;
                            }
                        }
                        break;
                    }
                }
                return jointNames;
            }

            public bool MultiMatch(string str, string[] includes, string[] excludes)
            {
                foreach (string include in includes)
                {
                    if (str.Contains(include))
                    {
                        if (excludes == null)
                        {
                            return true;
                        }

                        bool violation = false;
                        foreach (string exclude in excludes)
                        {
                            if (str.Contains(exclude))
                            {
                                violation = true;
                                break;
                            }
                        }

                        if (!violation)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public Label GetObjectFieldLabel(ObjectField item)
            {
                return item.ElementAt(0).ElementAt(0).ElementAt(1) as Label;
            }

            private void GenerateAllCustomBoneButtonFunc(ClickEvent evt)
            {
                CommonDialogWindow.ShowPopupConfirmDialog(() =>
                {
                    GenerateCustomBones();
                }, null, _hintText, default, "Add", "Cancel");
            }
            private void AddCustomBoneButtonFunc(ClickEvent evt)
            {
                AddCustomSettingItem(null);
            }
#endregion


#region Private Fields

            public Transform _avatarRootTransform = null;
            private Dictionary<JointType, string> _defaultNameMap = new Dictionary<JointType, string>()
            {
                { JointType.Root, "Root" },
                //{ "RootScale", "GlobalScale" },
                { JointType.Hips, "Hips" },
                { JointType.SpineLower, "Spine1" },
                { JointType.SpineUpper, "Spine2" },
                { JointType.Chest, "Chest" },
                { JointType.Neck, "Neck" },
                { JointType.Head, "Head" },

                //limbs
                { JointType.LeftShoulder, "Shoulder_L" },
                { JointType.LeftArmUpper, "Arm_L" },
               // { LeftArmUpperTwist, "Arm_L_twist" },
                { JointType.LeftArmLower, "ForeArm_L" },
                // { "LeftHandTwist", "ForeArm_L_twist" },
                // { "LeftHandTwist2", "ForeArm1_L_twist" },
                { JointType.LeftHandWrist, "Hand_L" },
                { JointType.LeftLegUpper, "UpLeg_L" },
                { JointType.LeftLegLower, "Leg_L" },
                { JointType.LeftFootAnkle, "Foot_L" },
                { JointType.LeftToe, "Toes_L" },
                { JointType.LeftToeEnd, "ToesEnd_L" },

                { JointType.RightShoulder, "Shoulder_R" },
                { JointType.RightArmUpper, "Arm_R" },
                //{"RightArmUpperTwist", "Arm_R_twist" },
                { JointType.RightArmLower, "ForeArm_R" },
                // { "RightHandTwist", "ForeArm_R_twist" },
                // { "RightHandTwist2", "ForeArm1_R_twist" },
                { JointType.RightHandWrist, "Hand_R" },
                { JointType.RightLegUpper, "UpLeg_R" },
                { JointType.RightLegLower, "Leg_R" },
                { JointType.RightFootAnkle, "Foot_R" },
                { JointType.RightToe, "Toes_R" },
                { JointType.RightToeEnd, "ToesEnd_R" },

                {JointType.LeftHandThumbTrapezium, "ThumbFinger0_L"},
                {JointType.LeftHandThumbMetacarpal, "ThumbFinger1_L"},
                {JointType.LeftHandThumbProximal, "ThumbFinger2_L"},
                {JointType.LeftHandThumbDistal, "ThumbFinger3_L"},
                {JointType.LeftHandThumbTip, "ThumbFinger4_L"},

                {JointType.LeftHandIndexMetacarpal, "IndexFinger0_L"},
                {JointType.LeftHandIndexProximal, "IndexFinger1_L"},
                {JointType.LeftHandIndexIntermediate, "IndexFinger2_L"},
                {JointType.LeftHandIndexDistal, "IndexFinger3_L"},
                {JointType.LeftHandIndexTip, "IndexFinger4_L"},

                {JointType.LeftHandMiddleMetacarpal, "MiddleFinger0_L"},
                {JointType.LeftHandMiddleProximal, "MiddleFinger1_L"},
                {JointType.LeftHandMiddleIntermediate, "MiddleFinger2_L"},
                {JointType.LeftHandMiddleDistal, "MiddleFinger3_L"},
                {JointType.LeftHandMiddleTip, "MiddleFinger4_L"},

                {JointType.LeftHandRingMetacarpal, "RingFinger0_L"},
                {JointType.LeftHandRingProximal, "RingFinger1_L"},
                {JointType.LeftHandRingIntermediate, "RingFinger2_L"},
                {JointType.LeftHandRingDistal, "RingFinger3_L"},
                {JointType.LeftHandRingTip, "RingFinger4_L"},

                {JointType.LeftHandPinkyMetacarpal, "PinkyFinger0_L"},
                {JointType.LeftHandPinkyProximal, "PinkyFinger1_L"},
                {JointType.LeftHandPinkyIntermediate, "PinkyFinger2_L"},
                {JointType.LeftHandPinkyDistal, "PinkyFinger3_L"},
                {JointType.LeftHandPinkyTip, "PinkyFinger4_L"},

                {JointType.RightHandThumbTrapezium, "ThumbFinger0_R"},
                {JointType.RightHandThumbMetacarpal, "ThumbFinger1_R"},
                {JointType.RightHandThumbProximal, "ThumbFinger2_R"},
                {JointType.RightHandThumbDistal, "ThumbFinger3_R"},
                {JointType.RightHandThumbTip, "ThumbFinger4_R"},

                {JointType.RightHandIndexMetacarpal, "IndexFinger0_R"},
                {JointType.RightHandIndexProximal, "IndexFinger1_R"},
                {JointType.RightHandIndexIntermediate, "IndexFinger2_R"},
                {JointType.RightHandIndexDistal, "IndexFinger3_R"},
                {JointType.RightHandIndexTip, "IndexFinger4_R"},

                {JointType.RightHandMiddleMetacarpal, "MiddleFinger0_R"},
                {JointType.RightHandMiddleProximal, "MiddleFinger1_R"},
                {JointType.RightHandMiddleIntermediate, "MiddleFinger2_R"},
                {JointType.RightHandMiddleDistal, "MiddleFinger3_R"},
                {JointType.RightHandMiddleTip, "MiddleFinger4_R"},

                {JointType.RightHandRingMetacarpal, "RingFinger0_R"},
                {JointType.RightHandRingProximal, "RingFinger1_R"},
                {JointType.RightHandRingIntermediate, "RingFinger2_R"},
                {JointType.RightHandRingDistal, "RingFinger3_R"},
                {JointType.RightHandRingTip, "RingFinger4_R"},

                {JointType.RightHandPinkyMetacarpal, "PinkyFinger0_R"},
                {JointType.RightHandPinkyProximal, "PinkyFinger1_R"},
                {JointType.RightHandPinkyIntermediate, "PinkyFinger2_R"},
                {JointType.RightHandPinkyDistal, "PinkyFinger3_R"},
                {JointType.RightHandPinkyTip, "PinkyFinger4_R"},

                 //extra
                {JointType.LeftArmUpperTwist1, "Arm_L_twist01"},
                {JointType.LeftArmUpperTwist2, "Arm_L_twist02"},

                {JointType.LeftArmLowerTwist1, "ForeArm_L_twist01"},
                {JointType.LeftArmLowerTwist2, "ForeArm_L_twist02"},
                {JointType.LeftArmLowerTwist3, "ForeArm_L_twist03"},

                {JointType.LeftLegUpperTwist1, "UpLeg_L_twist01"},
                {JointType.LeftLegUpperTwist2, "UpLeg_L_twist02"},

                {JointType.RightArmUpperTwist1, "Arm_R_twist01"},
                {JointType.RightArmUpperTwist2, "Arm_R_twist02"},

                {JointType.RightArmLowerTwist1, "ForeArm_R_twist01"},
                {JointType.RightArmLowerTwist2, "ForeArm_R_twist02"},
                {JointType.RightArmLowerTwist3, "ForeArm_R_twist03"},

                {JointType.RightLegUpperTwist1, "UpLeg_R_twist01"},
                {JointType.RightLegUpperTwist2, "UpLeg_R_twist02"},
            };

            // ui element
            protected Dictionary<JointType, ObjectField> _jointObjects = new Dictionary<JointType, ObjectField>();

            protected List<ObjectField> _customJointObjects = new List<ObjectField>();

            protected List<SkeletonPanel.JointErrorStatus> _customJointStatus = new List<SkeletonPanel.JointErrorStatus>();

            protected List<VisualElement> _customJointItems = new List<VisualElement>();

            protected string _settingGroupPrefix = "SkeletonConfig";

            protected Foldout _foldoutExtra = null;

            protected VisualTreeAsset _mappingJointItemAsset = null;

            protected VisualTreeAsset _customJointItemAsset = null;

            protected VisualTreeAsset _customJointListItemAsset = null;
            protected Dictionary<string,List<JointType>> _groupJointTypeDic = new Dictionary<string, List<JointType>>();

            private uint _customJointCount = 0;

            private HashSet<Transform> _mappedJoints = new HashSet<Transform>();

            private Transform _rootJointTrans = null;
            private HashSet<Transform> _leafJointsTrans = new HashSet<Transform>();

            private static Dictionary<string, string[]> _sideKeys = new Dictionary<string, string[]>()
            {
                {"Left", new string[] {"left", "lf", "_l", "l_", "-l", "l-", ".l", "l."}},
                {"Right",new string[] {"right", "rt", "_r", "r_", "-r", "r-", ".r", "r."}}
            };

            private static Dictionary<JointType, ObjectField> _allJointObjects = new Dictionary<JointType, ObjectField>();

            private string _hintText = "All successfully identified and unconfigured bones will be automatically added.";
#endregion
        }
    }
}
#endif