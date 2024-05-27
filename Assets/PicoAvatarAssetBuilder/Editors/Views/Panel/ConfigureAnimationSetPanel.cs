#if UNITY_EDITOR
using Pico.Avatar;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Pico.AvatarAssetBuilder.Protocol;
using Unity.Mathematics;
using Label = UnityEngine.UIElements.Label;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class ConfigureAnimationSetPanel : AssetImportSettingsPanel
        {
            protected enum AnimationSetType
            {
                Base,
                Custom
            }

            private enum CheckAnimationMode
            {
                Loose,
                Strict,
            }
            
            [Flags]
            protected enum AnimationRenderItemStatus : byte
            {
                None = 0,
                AnimationClipError = 1 << 0,
                AnimationClipWarn = 1 << 1,
                AnimationNameError = 1 << 2,
            }

            protected class AnimationRenderItem
            {
                public Label animationNameLabel;
                public TextField animationNameTextField;
                private TextFieldWithPlaceHolder animationNameTextFieldPlaceHolder;
                public Label animationClipLabel;
                public Toggle retargetToggle;
                public bool isRequired;
                public AnimationRenderItemStatus status = AnimationRenderItemStatus.None;

                public void BuildTextFieldHolder()
                {
                    if (animationNameTextField != null)
                    {
                        animationNameTextFieldPlaceHolder = null;
                        animationNameTextFieldPlaceHolder = new TextFieldWithPlaceHolder(animationNameTextField, "ClipName");
                    }
                }

                public void UpdateAnimationClipLabel(bool emptyObject)
                {
                    animationClipLabel.style.color =  emptyObject ? (Color)GetEmptyObjectColorInItem() : (Color)GetDefaultColor();
                    if ((status & AnimationRenderItemStatus.AnimationClipWarn) != 0)
                    {
                        animationClipLabel.style.color = (Color)GetWarningColor();
                    }
                    if ((status & AnimationRenderItemStatus.AnimationClipError) != 0)
                    {
                        animationClipLabel.style.color = (Color)GetErrorColor();
                    }
                }

                public Color GetEmptyObjectColorInItem()
                {
                    return isRequired ? (Color)GetEmptyObjectColor() :(Color)GetEmptyOptionalObjectColor(); 
                }
                
                public void UpdateAnimationNameLabelForBase()
                {
                    animationNameLabel.style.color = isRequired ? (Color)GetDefaultColor() : (Color)GetEmptyOptionalObjectColor();
                }

                public void UpdateAnimationNameTextForCustom()
                {
                    var ve = animationNameTextField.Q(_UnityTextInput);
                    ve.style.color = (Color)GetDefaultColor();
                    if ((status & AnimationRenderItemStatus.AnimationNameError) != 0)
                    {
                        if (animationNameTextField.text == "")
                        {
                            animationNameTextFieldPlaceHolder.ShowErrorOnce();
                        }
                        else
                        {
                            ve.style.color = (Color)GetErrorColor();
                        }
                    }
                }

                public void ResetStatus()
                {
                    status = AnimationRenderItemStatus.None;
                }
            }
            
            class WarningPanelManager
            {
                public List<CommonDialogWindow.Message> errorMessages = new List<CommonDialogWindow.Message>();
                public List<CommonDialogWindow.Message> warnMessages = new List<CommonDialogWindow.Message>();
                public int errorCount = 0;
                public int wanringCount = 0;

                public void AddError(string message)
                {
                    errorCount += 1;
                    Debug.LogError(message);
                    errorMessages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error, message));
                }

                public void AddWarning(string message)
                {
                    wanringCount += 1;
                    Debug.LogWarning(message);
                    warnMessages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Warning, message));
                }
            }

            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetBuilder/Editors/Views/"
            public override string uxmlPathName { get => "Uxml/ConfigureAnimationSetPanel.uxml"; }

#region Public Methods

            /**
             * @brief Set new import settings to ui. Derived class SHOULD override the method.
             */
            public override void BindOrUpdateFromData(PaabAssetImportSettings importConfig)
            {
                base.BindOrUpdateFromData(importConfig);

                // reset the check statue when enter the panel
                _isChecking = false;
                _renderItems.ForEach(item => item.ResetStatus());
                _baseInfoWidget.ResetError();
                ResetWarningCheck();

                // for test
                // {
                //     if (importConfig == null)
                //     {
                //         importConfig = ScriptableObject.CreateInstance<PaabAssetImportSettings>();
                //         importConfig.basicInfoSetting = new PaabBasicInfoImportSetting();
                //         importConfig.basicInfoSetting.SetAssetName("BaseAnimation");
                //         importConfig.basicInfoSetting.assetNickName = "Base Animation1";
                //     }
                //
                //     var tmpAnimationSetting = importConfig.GetImportSetting<PaabAnimationImportSetting>(false);
                //     if (tmpAnimationSetting == null)
                //     {
                //         // test PaabAnimationImportSetting
                //         var animationSetting = new PaabAnimationImportSetting();
                //         importConfig.settingItems = new PaabAssetImportSetting[]
                //         {
                //             animationSetting,
                //         };
                //     }
                // }

                _animationSetting = importConfig.GetImportSetting<PaabAnimationImportSetting>(false);
                LoadSkeleton(importConfig.basicInfoSetting.skeletonAssetName);
                
                // set value to base info widget 
                _baseInfoWidget.NameReadOnly = importConfig.opType == OperationType.Update || importConfig.opType == OperationType.UpdateAsset; 
                _baseInfoWidget.SetInputValue(importConfig.basicInfoSetting.assetName, 
                    importConfig.basicInfoSetting.assetNickName, importConfig.basicInfoSetting.assetIconPath);
                _baseInfoWidget.AutoSetName(importConfig.basicInfoSetting.characterName);
                _baseInfoWidget.AutoSetIconIfNotSet();
            }

            public override void UpdateToData(PaabAssetImportSettings importConfig)
            {
                importConfig.basicInfoSetting.SetAssetName(_baseInfoWidget.Name);
                importConfig.basicInfoSetting.assetNickName = _baseInfoWidget.ShowName;
                importConfig.basicInfoSetting.assetIconPath = _baseInfoWidget.Icon;

                // collect the data and set it to importConfig
                CollectAnimationClips();
                _animationSetting.animationClips = _addedClips;

                base.UpdateToData(importConfig);
            }

