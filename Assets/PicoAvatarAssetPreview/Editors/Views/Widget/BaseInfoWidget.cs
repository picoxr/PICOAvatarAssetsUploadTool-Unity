#if UNITY_EDITOR
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Pico.AvatarAssetPreview.Protocol;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using ComponentType = Pico.AvatarAssetPreview.PaabComponentImportSetting.ComponentType;

namespace Pico.AvatarAssetPreview
{
    public enum BaseInfoType
    {
        AssetComponent,
        AssetBaseBody,
        AssetSkeleton,
        AssetBaseAnimationSet,
        AssetCustomAnimationSet,
        Preset,
        Character,
    }

    [Flags]
    public enum BaseInfoValueCheckResult
    {
        Success = 0,
        NameIsEmpty = 1,    // name为空
        ShowNameIsEmpty = 2,    // showname为空
        IconIsEmpty = 4,    // icon为空
        NameContainsInvalidCharacter = 8,   // 名字包含非法字符
        NameNotUnique = 16,      // 名字重名
        RequestFailed = 32,      // 请求失败
        NameTooLong = 64,        // name太长
        ShowNameTooLong = 128,   // showName太长
        IconTooLarge = 256,      // 图片太大
        IconExtentionNotMatch = 512,  // 图片类型不匹配
        ShowNameContainsInvalidCharacter = 1024,   // showName包含非法字符
    }
    

    [Serializable]
    public class CheckNameResult
    {
        public int item_type;
        public string name;
        public bool is_uniq;
    }

    [Serializable]
    public class BaseInfoWidgetRestoreData
    {
        public string name;
        public string showName;
        public string icon;

        public void Clear()
        {
            name = "";
            showName = "";
            icon = "";
        }
    }
    
    internal partial class BaseInfoWidget : PavWidget
    {
        private TextField nameTextField;
        private TextField showNameTextField;
        private TextField iconTextField;
        private Button iconViewFolderBtn;
        private VisualElement iconImg;
        private VisualElement iconText;
        private WebImage iconImage;
        private VisualElement nameTip;
        private VisualElement showNameTip;
        private VisualElement iconTip;
        private VisualElement iconRegion;

        private TextFieldWithPlaceHolder nameTextFieldWithPlaceHolder;
        private TextFieldWithPlaceHolder showNameTextFieldWithPlaceHolder;
        private TextFieldWithPlaceHolder iconTextFieldWithPlaceHolder;
        private DateTime nameCheckTime = DateTime.MinValue;
        private bool isCheckName = false;

        public Action<string> onNameChanged;
        public Action<string> onShowNameChanged;
        public Action<string> onIconChanged;

        public BaseInfoType baseInfoType { get; set; }
        
        public const string NamePlaceholder = "(Required, only supports numbers, letters, underscores)";
        public const string ShowNamePlaceholder = "(Required, only supports numbers, letters, spaces, hyphens, underscores, Chinese)";
        public const string CharacterIconPlaceholder = "(Required, ratio 3:4, height & width ≤4096 px, png)";
        public const string AssetIconPlaceholder = "(Required, ratio 1:1, height & width ≤4096 px, png)";

        //private const string nameCheckPattern = @"^(?![.])([0-9a-zA-Z-][.]?)*(?<![.])$";
        private const string nameCheckPattern = @"^[0-9a-zA-Z_]*$";
        private const string showNameCheckPattern = "^(?![ ])[-0-9a-zA-Z_ \u4e00-\u9fa5]*(?<![ ])$";
        private const int maxNameLength = 100;
        private const int maxShowNameLength = 100;
        private const int maxIconWidth = 4096;
        private const int maxIconHeight = 4096;
        private const int maxIconSize = 10 * 1024 * 1024;
        private const float nameReadonlyAlpha = 0.08f;
        private const float nameNormalAlpha = 0.16f;

        private bool nameReadOnly;
        
        public override string uxmlPathName { get => "UxmlWidget/BaseinfoWidget.uxml"; }

        public BaseInfoWidget(VisualElement ve, BaseInfoType bit) : base(ve)
        {
            baseInfoType = bit;
            nameReadOnly = false;
        }
        
        public BaseInfoWidget(VisualElement ve, BaseInfoType bit, bool nameReadOnly) : base(ve)
        {
            baseInfoType = bit;
            this.nameReadOnly = nameReadOnly;
        } 
        
