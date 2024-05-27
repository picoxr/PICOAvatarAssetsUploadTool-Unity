#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using AssemblyCSharp.Assets.AmzAvatar.TestTools;
using Pico.Avatar;
using Pico.AvatarAssetPreview.Protocol;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using CharacterInfo = Pico.AvatarAssetPreview.Protocol.CharacterInfo;
using Label = UnityEngine.UIElements.Label;

namespace Pico.AvatarAssetPreview
{
    public class ComponentListPanel : PavPanel, IPavPanelExtra
    {
        private Button refreshBtn;
        private PavScrollView mainMenu;
        private PavScrollView subMenu;
        private PavScrollView itemList;
        private VisualElement assetListLoading;
        private PaabAssetImportSettings importSettings;
        private AssetListLoadingWidget assetListLoadingWidget;
        

        public SimpleToggleGroup mainMenuToggleGroup = new SimpleToggleGroup();
        public SimpleToggleGroup subMenuToggleGroup = new SimpleToggleGroup();


        private ServerAssetData localComponentAssetData = new ServerAssetData();

        public int mainMenuIndex
        {
            get
            {
                return currentSelectedMainMenuIndex;
            }
            set
            {
                currentSelectedMainMenuIndex = value;
            }
        }

        public int subMenuIndex
        {
            get
            {
                return currentSelectedSubMenuIndex;
            }
            set
            {
                currentSelectedSubMenuIndex = value;
            }
        }


        private AssetCategory assetCategory;
        private CharacterInfo characterInfo;
        private int currentSelectedMainMenuIndex = -1;
        private int currentSelectedSubMenuIndex = -1;
        private AssetList componentAssetList;

        public override string displayName
        {
            get
            {
                return "ComponentListPanel";
                if (CharacterInfo == null)
                    return "Component";

                return $"{CharacterInfo.character.name}-Component";
            }
        }

        public override string panelName { get => "ComponentListPanel"; }
        public override string uxmlPathName { get => "Uxml/ComponentListPanel.uxml"; }

        public CharacterInfo CharacterInfo => characterInfo;
        
        private static ComponentListPanel _instance;
        
