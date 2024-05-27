#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Pico.Avatar;
using Pico.AvatarAssetBuilder.Protocol;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;
using CharacterInfo = Pico.AvatarAssetBuilder.Protocol.CharacterInfo;
using FileInfo = Pico.AvatarAssetBuilder.Protocol.FileInfo;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        
        
        
        public partial class ConfigureNewCharacterPanel : PavPanel
        {
            // 组件
            private VisualElement baseInfo;
            private Button newSkeletonBtn;
            private VisualElement newSkeletonBtnMask;
            private VisualElement newAnimationSetBtnMask;
            private DropdownField skeletonDropdownField;
            private Button newAnimtionSetBtn;
            private VisualElement newAnimtionSetMask;
            private DropdownField animationSetDropdownField;
            private Button newBasebodyBtn;
            private Button newBasebodySmallBtn;
            private TextField newBasebodyTextField;
            private VisualElement newBasebodySettedBackground;
            private VisualElement skeletonMask;
            private VisualElement maskWarningText;
            private VisualElement animSetAndBaseBodyContent;
            private VisualElement allComponentContent;
            private Button nextBtn;
            private VisualElement skeletonTip;
            private VisualElement animationSetTip;
            private VisualElement baseBodyTip;

            private const string DuplicateNamePostfix = "\t  \t\t ";
            private const string Placeholder = "(Required)";
            private const string NewAssetID = "-99999";
            private readonly string NewSkeletonName = $"New Skeleton{DuplicateNamePostfix}";
            private readonly string NewAnimationSetName = $"New AnimationSet{DuplicateNamePostfix}";
            private readonly string NewBaseBodyName = $"New Base Body{DuplicateNamePostfix}";
            private SkeletonList skeletonList = null;
            private BaseAnimationSetList baseAnimationList = null;

            //private NewCharacterConfig newCharacterConfig = null;

            private BaseInfoWidget baseInfoWidget;

            private DropdownFieldWithPlaceholder skeletonDropdownFieldWithPlaceholder;
            private DropdownFieldWithPlaceholder animationSetDropdownFieldWithPlaceholder;

            private TextFieldWithPlaceHolder newBasebodyTextFieldWithPlaceHolder;
            //private string assetRootPath = AvatarEnv.cacheSpacePath + "/AvatarCacheLocal/";

            

            private HashSet<string> downloadedImage = new HashSet<string>();

            [SerializeField] private ConfigureNewCharacterPanelRestoreData restoreData;
            // private PaabAssetImportSettings curCharacterSettings;
            // public PaabCharacterImportSetting curCharacterSetting;

            private static ConfigureNewCharacterPanel _instance;
            public static ConfigureNewCharacterPanel instance {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<ConfigureNewCharacterPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath  + "PanelData/ConfigureNewCharacterPanel.asset");
                    }
                    return _instance;
                }
            }


            public override string displayName { get => "ConfigureNewAvatar"; }
            public override string panelName { get => "ConfigureNewCharacter"; }
            public override string uxmlPathName { get => "Uxml/ConfigureNewCharacterPanel.uxml"; }
            

#region base function
            
            
            

            public bool InitNewCharacter()
            {
                ClearContent();
                bool result = CharacterUtil.CreateCharacterFolder(CharacterUtil.NewCharacterTempName);
                if (!result)
                {
                    Debug.LogError("Create character folder failed");
                    return false;
                }
                
                CharacterManager.instance.SetCurrentCharacter(null, OperationType.Create);
                return true;
            }

            protected override bool BuildUIDOM(VisualElement parent)
            {
                if (!base.BuildUIDOM(parent))
                {
                    return false;
                }

                InitElements();
                
                return true;
            }

            protected override bool BindUIActions()
            {
                if (!base.BindUIActions())
                {
                    return false;
                }

                newSkeletonBtn.RegisterCallback<ClickEvent>(OnNewSkeletonBtnClick);
                newAnimtionSetBtn.RegisterCallback<ClickEvent>(OnNewAnimationSetBtnClick);
                newBasebodyBtn.RegisterCallback<ClickEvent>(OnNewBaseBodyBtnClick);
                newBasebodySmallBtn.RegisterCallback<ClickEvent>(OnNewBaseBodyBtnClick);
                nextBtn.RegisterCallback<ClickEvent>(OnNextBtnClick);
                
                skeletonDropdownField.RegisterCallback<ChangeEvent<string>>(OnSkeletonDropdownValueChange);
                animationSetDropdownField.RegisterCallback<ChangeEvent<string>>(OnAnimationSetDropdownValueChange);
                return true;
            }
            
            
            public override void OnDestroy()
            {
                base.OnDestroy();
                if(_instance == this)
                {
                    _instance = null;
                }
            }

            public override void OnShow()
            {
                base.OnShow();
                baseInfoWidget.AutoSetIconIfNotSet();
                UpdateMask();
                UpdateBaseBodyStatus();
                UpdateAssetBtnStatus(CharacterBaseAssetType.Skeleton);
                UpdateAssetBtnStatus(CharacterBaseAssetType.AnimationSet);
                CheckAnimationSetName();
                UpdateSeletonDropdown();
            }

            public override void OnExitEditMode()
            {
                if (restoreData == null)
                    restoreData = new ConfigureNewCharacterPanelRestoreData();

                restoreData.baseInfoData = baseInfoWidget.GetRestoreData();
                restoreData.selectedSkeletonId = GetSelectedSkeletonId();
            }

            public override void OnPanelRestore()
            {
            }

