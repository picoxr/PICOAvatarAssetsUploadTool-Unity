#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Pico.Avatar;
using Pico.AvatarAssetPreview.Protocol;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using CharacterInfo = Pico.AvatarAssetPreview.Protocol.CharacterInfo;
using FileInfo = Pico.AvatarAssetPreview.Protocol.FileInfo;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class UpdateCharacterPanel : AssetImportSettingsPanel
        {
            // 组件
            private VisualElement baseInfo;
            private Button editSkeletonBtn;
            private VisualElement editSkeletonBtnMask;
            private TextField skeletonTextField;
            private Button editAnimationSetBtn;
            private VisualElement editAnimationSetBtnMask;
            private TextField animationSetTextField;
            private Button editBasebodyBtn;
            private TextField basebodyTextField;
            private Button nextBtn;
            private VisualElement skeletonTip;
            private VisualElement animationSetTip;
            private VisualElement baseBodyTip;

            private TextFieldWithPlaceHolder skeletonTextFieldWithPlaceHolder;
            private TextFieldWithPlaceHolder animationSetTextFieldWithPlaceHolder;

            
            private SkeletonList skeletonList = null;
            private BaseInfoWidget baseInfoWidget;
            private CharacterInfo characterInfo;
            
            private PaabAssetImportSettings skeletonAssetImportSettings;
            private PaabAssetImportSettings animationSetAssetImportSettings;
            private PaabAssetImportSettings basebodyAssetImportSettings;

            private bool characterDataSet = false;
            
            
            private static UpdateCharacterPanel _instance;
            public static UpdateCharacterPanel instance {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<UpdateCharacterPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath  + "PanelData/UpdateCharacterPanel.asset");
                    }
                    return _instance;
                }
            }


            public override string displayName { get => "UpdateAvatar"; }
            public override string panelName { get => "UpdateCharacter"; }
            public override string uxmlPathName { get => "Uxml/UpdateCharacterPanel.uxml"; }
            

#region base function


            public void SetData(CharacterInfo data)
            {
                characterInfo = data;
                bool result = CharacterUtil.CreateCharacterFolder(characterInfo.character.name, false);
                if (!result)
                {
                    Debug.LogError("[UpdateCharacterPanel] Create character folder failed");
                    return;
                }
                
                // 测试中角色创建spec.json
                if (characterInfo.character.status == 2)
                    CharacterUtil.CreateCharacterSpecJsonFile(characterInfo.character.name, characterInfo.character.config);
                
                CharacterManager.instance.SetCurrentCharacter(characterInfo, OperationType.Update);
                ClearAssetImportSettings();
                CreateImportSettings();
                CharacterManager.instance.SetAssetLoadSource(PaabCharacterImportSetting.AssetState_Online, characterInfo.skeleton.asset_id, CharacterBaseAssetType.Skeleton);
                CharacterManager.instance.SetAssetLoadSource(PaabCharacterImportSetting.AssetState_Online, characterInfo.base_animation_set.asset_id, CharacterBaseAssetType.AnimationSet);
                CharacterManager.instance.SetAssetLoadSource(PaabCharacterImportSetting.AssetState_Online, characterInfo.base_body.asset_id, CharacterBaseAssetType.BaseBody);
                if(characterInfo.skeleton != null)
                    DownloadSkeleton(characterInfo.skeleton);
                characterDataSet = true;
            }

            public override void BindOrUpdateFromData(PaabAssetImportSettings importConfig)
            {
                base.BindOrUpdateFromData(importConfig);
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

                editSkeletonBtn.RegisterCallback<ClickEvent>(OnEditSkeletonBtnClick);
                editAnimationSetBtn.RegisterCallback<ClickEvent>(OnEditAnimationSetBtnClick);
                editBasebodyBtn.RegisterCallback<ClickEvent>(OnEditBaseBodyBtnClick);
                nextBtn.RegisterCallback<ClickEvent>(OnNextBtnClick);
                
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
                RefreshPanel();
                characterDataSet = false;
            }

            public override void OnExitEditMode()
            {
                
            }

            public override void OnPanelRestore()
            {
                
            }

#endregion
            
            

