#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using Pico.Avatar;
using Pico.AvatarAssetBuilder;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class UploadAssetsPanel : PavPanel
        {
            public enum SourceType : byte
            {
                CreateSkeleton = 0,
                CreateAnimationSet,
                CreateBaseBody,
                CreateComponent,
                CreateCustomAnimationSet,
                CreateHair,
                CreateShoe,
                CreateClothes,
                UpdateSkeleton,
                UpdateAnimationSet,
                UpdateBaseBody,
                UpdateComponent,
                UpdateCustomAnimationSet,
                UpdateHair,
                UpdateShoe,
                UpdateClothes,
                CreateCharacter,
                UpdateCharacter
            }  

            public override string displayName
            {
                get => title;
            }
            public override string panelName
            {
                get => "UploadAssets";
            }
            public override string uxmlPathName
            {
                get => "Uxml/UploadAssetsPanel.uxml";
            }

            private static UploadAssetsPanel _instance;

#region property

            private const string OkName = "Upload-Assets-OK";
            private const string CancelName = "Upload-Assets-Cancel";
            private const string AssetName = "Upload-Assets-Name";
            private const string AssetShowName = "Upload-Assets-ShowName";
            private const string IconImageName = "Upload-Assets-Icon";
            private const string IconShowName = "Upload-Assets-IconName";

            private Button _okBtn;
            private Button _cancelBtn;

            private Label _nameLabel;
            private Label _showNameLabel;
            private Label _iconNameLabel;

            private WebImage _iconWebImage;
            private VisualElement _icon;

            private string title = "UploadAssets";

#endregion

            public static UploadAssetsPanel instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<UploadAssetsPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/UploadAssetsPanel.asset");
                    }

                    return _instance;
                }
            }

            private CharacterData _panelData;
            private PaabAssetImportSettings _settingData;
            private PanelType _panelType;
            private OperationType _opType;
            public SourceType sourceType => _sourceType;
            private SourceType _sourceType;
            private AssetImportSettingsType _settingsType;

            public bool WebImageDownloadSucc = false;
            protected override bool BuildUIDOM(VisualElement parent) //SetVisualElements and BuildWithUxml
            {
                base.BuildUIDOM(parent);
                if (mainElement != null)
                {
                    _okBtn = mainElement.Q<Button>(OkName);
                    _cancelBtn = mainElement.Q<Button>(CancelName);
                    _nameLabel = mainElement.Q<Label>(AssetName);
                    _showNameLabel = mainElement.Q<Label>(AssetShowName);
                    _iconNameLabel = mainElement.Q<Label>(IconShowName);
                    _icon = mainElement.Q<VisualElement>(IconImageName);
                }

                return true;
            }

            protected override  bool BindUIActions(params object[] initParams)
            {
                _okBtn?.RegisterCallback<ClickEvent>(UploadCharacter);
                _cancelBtn?.RegisterCallback<ClickEvent>(CancelUploadCharacter);
                if (initParams is not { Length: > 1 })
                    return false;

                string targetName = string.Empty,
                    targetShowName = string.Empty,
                    targetIconUrl = string.Empty,
                    targetOkText = string.Empty;
                
                _panelData = initParams[0] as CharacterData;
             
                if (_panelData == null)
                {
                    _settingData = initParams[0] as PaabAssetImportSettings;
                    if (_settingData != null)
                    {
                        _opType = _settingData.opType;
                        _settingsType = _settingData.assetImportSettingsType;
                        RecordSourceFromType(_opType, _settingsType);
                        targetName = _settingData.basicInfoSetting?.assetName;
                        targetShowName = _settingData.basicInfoSetting?.assetNickName;
                        targetIconUrl = _settingData.basicInfoSetting?.assetIconPath;
                        targetOkText = (sourceType is SourceType.CreateBaseBody or SourceType.CreateSkeleton
                            or SourceType.CreateAnimationSet)?"Finish":((sourceType is SourceType.UpdateBaseBody 
                            or SourceType.UpdateSkeleton or SourceType.UpdateAnimationSet)?"Finish":"Upload");
                        if (sourceType is SourceType.CreateBaseBody or SourceType.CreateSkeleton or SourceType.CreateAnimationSet)
                            targetOkText = "Finish";
                        else if (sourceType is SourceType.UpdateBaseBody or SourceType.UpdateSkeleton or SourceType.UpdateAnimationSet)
                            targetOkText = "Finish";
                        else if (sourceType is SourceType.CreateComponent or SourceType.CreateCustomAnimationSet)
                            targetOkText = "Upload";
                        else 
                            targetOkText = "Update";

                        if (sourceType is SourceType.CreateSkeleton)
                            title = "Upload Skeleton";
                        else if (sourceType is SourceType.UpdateSkeleton)
                            title = "Update Skeleton";
                        else if (sourceType is SourceType.CreateAnimationSet or SourceType.CreateCustomAnimationSet)
                            title = "Upload AnimationSet";
                        else if (sourceType is SourceType.UpdateAnimationSet or SourceType.UpdateCustomAnimationSet)
                            title = "Update AnimationSet";
                        else if (sourceType is SourceType.CreateBaseBody)
                            title = "Upload Base Body";
                        else if (sourceType is SourceType.UpdateBaseBody)
                            title = "Update Base Body";
                        else if (sourceType is SourceType.CreateComponent)
                            title = "Upload Component";
                        else if (sourceType is SourceType.UpdateComponent)
                            title = "Update Component";
                        else
                            title = "UploadAssets";
                    }
                }
                else
                {
                    _panelType = (PanelType)initParams[1];
                    RecordSourceFromType(_panelType);
                    targetName = _panelData.character_base?.name;
                    targetShowName = _panelData.character_base?.show_name;
                    targetIconUrl = _panelData.character_base?.cover;
                    targetOkText = _panelType == PanelType.UpdateCharacter ? "Update" : "Upload";
                    if (_panelType == PanelType.ConfigureNewCharacter)
                        title = "Upload Avatar";
                    else if (_panelType == PanelType.UpdateCharacter)
                        title = "Update Avatar";
                    else
                        title = "UploadAssets";
                }
                
                var okText = targetOkText;
                if (_okBtn != null) _okBtn.text = okText;
                _showNameLabel.text = targetShowName;
                _nameLabel.text = targetName;
                _iconNameLabel.text = "";
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                targetIconUrl = targetIconUrl?.Replace("file:///", "/");
#endif
                if (File.Exists(targetIconUrl))
                {
                    WebImageDownloadSucc = true;
                    byte[] iconByte = File.ReadAllBytes(targetIconUrl);
                    Texture2D tex = new Texture2D(32, 32);
                    tex.LoadImage(iconByte);
                    _icon.style.backgroundImage = tex;
                    var iconPath = targetIconUrl.Split("/");
                    _iconNameLabel.text = iconPath[^1];
                }
                else
                {
                    WebImageDownloadSucc = false;
                    if (_iconImage?.texture != null)
                    {
                        WebImageDownloadSucc = true;
                        _icon.style.backgroundImage = _iconImage.texture;
                    }
                    else
                    {
                        DownloadHttpPicture(targetIconUrl);
                    }
                  
                }

                return base.BindUIActions(initParams);
            }

            private WebImage _iconImage;
            private void DownloadHttpPicture(string url)
            {
                if (_iconImage?.texture != null)
                    return;
                _iconImage = new WebImage(new VisualElement());
                _iconImage.ClearTexture();
                _iconImage.SetActive(false);
                _iconImage.SetTexture(url, ImageFileExtension.PNG);
                _iconImage.onTextureLoad += b =>
                {
                    //_iconImage.SetActive(b);
                    WebImageDownloadSucc = true;
                };
            }
            
            public void PreloadData(params object[] initParams)
            {
                if (initParams.Length < 1 || _iconImage?.texture != null)
                    return;
                _panelData = initParams[0] as CharacterData;
                string targetIconUrl = String.Empty;
                if (_panelData == null)
                {
                    _settingData = initParams[0] as PaabAssetImportSettings;
                    if (_settingData != null)
                    {
                        targetIconUrl = _settingData.basicInfoSetting?.assetIconPath;
                    }
                }
                else
                {
                    targetIconUrl = _panelData.character_base?.cover;
                }
                
                if (!File.Exists(targetIconUrl))
                {
                    WebImageDownloadSucc = false;
                    DownloadHttpPicture(targetIconUrl);
                }
                else
                {
                    WebImageDownloadSucc = true;
                }
            }

            private CommonHttpRequest _commonHttpRequest;
          

            private void RecordSourceFromType(OperationType opType, AssetImportSettingsType aisType)
            {
                switch (aisType)
                {
                    case AssetImportSettingsType.Unknown:
                        break;
                    case AssetImportSettingsType.Skeleton:
                        _sourceType = opType == OperationType.Create
                            ? SourceType.CreateSkeleton
                            : SourceType.UpdateSkeleton;
                        break;
                    case AssetImportSettingsType.AnimationSet:
                        {
                            if (opType == OperationType.Create)
                            {
                                _sourceType = SourceType.CreateAnimationSet;
                            }
                            else  if (opType == OperationType.CreateAsset)
                            {
                                _sourceType = SourceType.CreateCustomAnimationSet;
                            }
                            else  if (opType == OperationType.Update)
                            {
                                _sourceType = SourceType.UpdateAnimationSet;
                            }
                            else  if (opType == OperationType.UpdateAsset)
                            {
                                _sourceType = SourceType.UpdateCustomAnimationSet;
                            }
                        }
                       
                        break;
                    case AssetImportSettingsType.Clothes:
                        _sourceType = opType == OperationType.CreateAsset
                            ? SourceType.CreateClothes
                            : SourceType.UpdateClothes;
                        break;
                    case AssetImportSettingsType.Hair:
                        _sourceType = opType == OperationType.CreateAsset
                            ? SourceType.CreateHair
                            : SourceType.UpdateHair;
                        break;
                    case AssetImportSettingsType.Shoe:
                        _sourceType = opType == OperationType.CreateAsset
                            ? SourceType.CreateShoe
                            : SourceType.UpdateShoe;
                        break;
                    case AssetImportSettingsType.Character:
                        break;
                    case AssetImportSettingsType.BaseBody:
                        _sourceType = opType == OperationType.Create
                            ? SourceType.CreateBaseBody
                            : SourceType.UpdateBaseBody;
                        break;
                    case AssetImportSettingsType.Component:
                        _sourceType = opType == OperationType.CreateAsset
                            ? SourceType.CreateComponent
                            : SourceType.UpdateComponent;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(aisType), aisType, null);
                }
            }

            private void RecordSourceFromType(PanelType panelType)
            {
                if (panelType == PanelType.ConfigureNewCharacter)
                {
                    _sourceType = SourceType.CreateCharacter;
                }
                else if (panelType == PanelType.UpdateCharacter)
                {
                    _sourceType = SourceType.UpdateCharacter;
                }
            }

            private void UploadCharacter(ClickEvent evt)
            {
                if (sourceType is SourceType.CreateCharacter or SourceType.UpdateCharacter)
                {
                    AssetUploadManager.instance.CreateOrUpdateCharacter(_panelData, _sourceType);
                    OnDestroy();
                    ((EditorWindow)panelContainer)?.Close();
                }
                else if (sourceType is SourceType.CreateSkeleton or SourceType.CreateAnimationSet 
                         or SourceType.CreateBaseBody)
                {
                    //simulation nav btn -- spec rules don't modify
                    if (NavMenuBarRoute.isValid)
                    {
                        NavMenuBarRoute.instance.NavButtonClick("CharacterPanel", "ConfigureNewCharacter");
                    }
                    OnDestroy();
                }
                else if (sourceType is SourceType.UpdateSkeleton or SourceType.UpdateAnimationSet
                    or SourceType.UpdateBaseBody)
                {
                    if (NavMenuBarRoute.isValid)
                        NavMenuBarRoute.instance.NavButtonClick("CharacterPanel", "UpdateCharacter");
                    OnDestroy();
                }
                else
                {
                    AssetUploadManager.instance.CreateOrUpdateCharacterPart(_settingData, _sourceType);
                    OnDestroy();
                    ((EditorWindow)panelContainer)?.Close();
                }
            }

            private void CancelUploadCharacter(ClickEvent evt)
            {
                OnDestroy();
                ((EditorWindow)panelContainer)?.Close();
            }


            public override void OnDestroy()
            {
                base.OnDestroy();
                if (_icon != null) _icon.style.backgroundImage = null;
                if (_iconImage != null)
                {
                    _iconImage.ClearTexture();
                    _iconImage = null;
                }
                _instance = null;
            }

        }
    }
}
#endif