#endregion

#region Protected/Private Mehtods

            // Start is called before the first frame update
            protected override bool BuildUIDOM(VisualElement parent)
            {
                if (!base.BuildUIDOM(parent))
                {
                    Debug.LogError("AnimationSet Panel Base BuildUIDOM failed");
                    return false;
                }
                
                _baseInfoWidget = new BaseInfoWidget(mainElement.Q("BaseinfoWidget"), 
                    _animationSetType == AnimationSetType.Base ? BaseInfoType.AssetBaseAnimationSet : BaseInfoType.AssetCustomAnimationSet);
                AddWidget(_baseInfoWidget);
                _baseInfoWidget.BindUIActions();
                _baseInfoWidget.ShowWidget();
                
                _nextButton = mainElement.Q<Button>("NextButton");
                UIUtils.AddVisualElementHoverMask(_nextButton, _nextButton);
                return _nextButton != null && _baseInfoWidget != null;
            }
            
            /**
             * @brief Bind ui events. Derived class SHOULD override the method.
             * Invoked from EditorWindowBase.ShowMe after build the ui elements.
             */
            protected override bool BindUIActions()
            {
                if (!base.BindUIActions())
                {
                    return false;
                }

                _nextButton?.RegisterCallback<ClickEvent>(AnimationNextButtonFunc);
                
                return true;
            }

            public override void OnUpdate()
            {
                base.OnUpdate();

                UpdateLabelStyle();
                if (!_isChecking && _convertFinished)
                {
                    if (curImportSettings.opType == OperationType.CreateAsset || 
                        curImportSettings.opType == OperationType.UpdateAsset)
                    {
                        var panel = NavMenuBarRoute.instance.RouteNextByType(panelName, PanelType.AssetTestPanel);
                        ((AssetTestPanel)panel).InitPanel(panelName, curImportSettings);
                    }
                    else
                    {
                        AssetTestPanel.ShowAssetTestPanel(this, curImportSettings);
                    }
                    _convertFinished = false;
                }
            }

            public override void OnNextStep()
            {
                AnimationNextButtonFunc(null);
            }

            public override void OnShow()
            {
                base.OnShow();
            }

            public override void OnHide()
            {
                base.OnHide();
                this.UpdateToData(curImportSettings);   
            }

            private void UpdateLabelStyle()
            {
                foreach (var renderItem in _renderItems)
                {
                    if (_animationSetType == AnimationSetType.Base)
                    {
                        renderItem.UpdateAnimationNameLabelForBase();
                    }

                    //
                    var label = renderItem.animationClipLabel;
                    if (label.text == _NoneAnimationClipText)  // empty clip, should transition to required/optional
                    {
                        label.text = renderItem.isRequired ? _RequiredText : _OptionalText;
                        label.style.color = renderItem.GetEmptyObjectColorInItem();
                    }
                    else if (label.text == _RequiredText || label.text == _OptionalText || label.text == _UseCustomizedClipText)  // empty clip, required/optional, retarget
                    {
                        renderItem.UpdateAnimationClipLabel(true);
                        //
                        if (_animationSetType == AnimationSetType.Custom)
                        {
                            renderItem.UpdateAnimationNameTextForCustom();
                        }
                    }
                    else  // has clip
                    {
                        renderItem.UpdateAnimationClipLabel(false);
                        //
                        if (_animationSetType == AnimationSetType.Custom)
                        {
                            renderItem.UpdateAnimationNameTextForCustom();
                        }
                    }
                }
            }

            private void CollectAnimationClips()
            {
                _addedClips.Clear();
                _toRetargetClips.Clear();
                var containers = contentElement.Query<TemplateContainer>().ToList();
                foreach (var container in containers)
                {
                    Label label = container.Q<Label>(_ClipNameLabelText);
                    TextField textField = container.Q<TextField>(_ClipNameLabelText);

                    if (label == null && textField == null)
                    {
                        continue;
                    }
                    
                    var animationName = (label != null) ? label.text : textField.text;
                    var clip = container.Q<ObjectField>().value as AnimationClip;
                    _addedClips.Add(new KeyValuePair<string, AnimationClip>(animationName, clip));
                    
                    Toggle retargetToggle = container.Q<Toggle>(_RetargetToggleName);
                    if (retargetToggle != null && retargetToggle.value)
                    {
                        _toRetargetClips.Add(animationName);
                    }
                }
            }
            
            void CheckClips(WarningPanelManager manager)
            {
                CheckClipEmpty(manager);

                if (_animationSetType == AnimationSetType.Custom)
                {
                    CheckClipLocalDuplicate(manager);
                    CheckClipReserve(manager);
                }
            }

            private void CheckClipEmpty(WarningPanelManager manager)
            {
                for (int i = 0; i < _addedClips.Count; ++i)
                {
                    if (_toRetargetClips.Contains(_addedClips[i].Key)) continue;
                    
                    if (_addedClips[i].Value == null && _renderItems[i].isRequired)
                    {
                        _renderItems[i].status |= AnimationRenderItemStatus.AnimationClipError;
                        manager.AddError(_addedClips[i].Key + " has empty animation clip, you need use your customized animation or check 'Automatically Generate Animation' toggle.");
                    }
                }
            }

            private void CheckClipLocalDuplicate(WarningPanelManager manager)
            {
                // get animation name to id list
                Dictionary<string, List<int>> animNameToIdList = new Dictionary<string, List<int>>();
                for (int i = 0; i < _addedClips.Count; ++i)
                {
                    var animName = _addedClips[i].Key;
                    animName ??= ""; // null cannot be key of a dictionary
                    if (!animNameToIdList.ContainsKey(animName))
                    {
                        animNameToIdList[animName] = new List<int>();
                    }
                    animNameToIdList[animName].Add(i);
                }

                // check duplicate animation names
                List<string> duplicateNames = new List<string>();
                foreach (var pair in animNameToIdList)
                {
                    var idList = pair.Value;
                    if (idList.Count < 2)
                    {
                        continue;
                    }
                    
                    foreach (var id in idList)
                    {
                        // every item should display like error, but only one error message
                        _renderItems[id].status |= AnimationRenderItemStatus.AnimationNameError;
                        duplicateNames.Add(pair.Key);
                    }
                }

                duplicateNames = duplicateNames.Distinct().ToList();
                foreach (var duplicateName in duplicateNames)
                {
                    manager.AddError(duplicateName + " appears more than once in the current animation set");
                }
            }

            private void CheckClipReserve(WarningPanelManager manager)
            {
                for (int i = 0; i < _addedClips.Count; ++i)
                {
                    var animName = _addedClips[i].Key;
                    
                    // check animation names in base animation set
                    if (_requiredAnimationClips.Contains(animName) || _optionalAnimationClips.Contains(animName))
                    {
                        _renderItems[i].status |= AnimationRenderItemStatus.AnimationNameError;
                        manager.AddError(animName + " conflicts with animation name in base animation set");
                    }
                    
                    // check animation names in reserve animation names
                    if (animName.StartsWith(_reservePrefix) || _reserveAnimationClips.Contains(animName))
                    {
                        _renderItems[i].status |= AnimationRenderItemStatus.AnimationNameError;
                        manager.AddError(animName + " conflicts with animation name in reserve animation names");
                    }
                }
            }
            
            private void AnimationNextButtonFunc(ClickEvent evt)
            {
                if (_isChecking)
                {
                    Debug.Log("Animation Set is checking just now, please wait for a moment");
                    return;
                }

                _isChecking = true;
                this.UpdateToData(curImportSettings);
                _renderItems.ForEach(item => item.ResetStatus());

                // check basic info only when create character or create asset
                if (curImportSettings.opType == OperationType.Create || curImportSettings.opType == OperationType.CreateAsset)
                {
                    _baseInfoWidget.CheckValue(OnCheckFinish);    
                }
                else
                {
                    OnCheckFinish(BaseInfoValueCheckResult.Success);
                }
            }
            
            private void CheckAnimation(WarningPanelManager warningPanelManager, List<string> errorMessages = null)
            {
                if (warningPanelManager == null)
                {
                    Debug.LogError("WarningPanelManager should not be null");
                    return;
                }

                // 1. add existing error messages from base info to message list
                if(errorMessages != null)
                {
                    for (int i = 0; i < errorMessages.Count; ++i)
                    {
                        warningPanelManager.AddError(errorMessages[i]);
                    }
                }

                if (_addedClips.Count != _renderItems.Count)
                {
                    Debug.LogError("clips number conflicts with render items number");
                }

                // 2. check clips in an animation set
                // check empty, duplicate, and conflict
                CheckClips(warningPanelManager);

                // 3. check every clip
                for (int i = 0; i < _addedClips.Count; ++i)
                {
                    var item = _addedClips[i];
                    var animName = item.Key;
                    var clip = item.Value;
                    var renderItem = _renderItems[i];

                    // 3.1 check animation name
                    CheckClipName(animName, warningPanelManager, renderItem);
                    
                    // skip empty animation clip when checking animation clip info
                    if (item.Value == null)
                    {
                        continue;
                    }
                    
                    // 3.2 check clip length, too long or too short is undesirable
                    CheckClipLength(item, warningPanelManager, renderItem);
                    
                    var curveMap = ExtractInfoFromClip(clip); 
                    
                    // 3.3 check clip unit, should be in meter not in centimeter(cm)
                    CheckClipUnit(animName, curveMap, warningPanelManager, renderItem);
                        
                    // 3.4 check whether bones in clip match selected bones
                    if (_skeleton != null)
                    {
                        CheckSkeletonAnimation(animName, curveMap, _skeleton, warningPanelManager, renderItem);
                    }

                    // 3.5 check whether channel names in clips match those supported in official
                    CheckFacialAnimation(animName, curveMap, warningPanelManager, renderItem);
                }
            }

            private void CheckClipUnit(string animName, Dictionary<string, ClipCurve> curveMap, WarningPanelManager manager, AnimationRenderItem renderItem)
            {
                if(!IsClipUnitOk(curveMap))
                {
                    renderItem.status |= AnimationRenderItemStatus.AnimationClipError;
                    manager.AddError(animName + " has wrong skeleton unit, meter is needed.");
                }
            }

            private void CheckClipLength(KeyValuePair<string, AnimationClip> item, WarningPanelManager manager, AnimationRenderItem renderItem)
            {
                var animName = item.Key;
                var clip = item.Value;
                
                if (IsClipTooLong(clip))
                {
                    renderItem.status |= AnimationRenderItemStatus.AnimationClipWarn;
                    manager.AddWarning(animName + " clip is too long, should be no longer than 1min");
                }

                if (IsClipTooShort(clip))
                {
                    renderItem.status |= AnimationRenderItemStatus.AnimationClipWarn;
                    manager.AddWarning(animName + " clip is too short, should at least be more than two frames");
                }
            }

            private bool ContainsInvalidCharacter(string text)
            {
                if (string.IsNullOrEmpty(text))
                    return false;
            
                char[] c = text.ToCharArray();
                const string nameCheckPattern = @"^[a-zA-Z0-9_]*$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(text, nameCheckPattern))
                {
                    return true;
                }

                return false;
            }

            private void CheckClipName(string animName, WarningPanelManager manager, AnimationRenderItem renderItem)
            {
                // aniName is fixed in base animation set
                if (_animationSetType == AnimationSetType.Base)
                {
                    return;
                }
                
                if (string.IsNullOrEmpty(animName))
                {
                    renderItem.status |= AnimationRenderItemStatus.AnimationNameError;
                    manager.AddError("Empty animation name.");
                }
                else
                {
                    if (ContainsInvalidCharacter(animName))
                    {
                        renderItem.status |= AnimationRenderItemStatus.AnimationNameError;
                        manager.AddError(animName + " Contains invalid character.");
                    }
            
                    const int maxNameLength = 100;
                    if (animName.Length > maxNameLength)
                    {
                        renderItem.status |= AnimationRenderItemStatus.AnimationNameError;
                        manager.AddError(animName + " is too long, it should be no more than 100 characters.");
                    }
                }
            }
            
            private void CheckSkeletonAnimation(string animName, Dictionary<string, ClipCurve> curveMap, GameObject skeleton, WarningPanelManager manager, AnimationRenderItem renderItem)
            {
                Transform[] bones = skeleton.GetComponentsInChildren<Transform>();
                var boneNames = bones.Select(transform => transform.name).ToArray();
                
                string clipPathPrefix = "";
                bool prefixCalculated = false;

                List<string> looselyUnmatchedBoneNames = new List<string>();
                List<string> strictlyUnmatchedBoneNames = new List<string>();
                
                foreach (var item in curveMap)
                {
                    var curves = item.Value;
                    if (curves.blendShapeName != null)
                    {
                        continue;
                    }

                    if (!prefixCalculated)
                    {
                        // split the prefix of the animation clip
                        var curveBones = curves.path.Split('/');
                        var childCount = skeleton.transform.childCount;
                        var idx = -1;
                        for (int j = 0; j < childCount; ++j)
                        {
                            var childName = skeleton.transform.GetChild(j).name;
                            if (curveBones.Contains(childName))
                            {
                                idx = Array.IndexOf(curveBones, childName);
                                break;
                            }
                        }

                        for (int j = 0; j < idx; ++j)
                        {
                            clipPathPrefix += curveBones[j] + "/";
                        }

                        prefixCalculated = true;
                    }
                    
                    bool match = false;
                    for (int j = 0; j < bones.Length; ++j)
                    {
                        var bonePath = AnimationUtility.CalculateTransformPath(bones[j], skeleton.transform);
                        bonePath = clipPathPrefix + bonePath;

                        if (curves.path == bonePath)
                        {
                            match = true;
                            break;
                        }
                    }
                    
                    if (!match)
                    {
                        if (_checkMode == CheckAnimationMode.Loose)
                        {
                            var curveBones = curves.path.Split('/');
                            bool matchLastBone = boneNames.Contains(curveBones[^1]);
                            if (matchLastBone)
                            {
                                looselyUnmatchedBoneNames.Add(curves.path);
                            }
                            else
                            {
                                strictlyUnmatchedBoneNames.Add(curves.path);    
                            }
                        }
                        else if(_checkMode == CheckAnimationMode.Strict)
                        {
                            strictlyUnmatchedBoneNames.Add(curves.path);
                        }
                    }
                }

                if (_checkMode == CheckAnimationMode.Loose)
                {
                    if (strictlyUnmatchedBoneNames.Count > 0)
                    {
                        renderItem.status |= AnimationRenderItemStatus.AnimationClipError;
                        manager.AddError(animName + " strictly Mismatched bones: " + string.Join(Environment.NewLine, strictlyUnmatchedBoneNames));
                    }

                    if (looselyUnmatchedBoneNames.Count > 0)
                    {
                        renderItem.status |= AnimationRenderItemStatus.AnimationClipWarn;
                        manager.AddWarning(animName + " loosely Mismatched bones: " + string.Join(Environment.NewLine, looselyUnmatchedBoneNames));
                    }
                }
                else
                {
                    if (strictlyUnmatchedBoneNames.Count > 0)
                    {
                        renderItem.status |= AnimationRenderItemStatus.AnimationClipError;
                        manager.AddError(animName + " strictly Mismatched bones: " + string.Join(Environment.NewLine, strictlyUnmatchedBoneNames));
                    }
                }
            }
            
            private void CheckFacialAnimation(string animName, Dictionary<string, ClipCurve> curveMap, WarningPanelManager manager, AnimationRenderItem renderItem)
            {
                List<string> unmatchedBsNames = new List<string>();
                List<string> matchedBsNames = new List<string>();
                foreach (var item in curveMap)
                {
                    var curves = item.Value;
                    if (curves.blendShapeName == null)
                    {
                        continue;
                    }

                    if(!_blendshapeNames.Contains(curves.blendShapeName))
                    {
                        unmatchedBsNames.Add(curves.blendShapeName);
                    }
                    else
                    {
                        matchedBsNames.Add(curves.blendShapeName);
                    }
                }

                if (unmatchedBsNames.Count > 0)
                {
                    if (matchedBsNames.Count > 0)
                    {
                        renderItem.status |= AnimationRenderItemStatus.AnimationClipWarn;
                        manager.AddWarning(animName + " unsupported blendShape names: " + string.Join(" ", unmatchedBsNames));
                    }
                    else
                    {
                        renderItem.status |= AnimationRenderItemStatus.AnimationClipError;
                        manager.AddError(animName + " unsupported blendShape names: " + string.Join(" ", unmatchedBsNames));
                    }
                }
            }

            class ClipCurve
            {
                public AnimationCurve[] curves;
                public string blendShapeName;
                public string path;
            }
            const int ClipCurveTypeCount = 11;
            const int BlendShapeCurveIndex = 10;

            private Dictionary<string, ClipCurve> ExtractInfoFromClip(AnimationClip clip, bool enablePostProcess = true)
            {
                var curveMap = new Dictionary<string, ClipCurve>();
                var curveBindings = AnimationUtility.GetCurveBindings(clip);
                for (int i = 0; i < curveBindings.Length; ++i)
                {
                    var binding = curveBindings[i];
                    string path = binding.path;
                    string propertyName = binding.propertyName;

                    bool ignoreProperty = false;
                    int curveTypeIndex = -1;
                    string amazPropertyName = "";
                    switch (propertyName)
                    {
                        case "m_LocalPosition.x":
                            curveTypeIndex = 0;
                            amazPropertyName = "m_localMatrix";
                            break;
                        case "m_LocalPosition.y":
                            curveTypeIndex = 1;
                            amazPropertyName = "m_localMatrix";
                            break;
                        case "m_LocalPosition.z":
                            curveTypeIndex = 2;
                            amazPropertyName = "m_localMatrix";
                            break;

                        case "m_LocalRotation.x":
                            curveTypeIndex = 3;
                            amazPropertyName = "m_localMatrix";
                            break;
                        case "m_LocalRotation.y":
                            curveTypeIndex = 4;
                            amazPropertyName = "m_localMatrix";
                            break;
                        case "m_LocalRotation.z":
                            curveTypeIndex = 5;
                            amazPropertyName = "m_localMatrix";
                            break;
                        case "m_LocalRotation.w":
                            curveTypeIndex = 6;
                            amazPropertyName = "m_localMatrix";
                            break;

                        case "m_LocalScale.x":
                            curveTypeIndex = 7;
                            amazPropertyName = "m_localMatrix";
                            break;
                        case "m_LocalScale.y":
                            curveTypeIndex = 8;
                            amazPropertyName = "m_localMatrix";
                            break;
                        case "m_LocalScale.z":
                            curveTypeIndex = 9;
                            amazPropertyName = "m_localMatrix";
                            break;

                        default:
                            if (propertyName.StartsWith("blendShape."))
                            {
                                curveTypeIndex = BlendShapeCurveIndex;
                            }
                            else
                            {
                                ignoreProperty = true;
                            }
                            break;
                    }
                    if (ignoreProperty)
                    {
                        Debug.LogWarning("ignore Property:" + propertyName);
                        continue;
                    }

                    ClipCurve clipCurve;
                    if (!curveMap.TryGetValue(path + amazPropertyName, out clipCurve))
                    {
                        clipCurve = new ClipCurve();
                        clipCurve.curves = new AnimationCurve[ClipCurveTypeCount];
                        clipCurve.path = path;
                        curveMap.Add(path + amazPropertyName, clipCurve);
                    }
                    clipCurve.curves[curveTypeIndex] = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curveTypeIndex == BlendShapeCurveIndex)
                    {
                        int find = propertyName.LastIndexOf('.');
                        if (find >= 0)
                        {
                            clipCurve.blendShapeName = propertyName.Substring(find + 1);
                        }
                        else
                        {
                            clipCurve.blendShapeName = propertyName;
                        }
                    }
                }

                // postprocess: remove same blendShape name in different meshes
                if (enablePostProcess)
                {
                    Dictionary<string, List<string>> bsNameToPathList = new Dictionary<string, List<string>>();
                    foreach (var item in curveMap)
                    {
                        var bsName = item.Value.blendShapeName;
                        if (bsName != null)
                        {
                            if (!bsNameToPathList.ContainsKey(bsName))
                            {
                                bsNameToPathList[bsName] = new List<string>();
                            }

                            bsNameToPathList[bsName].Add(item.Key);
                        }
                    }

                    foreach (var item in bsNameToPathList)
                    {
                        var pathList = item.Value;
                        // start from index 1 to preserve the first bs
                        for (int i = 1; i < pathList.Count; ++i)
                        {
                            Debug.LogWarning("Duplicate blendShapes in different meshes, remove " + pathList[i] + "." + item.Key);
                            curveMap.Remove(pathList[i]);
                        }
                    }
                }

                return curveMap;
            }

            // check whether an animation clip has meter as its unit by sampling the animation clip
            private bool IsClipUnitOk(Dictionary<string, ClipCurve> curveMap)
            {
                foreach (var item in curveMap)
                {
                    var curves = item.Value;
                    bool transformCurves = curves.curves[BlendShapeCurveIndex] == null;
                    
                    if (!transformCurves)
                    {
                        continue;
                    }

                    if (curves.curves[0] == null || curves.curves[0].length == 0 ||
                        (curves.curves[0].length != curves.curves[1].length &&
                        curves.curves[0].length != curves.curves[2].length))
                    {
                        continue;
                    }

                    var x = curves.curves[0].keys.Select(keyframe => keyframe.value).ToArray();
                    var y = curves.curves[1].keys.Select(keyframe => keyframe.value).ToArray();
                    var z = curves.curves[2].keys.Select(keyframe => keyframe.value).ToArray();

                    // if any y coordinate absolute value is large than 5, consider its unit as meter
                    if (math.abs(y.Min()) > 5 || math.abs(y.Max()) > 5)
                    {
                        return false;
                    }
                    
                    Bounds bounds = new Bounds(new Vector3(x[0], y[0], z[0]), Vector3.zero);
                    for (int j = 1; j < x.Length; ++j)
                    {
                        bounds.Encapsulate(new Vector3(x[j], y[j], z[j]));
                        // if any bounding box of a joint is lager than 10, consider its unit as meter 
                        if (bounds.size.magnitude > 10)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            
            private bool IsClipTooLong(AnimationClip clip)
            {
                return clip.length >= _LongestAnimationClipInSeconds;
            }

            private bool IsClipTooShort(AnimationClip clip)
            {
                return (clip.length * clip.frameRate) <= _ShortestAnimationClipInFrames;
            }
            
            private void LoadSkeleton(string skeletonName)
            {
                if (!string.IsNullOrEmpty(skeletonName) && !string.IsNullOrEmpty(CharacterUtil.CharacterFolderPath))
                {
                    _skeletonPath = CharacterUtil.CharacterFolderPath + "/Skeleton/" + skeletonName + "/0.zip";
                    if (System.IO.File.Exists(_skeletonPath))
                    {
                        if (_skeleton)
                        {
                            DestroyImmediate(_skeleton);
                            _skeleton = null;
                        }
                        //
                        _skeleton = AvatarConverter.LoadSkeleton(_skeletonPath);
                        _skeleton.hideFlags = HideFlags.HideAndDontSave;
                    }
                    else
                    {
                        Debug.LogError("skeleton zip file not exist: " + _skeletonPath);
                    }
                }
                else
                {
                    _skeletonPath = AvatarConverter.builtinSkeletonPath;
                    var configText = AnimationListPanel.instance.CharacterInfo.character.config;
                    var config = JsonConvert.DeserializeObject<JObject>(configText);
                    if (config != null && config.TryGetValue("info", out JToken infoToken))
                    {
                        var info = infoToken as JObject;
                        if (info != null && info.TryGetValue("sex", out JToken sexToken))
                        {
                            var sex = sexToken.ToString();
                            switch (sex)
                            {
                                case "male":
                                {
                                    var prefab = Resources.Load<GameObject>(CharacterUtil.Official_1_0_MalePrefabPath);
                                    if (prefab)
                                    {
                                        _skeleton = Instantiate(prefab);
                                        _skeleton.hideFlags = HideFlags.HideAndDontSave;
                                        _skeleton.name = prefab.name;
                                    }
                                    else
                                    {
                                        Debug.LogError("Unable to load Official_1_0_MalePrefab");
                                    }
                                }
                                    break;
                                case "female":
                                {
                                    var prefab = Resources.Load<GameObject>(CharacterUtil.Official_1_0_FemalePrefabPath);

                                    if (prefab)
                                    {
                                        _skeleton = Instantiate(prefab);
                                        _skeleton.hideFlags = HideFlags.HideAndDontSave;
                                        _skeleton.name = prefab.name;
                                    }
                                    else
                                    {
                                        Debug.LogError("Unable to load Official_1_0_FemalePrefab");
                                    }
                                }
                                    break;
                                default:
                                    Debug.LogError("Invalid sex type " + sex);
                                    break;
                            }
                        }
                        else
                        {
                            Debug.LogError("Unable to find sex in config.json/info");
                        }
                    }
                    else
                    {
                        Debug.LogError("Unable to find info in config.json");
                    }
                }
            }
            
            private List<string> GetCustomAnimClipNames(CustomAnimationSetList list)
            {
                List<string> clipNames = new List<string>();
                foreach (var item in list.assets)
                {
                    if (item.asset_info.name == curImportSettings.assetName)
                    {
                        continue;
                    }
                    
                    var config =
                        JsonConvert.DeserializeObject<JObject>(item.asset_info.offline_config);
                    if (config == null || !config.TryGetValue("animations", out JToken aniValue)) continue;
                    var animations = JsonConvert.DeserializeObject<Dictionary<string, object>>(aniValue.ToString());
                    if (animations == null)
                    {
                        continue;
                    }
                    var tmpClipNames = animations.Keys;
                    clipNames.AddRange(tmpClipNames);
                }
                return clipNames;
            }
            
            void OnCheckFinish(BaseInfoValueCheckResult result)
            {
                var errors = BaseInfoWidget.GetBaseInfoErrorMessage(result);
                
                _renderItems.ForEach(item => item.ResetStatus());

                var warningPanelManager = new WarningPanelManager();
                CheckAnimation(warningPanelManager, errors);
                
                // custom animation set panel
                if (_animationSetType == AnimationSetType.Custom)
                {
                    _animationSetting.characterId ??= "";
                    var request = AssetServerManager.instance.GetCustomAnimationSetListByCharacter(_animationSetting.characterId);
                    request.Send(success =>
                        {
                            var response = ProtocolUtil.GetResponse<CustomAnimationSetList>(success);
                            if (response == null || response.data == null)
                            {
                                warningPanelManager.AddError("Failed to get online custom animation set config");
                                OnCheckAnimationFinish(warningPanelManager);
                                return;
                            }
                            //
                            List<string> clipNames = GetCustomAnimClipNames(response.data);
                            var animNames = _addedClips.Select(pair => pair.Key).ToArray();
                            foreach (var animName in animNames)
                            {
                                if (clipNames.Contains(animName))
                                {
                                    warningPanelManager.AddError("Duplicate animation name " + animName);
                                }
                            }
                            OnCheckAnimationFinish(warningPanelManager);
                        },
                        failure =>
                        {
                            warningPanelManager.AddError("GetCustomAnimationSetListByCharacter...failed : " + failure.ToString());
                            OnCheckAnimationFinish(warningPanelManager);
                        }
                    );
                }
                else
                {
                    OnCheckAnimationFinish(warningPanelManager);    
                }
            }

            private void OnCheckAnimationFinish(WarningPanelManager warningPanelManager)
            {
                Debug.Log("OnCheckAnimationFinish started.");
                Debug.Log("error " + warningPanelManager.errorCount.ToString() + " warn " + warningPanelManager.wanringCount.ToString());
                
                if (warningPanelManager.errorCount > 0 || (warningPanelManager.wanringCount > 0 && !_warningChecked))
                {
                    var mergedMessages = new List<CommonDialogWindow.Message>();
                    mergedMessages.AddRange(warningPanelManager.errorMessages);
                    mergedMessages.AddRange(warningPanelManager.warnMessages);
                    CommonDialogWindow.ShowCheckPopupDialog(mergedMessages);
                }
                
                if (warningPanelManager.errorCount > 0)
                {
                    _isChecking = false;
                    return;
                }

                if (warningPanelManager.wanringCount > 0 && !_warningChecked)
                {
                    _isChecking = false;
                    _warningChecked = true;
                    return;
                }

                _warningChecked = false;

                UpdateToData(curImportSettings);
                ConvertAnimationSet();
            }

            private void ConvertAnimationSet()
            {
                if (_skeleton == null)
                {
                    Debug.LogError("Skeleton is null, Unable to check whether the animation matches the skeleton");
                    _isChecking = false;
                    return;
                }

                // Todo. the curImportSettings is not null, but the cachePtr is 0x0.
                // if (curImportSettings == null)
                // {
                //     Debug.LogError("CurImportSetting is null");
                //     _isChecking = false;
                //     return;
                // }
                
                
                // add walking/lHandFist/rHandFist 
                PaabAnimationImportSetting.AddExtraToRetargetClips(_toRetargetClips);
                var newAnimationClips = PaabAnimationImportSetting.PostProcessAnimationClips(_addedClips);
                AnimationClip[] clips = newAnimationClips.Select(pair => pair.Value).ToArray();
                string[] names = newAnimationClips.Select(pair => pair.Key).ToArray();
                
                string assetName = curImportSettings.assetName;
                var tmpZipPath = Application.dataPath + "/../OutGLTF/" + assetName + ".zip";
                
                Debug.Log("Convert animation set started");
                AvatarConverter.ConvertAnimationSet(_skeleton, clips, names,
                    _toRetargetClips.ToArray(), _skeletonPath, tmpZipPath,
                    () =>
                    {
                        if (!string.IsNullOrEmpty(CharacterUtil.CharacterFolderPath) && System.IO.Directory.Exists(CharacterUtil.CharacterFolderPath))
                        {
                            var targetPath = CharacterUtil.CharacterFolderPath + "/" + CharacterUtil.AnimationSetFolderName + "/" + (_animationSetType == AnimationSetType.Base ? "Base" : "Custom") + "/" + assetName + "/0.zip";
                            var targetDir = new System.IO.FileInfo(targetPath).DirectoryName;
                            if (!System.IO.Directory.Exists(targetDir))
                            {
                                System.IO.Directory.CreateDirectory(targetDir);
                            }
                            var sourceDir = new System.IO.FileInfo(tmpZipPath).DirectoryName;
                            System.IO.File.Copy(tmpZipPath, targetPath, true);
                            System.IO.File.Copy(sourceDir + "/config.json", targetDir + "/" + "0.config.json", true);
                        }
                        if (_animationSetType == AnimationSetType.Base)
                        {
                            CharacterManager.instance.SetAssetLoadSource(PaabCharacterImportSetting.AssetState_Local, curImportSettings.assetName, CharacterBaseAssetType.AnimationSet);
                            CharacterManager.instance.characterInfo.base_animation_set.offline_config = curImportSettings.ToJsonText();
                        }
                        curImportSettings.assetStatus = AssetStatus.Ready;
                        Debug.Log("Convert animation set finished");
                        _isChecking = false;
                        _convertFinished = true;
                    }
                );
            }
            
            protected static Label GetObjectFieldLabel(ObjectField item)
            {
                return item.ElementAt(0).ElementAt(0).ElementAt(1) as Label;
            }
            
            private static  Color32 GetDefaultColor()
            {
                return new Color32(0xD2, 0xD2, 0xD2, 0xFF);
            }
            
            private static Color32 GetEmptyOptionalObjectColor()
            {
                return new Color32(84, 84, 84, 255);
            }

            private static Color32 GetEmptyObjectColor()
            {
                return new Color32(136, 136, 136, 255);
            }

            private static Color32 GetErrorColor()
            {
                return new Color32(0xFF, 0x57, 0x52, 0xFF);
            }

            private static Color32 GetWarningColor()
            { 
                return new Color32(0xFF, 0xBA, 0x00, 0xFF);
            }
            
            protected void ResetWarningCheck()
            {
                _warningChecked = false;
            }
            
#endregion

#region Protected Fields

            private static readonly string[] _blendshapeNames = {
                "eyeLookDownLeft", "noseSneerLeft", "eyeLookInLeft", "browInnerUp", "browDownRight", "mouthClose", "mouthLowerDownRight", "jawOpen",
                "mouthUpperUpRight", "mouthShrugUpper", "mouthFunnel", "eyeLookInRight", "eyeLookDownRight", "noseSneerRight", "mouthRollUpper", 
                "jawRight", "browDownLeft", "mouthShrugLower", "mouthRollLower", "mouthSmileLeft", "mouthPressLeft", "mouthSmileRight", "mouthPressRight", 
                "mouthDimpleRight", "mouthLeft", "jawForward", "eyeSquintLeft", "mouthFrownLeft", "eyeBlinkLeft", "cheekSquintLeft", "browOuterUpLeft", "eyeLookUpLeft", 
                "jawLeft", "mouthStretchLeft", "mouthPucker", "eyeLookUpRight", "browOuterUpRight", "cheekSquintRight", "eyeBlinkRight", "mouthUpperUpLeft", "mouthFrownRight", 
                "eyeSquintRight", "mouthStretchRight", "cheekPuff", "eyeLookOutLeft", "eyeLookOutRight", "eyeWideRight", "eyeWideLeft", "mouthRight", "mouthDimpleLeft", 
                "mouthLowerDownLeft", "tongueOut", "PP", "CH", "o", "O", "I", "uu", "RR", "XX", "aa", "i", "FF", "U", "TH", "kk", "SS", "e", "DD", "E", "nn", "sil"
            };
            protected static readonly string[] _requiredAnimationClips = { "idle", "walkingForward", "walkingBack", "walkingLeft", "walkingRight"};
            protected static readonly string[] _optionalAnimationClips = { "fist", "smile"};
            private static readonly string[] _reserveAnimationClips = {"walking", "sit_ground", "sit_midStoolNormal", "sit_highStool", "sitGround", "sitMidStoolNormal", "sitHighStool","lHandFist", "rHandFist"};
            private static readonly string _reservePrefix = "pav_";
            
            protected PaabAnimationImportSetting _animationSetting = new PaabAnimationImportSetting();
            private List<KeyValuePair<string, AnimationClip>> _addedClips = new List<KeyValuePair<string, AnimationClip>>();

            
            protected List<AnimationRenderItem> _renderItems = new List<AnimationRenderItem>();
            protected virtual AnimationSetType _animationSetType
            {
                get => AnimationSetType.Base;
            }
#endregion

#region Private Fields

            private BaseInfoWidget _baseInfoWidget = null;
            private Button _nextButton = null;
            private GameObject _skeleton = null;
            private bool _isChecking = false;
            private bool _convertFinished = false;
            private CheckAnimationMode _checkMode = CheckAnimationMode.Loose;

            protected const string _ClipNameLabelText = "ClipName";
            protected const string _RetargetToggleName = "RetargetToggle";
            protected const string _NoneAnimationClipText = "None (Animation Clip)";
            protected const string _RequiredText = "(Required)";
            protected const string _OptionalText = "(Optional)";
            protected const string _UseCustomizedClipText = "(Use Customized Animation)";

            private const string _UnityTextInput = "unity-text-input";
            private bool _warningChecked = false;
            
            private string _skeletonPath;
            private List<string> _toRetargetClips = new List<string>();

            private const float _LongestAnimationClipInSeconds = 60;
            private const float _ShortestAnimationClipInFrames = 2;

#endregion
        }
    }
}

#endif