        public BaseInfoWidget(BaseInfoType bit) : base()
        {
            baseInfoType = bit;
        }

        public string Name => nameTextField.value;
        
        public string ShowName => showNameTextField.value;

        public string Icon => iconTextField.value;

        public bool NameReadOnly
        {
            get { return nameReadOnly; }
            set
            {
                nameReadOnly = value;
                UpdateNameEditable();
            }
        }

        public TextField NameTextField => nameTextField;
        public TextField ShowNameTextField => showNameTextField;
        public TextField IconTextField => iconTextField;

        private string serverCheckedName = null;
        private BaseInfoValueCheckResult serverCheckResult;


        private float iconTextDefaultPaddingLeft = 34;
        private float iconTextHideIconPaddingLeft = 12;
        

#region Public Methods

        public static List<string> GetBaseInfoErrorMessage(BaseInfoValueCheckResult result)
        {
            List<string> messages = new List<string>();
            if (result.HasFlag(BaseInfoValueCheckResult.NameIsEmpty))
                messages.Add("Name is empty");
            
            if (result.HasFlag(BaseInfoValueCheckResult.NameNotUnique))
                messages.Add("Name is not unique");
            
            if (result.HasFlag(BaseInfoValueCheckResult.NameContainsInvalidCharacter))
                messages.Add("Name contains invalid characters, it only supports numbers, letters, underscores");
            
            if (result.HasFlag(BaseInfoValueCheckResult.ShowNameIsEmpty))
                messages.Add("ShowName is empty");
            
            if (result.HasFlag(BaseInfoValueCheckResult.ShowNameContainsInvalidCharacter))
                messages.Add("ShowName contains invalid characters, it only supports numbers, letters, spaces, hyphens, underscores, Chinese");
            
            if (result.HasFlag(BaseInfoValueCheckResult.IconIsEmpty))
                messages.Add("Icon is empty");

            if (result.HasFlag(BaseInfoValueCheckResult.RequestFailed))
                messages.Add("Network error");
            
            if (result.HasFlag(BaseInfoValueCheckResult.NameTooLong))
                messages.Add("Name too long");
            
            if (result.HasFlag(BaseInfoValueCheckResult.ShowNameTooLong))
                messages.Add("ShowName too long");
            
            if (result.HasFlag(BaseInfoValueCheckResult.IconTooLarge))
                messages.Add("Icon too large");
            
            if (result.HasFlag(BaseInfoValueCheckResult.IconExtentionNotMatch))
                messages.Add("Icon is not png");

            return messages;
        }


        public BaseInfoWidgetRestoreData GetRestoreData()
        {
            BaseInfoWidgetRestoreData data = new BaseInfoWidgetRestoreData();
            data.name = Name;
            data.showName = ShowName;
            data.icon = Icon;

            return data;
        }

        /// <summary>
        /// 清空内容
        /// </summary>
        public void Clear()
        {
            nameTextField.value = "";
            showNameTextField.value = "";
            SetIconByPath("");
        }

        public void Restore(BaseInfoWidgetRestoreData data)
        {
            SetInputValue(data.name, data.showName, data.icon);
        }