        public static ComponentListPanel instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Utils.LoadOrCreateAsset<ComponentListPanel>(
                        AssetBuilderConfig.instance.uiDataStorePath + "PanelData/ComponentListPanel.asset");
                }
                return _instance;
            }
        }

        public static PaabComponentImportSetting.ComponentType ParseComponentKeyToComponentType(string key)
        {
            switch (key)
            {
                case "Head":
                    return PaabComponentImportSetting.ComponentType.Head;
                case "Body":
                    return PaabComponentImportSetting.ComponentType.Body;
                case "Hand":
                    return PaabComponentImportSetting.ComponentType.Hand;
                case "Hair":
                    return PaabComponentImportSetting.ComponentType.Hair;
                case "Top":
                    return PaabComponentImportSetting.ComponentType.ClothTop;
                case "Bottom":
                    return PaabComponentImportSetting.ComponentType.ClothBottom;
                case "Shoes":
                    return PaabComponentImportSetting.ComponentType.ClothShoes;
                case "Socks":
                    return PaabComponentImportSetting.ComponentType.ClothSocks;
                case "Gloves":
                    return PaabComponentImportSetting.ComponentType.ClothGloves;
                case "Hood":
                    return PaabComponentImportSetting.ComponentType.ClothHood;
                case "Headdress":
                    return PaabComponentImportSetting.ComponentType.AccessoryHeaddress;
                case "Mask":
                    return PaabComponentImportSetting.ComponentType.AccessoryMask;
                case "Necklace":
                    return PaabComponentImportSetting.ComponentType.AccessoryNecklace;
                case "Bracelet":
                    return PaabComponentImportSetting.ComponentType.AccessoryBracelet;
                case "Armguard":
                    return PaabComponentImportSetting.ComponentType.AccessoryArmguard;
                case "Shoulderknot":
                    return PaabComponentImportSetting.ComponentType.AccessoryShoulderknot;
                case "LegRing":
                    return PaabComponentImportSetting.ComponentType.AccessoryLegRing;
                case "Prop":
                    return PaabComponentImportSetting.ComponentType.AccessoryProp;
            }

            return PaabComponentImportSetting.ComponentType.Invalid;
        }
        
    
        public static string ParseComponentTypeToComponentKey(PaabComponentImportSetting.ComponentType componentType)
        {
            switch (componentType)
            {
                case PaabComponentImportSetting.ComponentType.Head:
                    return "Head";
                case PaabComponentImportSetting.ComponentType.Body:
                    return "Body";
                case PaabComponentImportSetting.ComponentType.Hand:
                    return "Hand";
                case PaabComponentImportSetting.ComponentType.Hair:
                    return "Hair";
                case PaabComponentImportSetting.ComponentType.ClothTop:
                    return "Top";
                case PaabComponentImportSetting.ComponentType.ClothBottom:
                    return "Bottom";
                case PaabComponentImportSetting.ComponentType.ClothShoes:
                    return "Shoes";
                case PaabComponentImportSetting.ComponentType.ClothSocks:
                    return "Socks";
                case PaabComponentImportSetting.ComponentType.ClothGloves:
                    return "Gloves";
                case PaabComponentImportSetting.ComponentType.ClothHood:
                    return "Hood";
                case PaabComponentImportSetting.ComponentType.AccessoryHeaddress:
                    return "Headdress";
                case PaabComponentImportSetting.ComponentType.AccessoryMask:
                    return "Mask";
                case PaabComponentImportSetting.ComponentType.AccessoryNecklace:
                    return "Necklace";
                case PaabComponentImportSetting.ComponentType.AccessoryBracelet:
                    return "Bracelet";
                case PaabComponentImportSetting.ComponentType.AccessoryArmguard:
                    return "Armguard";
                case PaabComponentImportSetting.ComponentType.AccessoryShoulderknot:
                    return "Shoulderknot";
                case PaabComponentImportSetting.ComponentType.AccessoryLegRing:
                    return "LegRing";
                case PaabComponentImportSetting.ComponentType.AccessoryProp:
                    return "Prop";
            }

            return "";
        }
        

        public void SetCharacter(CharacterInfo data)
        {
            if (data == null)
            {
                Debug.LogError("[ComponentListPanel] Character is null");
                return;
            }
            
            characterInfo = data;
            bool result = CharacterUtil.CreateCharacterFolder(characterInfo.character.name, false);
            if (!result)
            {
                Debug.LogError("[ComponentListPanel] Create character folder failed");
                return;
            }

            CharacterManager.instance.SetCurrentCharacterPath(characterInfo);
            //InitImportSettings();
            if(characterInfo.skeleton != null)
                DownloadSkeleton(characterInfo.skeleton);
            
            mainMenuIndex = 0;
            subMenuIndex = 0;   
        }

        public void OnMainMenuChanged(int index)
        {
            if (mainMenuIndex != index)
            {
                mainMenuIndex = index;
                subMenuIndex = 0;
            }
            subMenu.Refresh();
            //if (mainMenuIndex == 0)
                //OnSubMenuChanged(subMenuIndex);
        }

        public void OnSubMenuChanged(int index)
        {
            subMenuIndex = index;
            itemList.ClearAllCell();
            RequestAssetList();
        }


