#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pico.Avatar;
using Pico.Avatar.AvatarAssetBuilder;
using Object = UnityEngine.Object;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class ConfigureComponentPanel : AssetImportSettingsPanel
        {
            enum ErrorType
            {
                None,
                Global,
                BodyType,
                GameObject,
                Material,
            }

            class RendererItem
            {
                public int lodIndex = -1;
                public GroupBox lodGroup;
                public VisualElement item;
                public Renderer renderer;
                public Material officialMaterial;
                public OfficialShaderTheme OfficialShaderTheme = OfficialShaderTheme.PicoPBR;
                public string shaderType1; // eg:(official PBR or NPR)
                public string shaderType2; // eg:(official shader body , hair , cloth)
                public Material customMaterial;
                public string checkResult;
                public ErrorType errorType;
                public CommonDialogWindow.CheckStatus status;
            }

            private class CheckContext
            {
                public List<string> globalResults = new List<string>();
                public bool showWaningPanel = true;
                public int errorCount = 0;
                public int warningCount = 0;
                public System.Action onCheckFinish = null;
            }

            private enum WorkState
            {
                Init,
                Converting,
                MoveTemp,
                Finish,
            }

            

            private class ConvertWork
            {
                public int lod;
                public string name;
                public SkinnedMeshRenderer[] skins;
                public Material[] officialMaterials;
                public Material[] customMaterials;
                public string zipPath;
                public string extrasJson;
                public WorkState state;
            }

            private static ConfigureComponentPanel _instance;
            const int LOD_GROUP_COUNT = 3;
            const int CHECK_LOD1_TRIANGLE_COUNT = 25000;
            const int CHECK_LOD2_TRIANGLE_COUNT = 15000;
            private PaabComponentImportSetting _setting;
            private GameObject _skeleton;
            private BaseInfoWidget _baseInfoWidget = null;
            private bool _previewCustom;
            private List<List<RendererItem>> _rendererItems = new List<List<RendererItem>>();
            private CheckContext _checkContext = null;
            private ConvertWork[] _convertWorks = null;
            private bool _warningChecked = false;
            private bool _forceClearData = false;

            private Dictionary<ObjectField, Material> _objectFieldMaterial = new Dictionary<ObjectField, Material>();

            private const string nameCheckPattern = @"^[0-9a-zA-Z_]*$";

            private List<string> supportShaderThemeList = null;

            public static ConfigureComponentPanel instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<ConfigureComponentPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/ConfigureComponentPanel.asset");
                    }

                    return _instance;
                }
            }

            public List<string> getSupportShaderThemeList()
            {
                if (supportShaderThemeList == null || supportShaderThemeList.Count == 0)
                {
                    supportShaderThemeList = new List<string>();
                    foreach (var VARIABLE in Enum.GetNames(typeof(OfficialShaderTheme)))
                    {
                        supportShaderThemeList.Add("PAV/URP/" + VARIABLE);
                    }
                }
                return supportShaderThemeList;
            }

            public override string displayName
            {
                get => "ConfigureComponent";
            }

            public override string panelName
            {
                get => "ConfigureComponent";
            }

            public override string uxmlPathName
            {
                get => "Uxml/ConfigureComponentPanel.uxml";
            }

            public override void OnShow()
            {
                base.OnShow();
                // test
                //if (_setting == null)
                //{
                //    BindOrUpdateFromData(null);
                //}
            }

            public override void OnHide()
            {
                base.OnHide();
                var importConfig = curImportSettings;
                if (importConfig)
                {
                    importConfig.basicInfoSetting.SetAssetName(_baseInfoWidget.Name);
                    importConfig.basicInfoSetting.assetNickName = _baseInfoWidget.ShowName;
                    importConfig.basicInfoSetting.assetIconPath = _baseInfoWidget.Icon;
                }
            }

            public void SetForceClearData(bool clearData)
            {
                _forceClearData = clearData;
            }

            public override void BindOrUpdateFromData(PaabAssetImportSettings importConfig)
            {
                bool hadInit = false;
                bool clearData = false;
                if (_setting != null)
                {
                    hadInit = true;
                    var setting = importConfig.GetImportSetting<PaabComponentImportSetting>(false);
                    if (setting != null)
                    {
                        if (setting.componentSource != _setting.componentSource ||
                            setting.componentType != _setting.componentType)
                        {
                            clearData = true;
                        }
                    }

                    if (_forceClearData)
                    {
                        clearData = true;
                    }
                }

                _forceClearData = false;

                // test
                //if (importConfig == null)
                //{
                //    importConfig = ScriptableObject.CreateInstance<PaabAssetImportSettings>();
                //    importConfig.basicInfoSetting = new PaabBasicInfoImportSetting();
                //    importConfig.basicInfoSetting.SetAssetName("BaseBodyComponent");
                //    importConfig.basicInfoSetting.assetNickName = "BaseBody";

                //    var componentSetting = new PaabComponentImportSetting();
                //    componentSetting.componentSource = PaabComponentImportSetting.ComponentSource.Custom;
                //    componentSetting.componentType = PaabComponentImportSetting.ComponentType.BaseBody;

                //    importConfig.settingItems = new PaabAssetImportSetting[] {
                //        componentSetting,
                //    };
                //}

                base.BindOrUpdateFromData(importConfig);

                _setting = importConfig.GetImportSetting<PaabComponentImportSetting>(false);
                LoadSkeleton(importConfig.basicInfoSetting.skeletonAssetName);
                if (!hadInit)
                {
                    _baseInfoWidget = new BaseInfoWidget(mainElement.Q<TemplateContainer>("BaseinfoWidget"),
                        _setting.componentType == PaabComponentImportSetting.ComponentType.BaseBody
                            ? BaseInfoType.AssetBaseBody
                            : BaseInfoType.AssetComponent);
                    AddWidget(_baseInfoWidget);
                    _baseInfoWidget.BindUIActions();
                    _baseInfoWidget.ShowWidget();
                }

                _baseInfoWidget.NameReadOnly = importConfig.opType == OperationType.Update ||
                                               importConfig.opType == OperationType.UpdateAsset;
                _baseInfoWidget.SetInputValue(importConfig.basicInfoSetting.assetName,
                    importConfig.basicInfoSetting.assetNickName, importConfig.basicInfoSetting.assetIconPath);
                _baseInfoWidget.AutoSetName(importConfig.basicInfoSetting.characterName, _setting.componentType);
                _baseInfoWidget.AutoSetIconIfNotSet(_setting.componentType);

                var componentTypeGroup = mainElement.Q<GroupBox>("ComponentTypeGroup");
                var componentTypeDropdown = mainElement.Q<DropdownField>("ComponentTypeDropdown");
                if (_setting.componentType == PaabComponentImportSetting.ComponentType.BaseBody)
                {
                    componentTypeGroup.style.display = DisplayStyle.None;
                }
                else
                {
                    PaabComponentImportSetting.ComponentType[] types;
                    if (_setting.componentSource == PaabComponentImportSetting.ComponentSource.Official)
                    {
                        types = new PaabComponentImportSetting.ComponentType[]
                        {
                            PaabComponentImportSetting.ComponentType.Hair,
                            PaabComponentImportSetting.ComponentType.ClothTop,
                            PaabComponentImportSetting.ComponentType.ClothBottom,
                            PaabComponentImportSetting.ComponentType.ClothShoes,
                            PaabComponentImportSetting.ComponentType.ClothSocks,
                            PaabComponentImportSetting.ComponentType.ClothGloves,
                            PaabComponentImportSetting.ComponentType.ClothHood,
                            PaabComponentImportSetting.ComponentType.AccessoryHeaddress,
                            PaabComponentImportSetting.ComponentType.AccessoryMask,
                            PaabComponentImportSetting.ComponentType.AccessoryNecklace,
                            PaabComponentImportSetting.ComponentType.AccessoryBracelet,
                            PaabComponentImportSetting.ComponentType.AccessoryArmguard,
                            PaabComponentImportSetting.ComponentType.AccessoryShoulderknot,
                            PaabComponentImportSetting.ComponentType.AccessoryLegRing,
                            PaabComponentImportSetting.ComponentType.AccessoryProp,
                        };
                    }
                    else
                    {
                        types = new PaabComponentImportSetting.ComponentType[]
                        {
                            PaabComponentImportSetting.ComponentType.Head,
                            PaabComponentImportSetting.ComponentType.Body,
                            PaabComponentImportSetting.ComponentType.Hand,
                        };
                    }

                    var choices = new List<string>();
                    for (int i = 0; i < types.Length; ++i)
                    {
                        choices.Add(types[i].ToString());
                    }

                    componentTypeDropdown.choices = choices;
                    var index = choices.IndexOf(_setting.componentType.ToString());
                    if (index < 0)
                    {
                        index = 0;
                    }

                    componentTypeDropdown.index = index;
                }

                RegisterDropdownHover(componentTypeDropdown);

                var shoesSettingGroup = mainElement.Q<GroupBox>("ShoesSettingGroup");
                if (_setting.componentType != PaabComponentImportSetting.ComponentType.ClothShoes)
                {
                    shoesSettingGroup.style.display = DisplayStyle.None;
                }
                else
                {
                    var heelHeightDropdown = mainElement.Q<DropdownField>("HeelHeightDropdown");
                    var soleThicknessSlider = mainElement.Q<Slider>("SoleThicknessSlider");
                    heelHeightDropdown.value =
                        Mathf.RoundToInt(_setting.shoeParams.heelHeight * 100).ToString() + " cm"; // m to cm
                    soleThicknessSlider.value = _setting.shoeParams.soleThickness * 100.0f; // m to cm
                    RegisterDropdownHover(heelHeightDropdown);
                }

                if (clearData)
                {
                    // reset ui state
                    for (int i = 0; i < LOD_GROUP_COUNT; ++i)
                    {
                        var lodGroup = mainElement.Q<GroupBox>("LodGroup" + i);
                        while (lodGroup.childCount > 2)
                        {
                            var item = lodGroup.ElementAt(1);
                            item.RemoveFromHierarchy();
                        }
                    }

                    InitUI();
                    SwitchShaderPreviewUI(false);

                    hadInit = false;
                }

                if (!hadInit)
                {
                    for (int i = 0; i < LOD_GROUP_COUNT; ++i)
                    {
                        var lodGroup = mainElement.Q<GroupBox>("LodGroup" + i);
                        var lodNameLabel = lodGroup.Q<Label>("LodNameLabel");
                        var lodTip = lodGroup.Q<GroupBox>("LodTip");
                        if (_setting.componentType == PaabComponentImportSetting.ComponentType.BaseBody)
                        {
                            lodNameLabel.text = "LOD " + i + " Base Body";
                            lodTip.style.marginLeft = 50 + 80;
                        }
                        else
                        {
                            lodNameLabel.text = "LOD " + i;
                            lodTip.style.marginLeft = 50;
                        }

                        if (_setting.componentType == PaabComponentImportSetting.ComponentType.BaseBody)
                        {
                            AddLodGroupItem(lodGroup, false, 1);
                        }

                        for (int j = 1; j < lodGroup.childCount - 1; ++j)
                        {
                            var item = lodGroup.ElementAt(j);
                            InitLodGroupItemBySetting(item);
                            // lod0 and lod1 items hide in init state
                            if (i > 0)
                            {
                                item.style.display = DisplayStyle.None;
                            }
                        }
                    }
                }
            }

            public override void UpdateToData(PaabAssetImportSettings importConfig)
            {
                base.UpdateToData(importConfig);

                importConfig.basicInfoSetting.SetAssetName(_baseInfoWidget.Name);
                importConfig.basicInfoSetting.assetNickName = _baseInfoWidget.ShowName;
                importConfig.basicInfoSetting.assetIconPath = _baseInfoWidget.Icon;

                var componentTypeDropdown = mainElement.Q<DropdownField>("ComponentTypeDropdown");
                if (_setting.componentType != PaabComponentImportSetting.ComponentType.BaseBody)
                {
                    _setting.componentType =
                        System.Enum.Parse<PaabComponentImportSetting.ComponentType>(componentTypeDropdown.value);
                }

                if (_setting.componentType == PaabComponentImportSetting.ComponentType.ClothShoes)
                {
                    var heelHeightDropdown = mainElement.Q<DropdownField>("HeelHeightDropdown");
                    var soleThicknessSlider = mainElement.Q<Slider>("SoleThicknessSlider");
                    var heelHeightStr = heelHeightDropdown.value;
                    heelHeightStr = heelHeightStr.Substring(0, heelHeightStr.IndexOf(' '));
                    _setting.shoeParams.heelHeight = int.Parse(heelHeightStr) / 100.0f; // cm to m
                    _setting.shoeParams.soleThickness = soleThicknessSlider.value / 100.0f; // cm to m
                }
            }

            private void LoadSkeleton(string skeletonName)
            {
                // test
                //_skeleton = GameObject.Find("Skeleton");
                //return;

                if (_skeleton)
                {
                    DestroyImmediate(_skeleton);
                    _skeleton = null;
                }

                if (!string.IsNullOrEmpty(skeletonName) && !string.IsNullOrEmpty(CharacterUtil.CharacterFolderPath))
                {
                    string skeletonPath = CharacterUtil.CharacterFolderPath + "/Skeleton/" + skeletonName + "/0.zip";
                    if (System.IO.File.Exists(skeletonPath))
                    {
                        _skeleton = AvatarConverter.LoadSkeleton(skeletonPath);
                        _skeleton.hideFlags = HideFlags.HideAndDontSave;
                    }
                    else
                    {
                        Debug.LogError("skeleton zip file not exist: " + skeletonPath);
                    }
                }
                else if (_setting.componentSource == PaabComponentImportSetting.ComponentSource.Official)
                {
                    var configText = ComponentListPanel.instance.CharacterInfo.character.config;
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
                                    var prefab =
                                        Resources.Load<GameObject>(CharacterUtil.Official_1_0_FemalePrefabPath);

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

            public override void OnDestroy()
            {
                base.OnDestroy();

                if (_skeleton)
                {
                    DestroyImmediate(_skeleton);
                    _skeleton = null;
                }
                _objectFieldMaterial?.Clear();
                _objectFieldMaterial = null;
                _instance = null;
            }

            public override void OnUpdate()
            {
                base.OnUpdate();

                UpdateLabelStyle();
                UpdateConvertWork();
            }

            private void UpdateLabelStyle()
            {
                for (int i = 0; i < LOD_GROUP_COUNT; ++i)
                {
                    if (_rendererItems == null || _rendererItems.Count == 0 || _rendererItems[i] == null)
                    {
                        break;
                    }

                    for (int j = 0; j < _rendererItems[i].Count; ++j)
                    {
                        var rendererItem = _rendererItems[i][j];
                        var item = rendererItem.item;
                        var gameObjectLabel = GetMeshObjectLabel(item);
                        if (gameObjectLabel.text == "None (Game Object)")
                        {
                            gameObjectLabel.text = "(Required)";
                            gameObjectLabel.style.color = (Color)GetEmptyObjectColor();
                        }
                        else if (gameObjectLabel.text == "(Required)")
                        {
                            if (rendererItem.errorType == ErrorType.GameObject &&
                                rendererItem.status == CommonDialogWindow.CheckStatus.Error)
                            {
                                gameObjectLabel.style.color = (Color)GetErrorColor();
                            }
                            else
                            {
                                gameObjectLabel.style.color = (Color)GetEmptyObjectColor();
                            }
                        }
                        else
                        {
                            if (rendererItem.errorType == ErrorType.GameObject &&
                                rendererItem.status == CommonDialogWindow.CheckStatus.Error)
                            {
                                gameObjectLabel.style.color = (Color)GetErrorColor();
                            }
                            else
                            {
                                gameObjectLabel.style.color = (Color)GetDefaultColor();
                            }
                        }
                    }
                }
            }

            private void UpdateConvertWork()
            {
                if (_convertWorks != null)
                {
                    bool allFinish = true;
                    float progress = 0.0f;
                    for (int i = 0; i < _convertWorks.Length; ++i)
                    {
                        var work = _convertWorks[i];
                        if (work.state == WorkState.Init)
                        {
                            work.state = WorkState.Converting;
                            allFinish = false;
                            int index = i;
                            AvatarConverter.ConvertAvatarComponent(work.name, _skeleton, work.skins,
                                work.officialMaterials, work.customMaterials, work.zipPath, work.extrasJson,
                                () => { _convertWorks[index].state = WorkState.MoveTemp; });
                            break;
                        }
                        else if (work.state == WorkState.Converting)
                        {
                            allFinish = false;
                            break;
                        }
                        else if (work.state == WorkState.MoveTemp)
                        {
                            if (!string.IsNullOrEmpty(CharacterUtil.CharacterFolderPath) &&
                                System.IO.Directory.Exists(CharacterUtil.CharacterFolderPath))
                            {
                                string targetPath;
                                if (_setting.componentType == PaabComponentImportSetting.ComponentType.BaseBody)
                                {
                                    targetPath = CharacterUtil.CharacterFolderPath + "/" +
                                                 _setting.componentType.ToString() + "/" + work.name + "/" +
                                                 work.lod.ToString() + ".zip";
                                }
                                else
                                {
                                    targetPath = CharacterUtil.CharacterFolderPath + "/Component/" +
                                                 _setting.componentType.ToString() + "/" + work.name + "/" +
                                                 work.lod.ToString() + ".zip";
                                }

                                var targetDir = new System.IO.FileInfo(targetPath).DirectoryName;
                                if (!System.IO.Directory.Exists(targetDir))
                                {
                                    System.IO.Directory.CreateDirectory(targetDir);
                                }
                                else if (work.lod == 0)
                                {
                                    System.IO.Directory.Delete(targetDir, true);
                                    System.IO.Directory.CreateDirectory(targetDir);
                                }

                                var sourceDir = new System.IO.FileInfo(work.zipPath).DirectoryName;
                                System.IO.File.Copy(work.zipPath, targetPath, true);
                                System.IO.File.Copy(sourceDir + "/config.json",
                                    targetDir + "/" + work.lod.ToString() + ".config.json", true);
                            }

                            work.state = WorkState.Finish;
                            progress += 1.0f / _convertWorks.Length;
                        }
                        else if (work.state == WorkState.Finish)
                        {
                            progress += 1.0f / _convertWorks.Length;
                        }
                    }

                    if (allFinish)
                    {
                        _convertWorks = null;
                        if (_setting.componentType == PaabComponentImportSetting.ComponentType.BaseBody)
                            CharacterManager.instance.SetAssetLoadSource(PaabCharacterImportSetting.AssetState_Local,
                                curImportSettings.assetName, CharacterBaseAssetType.BaseBody);
                        curImportSettings.assetStatus = AssetStatus.Ready;
                        EditorUtility.ClearProgressBar();

                        // to next panel
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
                    }
                    else
                    {
                        EditorUtility.DisplayProgressBar("Avatar Converter", "Working, please wait...", progress);
                    }
                }
            }

            private void RegisterDropdownHover(VisualElement ve)
            {
                ve.ElementAt(0).style.backgroundColor = (Color)new Color32(57, 57, 57, 255);
                ve.RegisterCallback<MouseEnterEvent>((e) =>
                {
                    ve.ElementAt(0).style.backgroundColor = (Color)new Color32(77, 77, 77, 255);
                });
                ve.RegisterCallback<MouseLeaveEvent>((e) =>
                {
                    ve.ElementAt(0).style.backgroundColor = (Color)new Color32(57, 57, 57, 255);
                });
            }

            private void RegisterFoldoutLabelHover(VisualElement ve)
            {
                var checkmark = ve.Q<VisualElement>("unity-checkmark");
                var parent = checkmark.parent;
                var label = parent.ElementAt(1);
                label.style.color = (Color)new Color32(210, 210, 210, 255);
                checkmark.style.unityBackgroundImageTintColor = (Color)new Color32(210, 210, 210, 255);
                parent.RegisterCallback<MouseEnterEvent>((e) =>
                {
                    label.style.color = (Color)new Color32(255, 255, 255, 255);
                    checkmark.style.unityBackgroundImageTintColor = (Color)new Color32(255, 255, 255, 255);
                });
                parent.RegisterCallback<MouseLeaveEvent>((e) =>
                {
                    label.style.color = (Color)new Color32(210, 210, 210, 255);
                    checkmark.style.unityBackgroundImageTintColor = (Color)new Color32(210, 210, 210, 255);
                });
            }

            private void InitUI()
            {
                _rendererItems.Clear();
                for (int i = 0; i < LOD_GROUP_COUNT; ++i)
                {
                    _rendererItems.Add(new List<RendererItem>());
                }

                var shaderType1DropDown = mainElement.Q<DropdownField>("ShaderType1DropDown");
                shaderType1DropDown.choices = GetShaderType1List();
                shaderType1DropDown.index = 0;
                RegisterDropdownHover(shaderType1DropDown);

                var preiviewCustom = mainElement.Q<Button>("PreiviewCustom");
                preiviewCustom.ElementAt(0).style.visibility = Visibility.Hidden;
                _previewCustom = false;

                for (int i = 0; i < LOD_GROUP_COUNT; ++i)
                {
                    var lodGroup = mainElement.Q<GroupBox>("LodGroup" + i);
                    var lodToggle = lodGroup.Q<Toggle>("LodToggle");
                    if (i == 0)
                    {
                        lodToggle.SetValueWithoutNotify(true);
                        lodToggle.SetEnabled(false);
                    }
                    else
                    {
                        lodToggle.SetValueWithoutNotify(false);
                    }

                    AddLodGroupItem(lodGroup, false, 0);

                    // lod0 and lod1 items hide in init state
                    if (i > 0)
                    {
                        for (int j = 1; j < lodGroup.childCount; ++j)
                        {
                            lodGroup.ElementAt(j).style.display = DisplayStyle.None;
                        }
                    }
                }
            }

            protected override bool BuildUIDOM(VisualElement parent)
            {
                // clear old data
                _setting = null;
                _convertWorks = null;

                if (!base.BuildUIDOM(parent))
                {
                    return false;
                }

                InitUI();
                SetTips();

                return true;
            }

            private void SetTips()
            {
                UIUtils.SetTipText(mainElement.Q<GroupBox>("ComponentTypeGroup").Q<GroupBox>("nameTip"),
                    StringTable.GetString("TipComponentType"));
                //UIUtils.SetTipText(mainElement.Q<GroupBox>("ShaderTypeGroup").Q<GroupBox>("nameTip"), StringTable.GetString("TipShaderType"));
                //UIUtils.SetTipText(mainElement.Q<GroupBox>("PreviewModeGroup").Q<GroupBox>("nameTip"), StringTable.GetString("TipPreviewMode"));
                UIUtils.SetTipText(mainElement.Q<GroupBox>("LodGroup0").Q<GroupBox>("LodTip"),
                    StringTable.GetString("TipLodGroup0"));
                UIUtils.SetTipText(mainElement.Q<GroupBox>("LodGroup1").Q<GroupBox>("LodTip"),
                    StringTable.GetString("TipLodGroup1"));
                UIUtils.SetTipText(mainElement.Q<GroupBox>("LodGroup2").Q<GroupBox>("LodTip"),
                    StringTable.GetString("TipLodGroup2"));
            }

            private void SetItemTips(VisualElement item)
            {
                UIUtils.SetTipText(item.Q<GroupBox>("ShaderType2Group").Q<GroupBox>("nameTip"),
                    StringTable.GetString("TipShaderType2"));
            }

            protected override bool BindUIActions()
            {
                if (!base.BindUIActions())
                {
                    return false;
                }

                mainElement.style.height = Length.Percent(100);

                //var shaderType1DropDown = mainElement.Q<DropdownField>("ShaderType1DropDown");
                //shaderType1DropDown.RegisterValueChangedCallback((eve) => { SwitchShaderType1(eve.newValue); });

                //var preiviewOfficial = mainElement.Q<Button>("PreiviewOfficial");
                //var preiviewCustom = mainElement.Q<Button>("PreiviewCustom");
                //preiviewOfficial.clicked += () => {
                //    SwitchShaderPreviewUI(false);
                //};
                //preiviewCustom.clicked += () => {
                //    SwitchShaderPreviewUI(true);
                //};

                var soleThicknessSlider = mainElement.Q<Slider>("SoleThicknessSlider");
                var SoleThicknessLabel = mainElement.Q<Label>("SoleThicknessLabel");
                soleThicknessSlider.RegisterValueChangedCallback((eve) =>
                {
                    SoleThicknessLabel.text = eve.newValue.ToString("F1") + " cm";
                });

                for (int i = 0; i < LOD_GROUP_COUNT; ++i)
                {
                    var lodGroup = mainElement.Q<GroupBox>("LodGroup" + i);
                    var lodToggle = lodGroup.Q<Toggle>("LodToggle");
                    if (i > 0)
                    {
                        var lodNameLabel = lodGroup.Q<Label>("LodNameLabel");
                        lodNameLabel.RegisterCallback<MouseUpEvent>((eve) =>
                        {
                            if (eve.button == 0)
                            {
                                lodToggle.value = !lodToggle.value;
                            }
                        });

                        int lodGroupIndex = i;
                        lodToggle.RegisterValueChangedCallback((eve) =>
                        {
                            if (!eve.newValue)
                            {
                                ResetLodGroup(lodGroupIndex);
                            }

                            for (int j = 1; j < lodGroup.childCount; ++j)
                            {
                                lodGroup.ElementAt(j).style.display =
                                    eve.newValue ? DisplayStyle.Flex : DisplayStyle.None;

                                ResetWarningCheck();
                            }
                        });
                    }

                    var addButton = lodGroup.Q<Button>("Add");
                    addButton.clicked += () =>
                    {
                        AddLodGroupItem(lodGroup, true, lodGroup.childCount - 2);

                        ResetWarningCheck();
                    };
                }

                var nextButton = mainElement.Q<Button>("NextButton");
                nextButton.clicked += () => { DoNext(); };

                return true;
            }

            private void ResetLodGroup(int lodGroupIndex)
            {
                // clear all
                var lodGroup = mainElement.Q<GroupBox>("LodGroup" + lodGroupIndex);
                while (lodGroup.childCount > 2)
                {
                    var item = lodGroup.ElementAt(1);
                    item.RemoveFromHierarchy();
                }

                _rendererItems[lodGroupIndex].Clear();

                // re-add new
                AddLodGroupItem(lodGroup, false, 0);
                if (_setting.componentType == PaabComponentImportSetting.ComponentType.BaseBody)
                {
                    AddLodGroupItem(lodGroup, false, 1);
                }
            }

            private void InitLodGroupItemBySetting(VisualElement item)
            {
                var bodyTypeGroup = item.Q<GroupBox>("BodyTypeGroup");
                if (_setting != null)
                {
                    if (_setting.componentType == PaabComponentImportSetting.ComponentType.BaseBody)
                    {
                        var bodyTypeDropdown = GetBodyTypeDropdown(item);
                        // second item init body type with body
                        if (item.parent.IndexOf(item) == 2)
                        {
                            bodyTypeDropdown.index = 1;
                        }
                    }
                    else
                    {
                        bodyTypeGroup.style.display = DisplayStyle.None;
                    }
                }
            }

            private void UpdateLodGroupItemName(VisualElement item)
            {
                int itemIndexUI = item.parent.IndexOf(item);
                var itemFoldout = item.Q<Foldout>("ItemFoldout");
                itemFoldout.text = "Skinned Mesh Renderer " + itemIndexUI;
            }

            private void UpdateLodGroupItemStates(GroupBox lodGroup)
            {
                for (int i = 1; i < lodGroup.childCount - 1; ++i)
                {
                    var item = lodGroup.ElementAt(i);
                    UpdateLodGroupItemName(item);

                    bool removeable = true;
                    if (_setting != null && _setting.componentType == PaabComponentImportSetting.ComponentType.BaseBody)
                    {
                        if (i <= 2)
                        {
                            removeable = false;
                        }
                    }
                    else
                    {
                        if (i <= 1)
                        {
                            removeable = false;
                        }
                    }

                    var removeButton = item.Q<Button>("Remove");
                    removeButton.style.display = removeable ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            private void AddLodGroupItem(GroupBox lodGroup, bool removeable, int itemIndex)
            {
                int lodIndex = GetLodGroupIndex(lodGroup);
                var itemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    AssetBuilderConfig.instance.uiDataAssetsPath + "UxmlWidget/ConfigureComponentItemWidget.uxml");
                var item = itemAsset.Instantiate();
                lodGroup.Insert(1 + itemIndex, item);

                InitLodGroupItemBySetting(item);
                UpdateLodGroupItemName(item);

                var bodyTypeDropdown = GetBodyTypeDropdown(item);
                bodyTypeDropdown.RegisterValueChangedCallback((eve) =>
                {
                    var bodyTypeElement = GetBodyTypeElement(item);
                    bodyTypeElement.style.color = (Color)GetDefaultColor();
                });
                RegisterDropdownHover(bodyTypeDropdown);
                RegisterFoldoutLabelHover(item.Q<Foldout>("ItemFoldout"));

                var officialMaterialGroup = item.Q<GroupBox>("MaterialGroup");
                officialMaterialGroup.style.display = DisplayStyle.None;
                var officialMaterialTip = item.Q<GroupBox>("OfficialMaterialTip");
                UIUtils.SetTipText(officialMaterialTip, StringTable.GetString("OfficialMaterialTip"));

                var customMaterialGroup = item.Q<GroupBox>("CustomMaterialGroup");
                customMaterialGroup.style.display = DisplayStyle.None;
                var customMaterialTip = item.Q<GroupBox>("CustomMaterialTip");
                UIUtils.SetTipText(customMaterialTip, StringTable.GetString("CustomMaterialTip"));

                //var shaderType2Group = item.Q<GroupBox>("ShaderType2Group");
                //shaderType2Group.style.display = DisplayStyle.None;

                var removeButton = item.Q<Button>("Remove");
                removeButton.style.display = removeable ? DisplayStyle.Flex : DisplayStyle.None;
                removeButton.clicked += () =>
                {
                    OnRemoveItem(lodIndex, lodGroup, lodGroup.IndexOf(item) - 1);
                    UpdateLodGroupItemStates(lodGroup);

                    ResetWarningCheck();
                };

                var gameObjectField = GetMeshObjectField(item);
                gameObjectField.allowSceneObjects = true;
                gameObjectField.RegisterValueChangedCallback((eve) =>
                {
                    (eve.target as ObjectField).SetValueWithoutNotify(eve.previousValue);
                    OnSetGameObjectUI(lodIndex, lodGroup, lodGroup.IndexOf(item) - 1, eve.newValue as GameObject);
                    ResetWarningCheck();
                });
                RegisterSelectObjectFieldValue(gameObjectField);

                var officialMaterialField = GetOfficalMaterialField(item);
                officialMaterialField.allowSceneObjects = false;
                officialMaterialField.RegisterValueChangedCallback((eve) =>
                {
                    // (eve.target as ObjectField).SetValueWithoutNotify(eve.previousValue);
                    SetOfficialMaterial(lodIndex, lodGroup, lodGroup.IndexOf(item) - 1, eve.newValue as Material);
                    var newMaterial = eve.newValue as Material;
                    if (newMaterial != null && !getSupportShaderThemeList().Contains(newMaterial.shader.name))
                    {
                        officialMaterialField.value = _objectFieldMaterial[officialMaterialField];
                    }
                    else
                    {
                        if (_objectFieldMaterial == null)
                            _objectFieldMaterial = new Dictionary<ObjectField, Material>();
                        
                        if (_objectFieldMaterial != null && _objectFieldMaterial.ContainsKey(officialMaterialField))
                            _objectFieldMaterial[officialMaterialField] = newMaterial;
                        else
                            _objectFieldMaterial.Add(officialMaterialField, newMaterial);
                    }
                });
                RegisterSelectObjectFieldValue(officialMaterialField);

                var customMaterialField = GetCustomMaterialField(item);
                customMaterialField.allowSceneObjects = false;
                customMaterialField.RegisterValueChangedCallback((eve) =>
                {
                    // (eve.target as ObjectField).SetValueWithoutNotify(eve.previousValue);
                    SetCustomMaterial(lodIndex, lodGroup, lodGroup.IndexOf(item) - 1, eve.newValue as Material);
                });
                RegisterSelectObjectFieldValue(customMaterialField);

                SetShaderType2(lodIndex, lodGroup, lodGroup.IndexOf(item) - 1);

                SetItemTips(item);

                var rendererItem = new RendererItem();
                rendererItem.lodIndex = lodIndex;
                rendererItem.lodGroup = lodGroup;
                rendererItem.item = item;
                _rendererItems[rendererItem.lodIndex].Insert(itemIndex, rendererItem);
            }

            private void OnRemoveItem(int lodIndex, GroupBox lodGroup, int itemIndex)
            {
                lodGroup.RemoveAt(itemIndex + 1);
                _rendererItems[lodIndex].RemoveAt(itemIndex);
            }

            private void RejectObject(ChangeEvent<Object> eve)
            {
                (eve.target as ObjectField).SetValueWithoutNotify(eve.previousValue);
            }

            private List<string> GetShaderType1List()
            {
                return new List<string>() { "PBR" };
            }

            private string GetShaderType1()
            {
                var shaderType1DropDown = mainElement.Q<DropdownField>("ShaderType1DropDown");
                return shaderType1DropDown.value;
            }

            private List<string> GetShaderType2By(string shaderType1)
            {
                var types = System.Enum.GetValues(typeof(Avatar.AvatarShaderType));
                var list = new List<string>();
                for (int i = 0; i < types.Length; ++i)
                {
                    var value = (Avatar.AvatarShaderType)types.GetValue(i);
                    if (value == Avatar.AvatarShaderType.Invalid)
                    {
                        continue;
                    }

                    list.Add(value.ToString());
                }

                return list;
            }

            public static OfficialShaderTheme getOfficialShaderThemeByName(string shaderName)
            {
                if (shaderName == "PAV/URP/PicoPBR")
                {
                    return OfficialShaderTheme.PicoPBR;
                }
                else if (shaderName == "PAV/URP/PicoNPR")
                {
                    return OfficialShaderTheme.PicoNPR;
                }
                else
                {
                    return OfficialShaderTheme.PicoPBR;
                }
            }

            private string GetDefaultShaderNameBy(string shaderType1)
            {
                if (shaderType1 == "PBR")
                {
                    return "PAV/URP/PicoPBR";
                }
                else if (shaderType1 == "Unlit")
                {
                    return "PAV/URP/Unlit";
                }
                else if (shaderType1 == "NRP")
                {
                    return "PAV/URP/PicoNPR";
                }

                return "";
            }

            private string GetShaderNameBy(string shaderType1, string shaderType2)
            {
                if (shaderType1 == "PBR")
                {
                    if (shaderType2 == Avatar.AvatarShaderType.Hair_KK.ToString()) //为什么hair要特殊处理一下？ 可以不处理吗？
                    {
                        return "PAV/URP/Hair";
                    }
                }

                return GetDefaultShaderNameBy(shaderType1);
            }

            private string[] GeShaderTextureNames(string shaderType1, string shaderType2)
            {
                if (shaderType1 == "PBR" || shaderType1 == "NPR")
                {
                    if (shaderType2 == Avatar.AvatarShaderType.Hair_KK.ToString())
                    {
                        return new string[] { "_BaseMap", "_NoiseTex" };
                    }
                    else
                    {
                        return new string[] { "_BaseMap", "_MetallicGlossMap", "_BumpMap" };
                    }
                }
                else if (shaderType1 == "Unlit")
                {
                    return new string[] { "_BaseMap" };
                }

                return new string[0];
            }

            private int GetLodGroupIndex(GroupBox lodGroup)
            {
                return int.Parse(lodGroup.name.Substring("LodGroup".Length));
            }

            private DropdownField GetBodyTypeDropdown(VisualElement item)
            {
                return item.Q<DropdownField>("BodyTypeDropdown");
            }

            private VisualElement GetBodyTypeElement(VisualElement item)
            {
                var bodyTypeDropdown = GetBodyTypeDropdown(item);
                return bodyTypeDropdown.ElementAt(0).ElementAt(0);
            }

            private ObjectField GetMeshObjectField(VisualElement item)
            {
                return item.Q<ObjectField>("MeshObject");
            }

            private Label GetMeshObjectLabel(VisualElement item)
            {
                var gameObjectField = GetMeshObjectField(item);
                return gameObjectField.ElementAt(0).ElementAt(0).ElementAt(1) as Label;
            }

            private ObjectField GetOfficalMaterialField(VisualElement item)
            {
                return item.Q<ObjectField>("MaterialObject");
            }

            private Label GetOfficialMaterialLabel(VisualElement item)
            {
                var materialField = GetOfficalMaterialField(item);
                return materialField.ElementAt(0).ElementAt(0).ElementAt(1) as Label;
            }

            private ObjectField GetCustomMaterialField(VisualElement item)
            {
                return item.Q<ObjectField>("CustomMaterialObject");
            }

            private Label GetCustomMaterialLabel(VisualElement item)
            {
                var materialField = GetCustomMaterialField(item);
                return materialField.ElementAt(0).ElementAt(0).ElementAt(1) as Label;
            }

            private DropdownField GetShaderType2Field(VisualElement item)
            {
                return item.Q<DropdownField>("ShaderType2Dropdown");
            }
            

            private void SwitchShaderType1(string shaderType1)
            {
                for (int i = 0; i < LOD_GROUP_COUNT; ++i)
                {
                    var lodGroup = mainElement.Q<GroupBox>("LodGroup" + i);
                    for (int j = 1; j < lodGroup.childCount - 1; ++j)
                    {
                        var item = lodGroup.ElementAt(j);
                        var materialField = GetOfficalMaterialField(item);
                        var material = materialField.value as Material;
                        var shaderType2Field = GetShaderType2Field(item);
                        var shaderType2 = shaderType2Field.value;
                    }
                }
            }

            private void SwitchShaderPreviewUI(bool custom)
            {
                var preiviewOfficial = mainElement.Q<Button>("PreiviewOfficial");
                var preiviewCustom = mainElement.Q<Button>("PreiviewCustom");
                if (custom)
                {
                    if (!_previewCustom)
                    {
                        _previewCustom = true;
                        preiviewOfficial.ElementAt(0).style.visibility = Visibility.Hidden;
                        //preiviewOfficial.style.backgroundColor = (Color) new Color32(61, 61, 61, 0);
                        preiviewOfficial.EnableInClassList("ButtonSelected", false);
                        preiviewOfficial.EnableInClassList("ButtonUnSelected", true);
                        preiviewCustom.ElementAt(0).style.visibility = Visibility.Visible;
                        //preiviewCustom.style.backgroundColor = (Color) new Color32(61, 61, 61, 255);
                        preiviewCustom.EnableInClassList("ButtonSelected", true);
                        preiviewCustom.EnableInClassList("ButtonUnSelected", false);
                        SwitchShaderPreview(true);
                    }
                }
                else
                {
                    if (_previewCustom)
                    {
                        _previewCustom = false;
                        preiviewOfficial.ElementAt(0).style.visibility = Visibility.Visible;
                        //preiviewOfficial.style.backgroundColor = (Color) new Color32(61, 61, 61, 255);
                        preiviewOfficial.EnableInClassList("ButtonSelected", true);
                        preiviewOfficial.EnableInClassList("ButtonUnSelected", false);
                        preiviewCustom.ElementAt(0).style.visibility = Visibility.Hidden;
                        //preiviewCustom.style.backgroundColor = (Color) new Color32(61, 61, 61, 0);
                        preiviewCustom.EnableInClassList("ButtonSelected", false);
                        preiviewCustom.EnableInClassList("ButtonUnSelected", true);
                        SwitchShaderPreview(false);
                    }
                }
            }

            private void SwitchShaderPreview(bool custom)
            {
                for (int i = 0; i < LOD_GROUP_COUNT; ++i)
                {
                    var lodGroup = mainElement.Q<GroupBox>("LodGroup" + i);
                    for (int j = 1; j < lodGroup.childCount - 1; ++j)
                    {
                        var item = lodGroup.ElementAt(j);
                        var materialField = GetOfficalMaterialField(item);
                        var material = materialField.value as Material;
                        var shaderType2Field = GetShaderType2Field(item);
                        var shaderType2 = shaderType2Field.value;
                    }
                }
            }

            private void SetOfficialMaterial(int lodIndex, GroupBox lodGroup, int itemIndex, Material obj)
            {
                RendererItem rendererItem = _rendererItems[lodIndex][itemIndex];
                if (obj == null)
                {
                    rendererItem.officialMaterial = obj;
                    return;
                }

                if (!getSupportShaderThemeList().Contains(obj.shader.name))
                {
                    EditorUtility.DisplayDialog("Error", "Only Support PAV Material!", "ok");
                    return;
                }
                
                rendererItem.officialMaterial = obj;
            }

            private void SetCustomMaterial(int lodIndex, GroupBox lodGroup, int itemIndex, Material obj)
            {
                if (obj == null)
                {
                    return;
                }

                RendererItem rendererItem = _rendererItems[lodIndex][itemIndex];
                rendererItem.customMaterial = obj;
            }

            private void SetShaderType2(int lodIndex, GroupBox lodGroup, int itemIndex)
            {
                var item = lodGroup.ElementAt(itemIndex + 1);
                var gameObjectField = GetMeshObjectField(item);
                if (gameObjectField.value == null)
                {
                    return;
                }

                var materialField = GetOfficalMaterialField(item);
                var material = materialField.value as Material;

                var shaderType = (Avatar.AvatarShaderType)material.GetInt("_ShaderType");
                string shaderType2 = shaderType.ToString();

                var shaderName = GetShaderNameBy(GetShaderType1(), shaderType2);
                if (string.IsNullOrEmpty(shaderName))
                {
                    return;
                }

                if (shaderName != material.shader.name)
                {
                    material.shader = Shader.Find(shaderName);
                }

                if (shaderType == Avatar.AvatarShaderType.Eyelash_Base)
                {
                    material.SetFloat("_Surface", (float)BaseShaderGUI.SurfaceType.Transparent);
                    material.SetFloat("_Blend", (float)BaseShaderGUI.BlendMode.Alpha);
                    material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_SrcAlphaBlend", (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat("_DstAlphaBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    material.SetFloat("_ZWrite", 0.0f);
                }
                else
                {
                    material.SetFloat("_Surface", (float)BaseShaderGUI.SurfaceType.Opaque);
                    material.SetFloat("_Blend", (float)BaseShaderGUI.BlendMode.Alpha);
                    material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetFloat("_SrcAlphaBlend", (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat("_DstAlphaBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    material.SetFloat("_ZWrite", 1.0f);
                }

                RendererItem rendererItem = _rendererItems[lodIndex][itemIndex];
                rendererItem.shaderType2 = shaderType2;
            }

            private static void RegisterSelectObjectFieldValue(ObjectField objectField)
            {
                objectField.RegisterCallback<MouseDownEvent>((eve) =>
                {
                    var obj = (eve.target as ObjectField).value;
                    if (obj)
                    {
                        Selection.activeObject = obj;
                    }
                });
            }

            private void OnSetGameObjectUI(int lodIndex, GroupBox lodGroup, int itemIndex, GameObject obj)
            {
                if (obj == null)
                {
                    SetRenderer(lodIndex, lodGroup, itemIndex, null);
                }
                else
                {
                    var skinnedMeshRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
                    int count = 0;
                    if (skinnedMeshRenderers.Length > 1)
                    {
                        itemIndex = 0;
                    }
                    
                    // 这里遍历一下 skinnedMeshRenderers 然后生成UI，根据UI的选择，得到配置。
                    // 这里的配置有：生成的官方材质的类型、是否删除原来的custom材质
                    var messages = new List<Renderer>();
                    for (int i = 0; i < skinnedMeshRenderers.Length; ++i)
                    {
                        var renderer = skinnedMeshRenderers[i];
                        bool firstSame = false;
                        if (count == 0 && _rendererItems[lodIndex][itemIndex].renderer && renderer.gameObject ==
                            _rendererItems[lodIndex][itemIndex].renderer.gameObject)
                        {
                            firstSame = true;
                        }

                        if (!renderer.gameObject.activeInHierarchy ||
                            !renderer.enabled ||
                            renderer.bones.Length == 0 ||
                            renderer.sharedMaterial == null ||
                            getSupportShaderThemeList().Contains(renderer.sharedMaterial.shader.name) ||
                            (IsExistRenderer(lodGroup, renderer) && !firstSame))
                        {
                            continue;
                        }
                        
                        messages.Add(renderer);
                    }
                    CommonDialogWindow.ShowMaterialConfigDialog(messages);
                    MaterialConfigPanel.instance.InitMaterialConfig(lodIndex, lodGroup, itemIndex, obj);
                    if (messages.Count == 0)
                    {
                        MaterialConfigPanel.instance.OnNextBtnClick(null);
                    }
                }
            }
            
            public void OnSetGameObject(int lodIndex, GroupBox lodGroup, int itemIndex, GameObject obj)
            {
                if (obj == null)
                {
                    return;
                }
                else
                {
                    var skinnedMeshRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
                    int count = 0;
                    if (skinnedMeshRenderers.Length > 1)
                    {
                        itemIndex = 0;
                    }
                    
                    for (int i = 0; i < skinnedMeshRenderers.Length; ++i)
                    {
                        var renderer = skinnedMeshRenderers[i];
                        bool firstSame = false;
                        if (count == 0 && _rendererItems[lodIndex][itemIndex].renderer && renderer.gameObject ==
                            _rendererItems[lodIndex][itemIndex].renderer.gameObject)
                        {
                            firstSame = true;
                        }

                        if (!renderer.gameObject.activeInHierarchy ||
                            !renderer.enabled ||
                            renderer.bones.Length == 0 ||
                            renderer.sharedMaterial == null ||
                            (IsExistRenderer(lodGroup, renderer) && !firstSame))
                        {
                            continue;
                        }

                        if (count > 0 && itemIndex + count >= lodGroup.childCount - 2)
                        {
                            AddLodGroupItem(lodGroup, true, itemIndex + count);
                        }

                        SetRenderer(lodIndex, lodGroup, itemIndex + count, renderer);
                        count += 1;
                    }

                    UpdateLodGroupItemStates(lodGroup);
                }
            }

            private bool IsExistRenderer(GroupBox lodGroup, Renderer renderer)
            {
                for (int i = 1; i < lodGroup.childCount - 1; ++i)
                {
                    var item = lodGroup.ElementAt(i);
                    var gameObjectField = GetMeshObjectField(item);
                    if (gameObjectField.value == renderer.gameObject)
                    {
                        return true;
                    }
                }

                return false;
            }

            private void SetRenderer(int lodIndex, GroupBox lodGroup, int itemIndex, Renderer renderer)
            {
                var item = lodGroup.ElementAt(itemIndex + 1);

                var dic = MaterialConfigPanel.instance._renderMaterialDic;

                var gameObjectField = GetMeshObjectField(item);
                gameObjectField.SetValueWithoutNotify(renderer?.gameObject);

                Material officialMaterial = null;
                Material customMaterial = null;

                if (renderer)
                {
                    var material = renderer.sharedMaterial;
                    // 判断当前render的材质，如果不是 PAV/URP/PicoPBR 或者 PAV/URP/Unlit，
                    // 将当前的材质分别设置到 customMaterial 以及 新构建的officialMaterial中
                    if (!getSupportShaderThemeList().Contains(material.shader.name))
                    {
                        if (dic[renderer] != null)
                        {
                            customMaterial = material;
                            var saveCustomMaterial = dic[renderer].saveCustomMaterial;
                            var shaderTheme = dic[renderer].ShaderTheme;
                            officialMaterial = MaterialConvertWindow.ConverterMaterial(customMaterial, officialMaterial, shaderTheme);//
                            
                            if (!saveCustomMaterial)
                                customMaterial = null;
                        }
                        else
                        {   
                            customMaterial = material;
                        }
                    }
                    else
                    {
                        officialMaterial = material;
                    }
                }

                var officialMaterialField = GetOfficalMaterialField(item);
                officialMaterialField.value = (officialMaterial);

                var officialMaterialLabel = GetOfficialMaterialLabel(item);
                officialMaterialLabel.style.color = (Color)GetDefaultColor();

                var customMaterialField = GetCustomMaterialField(item);
                customMaterialField.SetValueWithoutNotify(customMaterial);

                var customMaterialLabel = GetCustomMaterialLabel(item);
                customMaterialLabel.style.color = (Color)GetDefaultColor();

                //var shaderType2Field = GetShaderType2Field(item);
                //shaderType2Field.index = 0;
                //SetShaderType2(lodIndex, lodGroup, itemIndex, shaderType2Field.value);

                var officialMaterialGroup = item.Q<GroupBox>("MaterialGroup");
                officialMaterialGroup.style.display = renderer ? DisplayStyle.Flex : DisplayStyle.None;
                var customMaterialGroup = item.Q<GroupBox>("CustomMaterialGroup");
                customMaterialGroup.style.display = renderer ? DisplayStyle.Flex : DisplayStyle.None;
                //var shaderType2Group = item.Q<GroupBox>("ShaderType2Group");
                //shaderType2Group.style.display = renderer ? DisplayStyle.Flex : DisplayStyle.None;

                RendererItem rendererItem = _rendererItems[lodIndex][itemIndex];
                rendererItem.renderer = renderer;
                rendererItem.officialMaterial = officialMaterial;
                if (officialMaterial != null)
                    rendererItem.OfficialShaderTheme = getOfficialShaderThemeByName(officialMaterial.shader.name);
                rendererItem.customMaterial = customMaterial;
                rendererItem.errorType = ErrorType.None;
                rendererItem.status = CommonDialogWindow.CheckStatus.Right;
            }

            private bool IsLodGroupEnable(int lodIndex)
            {
                var lodGroup = mainElement.Q<GroupBox>("LodGroup" + lodIndex);
                var lodToggle = lodGroup.Q<Toggle>("LodToggle");
                return lodToggle.value;
            }

            private static bool CheckSkeletonCompatible(GameObject skeleton, Transform[] bones)
            {
                if (skeleton == null || bones == null || bones.Length == 0)
                {
                    return false;
                }

                for (int i = 0; i < bones.Length; ++i)
                {
                    if (bones[i] == null)
                    {
                        return false;
                    }

                    if (!Avatar.AnimationConverter.FindTransformByName(skeleton, bones[i].name))
                    {
                        return false;
                    }
                }

                return true;
            }

            const string ErrorPrefix = "Error:";
            const string WarningPrefix = "Warning:";

            private void DoCheck(CheckContext checkContext)
            {
                _baseInfoWidget.CheckValue((BaseInfoValueCheckResult result) =>
                {
                    var errors = BaseInfoWidget.GetBaseInfoErrorMessage(result);
                    for (int i = 0; i < errors.Count; ++i)
                    {
                        checkContext.globalResults.Add(string.Format(ErrorPrefix + errors[i]));
                    }

                    DoComponentCheck(checkContext);
                });
            }

            private void DoComponentCheck(CheckContext checkContext)
            {
                // restore official shader
                SwitchShaderPreviewUI(false);

                if (IsLodGroupEnable(0) && !IsLodGroupEnable(1) && IsLodGroupEnable(2))
                {
                    checkContext.globalResults.Add(string.Format(ErrorPrefix + "Please upload in the order of Lod0, Lod1, and Lod2."));
                }

                int minPrimitiveCount = int.MaxValue;
                // check per renderer
                for (int i = 0; i < _rendererItems.Count; ++i)
                {
                    if (!IsLodGroupEnable(i))
                    {
                        continue;
                    }

                    var vertsInComponent = 0;
                    var trianglesInComponent = 0;
                    for (int j = 0; j < _rendererItems[i].Count; ++j)
                    {
                        var rendererItem = _rendererItems[i][j];
                        rendererItem.checkResult = null;

                        var renderer = rendererItem.renderer;
                        if (renderer == null)
                        {
                            rendererItem.checkResult = string.Format(ErrorPrefix + "mesh is required");
                            rendererItem.errorType = ErrorType.GameObject;
                            continue;
                        }
                        
                        var officialMaterial = rendererItem.officialMaterial;
                        var shaderName = GetShaderNameBy(GetShaderType1(), rendererItem.shaderType2);

                        // error: not official shader
                        if (officialMaterial == null)
                        {
                            rendererItem.checkResult = string.Format(ErrorPrefix + "{0} has no official material", renderer.gameObject.name);
                            rendererItem.errorType = ErrorType.Material;
                        }
                        else if (!getSupportShaderThemeList().Contains(officialMaterial.shader.name))
                        {
                            rendererItem.checkResult = string.Format(ErrorPrefix + "{0} used unofficial shader",
                                renderer.gameObject.name);
                            rendererItem.errorType = ErrorType.Material;
                        }
                        else
                        {
                            // warning: miss official texture
                            var textureNames = GeShaderTextureNames(GetShaderType1(), rendererItem.shaderType2);
                            for (int k = 0; k < textureNames.Length; ++k)
                            {
                                if (officialMaterial.GetTexture(textureNames[k]) == null)
                                {
                                    rendererItem.checkResult = string.Format(WarningPrefix + "{0} missing texture: {1}",
                                        renderer.gameObject.name, textureNames[k]);
                                    rendererItem.errorType = ErrorType.Material;
                                    break;
                                }
                            }
                        }

                        // error: material count > 1
                        if (renderer.sharedMaterials.Length > 1)
                        {
                            rendererItem.checkResult = string.Format(ErrorPrefix + "{0} had more than 1 material",
                                renderer.gameObject.name);
                            rendererItem.errorType = ErrorType.GameObject;
                        }

                        if (renderer is SkinnedMeshRenderer)
                        {
                            // error: submesh count > 1
                            if ((renderer as SkinnedMeshRenderer).sharedMesh.subMeshCount > 1)
                            {
                                rendererItem.checkResult = string.Format(ErrorPrefix + "{0} had more than 1 submesh",
                                    renderer.gameObject.name);
                                rendererItem.errorType = ErrorType.GameObject;
                            }

                            if (!CheckSkeletonCompatible(_skeleton, (renderer as SkinnedMeshRenderer).bones))
                            {
                                rendererItem.checkResult = string.Format(
                                    ErrorPrefix + "{0} bones not compatible with skeleton",
                                    renderer.gameObject.name);
                                rendererItem.errorType = ErrorType.GameObject;
                            }

                            // check name is illegal ?
                            if (!System.Text.RegularExpressions.Regex.IsMatch(renderer.name, nameCheckPattern))
                            {
                                rendererItem.checkResult = string.Format(
                                    ErrorPrefix + "{0} only supports numbers, letters, underscores",
                                    renderer.gameObject.name);
                                rendererItem.errorType = ErrorType.GameObject;
                            }
                            
                            vertsInComponent += (renderer as SkinnedMeshRenderer).sharedMesh.vertexCount;
                            trianglesInComponent += (renderer as SkinnedMeshRenderer).sharedMesh.triangles.Length / 3;
                        }
                    }
                    if (curImportSettings.assetImportSettingsType == AssetImportSettingsType.BaseBody)
                    {
                        // check this _rendererItems[i] verts count is no more than 20000.
                        if (i == 1 && trianglesInComponent >= CHECK_LOD1_TRIANGLE_COUNT)
                        {
                            checkContext.globalResults.Add(string.Format(ErrorPrefix + "LOD {0} has {1} triangles, and must be less than {2} triangles to be uploaded.", i, trianglesInComponent, CHECK_LOD1_TRIANGLE_COUNT));
                        }
                        else if (i == 2 && trianglesInComponent >= CHECK_LOD2_TRIANGLE_COUNT)
                        {
                            checkContext.globalResults.Add(string.Format(ErrorPrefix + "LOD {0} has {1} triangles, and must be less than {2} triangles to be uploaded.", i, trianglesInComponent, CHECK_LOD2_TRIANGLE_COUNT));
                        }

                        if (trianglesInComponent < minPrimitiveCount)
                            minPrimitiveCount = trianglesInComponent;
                    }
                }

                if (curImportSettings.assetImportSettingsType == AssetImportSettingsType.BaseBody)
                {
                    if (minPrimitiveCount >= CHECK_LOD2_TRIANGLE_COUNT)
                    {
                        checkContext.globalResults.Add(string.Format(WarningPrefix + "Without an LOD under 15,000 triangles, the avatar cannot be converted to public. The recommended triangle count is 7,500."));
                    }
                }
                // error: shoes setting over limit
                // had limited by ui, do nothing

                // error: lod0 miss requirement
                {
                    int rendererCount = 0;
                    for (int i = 0; i < _rendererItems[0].Count; ++i)
                    {
                        if (_rendererItems[0][i].renderer)
                        {
                            rendererCount += 1;
                        }
                    }

                    if (rendererCount == 0)
                    {
                        var result = string.Format(ErrorPrefix + "LOD0 need at least one SkinnedMeshRenderer");
                        _rendererItems[0][0].checkResult = result;
                        _rendererItems[0][0].errorType = ErrorType.GameObject;
                    }
                }

                // error: base body require 1 head and 1 body at least
                if (_setting != null && _setting.componentType == PaabComponentImportSetting.ComponentType.BaseBody)
                {
                    for (int i = 0; i < _rendererItems.Count; ++i)
                    {
                        if (!IsLodGroupEnable(i))
                        {
                            continue;
                        }

                        int headCount = 0;
                        int bodyCount = 0;
                        int headExistCount = 0;
                        int bodyExistCount = 0;
                        for (int j = 0; j < _rendererItems[i].Count; ++j)
                        {
                            var rendererItem = _rendererItems[i][j];
                            var item = rendererItem.item;
                            var bodyTypeDropdown = GetBodyTypeDropdown(item);
                            if (bodyTypeDropdown.index == 0) // head
                            {
                                headCount += 1;
                                if (rendererItem.renderer)
                                {
                                    headExistCount += 1;
                                }
                            }
                            else if (bodyTypeDropdown.index == 1) // body
                            {
                                bodyCount += 1;
                                if (rendererItem.renderer)
                                {
                                    bodyExistCount += 1;
                                }
                            }
                            else
                            {
                                Debug.Assert(false);
                            }
                        }

                        if (headCount == 0 || bodyCount == 0)
                        {
                            for (int j = 0; j < _rendererItems[i].Count; ++j)
                            {
                                var rendererItem = _rendererItems[i][j];
                                rendererItem.checkResult =
                                    string.Format(ErrorPrefix + "Base body require 1 head and 1 body at least");
                                rendererItem.errorType = ErrorType.BodyType;
                            }
                        }
                        else if (headExistCount == 0 || bodyExistCount == 0)
                        {
                            for (int j = 0; j < _rendererItems[i].Count; ++j)
                            {
                                var rendererItem = _rendererItems[i][j];
                                rendererItem.checkResult =
                                    string.Format(ErrorPrefix + "Base body require 1 head and 1 body at least");
                                rendererItem.errorType = ErrorType.GameObject;
                            }
                        }
                    }
                }

                var messages = new List<CommonDialogWindow.Message>();
                checkContext.errorCount = 0;
                checkContext.warningCount = 0;
                for (int i = 0; i < checkContext.globalResults.Count; ++i)
                {
                    if (checkContext.globalResults[i].StartsWith(ErrorPrefix))
                    {
                        checkContext.errorCount += 1;
                        messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error,
                            checkContext.globalResults[i].Substring(ErrorPrefix.Length)));
                    }
                    else if (checkContext.globalResults[i].StartsWith(WarningPrefix))
                    {
                        checkContext.warningCount += 1;
                        messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Warning,
                            checkContext.globalResults[i].Substring(WarningPrefix.Length)));
                    }
                }

                for (int i = 0; i < _rendererItems.Count; ++i)
                {
                    if (!IsLodGroupEnable(i))
                    {
                        continue;
                    }

                    for (int j = 0; j < _rendererItems[i].Count; ++j)
                    {
                        var rendererItem = _rendererItems[i][j];
                        if (!string.IsNullOrEmpty(rendererItem.checkResult))
                        {
                            if (rendererItem.checkResult.StartsWith(ErrorPrefix))
                            {
                                checkContext.errorCount += 1;
                                SetItemCheckStatus(rendererItem, true, CommonDialogWindow.CheckStatus.Error);
                                messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Error,
                                    rendererItem.checkResult.Substring(ErrorPrefix.Length)));
                            }
                            else if (rendererItem.checkResult.StartsWith(WarningPrefix))
                            {
                                checkContext.warningCount += 1;
                                SetItemCheckStatus(rendererItem, true, CommonDialogWindow.CheckStatus.Warning);
                                messages.Add(new CommonDialogWindow.Message(CommonDialogWindow.CheckStatus.Warning,
                                    rendererItem.checkResult.Substring(WarningPrefix.Length)));
                            }
                        }
                        else
                        {
                            SetItemCheckStatus(rendererItem, true, CommonDialogWindow.CheckStatus.Right);
                        }
                    }
                }

                if (checkContext.showWaningPanel)
                {
                    if (checkContext.errorCount > 0 || (checkContext.warningCount > 0 && !_warningChecked))
                    {
                        CommonDialogWindow.ShowCheckPopupDialog(messages);
                    }
                }

                if (checkContext.onCheckFinish != null)
                {
                    checkContext.onCheckFinish();
                }
            }

            private Color32 GetDefaultColor()
            {
                return new Color32(0xD2, 0xD2, 0xD2, 0xFF);
            }

            private Color32 GetEmptyObjectColor()
            {
                return new Color32(136, 136, 136, 255);
            }

            private Color32 GetErrorColor()
            {
                return new Color32(0xFF, 0x57, 0x52, 0xFF);
            }

            private Color32 GetWarningColor()
            {
                return new Color32(0xFF, 0xBA, 0x00, 0xFF);
            }

            private void SetItemCheckStatus(RendererItem rendererItem, bool visible,
                CommonDialogWindow.CheckStatus status)
            {
                if (rendererItem.errorType == ErrorType.GameObject)
                {
                    rendererItem.status = status;
                }

                Color32 defaultColor = GetDefaultColor();
                Color32 textColor = defaultColor;
                if (status == CommonDialogWindow.CheckStatus.Error)
                {
                    textColor = GetErrorColor();
                }
                else if (status == CommonDialogWindow.CheckStatus.Warning)
                {
                    textColor = GetWarningColor();
                }

                var item = rendererItem.item;
                var bodyTypeElement = GetBodyTypeElement(item);
                bodyTypeElement.style.color = (Color)defaultColor;
                var officialMaterialLabel = GetOfficialMaterialLabel(item);
                officialMaterialLabel.style.color = (Color)defaultColor;

                if (status != CommonDialogWindow.CheckStatus.Right)
                {
                    if (rendererItem.errorType == ErrorType.BodyType)
                    {
                        bodyTypeElement.style.color = (Color)textColor;
                    }
                    else if (rendererItem.errorType == ErrorType.Material)
                    {
                        officialMaterialLabel.style.color = (Color)textColor;
                    }
                }
            }

            private void ResetWarningCheck()
            {
                _warningChecked = false;
            }

            public override void OnNextStep()
            {
                DoNext();
            }

            private void DoNext()
            {
                if (_checkContext != null || _convertWorks != null)
                {
                    return;
                }

                _checkContext = new CheckContext();
                _checkContext.showWaningPanel = true;
                _checkContext.errorCount = 0;
                _checkContext.warningCount = 0;
                _checkContext.onCheckFinish = OnCheckFinish;
                DoCheck(_checkContext);
            }

            public GameObject[] OnPreEnterPreview()
            {
                if (_rendererItems.Count == 0)
                {
                    return null;
                }

                List<GameObject> objs = new List<GameObject>();
                for (int i = 0; i < _rendererItems.Count; ++i)
                {
                    var lod = _rendererItems[i];
                    for (int j = 0; j < lod.Count; ++j)
                    {
                        var renderer = lod[j].renderer;
                        if (renderer && renderer is SkinnedMeshRenderer)
                        {
                            objs.Add(renderer.gameObject);
                        }
                        else
                        {
                            objs.Add(null);
                        }
                    }
                }

                return objs.ToArray();
            }

            public void OnPostExitPreview(GameObject[] objs)
            {
                if (_rendererItems.Count == 0)
                {
                    return;
                }

                int index = 0;
                for (int i = 0; i < _rendererItems.Count; ++i)
                {
                    var lod = _rendererItems[i];
                    for (int j = 0; j < lod.Count && index < objs.Length; ++j)
                    {
                        var obj = objs[index++];
                        if (obj)
                        {
                            GetMeshObjectField(lod[j].item).SetValueWithoutNotify(obj);
                            lod[j].renderer = obj.GetComponent<SkinnedMeshRenderer>();
                        }
                    }
                }
            }

            private void OnCheckFinish()
            {
                int errorCount = _checkContext.errorCount;
                int warningCount = _checkContext.warningCount;
                _checkContext = null;

                if (errorCount > 0)
                {
                    return;
                }

                if (warningCount > 0 && !_warningChecked)
                {
                    _warningChecked = true;
                    return;
                }

                // do next
                _warningChecked = false;

                UpdateToData(curImportSettings);

                var convertWorks = new List<ConvertWork>();
                var settingLods = new List<PaabComponentImportSetting.LodRenderers>();
                for (int i = 0; i < _rendererItems.Count; ++i)
                {
                    if (!IsLodGroupEnable(i))
                    {
                        continue;
                    }

                    var lod = _rendererItems[i];
                    var skins = new List<SkinnedMeshRenderer>();
                    var officialMaterials = new List<Material>();
                    var customMaterials = new List<Material>();

                    var renderItems = new List<PaabComponentImportSetting.RendererItem>();
                    for (int j = 0; j < lod.Count; ++j)
                    {
                        SkinnedMeshRenderer renderer = (SkinnedMeshRenderer)lod[j].renderer;
                        if (renderer && renderer is SkinnedMeshRenderer)
                        {
                            skins.Add(renderer as SkinnedMeshRenderer);

                            officialMaterials.Add(lod[j].officialMaterial);
                            customMaterials.Add(lod[j].customMaterial);

                            PaabComponentImportSetting.RendererItem rendererItem =
                                new PaabComponentImportSetting.RendererItem();
                            rendererItem.baseBodyType = GetBodyTypeDropdown(lod[j].item).index;
                            rendererItem.meshName = "mesh_" + renderer.name;
                            rendererItem.verts = renderer.sharedMesh.vertexCount;
                            rendererItem.triangles = renderer.sharedMesh.triangles.Length / 3;

                            renderItems.Add(rendererItem);
                        }
                    }

                    if (skins.Count > 0)
                    {
                        var settingLod = new PaabComponentImportSetting.LodRenderers();
                        settingLod.lod = i;
                        settingLod.rendererItems = renderItems.ToArray();
                        settingLods.Add(settingLod);

                        string name = "component";
                        // if (curImportSettings) // Todo. the curImportSettings is not null, but the cachePtr is 0x0.
                        {
                            name = curImportSettings.basicInfoSetting.assetName;
                        }

                        ConvertWork work = new ConvertWork();
                        work.lod = i;
                        work.name = name;
                        work.skins = skins.ToArray();
                        work.officialMaterials = officialMaterials.ToArray();
                        work.customMaterials = customMaterials.ToArray();
                        work.zipPath = Application.dataPath + "/../OutGLTF/" + i + ".zip";
                        work.extrasJson = null;
                        work.state = WorkState.Init;
                        convertWorks.Add(work);
                    }
                }

                _setting.lods = settingLods.ToArray();

                for (int i = 0; i < convertWorks.Count; ++i)
                {
                    var work = convertWorks[i];

                    string extrasJson = null;
                    // if (curImportSettings) // Todo. the curImportSettings is not null, but the cachePtr is 0x0.
                    {
                        _setting.currentLod = work.lod;
                        extrasJson = curImportSettings.ToJsonText();
                    }
                    work.extrasJson = extrasJson;
                }

                _setting.currentLod = -1;

                if (convertWorks.Count > 0)
                {
                    _convertWorks = convertWorks.ToArray();
                }

                // test ConvertSkeleton
                /*
                {
                    AvatarConverter.ConvertSkeleton(name, skeleton, zipPath, extrasJson, () => {
                        Debug.LogError("ConvertSkeleton complete " + zipPath);
                        //_convertOutputZipPath = zipPath;
                    });
                }
                */

                // test ConvertAnimationSet
                /*
                {
#if false
                    var clips = skeleton.GetComponent<Animator>().runtimeAnimatorController.animationClips;
#else
                    var anim = skeleton.GetComponent<Animation>();
                    var clipList = new List<AnimationClip>();
                    foreach (AnimationState state in anim)
                    {
                        clipList.Add(state.clip);
                    }
                    var clips = clipList.ToArray();
#endif
                    var clipNames = new string[clips.Length];
                    for (int j = 0; j < clips.Length; ++j)
                    {
                        clipNames[j] = clips[j].name;
                    }

                    AvatarConverter.ConvertAnimationSet(name, skeleton, clips, clipNames, zipPath, extrasJson, () => {
                        Debug.LogError("ConvertAnimationSet complete " + zipPath);
                        //_convertOutputZipPath = zipPath;
                    });
                }
                */
            }
        }
    }
}

#endif