        public void CheckValue(Action<BaseInfoValueCheckResult> onCheckFinish, bool updateWidgetErrorStatus = true)
        {
            if (IsCheckName())
                return;
            
            var localCheckResult = LocalNameCheck();
            if (nameReadOnly)
            {
                onCheckFinish?.Invoke(localCheckResult);
                if (updateWidgetErrorStatus)
                    CheckWidgetErrorStatus(serverCheckResult);
                return;
            }

            if (string.IsNullOrEmpty(NameTextField.value))
            {
                serverCheckedName = null;
                onCheckFinish?.Invoke(localCheckResult);
                if (updateWidgetErrorStatus)
                    CheckWidgetErrorStatus(localCheckResult);
                return;
            }
            
            if (serverCheckedName == NameTextField.value)
            {
                onCheckFinish?.Invoke(serverCheckResult | localCheckResult);
                if (updateWidgetErrorStatus)
                    CheckWidgetErrorStatus(localCheckResult | serverCheckResult);
                return;
            }
            
            serverCheckedName = null;
            isCheckName = true;
            nameCheckTime = DateTime.Now;
            var request = AssetServerManager.instance.CheckAssetName(NameTextField.value, (long)baseInfoType);
            request.Send(success =>
                {
                    Debug.Log($"Check name result : {success}");
                    ResetNameCheckState();
                    var response = ProtocolUtil.GetResponse<CheckNameResult>(success);
                    if (response == null || response.data == null)
                    {
                        serverCheckedName = null;
                        serverCheckResult = BaseInfoValueCheckResult.RequestFailed;
                    }
                    else
                    {
                        if (NameTextField.value != response.data.name)
                        {
                            Debug.LogWarning($"Name not match : {NameTextField.value} != {response.data.name}");
                            return;
                        }
                        
                        serverCheckedName = response.data.name;
                        if (response.data.is_uniq)
                            serverCheckResult = BaseInfoValueCheckResult.Success;
                        else
                            serverCheckResult = BaseInfoValueCheckResult.NameNotUnique;
                    }
                    
                    onCheckFinish?.Invoke(localCheckResult | serverCheckResult);
                    if (updateWidgetErrorStatus)
                        CheckWidgetErrorStatus(localCheckResult | serverCheckResult);
                },
                failure =>
                {
                    Debug.LogError("Check name...failed : " + failure.ToString());
                    ResetNameCheckState();
                    serverCheckedName = null;
                    serverCheckResult = BaseInfoValueCheckResult.RequestFailed;
                    onCheckFinish?.Invoke(localCheckResult | serverCheckResult);
                });
        }

        public void ResetError()
        {
            nameTextFieldWithPlaceHolder.ResetError();
            showNameTextFieldWithPlaceHolder.ResetError();
            iconTextFieldWithPlaceHolder.ResetError();
        }
        
        public void SetInputValue(string name, string showName, string iconPath)
        {
            nameTextField.value = name;
            showNameTextField.value = showName;
            SetIconByPath(iconPath);
        }


        public override VisualElement BuildUIDOM()
        {
            var root = base.BuildUIDOM();
            
            nameTextField = root.Q<TextField>("nameTextField");
            nameTextField.isDelayed = true;
            showNameTextField = root.Q<TextField>("showNameTextField");
            iconTextField = root.Q<TextField>("iconTextField");
            iconRegion = root.Q("iconRegion");
            iconViewFolderBtn = root.Q<Button>("viewFolderBtn");
            iconImg = root.Q("thumbnail");
            nameTip = root.Q("nameTip");
            showNameTip = root.Q("showNameTip");
            iconTip = root.Q("iconTip");
            
            iconText = iconTextField.FindChildRecursive("unity-text-input");

            iconImage = new WebImage(iconImg);
            if (baseInfoType == BaseInfoType.Character)
                iconImage.SetAspect(UIUtils.DefaultCharacterIconAspect);
            else
                iconImage.SetAspect(UIUtils.DefaultAssetIconAspect);
            iconImage.onTextureLoad += b =>
            {
                iconImage.SetActive(b);
                SetIconText(iconImage.URL);
                // if (b)
                //     SetIconText(iconImage.URL);
                // else
                //     SetIconText("");
            };

            nameTextFieldWithPlaceHolder = new TextFieldWithPlaceHolder(nameTextField, NamePlaceholder);
            showNameTextFieldWithPlaceHolder = new TextFieldWithPlaceHolder(showNameTextField, ShowNamePlaceholder);
            if (baseInfoType == BaseInfoType.Character)
                iconTextFieldWithPlaceHolder = new TextFieldWithPlaceHolder(iconTextField, CharacterIconPlaceholder);
            else
                iconTextFieldWithPlaceHolder = new TextFieldWithPlaceHolder(iconTextField, AssetIconPlaceholder);

            nameTextFieldWithPlaceHolder.DoubleClickSelectAll = true;
            showNameTextFieldWithPlaceHolder.DoubleClickSelectAll = true;
            UIUtils.AddVisualElementHoverMask(nameTextField, nameTextField);
            UIUtils.AddVisualElementHoverMask(showNameTextField, showNameTextField);
            UIUtils.AddVisualElementHoverMask(iconRegion, iconRegion, true);
            UIUtils.SetTipText(nameTip, StringTable.GetString("NameTip"));
            UIUtils.SetTipText(showNameTip, StringTable.GetString("ShowNameTip"));
            UIUtils.SetTipText(iconTip, StringTable.GetString("IconTip"));
            
            SetIconByPath("");
            UpdateNameEditable();

            return root;
        }