#region 私有函数

            private void InitElements()
            {
                baseInfo = mainElement.Q("baseInfo");
                baseInfoWidget = new BaseInfoWidget(baseInfo, BaseInfoType.Character, true);
                AddWidget(baseInfoWidget);
                baseInfoWidget.ShowWidget();
                
                editSkeletonBtn = mainElement.Q<Button>("newSkeletonBtn");
                editAnimationSetBtn = mainElement.Q<Button>("newAnimSetBtn");
                editBasebodyBtn = mainElement.Q<Button>("newBaseBodySmallBtn");

                skeletonTextField = mainElement.Q<TextField>("skeletonTextField");
                animationSetTextField = mainElement.Q<TextField>("animSetTextField");
                basebodyTextField = mainElement.Q<TextField>("basebodyName");

                // skeletonTextFieldWithPlaceHolder = new TextFieldWithPlaceHolder(skeletonTextField, "", );
                // animationSetTextFieldWithPlaceHolder = new TextFieldWithPlaceHolder(animationSetTextField, Placeholder);
                
                nextBtn = mainElement.Q<Button>("NextButton");

                skeletonTip = mainElement.Q("skeletonTip");
                animationSetTip = mainElement.Q("animationSetTip");
                baseBodyTip = mainElement.Q("baseBodyTip");

                
                
                UIUtils.AddVisualElementHoverMask(nextBtn);
                UIUtils.AddVisualElementHoverMask(editSkeletonBtn);
                UIUtils.AddVisualElementHoverMask(editAnimationSetBtn);
                UIUtils.AddVisualElementHoverMask(editBasebodyBtn);
                UIUtils.AddVisualElementHoverMask(skeletonTextField, skeletonTextField, true);
                UIUtils.AddVisualElementHoverMask(animationSetTextField, animationSetTextField, true);
                UIUtils.AddVisualElementHoverMask(basebodyTextField, basebodyTextField, true);
                
                UIUtils.SetTipText(skeletonTip, StringTable.GetString("ConfigureSkeletonTip"));
                UIUtils.SetTipText(animationSetTip, StringTable.GetString("ConfigureAnimationSetTip"));
                UIUtils.SetTipText(baseBodyTip, StringTable.GetString("ConfigureBaseBodyTip"));
            }
            

            private void MoveNext()
            {
                
            }
            
            
            private bool CheckError()
            {
                baseInfoWidget.CheckValue(OnCheckFinish);
                return false;
            }
            
            private CharacterData GenUpdateCharacterData()
            {
                var characterCover = UIUtils.SaveTextureToTmpPathWithAspect(baseInfoWidget.Icon, baseInfoWidget.Name, UIUtils.DefaultCharacterIconAspect);
                if (string.IsNullOrEmpty(characterCover))
                    return null;
                
                CharacterData data = new CharacterData();
                data.character_id = characterInfo.character.character_id;
                data.character_base = new CharacterBase();
                data.character_base.name = baseInfoWidget.Name;
                data.character_base.show_name = baseInfoWidget.ShowName;
                data.character_base.cover = characterCover;
                data.character_base.config = CharacterManager.instance.GetSpecString();
                if (string.IsNullOrEmpty(data.character_base.config))
                {
                    Debug.LogError("[UpdateCharacter] Config is empty!!");
                    return null;
                }
                
                data.skeleton = null;
                data.base_animation_set = null;
                data.base_body = null;

                if (skeletonAssetImportSettings.assetStatus == AssetStatus.Ready)
                    data.skeleton = CharacterUtil.CreateNewAssetUploadData(skeletonAssetImportSettings);

                if (animationSetAssetImportSettings.assetStatus == AssetStatus.Ready)
                    data.base_animation_set = CharacterUtil.CreateNewAssetUploadData(animationSetAssetImportSettings);

                if (basebodyAssetImportSettings.assetStatus == AssetStatus.Ready)
                    data.base_body = CharacterUtil.CreateNewAssetUploadData(basebodyAssetImportSettings);
                    
                return data;
            }
            
            private void OnCheckFinish(BaseInfoValueCheckResult result)
            {
                List<string> errorMsg = new List<string>();
                errorMsg.AddRange(BaseInfoWidget.GetBaseInfoErrorMessage(result));
                
                if (errorMsg.Count == 0)
                {
                    //SendCreateRequest();
                    bool copyResult = CopyIcon();
                    if (copyResult)
                    {
                        var assetData = GenUpdateCharacterData();
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
                      
                    }
                    else
                    {
                        // TODO
                        Debug.LogError("OnCheckFinish TODO");
                    }
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
            
            private bool CopyIcon()
            {
                try
                {
                    // TODO 裁切图片
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Copy icon failed : {e.Message} / {e.StackTrace}");
                    return false;
                }
            }
            

            private void RefreshPanel()
            {
                if (characterInfo == null)
                    return;

                if (characterDataSet)
                    baseInfoWidget.SetInputValue(characterInfo.character.name, characterInfo.character.show_name, characterInfo.character.cover);
                
                skeletonTextField.value = skeletonAssetImportSettings.assetName;
                animationSetTextField.value = animationSetAssetImportSettings.assetName;
                basebodyTextField.value = basebodyAssetImportSettings.assetName;
            }
            
            private void DownloadSkeleton(AssetInfo assetInfo)
            {
                if (MainMenuUIManager.instance.PAAB_OPEN == false)
                {
                    return;
                }
                if (!CharacterUtil.DownloadAsset(new AssetDirInfo(CharacterBaseAssetType.Skeleton), assetInfo))
                    return;

                CharacterUtil.DownloadAssetIcon(new AssetDirInfo(CharacterBaseAssetType.Skeleton), assetInfo);
            }
            
            
#region UI控件事件回调函数
            
            
            private void OnEditSkeletonBtnClick(ClickEvent evt)
            {
                var skeletonPanel = NavMenuBarRoute.instance.RouteNextByPanelName(panelName, SkeletonPanel.instance.panelName);
                if (skeletonPanel is AssetImportSettingsPanel)
                    (skeletonPanel as AssetImportSettingsPanel).BindOrUpdateFromData(skeletonAssetImportSettings);
            }
            
            private void OnEditAnimationSetBtnClick(ClickEvent evt)
            {
                var baseAnimationSetPanel = NavMenuBarRoute.instance.RouteNextByPanelName(panelName, ConfigureBaseAnimationSetPanel.instance.panelName);
                if (baseAnimationSetPanel is AssetImportSettingsPanel)
                    (baseAnimationSetPanel as AssetImportSettingsPanel).BindOrUpdateFromData(animationSetAssetImportSettings);
            }
            
            private void OnEditBaseBodyBtnClick(ClickEvent evt)
            {
                ConfigureComponentPanel.instance.SetForceClearData(false);
                var baseBodyPanel = NavMenuBarRoute.instance.RouteNextByPanelName(panelName, ConfigureComponentPanel.instance.panelName);
                if (baseBodyPanel is AssetImportSettingsPanel)
                    (baseBodyPanel as AssetImportSettingsPanel).BindOrUpdateFromData(basebodyAssetImportSettings);
            }

            private void OnNextBtnClick(ClickEvent evt)
            {
                CheckError();
            }

#endregion

#region importSettings

            private void ClearAssetImportSettings()
            {
                if (skeletonAssetImportSettings != null)
                    GameObject.DestroyImmediate(skeletonAssetImportSettings, true);
                skeletonAssetImportSettings = null;
                
                if (animationSetAssetImportSettings != null)
                    GameObject.DestroyImmediate(animationSetAssetImportSettings, true);
                animationSetAssetImportSettings = null;
                
                if (basebodyAssetImportSettings != null)
                    GameObject.DestroyImmediate(basebodyAssetImportSettings, true);
                basebodyAssetImportSettings = null;
            }

            private void CreateImportSettings()
            {
                ClearAssetImportSettings();
                // 骨骼
                skeletonAssetImportSettings = ScriptableObject.CreateInstance<PaabAssetImportSettings>();
                skeletonAssetImportSettings.SetAssetTypeName(AssetImportSettingsType.Skeleton);
                skeletonAssetImportSettings.basicInfoSetting = new PaabBasicInfoImportSetting();
                skeletonAssetImportSettings.basicInfoSetting.SetAssetName(characterInfo.skeleton.name);
                skeletonAssetImportSettings.basicInfoSetting.characterFolderName = characterInfo.character.name;
                skeletonAssetImportSettings.basicInfoSetting.characterName = "";
                skeletonAssetImportSettings.basicInfoSetting.assetNickName = characterInfo.skeleton.show_name;
                skeletonAssetImportSettings.basicInfoSetting.assetIconPath = characterInfo.skeleton.cover;
                skeletonAssetImportSettings.opType = OperationType.Update;
                skeletonAssetImportSettings.hideFlags = HideFlags.DontSaveInEditor;
                var skeletonSetting = new PaabSkeletonImportSetting();
                skeletonAssetImportSettings.settingItems = new[]
                {
                    skeletonSetting
                };
                
                // 动画集
                animationSetAssetImportSettings = ScriptableObject.CreateInstance<PaabAssetImportSettings>();
                animationSetAssetImportSettings.SetAssetTypeName(AssetImportSettingsType.AnimationSet);
                animationSetAssetImportSettings.basicInfoSetting = new PaabBasicInfoImportSetting();
                animationSetAssetImportSettings.basicInfoSetting.SetAssetName(characterInfo.base_animation_set.name);
                animationSetAssetImportSettings.basicInfoSetting.characterFolderName = characterInfo.character.name;
                animationSetAssetImportSettings.basicInfoSetting.characterName = "";
                animationSetAssetImportSettings.basicInfoSetting.skeletonAssetName = characterInfo.skeleton.name;
                animationSetAssetImportSettings.basicInfoSetting.assetNickName = characterInfo.base_animation_set.show_name;
                animationSetAssetImportSettings.basicInfoSetting.assetIconPath = characterInfo.base_animation_set.cover;
                animationSetAssetImportSettings.opType = OperationType.Update;
                animationSetAssetImportSettings.hideFlags = HideFlags.DontSaveInEditor;

                var animationSetSetting = new PaabAnimationImportSetting();
                animationSetSetting.isBasicAnimationSet = true;
                animationSetAssetImportSettings.settingItems = new[]
                {
                    animationSetSetting
                };
                
                // 素体
                basebodyAssetImportSettings = ScriptableObject.CreateInstance<PaabAssetImportSettings>();
                basebodyAssetImportSettings.SetAssetTypeName(AssetImportSettingsType.BaseBody);
                basebodyAssetImportSettings.basicInfoSetting = new PaabBasicInfoImportSetting();
                basebodyAssetImportSettings.basicInfoSetting.SetAssetName(characterInfo.base_body.name);
                basebodyAssetImportSettings.basicInfoSetting.characterFolderName = CharacterUtil.NewCharacterTempName;
                basebodyAssetImportSettings.basicInfoSetting.characterName = "";
                basebodyAssetImportSettings.basicInfoSetting.skeletonAssetName = characterInfo.skeleton.name;
                basebodyAssetImportSettings.basicInfoSetting.assetNickName = characterInfo.base_body.show_name;
                basebodyAssetImportSettings.basicInfoSetting.assetIconPath = characterInfo.base_body.cover;
                basebodyAssetImportSettings.opType = OperationType.Update;
                basebodyAssetImportSettings.hideFlags = HideFlags.DontSaveInEditor;
            
                var componentSetting = new PaabComponentImportSetting();
                componentSetting.componentSource = PaabComponentImportSetting.ComponentSource.Custom;
                componentSetting.componentType = PaabComponentImportSetting.ComponentType.BaseBody;
                basebodyAssetImportSettings.settingItems = new[]
                {
                    componentSetting
                };
            }

            // private void RequestAssetInfo()
            // {
            //     var skeletonAssetRequest =
            //         AssetServerManager.instance.GetAssetDetailInfo(characterInfo.character.skeleton_id);
            //     
            //     skeletonAssetRequest.Send(success =>
            //         {
            //             var response = ProtocolUtil.GetResponse<SkeletonList>(success);
            //             if (response == null || response.data == null)
            //             {
            //                 Debug.LogError("Get response failed");
            //                 return;
            //             }
            //             else
            //                 UpdateSkeletonDropdownList(response.data);
            //         },
            //         failure => { Debug.Log("GetSkeletonListByApp...failed : " + failure.ToString()); });
            // }
           

#endregion
            
#endregion
        }
        
        // [Serializable]
        // public class UpdateCharacterData
        // {
        //     public string character_id;
        //     public CharacterBase character_base;
        //     public NewAssetData skeleton;
        //     public NewAssetData base_animation_set;
        //     public NewAssetData base_body;
        // }
    }
}
#endif