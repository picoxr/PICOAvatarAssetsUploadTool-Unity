#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AssemblyCSharp.Assets.AmzAvatar.TestTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pico.Avatar;
using Pico.AvatarAssetBuilder.Protocol;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using CharacterInfo = Pico.AvatarAssetBuilder.Protocol.CharacterInfo;
using Label = UnityEngine.UIElements.Label;

namespace Pico.AvatarAssetBuilder
{
    public class AnimationListPanel : PavPanel, IPavPanelExtra
    {
        private Button refreshBtn;
        private VisualElement createBtnRoot;
        private Button createBtn;
        private PavScrollView itemList;
        private PavScrollView menuList;
        private PavScrollView animList;

        private AssetCategory assetCategory;
        private CharacterInfo characterInfo;
        private AssetList animationSetAssetList;
        private VisualElement assetListLoading;
        private AssetListLoadingWidget assetListLoadingWidget;
        private PaabAssetImportSettings importSettings;


        public int currentSelectedMenuIndex = 0;
        public SimpleToggleGroup menuToggleGroup = new SimpleToggleGroup();
        
        public override string displayName
        {
            get
            {
                return "AnimationListPanel";
                if (CharacterInfo == null)
                    return "AnimationSet";

                return $"{CharacterInfo.character.name}-AnimationSet";
            }
        }

        public override string panelName { get => "AnimationListPanel"; }
        public override string uxmlPathName { get => "Uxml/AnimationListPanel.uxml"; }
        
        public CharacterInfo CharacterInfo => characterInfo;
        public PaabAssetImportSettings ImportSettings => importSettings;
        
        private static AnimationListPanel _instance;
        
        private ServerAssetData localAnimationAssetData = new ServerAssetData();
        private List<AnimationTestCellData> animDatas = new List<AnimationTestCellData>();
        private List<AnimationMenuData> animMenuDatas = new();

        public static AnimationListPanel instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Utils.LoadOrCreateAsset<AnimationListPanel>(
                        AssetBuilderConfig.instance.uiDataStorePath + "PanelData/AnimationListPanel.asset");
                }
                return _instance;
            }
        }


        public void SetCharacter(CharacterInfo data)
        {
            if (data == null)
            {
                Debug.LogError("[AnimationListPanel] Character is null");
                return;
            }
            
            characterInfo = data;
            bool result = CharacterUtil.CreateCharacterFolder(characterInfo.character.name, false);
            if (!result)
            {
                Debug.LogError("[AnimationListPanel] Create character folder failed");
                return;
            }

            currentSelectedMenuIndex = 0;
            CharacterManager.instance.SetCurrentCharacterPath(characterInfo);
            //InitImportSettings();
            if(characterInfo.skeleton != null)
                DownloadSkeleton(characterInfo.skeleton);
        }

#region base function


        private List<string> GetBaseAnimClipNames()
        {
            List<string> clipNames = new List<string>();
            String characterBaseAnimString = characterInfo.base_animation_set.offline_config;
            PAAPRuntimeManager manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();
            if (manager != null && manager.characterinfo != null && manager.characterinfo.base_animation_set != null)
            {
                var settingString = manager.characterinfo.base_animation_set.offline_config;
                var settingJson = JsonConvert.DeserializeObject<JObject>(settingString);
                if (settingJson != null)
                {
                    var configString = settingJson.ToString();
                    characterBaseAnimString = configString;
                }
            }
            if(characterBaseAnimString != null)
            {
                var characterBaseAnimConfig =
                    JsonConvert.DeserializeObject<JObject>(characterBaseAnimString);
                if (characterBaseAnimConfig != null && characterBaseAnimConfig.TryGetValue("animations", out JToken aniValue))
                {
                    var animations = JsonConvert.DeserializeObject<Dictionary<string, object>>(aniValue.ToString());
                    if (animations == null) return clipNames;
                    var tmpClipNames = animations.Keys;
                    clipNames.AddRange(tmpClipNames);
                }
            }
            // PicoAvatar3 style
            else if(characterInfo.character.avatar_style == "PicoAvatar3" || characterInfo.character.avatar_style == "PicoAvatar4")
            {
                clipNames.AddRange(new List<string>{"idle", "lHandFist", "rHandFist", "walking", "walkingBack", "walkingLeft", "walkingRight", "sit_ground", "sit_midStoolNormal", "sit_highStool", "smile"});
            }
            return clipNames;
        }


        public override void OnShow()
        {
            base.OnShow();
            CharacterManager.instance.ClearCurrentCharacter();
            InitImportSettings();
            RequestAssetList();
            InitTestAnimList();
        }

        protected override bool BuildUIDOM(VisualElement parent) //SetVisualElements and BuildWithUxml
        {
            base.BuildUIDOM(parent);

            InitElements();
                
            return true;
        }

        protected override bool BindUIActions() //RegisterButtonCallbacks
        {
            createBtn.RegisterCallback<ClickEvent>(OnCreateNewBtnClick);
            refreshBtn.RegisterCallback<ClickEvent>(OnRefreshBtnClick);
            
            return base.BindUIActions();
        }
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            itemList.OnDestroy();
            menuList.OnDestroy();
            animList.OnDestroy();
            DestroyImportSettings();
            //
            if(_instance == this)
            {
                _instance = null;
            }
        }
        
        public void ClearAssets()
        {
            itemList.ClearAllCell();
        }
        
        