        public override bool BindUIActions()
        {
            if(!base.BindUIActions())
            {
                return false;
            }

            iconViewFolderBtn.RegisterCallback<ClickEvent>(OnIconViewFolderBtnClick);
            iconRegion.RegisterCallback<ClickEvent>(OnIconViewFolderBtnClick);
            nameTextField.RegisterValueChangedCallback(OnNameTextFieldValueChange);
            showNameTextField.RegisterValueChangedCallback(OnShowNameTextFieldValueChange);

            return true;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            iconImage.OnDestroy();
            iconViewFolderBtn.UnregisterCallback<ClickEvent>(OnIconViewFolderBtnClick);
            nameTextField.UnregisterValueChangedCallback(OnNameTextFieldValueChange);
            showNameTextField.UnregisterValueChangedCallback(OnShowNameTextFieldValueChange);
            onNameChanged = null;
            onShowNameChanged = null;
            onIconChanged = null;
        }

        public void AutoSetName(string characterName, ComponentType componentType = ComponentType.Invalid, bool forceSet = false)
        {
            if (string.IsNullOrEmpty(characterName))
                return;
            
            var currentName = Name;
            if (!string.IsNullOrEmpty(currentName) && !forceSet)
            {
                // var segments = currentName.Split("_");
                // if (!forceSet && (segments.Length >= 1 && segments[0] == characterName))
                return;
            }

            string autoName = "";
            var postfix = GetAutoNamePostfix();
            switch (baseInfoType)
            {
                case BaseInfoType.AssetSkeleton:
                case BaseInfoType.AssetBaseAnimationSet:
                case BaseInfoType.AssetCustomAnimationSet:
                case BaseInfoType.AssetBaseBody:
                    autoName = $"{characterName}_{baseTypeNameString[baseInfoType]}{postfix}";
                    break;
                case BaseInfoType.AssetComponent:
                    autoName = $"{characterName}_{componentTypeNameString[componentType]}{postfix}";
                    break;
            }

            if (autoName.Length > maxNameLength)
                return;

            nameTextField.value = autoName;
        }

        public string GetAutoNamePostfix()
        {
            var dateTime = DateTime.Now;
            var dateTimeString = dateTime.ToString("yyyyMMddHHmmss");
            return dateTimeString;
        }

        public void AutoSetIconIfNotSet(ComponentType componentType = ComponentType.Invalid)
        {
            if (!iconImage.IsEmpty())
                return;
            
            string iconPath = "";
            
            if (BaseIcons.ContainsKey(baseInfoType))
                iconPath = BaseIcons[baseInfoType];
            else if (ComponentIcons.ContainsKey(componentType))
                iconPath = ComponentIcons[componentType];
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            SetIconByPath(Path.Combine("file://" + DefaultAssetIconPath, iconPath));
#else
            SetIconByPath(Path.Combine(DefaultAssetIconPath, iconPath));
#endif
        }
        
        private const string DefaultAssetIconRelativePath = "PicoAvatarAssetPreview/Assets/Icon/AssetDefaultIcon";
        private static string DefaultAssetIconPath => Path.Combine(Application.dataPath, DefaultAssetIconRelativePath);
        
        private static readonly Dictionary<BaseInfoType, string> baseTypeNameString = new ()
        {
            { BaseInfoType.AssetBaseBody, "BaseBody" },
            { BaseInfoType.AssetSkeleton, "Skeleton" },
            { BaseInfoType.AssetBaseAnimationSet, "BaseAnimation" },
            { BaseInfoType.AssetCustomAnimationSet, "CustomAnimation" },
        };
        