#endregion
            
            

#region 私有函数

            private void InitElements()
            {
                baseInfo = mainElement.Q("baseInfo");
                baseInfoWidget = new BaseInfoWidget(baseInfo, BaseInfoType.Character);
                AddWidget(baseInfoWidget);
                baseInfoWidget.ShowWidget();
                
                newSkeletonBtn = mainElement.Q<Button>("newSkeletonBtn");
                newSkeletonBtnMask = mainElement.Q("newSkeletonBtnMask");
                newAnimtionSetBtn = mainElement.Q<Button>("newAnimSetBtn");
                newAnimationSetBtnMask = mainElement.Q("newAnimationSetBtnMask");
                newBasebodyBtn = mainElement.Q<Button>("newBaseBodyBtn");
                newBasebodySmallBtn = mainElement.Q<Button>("newBaseBodySmallBtn");
                newBasebodyTextField = mainElement.Q<TextField>("basebodyName");
                newBasebodySettedBackground  = mainElement.Q("basebodyBg");
                
                skeletonDropdownField = mainElement.Q<DropdownField>("skeletonDropdown");
                animationSetDropdownField = mainElement.Q<DropdownField>("animSetDropdown");

                skeletonDropdownFieldWithPlaceholder = new DropdownFieldWithPlaceholder(skeletonDropdownField, Placeholder);
                animationSetDropdownFieldWithPlaceholder = new DropdownFieldWithPlaceholder(animationSetDropdownField, Placeholder);
                newBasebodyTextFieldWithPlaceHolder = new TextFieldWithPlaceHolder(newBasebodyTextField, "");
                newBasebodyTextFieldWithPlaceHolder.SetNormalTextColor(new Color(0.82f, 0.82f, 0.82f, 1));

                skeletonMask = mainElement.Q("mask");
                maskWarningText = mainElement.Q("warningText");
                animSetAndBaseBodyContent = mainElement.Q("secondContent");
                allComponentContent = mainElement.Q("skeletonConfigureContent");
                nextBtn = mainElement.Q<Button>("NextButton");

                skeletonTip = mainElement.Q("skeletonTip");
                animationSetTip = mainElement.Q("animationSetTip");
                baseBodyTip = mainElement.Q("baseBodyTip");

                // 骨骼、动画集下拉列表
                skeletonDropdownField.AddToClassList("customDropdown");
                animationSetDropdownField.AddToClassList("customDropdown");
                
                if (skeletonDropdownField.choices == null)
                    skeletonDropdownField.choices = new List<string>();
                else
                    skeletonDropdownField.choices.Clear();
                skeletonDropdownField.index = -1;
                
                if (animationSetDropdownField.choices == null)
                    animationSetDropdownField.choices = new List<string>();
                else
                    animationSetDropdownField.choices.Clear();
                animationSetDropdownField.index = -1;
                
                
                UIUtils.AddVisualElementHoverMask(nextBtn);
                UIUtils.AddVisualElementHoverMask(newSkeletonBtn);
                UIUtils.AddVisualElementHoverMask(newAnimtionSetBtn);
                UIUtils.AddVisualElementHoverMask(newBasebodyBtn);
                UIUtils.AddVisualElementHoverMask(newBasebodySmallBtn);
                UIUtils.AddVisualElementHoverMask(skeletonDropdownField, skeletonDropdownField, true);
                UIUtils.AddVisualElementHoverMask(animationSetDropdownField, animationSetDropdownField, true);
                UIUtils.SetTipText(skeletonTip, StringTable.GetString("ConfigureSkeletonTip"));
                UIUtils.SetTipText(animationSetTip, StringTable.GetString("ConfigureAnimationSetTip"));
                UIUtils.SetTipText(baseBodyTip, StringTable.GetString("ConfigureBaseBodyTip"));
            }

            private void ClearContent()
            {
                baseInfoWidget.Clear();
                baseAnimationList = null;
                skeletonList = null;
                skeletonDropdownField.index = -1;
                animationSetDropdownField.index = -1;
                newBasebodyTextField.value = "";
                ClearAssetImportSettings();
            }

            private void UpdateAssetBtnStatus(CharacterBaseAssetType btnType)
            {
                Button btn = null;
                VisualElement btnMask = null;
                string curId = "";
                bool hasNewAsset = false;
                switch (btnType)
                {
                    case CharacterBaseAssetType.Skeleton:
                        btn = newSkeletonBtn;
                        btnMask = newSkeletonBtnMask;
                        hasNewAsset = skeletonAssetImportSettings != null;
                        curId = GetSelectedSkeletonId();
                        break;
                    case CharacterBaseAssetType.AnimationSet:
                        btn = newAnimtionSetBtn;
                        btnMask = newAnimationSetBtnMask;
                        hasNewAsset = animationSetAssetImportSettings != null;
                        curId = GetSelectedBaseAnimationSetId();
                        break;
                    default:
                        return;
                }
            
                if (!hasNewAsset)
                {
                    btn.text = GetNewAssetBtnText(btnType);
                    btnMask?.SetActive(false);
                    //TODO 查一下为啥这么设置不生效
                    //btn.pickingMode = PickingMode.Position;
                    btn.style.opacity = 1;
                }
                else if (curId == NewAssetID)
                {
                    btn.text = "";
                    btn.style.opacity = 1;
                    btnMask?.SetActive(false);
                    //btn.pickingMode = PickingMode.Position;
                }
                else
                {
                    btn.text = "";
                    btn.style.opacity = 0.3f;
                    btnMask?.SetActive(true);
                    //btn.pickingMode = PickingMode.Ignore;
                }
                
                for (int i = 0; i < btn.childCount; i++)
                {
                    var child = btn[i];
                    child.SetActive(hasNewAsset);
                }
            }


            private void UpdateMask()
            {
                var skeletonId = GetSelectedSkeletonId();
                bool maskShow = string.IsNullOrEmpty(skeletonId);
                if (skeletonId == NewAssetID)
                {
                    if (skeletonAssetImportSettings.assetStatus == AssetStatus.Uncheck)
                        maskShow = true;
                }
                
                if (maskShow)
                {
                    skeletonMask.SetActive(true);
                    maskWarningText.SetActive(true);
                    animSetAndBaseBodyContent.style.opacity = 0.3f;
                }
                else
                {
                    skeletonMask.SetActive(false);
                    maskWarningText.SetActive(false);
                    animSetAndBaseBodyContent.style.opacity = 1f;
                }
            }
            
            private void MoveNext()
            {
                
            }
            
            private void UpdateSeletonDropdown()
            {
                var request = AssetServerManager.instance.GetSkeletonListByApp();
                request.Send(success =>
                    {
                        var response = ProtocolUtil.GetResponse<SkeletonList>(success);
                        if (response == null || response.data == null)
                        {
                            Debug.LogError("Get response failed");
                            return;
                        }
                        else
                            UpdateSkeletonDropdownList(response.data);
                    },
                    failure => { Debug.Log("GetSkeletonListByApp...failed : " + failure.ToString()); });
            }
            
            
            private void UpdateAnimationSetDropdown()
            {
                var skeletonId = GetSelectedSkeletonId();
                // 新骨架
                if (string.IsNullOrEmpty(skeletonId) || skeletonId == NewAssetID)
                {
                    if (skeletonId == NewAssetID)
                    {
                        List<string> items = new List<string>();
                        string currentValue = animationSetDropdownField.value;
                        int index = 0;
                        var curSkeletonName = GetSelectedSkeletonName();
                        if (animationSetAssetImportSettings != null && animationSetAssetImportSettings.basicInfoSetting.skeletonAssetName == curSkeletonName)
                        {
                            var animationSetName = GetNewAnimationName();
                            if (!string.IsNullOrEmpty(animationSetName))
                                items.Add(animationSetName);

                            if (currentValue == animationSetName || currentValue == NewAnimationSetName)
                                index = items.Count - 1;
                        }
                        
                        animationSetDropdownField.choices = items;
                        animationSetDropdownField.index = index;
                    }
                    
                    return;
                }
                
                var request = AssetServerManager.instance.GetBaseAnimationSetListByApp(skeletonId);
                request.Send(success =>
                    {
                        Debug.Log("GetAnimationSetListByApp...success= " + success.ToString());
                        var response = ProtocolUtil.GetResponse<BaseAnimationSetList>(success);
                        if (response == null || response.data == null)
                        {
                            Debug.LogError("Get response failed");
                            return;
                        }
                        else
                            UpdateAnimationSetDropdownList(response.data);
                    },
                    failure => { Debug.Log("GetAnimationSetListByApp...failed : " + failure.ToString()); });
            }

            private void CheckAnimationSetName()
            {
                int serverAnimationSetCount = 0;
                if (baseAnimationList != null)
                    serverAnimationSetCount = baseAnimationList.assets.Count;
                
                if (animationSetDropdownField.choices.Count == serverAnimationSetCount + 1)
                {
                    int newAnimationSetIndex = animationSetDropdownField.choices.Count - 1;
                    if (animationSetAssetImportSettings == null || string.IsNullOrEmpty(animationSetAssetImportSettings.assetName))
                    {
                        animationSetDropdownField.choices[newAnimationSetIndex] = NewAnimationSetName;
                        if (animationSetDropdownField.index == newAnimationSetIndex)
                            animationSetDropdownField.SetValueWithoutNotify(NewAnimationSetName);
                    }
                    else
                    {
                        animationSetDropdownField.choices[newAnimationSetIndex] = $"{animationSetAssetImportSettings.assetName}{DuplicateNamePostfix}";
                        if (animationSetDropdownField.index == newAnimationSetIndex)
                            animationSetDropdownField.SetValueWithoutNotify(animationSetDropdownField.choices[newAnimationSetIndex]);
                    }
                }
            }
            
            private void AfterCreateNewSkeleton()
            {
                skeletonDropdownField.choices.Add(NewSkeletonName);
                skeletonDropdownField.index = skeletonDropdownField.choices.Count - 1;
            }
            
            private void AfterCreateAnimationSet()
            {
                animationSetDropdownField.choices.Add(NewAnimationSetName);
                animationSetDropdownField.index = animationSetDropdownField.choices.Count - 1;
            }

            private void ResetAnimationSetDropdown()
            {
                baseAnimationList = null;
                animationSetDropdownField.index = -1;
                animationSetDropdownField.choices.Clear();
            }

            private bool CheckError()
            {
                baseInfoWidget.CheckValue(OnCheckFinish);
                
                return false;
            }

            private bool IsSelectNewSkeleton()
            {
                var skeletonId = GetSelectedSkeletonId();
                return skeletonId == NewAssetID;
            }
            
            private bool IsSelectNewAnimationSet()
            {
                var animationSetId = GetSelectedBaseAnimationSetId();
                return animationSetId == NewAssetID;
            }

            private CharacterData GenCreateCharacterData()
            {
                var characterCover = UIUtils.SaveTextureToTmpPathWithAspect(baseInfoWidget.Icon, baseInfoWidget.Name, UIUtils.DefaultCharacterIconAspect);
                if (string.IsNullOrEmpty(characterCover))
                    return null;
                
                CharacterData data = new CharacterData();
                data.character_id = null;
                data.character_base = new CharacterBase();
                data.character_base.name = baseInfoWidget.Name;
                data.character_base.show_name = baseInfoWidget.ShowName;
                data.character_base.cover = characterCover;
                // data.character_base.config = "{\"info\":{\"sex\":\"male\",\"status\":\"Online\",\"continent\":\"EU\",\"background\":{\"image\":\"https: //dfsedffe.png\",\"end_color\":[133,182,255],\"start_color\":[148,111,253]},\"avatar_type\":\"preset\"},\"avatar\":{\"body\":{\"version\":1,\"perParams\":[],\"technique\":\"Pico2-Bone\",\"floatIdParams\":[]},\"head\":{\"version\":1,\"perParams\":[],\"technique\":\"Pico2-BS\",\"floatIdParams\":[]},\"skin\":{\"color\":\"\",\"white\":0,\"softening\":0},\"skeleton\":{\"assetId\":\"7236517532134506540\"},\"baseAnimation\":{\"assetId\":\"7236517532134572076\"},\"baseBody\":{\"assetId\":\"-1\"},\"assetPins\":[]},\"avatarStyle\":\"PicoCustomAvatar\"}";
                data.character_base.config = CharacterManager.instance.GetSpecString();
                if (string.IsNullOrEmpty(data.character_base.config))
                {
                    Debug.LogError("[CreateCharacter] Config is empty!!");
                    return null;
                }
                
                var skeletonId = GetSelectedSkeletonId();
                if (skeletonId == NewAssetID)
                {
                    var skeletonAssetData = CharacterUtil.CreateNewAssetUploadData(skeletonAssetImportSettings);
                    if (skeletonAssetData == null)
                    {
                        Debug.LogError("[GenCreateCharacterData] Create skeleton asset data failed");
                        return null;
                    }

                    data.skeleton = skeletonAssetData;
                }
                else
                {
                    data.skeleton_id = skeletonId;
                    data.skeleton = null;
                }

                var animationSetId = GetSelectedBaseAnimationSetId();
                var animationSetConfig = GetSelectedBaseAnimationSetConfig();
                if (animationSetId == NewAssetID)
                {
                    var animationSetAssetData = CharacterUtil.CreateNewAssetUploadData(animationSetAssetImportSettings);
                    if (animationSetAssetData == null)
                    {
                        Debug.LogError("[GenCreateCharacterData] Create animationSet asset data failed");
                        return null;
                    }

                    data.base_animation_set = animationSetAssetData;
                }
                else
                {
                    data.base_animation_set_id = animationSetId;
                    data.base_animation_set = new NewAssetData();
                    data.base_animation_set.offline_config = animationSetConfig;
                }
                
                if (basebodyAssetImportSettings == null)
                {
                    Debug.LogError("[GenCreateCharacterData] BasebodyAssetImportSettings is null");
                    return null;
                }

                var baseBodyAssetData = CharacterUtil.CreateNewAssetUploadData(basebodyAssetImportSettings);
                if (baseBodyAssetData == null)
                {
                    Debug.LogError("[GenCreateCharacterData] Create base body asset data failed");
                    return null;
                }

                data.base_body = baseBodyAssetData;
                
                return data;
            }

            // private bool CopyIcon()
            // {
            //     try
            //     {
            //         // TODO 裁切图片
            //         UIUtils.SaveTextureToTmpPathWithAspect(baseInfoWidget.Icon, baseInfoWidget.Name, 0.75f);
            //         return true;
            //     }
            //     catch (Exception e)
            //     {
            //         Debug.LogError($"Copy icon failed : {e.Message} / {e.StackTrace}");
            //         return false;
            //     }
            // }



            

            private void OnCheckFinish(BaseInfoValueCheckResult result)
            {
                // AssetTestPanel.ShowAssetTestPanel(this, GenCreateCharacterData());
                // return;
                List<string> errorMsg = new List<string>();
                if (skeletonDropdownField.index == -1)
                {
                    skeletonDropdownFieldWithPlaceholder.ShowErrorOnce();
                    errorMsg.Add("Need select a skeleton");
                } 
                else if (IsSelectNewSkeleton() && skeletonAssetImportSettings.assetStatus != AssetStatus.Ready)
                {
                    skeletonDropdownFieldWithPlaceholder.ShowErrorOnce();
                    errorMsg.Add("New skeleton doesn't config finish");
                }
                
                if (animationSetDropdownField.index == -1)
                {
                    animationSetDropdownFieldWithPlaceholder.ShowErrorOnce();
                    errorMsg.Add("Need select a animation set");
                }
                else if (IsSelectNewAnimationSet() && animationSetAssetImportSettings.assetStatus != AssetStatus.Ready)
                {
                    animationSetDropdownFieldWithPlaceholder.ShowErrorOnce();
                    errorMsg.Add("New animationSet doesn't config finish");
                }
                
                if (string.IsNullOrEmpty(newBasebodyTextField.value))
                    errorMsg.Add("Need create a base body");
                else if (basebodyAssetImportSettings == null ||
                         basebodyAssetImportSettings.assetStatus != AssetStatus.Ready)
                {
                    newBasebodyTextFieldWithPlaceHolder.ShowErrorOnce();
                    errorMsg.Add("New baseBody doesn't config finish");
                }
                
                errorMsg.AddRange(BaseInfoWidget.GetBaseInfoErrorMessage(result));

                if (errorMsg.Count == 0)
                {
                    //SendCreateRequest();
                    // bool copyResult = CopyIcon();
                    // if (copyResult)
                    // {
                        var assetData = GenCreateCharacterData();
                        // set characterData to characterInfo.
                        var characterInfo = CharacterManager.instance.GetCurrentCharacter();
                        //baseBody
                        characterInfo.base_body.offline_config = assetData.base_body.offline_config;
                        //baseAnimation
                        if (assetData.base_animation_set != null)
                            characterInfo.base_animation_set.offline_config = assetData.base_animation_set.offline_config;
                        else
                            characterInfo.base_animation_set.asset_id = assetData.base_animation_set_id;
                        //character
                        characterInfo.character.cover = assetData.character_base.cover;
                        characterInfo.character.show_name = assetData.character_base.show_name;
                        characterInfo.character.config = assetData.character_base.config;
                        
                        if (assetData == null)
                        {
                            Debug.LogError("Gen createCharacterData failed");
                            return;
                        }
                        
                        var panel = NavMenuBarRoute.instance.RouteNextByType(panelName, PanelType.AssetTestPanel);
                        if (panel != null)
                        {
                            ((AssetTestPanel)panel).InitPanel(panelName, assetData);
                        }
                    // }
                    // else
                    // {
                    //     // TODO
                    //     Debug.LogError("OnCheckFinish TODO");
                    // }
                }
                else
                {
                    var messages = new List<CommonDialogWindow.Message>();
                    foreach (var msg in errorMsg)
                    {
                        messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error, msg));
                    }
                    CommonDialogWindow.ShowCheckPopupDialog(messages);
                }
            }

            private void UpdateSkeletonDropdownList(SkeletonList data)
            {
                string curAssetId = "";
                var currentValue = skeletonDropdownField.value;
                if (skeletonList != null && skeletonList.skeletons.Count > 0)
                {
                    for (int i = 0; i < skeletonList.skeletons.Count; i++)
                    {
                        if (currentValue == skeletonList.skeletons[i].asset_info.name)
                        {
                            curAssetId = skeletonList.skeletons[i].asset_info.asset_id;
                            break;
                        }
                    }
                }

                skeletonList = data;
                List<string> items = new List<string>();
                int index = -1;
                for (int i = 0; i < skeletonList.skeletons.Count; i++)
                {
                    if (!string.IsNullOrEmpty(curAssetId) && curAssetId == skeletonList.skeletons[i].asset_info.asset_id)
                        index = i;
                    
                    items.Add(skeletonList.skeletons[i].asset_info.name);
                }

                if (skeletonAssetImportSettings != null)
                {
                    var skeletonName = GetNewSkeletonName(true);
                    if (!string.IsNullOrEmpty(skeletonName))
                    {
                        items.Add(skeletonName);
                        // 感觉如果有新资源可以直接切换过去
                        if (index == -1)
                            index = items.Count - 1;
                    }
                }
                
                if (items.Count == 0)
                    Debug.LogWarning("SkeletonList data is empty");

                skeletonDropdownField.choices = items;
                skeletonDropdownField.index = index;
                UpdateMask();
            }

            private void UpdateAnimationSetDropdownList(BaseAnimationSetList data)
            {
                List<string> items = new List<string>();
                foreach (var item in data.assets)
                {
                    items.Add(item.asset_info.name);
                }
                
                if (items.Count == 0)
                    Debug.LogWarning("AnimationSetList data is empty");

                var curSkeletonName = GetSelectedSkeletonName();
                if (animationSetAssetImportSettings != null && animationSetAssetImportSettings.basicInfoSetting.skeletonAssetName == curSkeletonName)
                {
                    var animationSetName = GetNewAnimationName();
                    if (!string.IsNullOrEmpty(animationSetName))
                        items.Add(animationSetName);
                }
                
                baseAnimationList = data;
                animationSetDropdownField.choices = items;
                animationSetDropdownField.index = 0;
            }

            private string GetSelectedSkeletonId()
            {
                if (skeletonList == null)
                    return null;
                
                if (skeletonDropdownField.index >= 0 && skeletonDropdownField.index < skeletonList.skeletons.Count)
                    return skeletonList.skeletons[skeletonDropdownField.index].asset_info.asset_id;

                if (skeletonDropdownField.index == skeletonList.skeletons.Count)
                {
                    return NewAssetID;
                }

                return "";
            }
            
            private string GetSelectedSkeletonName()
            {
                if (skeletonList == null)
                    return null;
                
                if (skeletonDropdownField.index >= 0 && skeletonDropdownField.index < skeletonList.skeletons.Count)
                    return skeletonList.skeletons[skeletonDropdownField.index].asset_info.name;

                if (skeletonDropdownField.index == skeletonList.skeletons.Count)
                {
                    return GetNewSkeletonName(false);
                }

                return "";
            }

            private string GetNewSkeletonName(bool addPostFix)
            {
                if (skeletonAssetImportSettings == null)
                    return "";
                
                if (string.IsNullOrEmpty(skeletonAssetImportSettings.assetName))
                    return NewSkeletonName;
                else if (addPostFix)
                    return $"{skeletonAssetImportSettings.assetName}{DuplicateNamePostfix}";
                else
                    return skeletonAssetImportSettings.assetName;
            }

            private string GetNewAnimationName()
            {
                if (animationSetAssetImportSettings == null)
                    return "";

                if (string.IsNullOrEmpty(animationSetAssetImportSettings.assetName))
                    return NewAnimationSetName;
                else
                    return $"{animationSetAssetImportSettings.assetName}{DuplicateNamePostfix}";
            }
            
            private SkeletonListItem GetSelectedSkeletonInfo()
            {
                if (skeletonList == null)
                    return null;
                
                if (skeletonDropdownField.index >= 0 && skeletonDropdownField.index < skeletonList.skeletons.Count)
                    return skeletonList.skeletons[skeletonDropdownField.index];
            
                return null;
            }

            private string GetSelectedBaseAnimationSetId()
            {
                if (baseAnimationList == null)
                {
                    if (animationSetDropdownField.index == 0)
                        return NewAssetID;
                    
                    return null;
                }
                
                if (animationSetDropdownField.index >= 0 && animationSetDropdownField.index < baseAnimationList.assets.Count)
                    return baseAnimationList.assets[animationSetDropdownField.index].asset_info.asset_id;

                if (animationSetDropdownField.index == baseAnimationList.assets.Count)
                {
                    return NewAssetID;
                }

                return "";
            }
            
            private string GetSelectedBaseAnimationSetConfig()
            {
                if (baseAnimationList == null)
                {
                    if (animationSetDropdownField.index == 0)
                        return NewAssetID;
                    
                    return null;
                }
                
                if (animationSetDropdownField.index >= 0 && animationSetDropdownField.index < baseAnimationList.assets.Count)
                    return baseAnimationList.assets[animationSetDropdownField.index].asset_info.offline_config;

                if (animationSetDropdownField.index == baseAnimationList.assets.Count)
                {
                    return "";
                }

                return "";
            }
            
            private BaseAnimationSetListItem GetSelectedAnimationSetInfo()
            {
                if (baseAnimationList == null)
                {
                    return null;
                }
                
                if (animationSetDropdownField.index >= 0 && animationSetDropdownField.index < baseAnimationList.assets.Count)
                    return baseAnimationList.assets[animationSetDropdownField.index];
                

                return null;
            }
            

            private void UpdateBaseBodyStatus()
            {
                if (basebodyAssetImportSettings == null)
                {
                    newBasebodySettedBackground.SetActive(false);
                    newBasebodySmallBtn.SetActive(false);
                    newBasebodyBtn.SetActive(true);
                }
                else
                {
                    if (!string.IsNullOrEmpty(basebodyAssetImportSettings.assetName))
                        newBasebodyTextField.value = basebodyAssetImportSettings.assetName;
                    else
                        newBasebodyTextField.value = NewBaseBodyName;
                    
                    newBasebodySettedBackground.SetActive(true);
                    newBasebodySmallBtn.SetActive(true);
                    newBasebodyBtn.SetActive(false);
                }
            }

            private string GetNewAssetBtnText(CharacterBaseAssetType assetBtnType)
            {
                if (assetBtnType == CharacterBaseAssetType.Skeleton)
                    return "+ New Skeleton";

                if (assetBtnType == CharacterBaseAssetType.AnimationSet)
                    return "+ New AnimationSet";
                
                return "";
            }
            

            private void DownloadSkeleton(AssetInfo assetInfo)
            {
                if (MainMenuUIManager.instance.PAAB_OPEN == false)
                {
                    return;
                }

                if (!CharacterUtil.DownloadAsset(new AssetDirInfo(CharacterBaseAssetType.Skeleton), assetInfo))
                    return;

                if (!downloadedImage.Contains(assetInfo.cover))
                {
                    if (CharacterUtil.DownloadAssetIcon(new AssetDirInfo(CharacterBaseAssetType.Skeleton), assetInfo))
                        downloadedImage.Add(assetInfo.cover);
                }
            }