#region base function

        public override void OnShow()
        {
            base.OnShow();
            
            CharacterManager.instance.ClearCurrentCharacter();
            InitImportSettings();
            RequestCategory();
        }

        protected override bool BuildUIDOM(VisualElement parent) 
        {
            base.BuildUIDOM(parent);

            InitElements();
                
            return true;
        }

        protected override bool BindUIActions() 
        {
            refreshBtn.RegisterCallback<ClickEvent>(OnRefreshBtnClick);
            
            return base.BindUIActions();
        }
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            mainMenu.OnDestroy();
            subMenu.OnDestroy();
            itemList.OnDestroy();
            DestroyImportSettings();
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
            importSettings.SetAssetTypeName(AssetImportSettingsType.Component);
            importSettings.basicInfoSetting = new PaabBasicInfoImportSetting();
            importSettings.basicInfoSetting.characterId = characterInfo.character.character_id;
            importSettings.basicInfoSetting.characterFolderName = characterInfo.character.name;
            importSettings.basicInfoSetting.characterName = characterInfo.character.name;
            importSettings.basicInfoSetting.skeletonAssetName = characterInfo.skeleton.name;
            importSettings.hideFlags = HideFlags.DontSaveInEditor;
        }

        private void DestroyImportSettings()
        {
            if (importSettings == null)
                return;
            
            if(importSettings != null)
                GameObject.DestroyImmediate(importSettings, true);
            importSettings = null;
        }

        private void RequestCategory()
        {
            var request = AssetServerManager.instance.GetAssetCategory(characterInfo.character.character_id);
            request.Send(success =>
                {
                    var response = ProtocolUtil.GetResponse<AssetCategory>(success);
                    if (response == null || response.data == null)
                    {
                        Debug.LogError("Get response failed");
                        return;
                    }
                    else
                    {
                        assetCategory = response.data;
                        mainMenu.Refresh();
                    }
                },
                failure => { Debug.Log("RequestCategory...failed : " + failure.ToString()); });
        }

        private void RequestAssetList()
        {
            List<string> categoryList = new List<string>();
            categoryList.Add(assetCategory.data[mainMenuIndex].subs[subMenuIndex].key);

            ReqAssetList reqData = new ReqAssetList();
            reqData.category_keys = categoryList;
            reqData.need_paging = 0;
            reqData.character_id = characterInfo.character.character_id;
            int requestMainMenuIndex = mainMenuIndex;
            int requestSubMenuIndex = subMenuIndex;
            
            var request = AssetServerManager.instance.GetComponentAssetList(JsonUtility.ToJson(reqData));
            assetListLoadingWidget.ShowLoading();
            request.Send(success =>
                {
                    assetListLoadingWidget.HideLoading();
                    // 如果页签变换就丢弃数据
                    if (requestMainMenuIndex != mainMenuIndex || requestSubMenuIndex != subMenuIndex)
                        return;
                    
                    var response = ProtocolUtil.GetResponse<AssetList>(success);
                    if (response == null || response.data == null)
                    {
                        Debug.LogError("Get response failed");
                        return;
                    }
                    else
                    {
                        componentAssetList = response.data;
                        // store local asset info in PAAPRuntimeManager.
                        if (MainMenuUIManager.instance.PAAB_OPEN == false)
                        {
                            var data = reqData;
                            PAAPRuntimeManager manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();
                            if (localComponentAssetData.asset_info == null)
                                localComponentAssetData.asset_info = new AssetInfo();
                            localComponentAssetData.asset_info.name = manager.assetName;
                            localComponentAssetData.asset_info.asset_id = manager.assetPath;
                            localComponentAssetData.asset_info.status = AvatarAssetPreviewConst.PseudoLocalState;
                            localComponentAssetData.asset_info.cover = manager.assetBasicInfoSetting.assetIconPath;
                            
                            if (localComponentAssetData.category == null)
                                localComponentAssetData.category = new AssetCategoryEntry();
                            localComponentAssetData.category.key = ParseComponentTypeToComponentKey((PaabComponentImportSetting.ComponentType)(int)manager.assetComponentSetting.componentType);
                            localComponentAssetData.category.name = ParseComponentTypeToComponentKey((PaabComponentImportSetting.ComponentType)(int)manager.assetComponentSetting.componentType);
                            localComponentAssetData.category.asset_type = 11; //AvatarAssetType.Clothes todo
                            if ((int)manager.assetComponentSetting.componentType == (int)PaabComponentImportSetting.ComponentType.Hand)
                            {
                                localComponentAssetData.category.asset_type = 16; // AvatarAssetType.Glove
                            }
                            else if ((int)manager.assetComponentSetting.componentType == (int)PaabComponentImportSetting.ComponentType.ClothGloves)
                            {
                                localComponentAssetData.category.asset_type = 16; // AvatarAssetType.Glove
                            }
                            else if ((int)manager.assetComponentSetting.componentType == (int)PaabComponentImportSetting.ComponentType.Hair)
                            {
                                localComponentAssetData.category.asset_type = 13; // AvatarAssetType.Hair
                            }
                            else if ((int)manager.assetComponentSetting.componentType == (int)PaabComponentImportSetting.ComponentType.ClothShoes)
                            {
                                localComponentAssetData.category.asset_type = 12; // AvatarAssetType.Shoe
                            }
                            
                            if (manager.assetName != "" && reqData.category_keys.Contains(ParseComponentTypeToComponentKey((PaabComponentImportSetting.ComponentType)(int)manager.assetComponentSetting.componentType)))
                                componentAssetList.assets.Insert(0, localComponentAssetData);
                        }
                        itemList.Refresh();
                    }
                },
                failure =>
                {
                    assetListLoadingWidget.HideLoading();
                    Debug.Log("RequestAssetList...failed : " + failure.ToString());
                });
                
        }
        
        

        private void InitElements()
        {
            // createVE = mainElement.Q<VisualElement>("CreateNew");
            // if (MainMenuUIManager.instance.PAAB_OPEN == false)
            // {
            //     createVE.SetActive(false);
            // }
            // createBtn = mainElement.Q<Button>("CreateNewBtn");
            var mainMenuSV = mainElement.Q<ScrollView>("mainMenu");
            mainMenu = new PavScrollView(this, mainMenuSV);
            mainMenu.WrapMode = Wrap.NoWrap;
            var subMenuSV = mainElement.Q<ScrollView>("subMenu");
            subMenu = new PavScrollView(this, subMenuSV);
            subMenu.WrapMode = Wrap.NoWrap;
            var itemListSV = mainElement.Q<ScrollView>("itemList");
            itemList = new PavScrollView(this, itemListSV);
            
            mainMenu.CellCount = () => CellCount(mainMenu);
            mainMenu.CellAtIndex = index => CellAtIndex(mainMenu, index);
            mainMenu.DataAtIndex = index => DataAtIndex(mainMenu, index);
            
            subMenu.CellCount = () => CellCount(subMenu);
            subMenu.CellAtIndex = index => CellAtIndex(subMenu, index);
            subMenu.DataAtIndex = index => DataAtIndex(subMenu, index);
            
            itemList.CellCount = () => CellCount(itemList);
            itemList.CellAtIndex = index => CellAtIndex(itemList, index);
            itemList.DataAtIndex = index => DataAtIndex(itemList, index);
            
            refreshBtn = mainElement.Q<Button>("refreshBtn");
            assetListLoading = mainElement.Q("AssetListLoading");
            assetListLoadingWidget = new AssetListLoadingWidget(assetListLoading);
            AddWidget(assetListLoadingWidget);
            assetListLoadingWidget.ShowWidget();
            
            UIUtils.AddVisualElementHoverMask(refreshBtn, refreshBtn);
        }

        private bool NeedShowCreateNew()
        {
            if (MainMenuUIManager.instance.PAAB_OPEN == false)
                return false;

            // if (mainMenuIndex == 0 || subMenuIndex == 0)
            //     return false;

            return true;
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
            if (scrollView == mainMenu)
                return assetCategory.data.Count;

            if (scrollView == subMenu)
                return assetCategory.data[mainMenuIndex].subs.Count;

            if (scrollView == itemList)
            {
                if (NeedShowCreateNew())
                    return componentAssetList.assets.Count + 1;
                else
                    return componentAssetList.assets.Count;
            }
                

            return 0;
        }
        
        private Type CellAtIndex(PavScrollView scrollView, int index)
        {
            if (scrollView == mainMenu)
                return typeof(MainElementCell);
            if (scrollView == subMenu)
                return typeof(SubElementCell);
            if (scrollView == itemList)
            {
                if (NeedShowCreateNew() && index == 0)
                    return typeof(CreateComponentAssetCell);
                
                return typeof(ComponentAssetCell);
            }
            
            return null;
        }

        private PavScrollViewCellDataBase DataAtIndex(PavScrollView scrollView, int index)
        {
            if (scrollView == mainMenu)
            {
                return new ElementCellData()
                {
                    entry = assetCategory.data[index].category
                };
            }

            if (scrollView == subMenu)
            {
                return new ElementCellData()
                {
                    entry = assetCategory.data[mainMenuIndex].subs[index]
                };
            }

            
            if (scrollView == itemList)
            {
                if (NeedShowCreateNew() && index == 0)
                {
                    AssetCategoryEntry entry = null;
                    entry = assetCategory.data[mainMenuIndex].subs[subMenuIndex];
                    return new CreateComponentAssetCellData()
                    {
                        assetImportSettings = importSettings,
                        asset_type = entry.asset_type,
                        categoryKey = entry.key
                    };
                }
                return new ComponentAssetCellData()
                {
                    asset = componentAssetList.assets[NeedShowCreateNew() ? index - 1 : index],
                    assetImportSettings = importSettings
                };
            }  

            return null;
        }