        private static readonly Dictionary<ComponentType, string> componentTypeNameString = new ()
        {
            { ComponentType.Invalid, "" },
            { ComponentType.Hair, "Hair" },
            { ComponentType.ClothTop, "Top" },
            { ComponentType.ClothBottom, "Bottom" },
            { ComponentType.ClothShoes, "Shoes" },
            { ComponentType.ClothSocks, "Socks" },
            { ComponentType.ClothGloves, "Gloves" },
            { ComponentType.ClothHood, "Hood" },
            { ComponentType.AccessoryHeaddress, "Headdress" },
            { ComponentType.AccessoryMask, "Mask" },
            { ComponentType.AccessoryNecklace, "Necklace" },
            { ComponentType.AccessoryBracelet, "Bracelet" },
            { ComponentType.AccessoryArmguard, "Armguard" },
            { ComponentType.AccessoryShoulderknot, "Shoulderknot" },
            { ComponentType.AccessoryLegRing, "LegRing" },
            { ComponentType.AccessoryProp, "Prop" },
            { ComponentType.Head, "Head" },
            { ComponentType.Body, "Body" },
            { ComponentType.Hand, "Hand" },
        };

        
        private static readonly Dictionary<BaseInfoType, string> BaseIcons = new Dictionary<BaseInfoType, string>()
        {
            { BaseInfoType.Character, "character_icon.png" },
            { BaseInfoType.AssetSkeleton, "skeleton_icon.png" },
            { BaseInfoType.AssetBaseBody, "base_body_icon.png" },
            { BaseInfoType.AssetBaseAnimationSet, "base_anim_icon.png" },
            { BaseInfoType.AssetCustomAnimationSet, "custom_anim_icon.png" },
        };
        
        private static readonly Dictionary<ComponentType, string> ComponentIcons = new Dictionary<ComponentType, string>()
        {
            { ComponentType.Invalid, "component_icon.png" },
            { ComponentType.BaseBody, "component_icon.png" },
            { ComponentType.Head, "component_icon.png" },
            { ComponentType.Body, "component_icon.png" },
            { ComponentType.Hand, "component_icon.png" },
            { ComponentType.Hair, "component_icon.png" },
            { ComponentType.ClothTop, "component_icon.png" },
            { ComponentType.ClothBottom, "component_icon.png" },
            { ComponentType.ClothShoes, "component_icon.png" },
            { ComponentType.ClothSocks, "component_icon.png" },
            { ComponentType.ClothGloves, "component_icon.png" },
            { ComponentType.ClothHood, "component_icon.png" },
            { ComponentType.AccessoryHeaddress, "component_icon.png" },
            { ComponentType.AccessoryMask, "component_icon.png" },
            { ComponentType.AccessoryNecklace, "component_icon.png" },
            { ComponentType.AccessoryBracelet, "component_icon.png" },
            { ComponentType.AccessoryArmguard, "component_icon.png" },
            { ComponentType.AccessoryShoulderknot, "component_icon.png" },
            { ComponentType.AccessoryLegRing, "component_icon.png" },
            { ComponentType.AccessoryProp, "component_icon.png" },
        }; 
        

#endregion

#region Private Methods

        private void UpdateNameEditable()
        {
            if (nameTextField == null)
                return;

            nameTextField.isReadOnly = nameReadOnly;
            Color c = nameTextField.resolvedStyle.backgroundColor;
            if (nameReadOnly)
                c.a = nameReadonlyAlpha;
            else
                c.a = nameNormalAlpha;
            
            // 临时关闭所有编辑
            // showNameTextField.isReadOnly = nameReadOnly;
            

            nameTextField.style.backgroundColor = c;
            //showNameTextField.style.backgroundColor = c;
            
            //iconViewFolderBtn.SetActive(!nameReadOnly);
        }

        private void CheckWidgetErrorStatus(BaseInfoValueCheckResult checkResult)
        {
            if (checkResult.HasFlag(BaseInfoValueCheckResult.NameIsEmpty) || checkResult.HasFlag(BaseInfoValueCheckResult.NameContainsInvalidCharacter) ||
                    checkResult.HasFlag(BaseInfoValueCheckResult.NameNotUnique) || checkResult.HasFlag(BaseInfoValueCheckResult.NameTooLong))
                nameTextFieldWithPlaceHolder.ShowErrorOnce();
            
            if (checkResult.HasFlag(BaseInfoValueCheckResult.ShowNameIsEmpty) || checkResult.HasFlag(BaseInfoValueCheckResult.ShowNameTooLong) || checkResult.HasFlag(BaseInfoValueCheckResult.ShowNameContainsInvalidCharacter))
                showNameTextFieldWithPlaceHolder.ShowErrorOnce();
            
            if (checkResult.HasFlag(BaseInfoValueCheckResult.IconIsEmpty) || checkResult.HasFlag(BaseInfoValueCheckResult.IconTooLarge) || checkResult.HasFlag(BaseInfoValueCheckResult.IconExtentionNotMatch))
                iconTextFieldWithPlaceHolder.ShowErrorOnce();
        }