#region UI控件事件回调函数
            
            
            private void OnNewSkeletonBtnClick(ClickEvent evt)
            {
                bool isNew = skeletonAssetImportSettings == null;
                // 骨骼创建
                var skeletonPanel = NavMenuBarRoute.instance.RouteNextByPanelName(panelName, SkeletonPanel.instance.panelName);
                if (skeletonPanel is AssetImportSettingsPanel)
                    (skeletonPanel as AssetImportSettingsPanel).BindOrUpdateFromData(GetSkeletonAssetImportSettings());
                if (isNew)
                    AfterCreateNewSkeleton();
            }
            
            private void OnNewAnimationSetBtnClick(ClickEvent evt)
            {
                bool isNew = animationSetAssetImportSettings == null;
                // 动画集创建
                var baseAnimationSetPanel = NavMenuBarRoute.instance.RouteNextByPanelName(panelName, ConfigureBaseAnimationSetPanel.instance.panelName);
                if (baseAnimationSetPanel is AssetImportSettingsPanel)
                    (baseAnimationSetPanel as AssetImportSettingsPanel).BindOrUpdateFromData(GetAnimationSetImportSettings());
                if (isNew)
                    AfterCreateAnimationSet();
            }
            
            private void OnNewBaseBodyBtnClick(ClickEvent evt)
            {
                // 素体创建
                ConfigureComponentPanel.instance.SetForceClearData(false);
                var baseBodyPanel = NavMenuBarRoute.instance.RouteNextByPanelName(panelName, ConfigureComponentPanel.instance.panelName);
                if (baseBodyPanel is AssetImportSettingsPanel)
                    (baseBodyPanel as AssetImportSettingsPanel).BindOrUpdateFromData(GetBaseBodyImportSettings());
                newBasebodyTextFieldWithPlaceHolder.ResetError();
            }

            private void OnNextBtnClick(ClickEvent evt)
            {
                CheckError();
            }

            private void OnSkeletonDropdownValueChange(ChangeEvent<string> evt)
            {
                if (animationSetAssetImportSettings != null)
                {
                    GameObject.DestroyImmediate(animationSetAssetImportSettings, true);
                    animationSetAssetImportSettings = null;
                    MainMenuUIManager.instance.DestroyPanels(new List<PavPanel>{ConfigureBaseAnimationSetPanel.instance});
                }

                if (basebodyAssetImportSettings != null)
                {
                    GameObject.DestroyImmediate(basebodyAssetImportSettings, true);
                    basebodyAssetImportSettings = null;
                    MainMenuUIManager.instance.DestroyPanels(new List<PavPanel>{ConfigureComponentPanel.instance});
                }
                
                
                UpdateMask();
                UpdateAssetBtnStatus(CharacterBaseAssetType.Skeleton);
                UpdateAssetBtnStatus(CharacterBaseAssetType.AnimationSet);
                ResetAnimationSetDropdown();
                UpdateAnimationSetDropdown();
                UpdateBaseBodyStatus();
                var info = GetSelectedSkeletonInfo();
                if (info != null)
                {
                    DownloadSkeleton(info.asset_info);
                    //curCharacterSetting.setSkeletonAsset(info.asset_info.asset_id, PaabCharacterImportSetting.AssetState_Online);
                    CharacterManager.instance.SetAssetLoadSource(PaabCharacterImportSetting.AssetState_Online, info.asset_info.asset_id, CharacterBaseAssetType.Skeleton);
                }
                else
                {
                    if (skeletonAssetImportSettings != null && skeletonAssetImportSettings.assetStatus == AssetStatus.Ready)
                    {
                        // 应该是不用处理改名的情况
                        CharacterManager.instance.SetAssetLoadSource(PaabCharacterImportSetting.AssetState_Local, skeletonAssetImportSettings.assetName, CharacterBaseAssetType.Skeleton);
                    }
                    
                    //curCharacterSetting.setSkeletonAsset($"{CharacterUtil.SkeletonFolderName}/{skeletonAssetImportSettings.assetName}", PaabCharacterImportSetting.AssetState_Local);
                }
            }

            private void OnAnimationSetDropdownValueChange(ChangeEvent<string> evt)
            {
                UpdateAssetBtnStatus(CharacterBaseAssetType.AnimationSet);
                var info = GetSelectedAnimationSetInfo();
                if (info != null)
                {
                    CharacterManager.instance.SetAssetLoadSource(PaabCharacterImportSetting.AssetState_Online, info.asset_info.asset_id, CharacterBaseAssetType.AnimationSet);
                }
                else
                {
                    if (animationSetAssetImportSettings != null && animationSetAssetImportSettings.assetStatus == AssetStatus.Ready)
                    {
                        // 应该是不用处理改名的情况
                        CharacterManager.instance.SetAssetLoadSource(PaabCharacterImportSetting.AssetState_Local, animationSetAssetImportSettings.assetName, CharacterBaseAssetType.AnimationSet);
                    }
                }
            }

#endregion
            
#endregion
            
            
        }
        
        public enum CharacterBaseAssetType
        {
            Skeleton,
            AnimationSet,
            BaseBody,
            Component
        }
        
        
        
        
        
        [Serializable]
        public class ConfigureNewCharacterPanelRestoreData
        {
            public BaseInfoWidgetRestoreData baseInfoData;
            public string selectedSkeletonId;


            public void Clear()
            {
                baseInfoData = null;
            }
        }

        [Serializable]
        public class CharacterData
        {
            public string character_id;
            public CharacterBase character_base;
            public string skeleton_id;
            public string base_animation_set_id;
            public NewAssetData skeleton;
            public NewAssetData base_animation_set;
            public NewAssetData base_body;

            public CharacterData()
            {
            }

            public CharacterData(AssetUploadManager.NewUploadAssetData uploadAssetData)
            {
                character_id = uploadAssetData.character_id;
                character_base = uploadAssetData.character_base;
                skeleton_id = uploadAssetData.skeleton_id;
                base_animation_set_id = uploadAssetData.base_animation_set_id;
                skeleton = uploadAssetData.skeleton;
                base_animation_set = uploadAssetData.base_animation_set;
                base_body = uploadAssetData.base_body;
            }
        }
    }
}
#endif