#region UIEvent
        
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

    public class MainElementCell : PavScrollViewCell
    {
        private Button btn;
        private VisualElement line;
        private SimpleButtonToggle toggle;
        public override string AssetPath => "Assets/PicoAvatarAssetPreview/Editors/Views/UxmlWidget/ComponentListMainMenuCell.uxml";
        
        public override void OnInit()
        {
            base.OnInit();
            btn = _cellVisualElement.Q<Button>("btn");
            line = _cellVisualElement.Q("line");
            SetToggleState(false);
            
            var panel = GetPanel<ComponentListPanel>();
            toggle = new SimpleButtonToggle(panel.mainMenuToggleGroup, btn);
            toggle.onValueChange += OnValueChanged;
            UIUtils.AddVisualElementHoverMask(btn, btn, true);
        }

        public override void RefreshCell()
        {
            var data = GetData<ElementCellData>();
            var panel = GetPanel<ComponentListPanel>();
            if (data == null || panel == null)
                return;
            
            // if (Index == 0)
            // {
            //     btn.style.marginLeft = 16;
            //     btn.style.marginLeft = 16;
            //     line.style.marginLeft = 16;
            //     line.style.marginLeft = 16;
            // }

            btn.text = data.entry.name;
            if (panel.mainMenuIndex == Index)
                toggle.Toggle();
        }

        public void OnValueChanged(bool value)
        {
            SetToggleState(value);
            //btn.style.opacity = value ? 1 : 0.45f;
            if (value)
            {
                var panel = GetPanel<ComponentListPanel>();
                panel.OnMainMenuChanged(Index);
            }
        }
        
        private void SetToggleState(bool isOn)
        {
            line.SetActive(isOn);
            btn.style.color = new Color(1, 1, 1, (isOn ? 1 : 0.45f));
            btn.style.unityFontStyleAndWeight = (isOn ? FontStyle.Bold : FontStyle.Normal);
        }
    }
    
    public class SubElementCell : PavScrollViewCell
    {
        private Button btn;
        private VisualElement line;
        private SimpleButtonToggle toggle;
        public override string AssetPath => "Assets/PicoAvatarAssetPreview/Editors/Views/UxmlWidget/ComponentListSubMenuCell.uxml";
        
        public override void OnInit()
        {
            base.OnInit();
            btn = _cellVisualElement.Q<Button>("button");
            line = _cellVisualElement.Q("line");
            SetToggleState(false);
            
            var panel = GetPanel<ComponentListPanel>();
            toggle = new SimpleButtonToggle(panel.subMenuToggleGroup, btn);
            toggle.onValueChange += OnValueChanged;
            UIUtils.AddVisualElementHoverMask(btn, btn, true);
        }

        public override void RefreshCell()
        {
            var data = GetData<ElementCellData>();
            var panel = GetPanel<ComponentListPanel>();
            if (data == null || panel == null)
                return;

            btn.text = data.entry.name;
            if (panel.subMenuIndex == Index)
                toggle.Toggle();
        }

        public void OnValueChanged(bool value)
        {
            SetToggleState(value);
            if (value)
            {
                var panel = GetPanel<ComponentListPanel>();
                panel.OnSubMenuChanged(Index);
            }
        }

        private void SetToggleState(bool isOn)
        {
            line.SetActive(isOn);
            btn.style.opacity = isOn ? 1 : 0.45f;
            btn.style.unityFontStyleAndWeight = (isOn ? FontStyle.Bold : FontStyle.Normal);
        }
    }

    public class ElementCellData : PavScrollViewCellDataBase
    {
        public AssetCategoryEntry entry;
    }

    public class ComponentAssetCell : PavScrollViewCell
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
        private string assetId;
        private int assetType;
        public override string AssetPath => "Assets/PicoAvatarAssetPreview/Editors/Views/UxmlWidget/ComponentAssetCell.uxml";
        
        public override void OnInit()
        {
            base.OnInit();
            maskRoot = _cellVisualElement.Q("maskRoot");
            icon = _cellVisualElement.Q("icon");
            name = _cellVisualElement.Q<Label>("nameText");
            updateBtn = _cellVisualElement.Q<Button>("updateBtn");
            testIcon = _cellVisualElement.Q("testIcon");
            publishIcon = _cellVisualElement.Q("publishIcon");
            localIcon = _cellVisualElement.Q("localIcon");
            configIcon = _cellVisualElement.Q("configIcon");
            if (MainMenuUIManager.instance.PAAB_OPEN == true)
            {
                updateBtn.SetActive(true);
                updateBtn.RegisterCallback<ClickEvent>(OnUpdateBtnClick);
                UIUtils.AddVisualElementHoverMask(maskRoot, updateBtn, false, 0.12f);
            }
            else
            {
                PAAPRuntimeManager manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();
                updateBtn.SetActive(true);
                updateBtn.clickable.clicked += () => { manager.PutOnAsset(assetId, (AvatarAssetType)(assetType)); };
                UIUtils.AddVisualElementHoverMask(maskRoot, updateBtn, false, 0.12f);
            }
            iconImage = new WebImage(icon);
            
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            iconImage.OnDestroy();
            name = null;
        }

        public override void RefreshCell()
        {
            var data = GetData<ComponentAssetCellData>();
            if (data == null)
                return;
            
            name.text = data.asset.asset_info.name;
            iconImage.SetTexture(data.asset.asset_info.cover);
            assetType = data.asset.category.asset_type;
            assetId = data.asset.asset_info.asset_id;
            testIcon.SetActive(data.asset.asset_info.status == 2); // 测试中
            publishIcon.SetActive(data.asset.asset_info.status == 3); // 已发布
            localIcon.SetActive(data.asset.asset_info.status == AvatarAssetPreviewConst.PseudoLocalState); // 已发布
            configIcon.SetActive(data.asset.asset_info.status == 0); // 待配置
            EditorCoroutineUtility.StartCoroutineOwnerless(UpdateNameTip());
        }

        private IEnumerator UpdateNameTip()
        {
            yield return null;
            var data = GetData<ComponentAssetCellData>();
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
            var data = GetData<ComponentAssetCellData>();
            if (data == null)
                return;

            var panel = GetPanel<ComponentListPanel>();
            
            var componentType = ComponentListPanel.ParseComponentKeyToComponentType(data.asset.category.key);
            if (componentType == PaabComponentImportSetting.ComponentType.Invalid)
            {
                Debug.LogError($"Invalid category type {data.asset.category.key}");
                return;
            }

            data.assetImportSettings.basicInfoSetting.SetAssetName(data.asset.asset_info.name); 
            data.assetImportSettings.basicInfoSetting.SetAssetId(data.asset.asset_info.asset_id);
            data.assetImportSettings.basicInfoSetting.assetNickName = data.asset.asset_info.show_name;
            data.assetImportSettings.basicInfoSetting.assetIconPath = data.asset.asset_info.cover;
            data.assetImportSettings.opType = OperationType.UpdateAsset;
            
            var componentSetting = new PaabComponentImportSetting();
            if (panel.CharacterInfo.character.avatar_style == "PicoAvatar3")
                componentSetting.componentSource = PaabComponentImportSetting.ComponentSource.Official;
            else
                componentSetting.componentSource = PaabComponentImportSetting.ComponentSource.Custom;
            componentSetting.componentType = componentType;
            data.assetImportSettings.settingItems = new[]
            {
                componentSetting
            };
            
            GetPanel<ComponentListPanel>().ClearAssets();
            ConfigureComponentPanel.instance.SetForceClearData(true);
            CharacterManager.instance.SetCurrentCharacter(panel.CharacterInfo, OperationType.UpdateAsset);
            var baseBodyPanel = NavMenuBarRoute.instance.RouteNextByPanelName(panel.panelName, ConfigureComponentPanel.instance.panelName);
            if (baseBodyPanel is AssetImportSettingsPanel)
                (baseBodyPanel as AssetImportSettingsPanel).BindOrUpdateFromData(data.assetImportSettings);
        }
    }
    
    public class ComponentAssetCellData : PavScrollViewCellDataBase
    {
        public ServerAssetData asset;
        public PaabAssetImportSettings assetImportSettings;
    }

    public class CreateComponentAssetCell : PavScrollViewCell
    {
        private Button createBtn;
        public override string AssetPath => "Assets/PicoAvatarAssetPreview/Editors/Views/UxmlWidget/CreateNewComponentCell.uxml";


        public override void OnInit()
        {
            base.OnInit();
            createBtn = _cellVisualElement.Q<Button>("CreateNewBtn");
            createBtn.RegisterCallback<ClickEvent>(OnCreateBtnClick);
            //UIUtils.SetVisualElementMoveHoverAndUnhoverColor(createBtn);
            UIUtils.AddVisualElementHoverMask(createBtn, createBtn);
        }

        public override void RefreshCell()
        {
            
        }

        private void OnCreateBtnClick(ClickEvent evt)
        {
            var data = GetData<CreateComponentAssetCellData>();
            if (data == null)
                return;

            var panel = GetPanel<ComponentListPanel>();

            var componentType = ComponentListPanel.ParseComponentKeyToComponentType(data.categoryKey);
            if (componentType == PaabComponentImportSetting.ComponentType.Invalid)
            {
                Debug.LogError($"Invalid category type {data.categoryKey}");
                return;
            }
            
            data.assetImportSettings.basicInfoSetting.SetAssetName(""); 
            data.assetImportSettings.basicInfoSetting.SetAssetId("");
            data.assetImportSettings.basicInfoSetting.assetNickName = "";
            data.assetImportSettings.basicInfoSetting.assetIconPath = "";
            data.assetImportSettings.opType = OperationType.CreateAsset;
            
            var componentSetting = new PaabComponentImportSetting();
            if (panel.CharacterInfo.character.avatar_style == "PicoAvatar3")
                componentSetting.componentSource = PaabComponentImportSetting.ComponentSource.Official;
            else
                componentSetting.componentSource = PaabComponentImportSetting.ComponentSource.Custom;
            componentSetting.componentType = componentType;
            data.assetImportSettings.settingItems = new[]
            {
                componentSetting
            };
            
            CharacterManager.instance.SetCurrentCharacter(panel.CharacterInfo, OperationType.CreateAsset);
            GetPanel<ComponentListPanel>().ClearAssets();
            ConfigureComponentPanel.instance.SetForceClearData(true);
            var baseBodyPanel = NavMenuBarRoute.instance.RouteNextByPanelName(panel.panelName, ConfigureComponentPanel.instance.panelName);;
            if (baseBodyPanel is AssetImportSettingsPanel)
                (baseBodyPanel as AssetImportSettingsPanel).BindOrUpdateFromData(data.assetImportSettings);
        }
    }
    
    public class CreateComponentAssetCellData : PavScrollViewCellDataBase
    {
        public PaabAssetImportSettings assetImportSettings;
        public int asset_type;
        public string categoryKey;
    }
}
#endif