        private void SetIconByPath(string path)
        {
            //iconImg.SetActive(false);
            if (!string.IsNullOrEmpty(path))
            {
                iconImage.SetTexture(path, ImageFileExtension.PNG);
            }
            else
            {
                SetIconText("");
                iconImage.ClearTexture();
                iconImage.SetActive(false);
            } 
        }
        

#region UI控件事件回调函数
        
        private void OnIconViewFolderBtnClick(ClickEvent evt)
        {
            // if (nameReadOnly)
            //     return;
            
            var pathStr = EditorUtility.OpenFilePanel("Select Icon", "Assets", "png");
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            if (!string.IsNullOrEmpty(pathStr))
                pathStr = $"file://{pathStr}";
#endif
            SetIconByPath(pathStr);
            //SetIconText(pathStr);
        }
        
        private void OnNameTextFieldValueChange(ChangeEvent<string> evt)
        {
            // if (ContainsInvalidCharacter(evt.newValue))
            //     nameTextField.SetValueWithoutNotify(evt.previousValue);
            
            // CheckValue(null, false);
            
            onNameChanged?.Invoke(evt.newValue);
        }
            
        private void OnShowNameTextFieldValueChange(ChangeEvent<string> evt)
        {
            // if (ContainsInvalidCharacter(evt.newValue))
            //     showNameTextField.SetValueWithoutNotify(evt.previousValue);
        }

        private BaseInfoValueCheckResult LocalNameCheck()
        {
            BaseInfoValueCheckResult result = BaseInfoValueCheckResult.Success;
            
            //TODO 如果中途改规则 要看一下是否会影响线上
            if (string.IsNullOrEmpty(NameTextField.value))
            {
                result |= BaseInfoValueCheckResult.NameIsEmpty;
            }
            else
            {
                if (ContainsInvalidCharacter(NameTextField.value, nameCheckPattern))
                {
                    result |= BaseInfoValueCheckResult.NameContainsInvalidCharacter;
                }
            
                if (NameTextField.value.Length > maxNameLength)
                {
                    result |= BaseInfoValueCheckResult.NameTooLong;
                }
            }
            
            
            if (string.IsNullOrEmpty(ShowNameTextField.value))
            {
                result |= BaseInfoValueCheckResult.ShowNameIsEmpty;
            }
            else
            {
                if (ContainsInvalidCharacter(ShowNameTextField.value, showNameCheckPattern))
                {
                    result |= BaseInfoValueCheckResult.ShowNameContainsInvalidCharacter;
                }
                
                if (ShowNameTextField.value.Length > maxShowNameLength)
                {
                    result |= BaseInfoValueCheckResult.ShowNameTooLong;
                }
            }
            
            
            if (string.IsNullOrEmpty(IconTextField.value))
            {
                result |= BaseInfoValueCheckResult.IconIsEmpty;
            }

            if (iconImage.status == WebImageStatus.Failed || iconImage.status == WebImageStatus.ExtensionNotMatch)
            {
                result |= BaseInfoValueCheckResult.IconExtentionNotMatch;
            }
            
            
            if (iconImage.texture != null && (iconImage.texture.width >= maxIconWidth ||
                                              iconImage.texture.height >= maxIconHeight ||
                                              iconImage.textureFileSize > maxIconSize))
            {
                result |= BaseInfoValueCheckResult.IconTooLarge;
            }

            return result;
        }


        private void ResetNameCheckState()
        {
            isCheckName = false;
            nameCheckTime = DateTime.MinValue;
        }

        private bool IsCheckName()
        {
            if ((DateTime.Now - nameCheckTime).TotalSeconds > 5)
            {
                ResetNameCheckState();
                return false;
            }

            return true;
        }

#endregion
        
        private void SetIconText(string value)
        {
            iconTextField.value = value;
            if (string.IsNullOrEmpty(value))
                iconTextField.style.paddingLeft = iconTextHideIconPaddingLeft;
            else
                iconTextField.style.paddingLeft = iconTextDefaultPaddingLeft;
        }

        private bool ContainsInvalidCharacter(string text, string pattern)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            
            char[] c = text.ToCharArray();
            if (!System.Text.RegularExpressions.Regex.IsMatch(text, pattern))
            {
                return true;
            }

            return false;
        }
        
        

#endregion
    }
}
#endif