#endregion

#region 私有函数

        private void InitImportSettings()
        {
            DestroyImportSettings();
            importSettings = ScriptableObject.CreateInstance<PaabAssetImportSettings>();
            importSettings.SetAssetTypeName(AssetImportSettingsType.AnimationSet);
            importSettings.basicInfoSetting = new PaabBasicInfoImportSetting();
            importSettings.basicInfoSetting.characterId = characterInfo.character.character_id;
            importSettings.basicInfoSetting.characterFolderName = characterInfo.character.name;
            importSettings.basicInfoSetting.characterName = characterInfo.character.name;
            importSettings.basicInfoSetting.skeletonAssetName = characterInfo.skeleton.name;
            importSettings.hideFlags = HideFlags.DontSaveInEditor;
            var componentSetting = new PaabAnimationImportSetting();
            componentSetting.characterId = characterInfo.character.character_id;
            componentSetting.isBasicAnimationSet = false;
            importSettings.settingItems = new[]
            {
                componentSetting
            };
        }
        
        private void DestroyImportSettings()
        {
            if (importSettings == null)
                return;
            if (importSettings != null)
                GameObject.DestroyImmediate(importSettings, true);
            importSettings = null;
        }
        
        private void RequestAssetList()
        {
            if (characterInfo == null)
                return;

            var characterId = characterInfo.character.character_id;
            if (characterId == "")
                return;
            var request = AssetServerManager.instance.GetAnimationSetAssetList(characterId);
            assetListLoadingWidget.ShowLoading();
            if (MainMenuUIManager.instance.PAAB_OPEN)
                createBtnRoot.SetActive(false);
            request.Send(success =>
                {
                    assetListLoadingWidget.HideLoading();
                    if (MainMenuUIManager.instance.PAAB_OPEN)
                        createBtnRoot.SetActive(true);
                    
                    if (characterInfo == null || characterId != characterInfo.character.character_id)
                        return;
                    
                    var response = ProtocolUtil.GetResponse<AssetList>(success);
                    if (response == null || response.data == null)
                    {
                        Debug.LogError("Get response failed");
                    }
                    else
                    {
                        animationSetAssetList = response.data;
                        // store local asset info in PAAPRuntimeManager.
                        if (MainMenuUIManager.instance.PAAB_OPEN == false)
                        {
                            PAAPRuntimeManager manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();
                            if (localAnimationAssetData.asset_info == null)
                                localAnimationAssetData.asset_info = new AssetInfo();
                            localAnimationAssetData.asset_info.name = manager.assetName;
                            localAnimationAssetData.asset_info.asset_id = manager.assetPath;
                            localAnimationAssetData.asset_info.offline_config = manager.assetConfig;
                            localAnimationAssetData.asset_info.online_config = manager.assetConfig;
                            localAnimationAssetData.asset_info.status = AvatarAssetBuilderConst.PseudoLocalState;
                            localAnimationAssetData.asset_info.cover = manager.assetBasicInfoSetting.assetIconPath;
                            
                            if (localAnimationAssetData.category == null)
                                localAnimationAssetData.category = new AssetCategoryEntry();
                            localAnimationAssetData.category.asset_type = 10; //AvatarAssetType.Skeleton todo
                            if (manager.assetName != "" && manager.assetAnimationSetting.characterId != "")
                                animationSetAssetList.assets.Insert(0, localAnimationAssetData);
                        }
                        
                        itemList.Refresh();
                    }
                },
                failure =>
                {
                    assetListLoadingWidget.HideLoading();
                    Debug.Log("RequestAnimationSetAssetList...failed : " + failure.ToString());
                });
        }


        private void InitTestAnimList()
        {
            if (MainMenuUIManager.instance.PAAB_OPEN)
                ShowCustomAnimationList();
            else
                InitMenu();
        }

        public void ShowBaseAnimtion()
        {
            itemList.SetActive(false);
            animList.SetActive(true);
            refreshBtn.SetActive(false);
            assetListLoadingWidget.HideWidget();
            var animClipNames = GetBaseAnimClipNames();
            if (animClipNames.Contains(PaabAnimationImportSetting.walkingFwd))
            {
                animClipNames.Remove(PaabAnimationImportSetting.walking);
            }
            // remove rHandFist/lHandFist if fist exists
            if (animClipNames.Contains(PaabAnimationImportSetting.fist))
            {
                animClipNames.Remove(PaabAnimationImportSetting.lHandFist);
                animClipNames.Remove(PaabAnimationImportSetting.rHandFist);
            }
            animClipNames.Insert(0, "StopAnimation");
            animDatas.Clear();
            for (int i = 0; i < animClipNames.Count; i++)
            {
                animDatas.Add(new AnimationTestCellData()
                {
                    isBaseAnim = true,
                    isStopBtn = i == 0,
                    name = animClipNames[i]
                });
            }
            
            animList.Refresh();
        }


        public void ShowCustomAnimationList()
        {
            itemList.SetActive(true);
            animList.SetActive(false);
            refreshBtn.SetActive(true);
            assetListLoadingWidget.ShowWidget();
        }

        private void InitMenu()
        {
            animMenuDatas.Clear();
            if (!MainMenuUIManager.instance.PAAB_OPEN)
            {
                animMenuDatas = new List<AnimationMenuData>
                {
                    new AnimationMenuData()
                    {
                        name = "Base AnimationSet",
                        animationMenuType = AnimationMenuType.BaseAnim
                    },
                    new AnimationMenuData()
                    {
                        name = "Custom AnimationSet",
                        animationMenuType = AnimationMenuType.CustomAnim
                    }
                };
            }
            
            menuList.Refresh();
        }

        private void InitElements()
        {
            createBtnRoot = mainElement.Q("CreateNew");
            refreshBtn = mainElement.Q<Button>("refreshBtn");
            createBtn = mainElement.Q<Button>("CreateNewBtn");
            createBtnRoot.SetActive(MainMenuUIManager.instance.PAAB_OPEN);
            var itemListSV = mainElement.Q<ScrollView>("itemList");
            itemList = new PavScrollView(this, itemListSV);
            var menuListSV = mainElement.Q<ScrollView>("menuList");
            menuList = new PavScrollView(this, menuListSV);
            var animListSV = mainElement.Q<ScrollView>("animList");
            animList = new PavScrollView(this, animListSV);
            animList.Direction = ScrollViewDirection.Vertical;
            animList.WrapMode = Wrap.NoWrap;
            
            assetListLoading = mainElement.Q("AssetListLoading");
            assetListLoadingWidget = new AssetListLoadingWidget(assetListLoading);
            AddWidget(assetListLoadingWidget);
            assetListLoadingWidget.ShowWidget();

            itemList.CellCount = () => CellCount(itemList);
            itemList.CellAtIndex = (index) => CellAtIndex(itemList, index);
            itemList.DataAtIndex = (index) => DataAtIndex(itemList, index);
            
            animList.CellCount = () => CellCount(animList);
            animList.CellAtIndex = (index) => CellAtIndex(animList, index);
            animList.DataAtIndex = (index) => DataAtIndex(animList, index);
            
            menuList.CellCount = () => CellCount(menuList);
            menuList.CellAtIndex = (index) => CellAtIndex(menuList, index);
            menuList.DataAtIndex = (index) => DataAtIndex(menuList, index);
            
            UIUtils.AddVisualElementHoverMask(createBtn, createBtn);
            UIUtils.AddVisualElementHoverMask(refreshBtn, refreshBtn);
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

        private int CellCount(PavScrollView scrollView)
        {
            if (scrollView == itemList)
            {
                if (animationSetAssetList == null)
                    return 0;

                return animationSetAssetList.assets.Count;
            }

            if (scrollView == animList)
                return animDatas.Count;
            
            if (scrollView == menuList)
                return animMenuDatas.Count;

            return 0;
        }
        
        private Type CellAtIndex(PavScrollView scrollView, int index)
        {
            if (scrollView == itemList)
                return typeof(AnimationSetAssetCell);

            if (scrollView == animList)
                return typeof(AnimtionTestCell);
            
            if (scrollView == menuList)
                return typeof(AnimtionMenuCell);

            return null;
        }

        private PavScrollViewCellDataBase DataAtIndex(PavScrollView scrollView, int index)
        {
            if (scrollView == itemList)
            {
                if (animationSetAssetList == null)
                    return null;

                return new AnimationSetAssetCellData()
                {
                    asset = animationSetAssetList.assets[index]
                };
            }

            if (scrollView == animList)
                return animDatas[index];

            if (scrollView == menuList)
                return animMenuDatas[index];


            return null;
        }
        
#region UIEvent

        

        void OnCreateNewBtnClick(ClickEvent evt)
        {
            ImportSettings.basicInfoSetting.SetAssetName(""); 
            ImportSettings.basicInfoSetting.SetAssetId("");
            ImportSettings.basicInfoSetting.assetNickName = "";
            ImportSettings.basicInfoSetting.assetIconPath = "";
            ImportSettings.opType = OperationType.CreateAsset;
            ClearAssets();
            CharacterManager.instance.SetCurrentCharacter(CharacterInfo, OperationType.CreateAsset);
            var customAnimationSetPanel = NavMenuBarRoute.instance.RouteNextByPanelName(panelName, ConfigureCustomAnimationSetPanel.instance.panelName);
            if (customAnimationSetPanel is AssetImportSettingsPanel)
                (customAnimationSetPanel as AssetImportSettingsPanel).BindOrUpdateFromData(ImportSettings);
        }
        
        private void OnRefreshBtnClick(ClickEvent evt)
        {
            ClearAssets();
            RequestAssetList();
        }

#endregion

#endregion

        public bool CheckNavShowWarningWhenSelfIsShow()
        {
            return false;
        }

        public void OnRefresh()
        {
            
        }
        
        public bool IsRefreshVisible()
        {
            return false;
        }
    }
    
    public class AnimationSetAssetCell : PavScrollViewCell
    {
        private VisualElement maskRoot;
        private VisualElement icon;
        private Label name;
        private WebImage iconImage;
        private Button updateBtn;
        private VisualElement testIcon;
        private VisualElement publishIcon;
        private VisualElement localIcon;
        private VisualElement configIcon;
        public override string AssetPath => "Assets/PicoAvatarAssetBuilder/Editors/Views/UxmlWidget/AnimationSetAssetCell.uxml";
        
        public override void OnInit()
        {
            base.OnInit();
            maskRoot = _cellVisualElement.Q("maskRoot");
            icon = _cellVisualElement.Q("icon");
            name = _cellVisualElement.Q<Label>("nameText");
            iconImage = new WebImage(icon);
            updateBtn = _cellVisualElement.Q<Button>("updateBtn");
            updateBtn.SetActive(true);
            testIcon = _cellVisualElement.Q("testIcon");
            publishIcon = _cellVisualElement.Q("publishIcon");
            localIcon = _cellVisualElement.Q("localIcon");
            configIcon = _cellVisualElement.Q("configIcon");
            if (MainMenuUIManager.instance.PAAB_OPEN == true)
            {
                updateBtn.RegisterCallback<ClickEvent>(OnUpdateBtnClick);
                UIUtils.AddVisualElementHoverMask(maskRoot, updateBtn, false, 0.12f);
            }
            else
            {
                updateBtn.RegisterCallback<ClickEvent>(OnRuntimeUpdateBtnClick);
                UIUtils.AddVisualElementHoverMask(maskRoot, updateBtn, false, 0.12f);
            }
            
        }
        
        private List<string> GetCustomAnimClipNames()
        {
            var data = GetData<AnimationSetAssetCellData>();
            var clipNames = ParseCustomAnimClipNames();

            PAAPRuntimeManager manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();
            if (manager != null)
            {
                String assetId = data.asset.asset_info.asset_id;
                manager.AddAnimationSet(assetId);
            }
            
            return clipNames;
        }

        private List<string> ParseCustomAnimClipNames()
        {
            var data = GetData<AnimationSetAssetCellData>();
            List<string> clipNames = new List<string>();
            String characterCustomAnimString = data.asset.asset_info.offline_config;
            var characterCustomAnimConfig = JsonConvert.DeserializeObject<JObject>(characterCustomAnimString);
            if (characterCustomAnimConfig != null && characterCustomAnimConfig.TryGetValue("animations", out JToken aniValue))
            {
                //var animations = characterCustomAnimConfig!.Value<Dictionary<string, object>>(aniValue.ToString());
                var animations = JsonConvert.DeserializeObject<Dictionary<string, object>>(aniValue.ToString());
                if (animations != null)
                {
                    var tmpClipNames = animations.Keys;
                    clipNames.AddRange(tmpClipNames);
                }
            }
            
            return clipNames;
        }


        public override void OnDestroy()
        {
            base.OnDestroy();
            iconImage.OnDestroy();
            name = null;
            
        }

        public override void RefreshCell()
        {
            var data = GetData<AnimationSetAssetCellData>();
            if (data == null)
                return;
            
            name.text = data.asset.asset_info.name;
            iconImage.SetTexture(data.asset.asset_info.cover);
            testIcon.SetActive(data.asset.asset_info.status == 2); // 测试中
            publishIcon.SetActive(data.asset.asset_info.status == 3); // 已发布
            localIcon.SetActive(data.asset.asset_info.status == AvatarAssetBuilderConst.PseudoLocalState); // 已发布
            configIcon.SetActive(data.asset.asset_info.status == 0); // 待配置
            EditorCoroutineUtility.StartCoroutineOwnerless(UpdateNameTip());
        }
        
        private IEnumerator UpdateNameTip()
        {
            yield return null;
            var data = GetData<AnimationSetAssetCellData>();
            if (name == null || data == null)
                yield break;
                
            var size = name.MeasureTextSize(data.asset.asset_info.name, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            name.pickingMode = size.x >= name.resolvedStyle.maxWidth.value
                ? PickingMode.Position
                : PickingMode.Ignore;
            UIUtils.SetTipText(name, data.asset.asset_info.name, 400);
        }

        private void OnUpdateBtnClick(ClickEvent evt)
        {
            var data = GetData<AnimationSetAssetCellData>();
            if (data == null)
                return;

            var panel = GetPanel<AnimationListPanel>();
            panel.ImportSettings.basicInfoSetting.SetAssetName(data.asset.asset_info.name); 
            panel.ImportSettings.basicInfoSetting.SetAssetId(data.asset.asset_info.asset_id);
            panel.ImportSettings.basicInfoSetting.assetNickName = data.asset.asset_info.show_name;
            panel.ImportSettings.basicInfoSetting.assetIconPath = data.asset.asset_info.cover;
            panel.ImportSettings.opType = OperationType.UpdateAsset;
            var animationImportSetting = panel.ImportSettings.GetImportSetting<PaabAnimationImportSetting>(false);
            animationImportSetting.animationClips.Clear();
            var animations = ParseCustomAnimClipNames();
            for (int i = 0; i < animations.Count; i++)
                animationImportSetting.animationClips.Add(new KeyValuePair<string, AnimationClip>(animations[i], null));
            
            GetPanel<AnimationListPanel>().ClearAssets();
            CharacterManager.instance.SetCurrentCharacter(panel.CharacterInfo, OperationType.UpdateAsset);
            var customAnimationSetPanel = NavMenuBarRoute.instance.RouteNextByPanelName(panel.panelName, ConfigureCustomAnimationSetPanel.instance.panelName);
            if (customAnimationSetPanel is AssetImportSettingsPanel)
                (customAnimationSetPanel as AssetImportSettingsPanel).BindOrUpdateFromData(panel.ImportSettings);
        }

        private void OnRuntimeUpdateBtnClick(ClickEvent evt)
        {
            var panel = GetPanel<AnimationListPanel>();
            var animList = GetCustomAnimClipNames();
            // panel.UpdateCustomAnimList(animList);

            var testAnimPanel = NavMenuBarRoute.instance.RouteNextByPanelName(panel.panelName,
                TestCustomAnimationListPanel.instance.panelName);
            if (testAnimPanel is TestCustomAnimationListPanel)
            {
                (testAnimPanel as TestCustomAnimationListPanel).SetPanelData(panel.CharacterInfo, animList);
            }
        }
    }
    
    public class AnimationSetAssetCellData : PavScrollViewCellDataBase
    {
        public ServerAssetData asset;
    }

    public class AnimtionTestCell : PavScrollViewCell
    {
        private Button playBtn;
        public override string AssetPath => "Assets/PicoAvatarAssetBuilder/Editors/Views/UxmlWidget/AnimationTestCell.uxml";

        public override void OnInit()
        {
            base.OnInit();
            playBtn = _cellVisualElement.Q<Button>("btn");
            playBtn.RegisterCallback<ClickEvent>(OnPlayBtnClick);
            UIUtils.AddVisualElementHoverMask(playBtn);
        }

        public override void RefreshCell()
        {
            var data = GetData<AnimationTestCellData>();
            if (data == null)
                return;

            playBtn.text = data.name;
        }

        private void OnPlayBtnClick(ClickEvent @event)
        {
            var data = GetData<AnimationTestCellData>();
            if (data == null)
                return;
            
            PAAPRuntimeManager manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();
            if (manager == null)
                return;
            
            if (data.isStopBtn)
                manager.StopAnimation();
            else
                manager.PlayAnimationByName(data.name);
        }
    }
    
    public class AnimationTestCellData : PavScrollViewCellDataBase
    {
        public string name;
        public bool isBaseAnim;
        public bool isStopBtn;
    }


    public class AnimtionMenuCell : PavScrollViewCell
    {
        private Button btn;
        private VisualElement line;
        private SimpleButtonToggle toggle;
        
        public override string AssetPath => "Assets/PicoAvatarAssetBuilder/Editors/Views/UxmlWidget/ComponentListMainMenuCell.uxml";

        public override void OnInit()
        {
            base.OnInit();
            btn = _cellVisualElement.Q<Button>("btn");
            line = _cellVisualElement.Q("line");
            
            var panel = GetPanel<AnimationListPanel>();
            toggle = new SimpleButtonToggle(panel.menuToggleGroup, btn);
            toggle.onValueChange += OnValueChanged;
            UIUtils.AddVisualElementHoverMask(btn, btn, true);
        }

        public override void RefreshCell()
        {
            var data = GetData<AnimationMenuData>();
            var panel = GetPanel<AnimationListPanel>();
            if (data == null || panel == null)
                return;
            
            btn.text = data.name;
            if (panel.currentSelectedMenuIndex == Index)
                toggle.Toggle();
        }
        
        public void OnValueChanged(bool value)
        {
            var data = GetData<AnimationMenuData>();
            SetToggleState(value);
            //btn.style.opacity = value ? 1 : 0.45f;
            if (value)
            {
                var panel = GetPanel<AnimationListPanel>();
                if (data.animationMenuType == AnimationMenuType.BaseAnim)
                    panel.ShowBaseAnimtion();
                else if (data.animationMenuType == AnimationMenuType.CustomAnim)
                    panel.ShowCustomAnimationList();

                panel.currentSelectedMenuIndex = Index;
            }
        }
        
        private void SetToggleState(bool isOn)
        {
            line.SetActive(isOn);
            btn.style.color = new Color(1, 1, 1, (isOn ? 1 : 0.45f));
            btn.style.unityFontStyleAndWeight = (isOn ? FontStyle.Bold : FontStyle.Normal);
        }
    }

    public enum AnimationMenuType
    {
        BaseAnim,
        CustomAnim
    }

    public class AnimationMenuData : PavScrollViewCellDataBase
    {
        public string name;
        public AnimationMenuType animationMenuType;
    }
}
#endif