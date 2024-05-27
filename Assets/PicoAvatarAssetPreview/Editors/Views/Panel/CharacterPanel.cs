#if UNITY_EDITOR
using Pico.Avatar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssemblyCSharp.Assets.AmzAvatar.TestTools;
using Pico.AvatarAssetPreview;
using Pico.AvatarAssetPreview.Protocol;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using CharacterInfo = Pico.AvatarAssetPreview.Protocol.CharacterInfo;
using Label = UnityEngine.UIElements.Label;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class CharacterPanel : AssetImportSettingsPanel, IPavPanelExtra
        {
            // 控件
            private Button createNewBtn;
            private VisualElement createNewVE;
            private PavScrollView pavScrollView;
            private VisualElement assetListLoading;
            
            private AssetListLoadingWidget assetListLoadingWidget;


            private List<CharacterCellData> chararcterListData = new List<CharacterCellData>();
            
            
            
            public override string displayName { get => "Home"; }
            public override string panelName { get => "CharacterPanel"; }
            public override string uxmlPathName { get => "Uxml/CharacterPanel.uxml"; }
            
            private static CharacterPanel _instance;
            
            
            // menu button (bottom)
            const string k_HomeScreenMenuButton = "menu__test-button";
            

            // UI Buttons
            Button m_TestButton;
            
            
            // 
            public static CharacterPanel instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<CharacterPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/CharacterPanel.asset");
                    }
                    return _instance;
                }
            }

            public bool CheckNavShowWarningWhenSelfIsShow()
            {
                return false;
            }

            public void OnRefresh()
            {
                pavScrollView.ClearAllCell();
                RequestCharacterList();
            }

            public bool IsRefreshVisible()
            {
                return true;
            }

#region base function

            public override void OnShow()
            {
                base.OnShow();
                pavScrollView.ClearAllCell();
                RequestCharacterList();
                PICOMenuBar.OnGetSvrAppInfo -= OnGetSvrAppInfo;
                PICOMenuBar.OnGetSvrAppInfo += OnGetSvrAppInfo;
            }

            public override void OnDestroy()
            {
                base.OnDestroy();
                pavScrollView.OnDestroy();
                //
                if(_instance == this)
                {
                    _instance = null;
                }
            }
            
            protected override bool BuildUIDOM(VisualElement parent) //SetVisualElements and BuildWithUxml
            {
                base.BuildUIDOM(parent);

                InitElements();
                
                return true;
            }

            protected override bool BindUIActions() //RegisterButtonCallbacks
            {
                // register action when each button is clicked
                createNewBtn?.RegisterCallback<ClickEvent>(OnCreateNewBtnClick);
                
                
                return base.BindUIActions();
            }

#endregion


#region 私有函数

            private void InitElements()
            {
                createNewVE = mainElement.Q("CreateNew");
                assetListLoading = mainElement.Q("AssetListLoading");
                UpdateCreateNewBtn();
                
                createNewBtn = mainElement.Q<Button>("CreateNewBtn");

                var scrollView = mainElement.Q<ScrollView>("characterList");
                pavScrollView = new PavScrollView(this, scrollView);
                pavScrollView.Direction = ScrollViewDirection.Horizontal;
                pavScrollView.CellCount = CellCount;
                pavScrollView.CellAtIndex = CellAtIndex;
                pavScrollView.DataAtIndex = DataAtIndex;
                
                UIUtils.AddVisualElementHoverMask(createNewBtn, createNewBtn);
                
                assetListLoadingWidget = new AssetListLoadingWidget(assetListLoading);
                AddWidget(assetListLoadingWidget);
                assetListLoadingWidget.ShowWidget();
            }

            private void UpdateCreateNewBtn()
            {
                if (createNewVE == null)
                    return;
                
                if (MainMenuUIManager.instance.PAAB_OPEN == false)
                {
                    createNewVE.SetActive(false);
                }
                else
                {
                    createNewVE.SetActive(LoginUtils.HasBindInfo());
                }
            }
            

            private void RequestCharacterList()
            {
                if (!LoginUtils.HasBindInfo())
                    return;
                
                var request = AssetServerManager.instance.GetCharacterListByApp();
                assetListLoadingWidget.ShowLoading();
                request.Send(success =>
                    {
                        // Debug.Log("GetCharacterListByApp...success= " + success.ToString());
                        assetListLoadingWidget.HideLoading();
                        var response = ProtocolUtil.GetResponse<CharacterList>(success);
                        if (response == null || response.data == null)
                        {
                            Debug.LogError("Get response failed");
                            chararcterListData.Clear();
                            pavScrollView.ClearAllCell();
                            return;
                        }
                        else
                            UpdateCharacterList(response.data);
                    },
                    failure =>
                    {
                        assetListLoadingWidget.HideLoading();
                        Debug.Log("GetCharacterListByApp...failed : " + failure.ToString());
                    });
            }

            private void UpdateCharacterList(CharacterList data)
            {
                UpdateCreateNewBtn();
                chararcterListData.Clear();
                foreach (var info in data.characters)
                {
                    if (MainMenuUIManager.instance.PAAB_OPEN == false)
                    {
                        PAAPRuntimeManager manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();
                        if (manager.avatarName == info.character.name)
                        {
                            chararcterListData.Add(new CharacterCellData() { characterData = info });
                        }
                        else if (manager.avatarName == "")
                        {
                            chararcterListData.Add(new CharacterCellData() { characterData = info });
                        }
                        else if (manager.avatarName == CharacterUtil.NewCharacterTempName)
                        {
                            CharacterCellData cellData = new CharacterCellData();
                            cellData.characterData = new CharacterInfo();
                            cellData.characterData.app = new CharacterApp();
                            cellData.characterData.app.is_official = false;
                            cellData.characterData.base_animation_set = new AssetInfo();
                            cellData.characterData.base_animation_set.offline_config = manager.characterinfo.base_animation_set.offline_config;
                            cellData.characterData.base_body = new AssetInfo();
                            cellData.characterData.skeleton = new AssetInfo();
                            cellData.characterData.character = new CharacterBaseInfo();
                            cellData.characterData.character.config = manager.avatarSpec;
                            cellData.characterData.character.status = AvatarAssetPreviewConst.PseudoLocalState;
                            cellData.characterData.character.show_name = manager.characterinfo.character.show_name;
                            cellData.characterData.character.name = manager.characterinfo.character.show_name;
                            cellData.characterData.character.cover = manager.characterinfo.character.cover;
                            cellData.characterData.character.character_id = "";
                            cellData.characterData.character.avatar_style = "PicoCustomAvatar";
                            chararcterListData.Add(cellData);
                            break;
                        }
                    }
                    else
                    {
                        chararcterListData.Add(new CharacterCellData() { characterData = info });
                    }
                }
                pavScrollView.Refresh();
            }

            private void OnGetSvrAppInfo(bool success)
            {
                if (success)
                    RequestCharacterList();
            }

#region UI控件事件回调函数

            private void OnCreateNewBtnClick(ClickEvent evt)
            {
                //CharacterUtil.SetCharacterFolderPath(true);
                ConfigureNewCharacterPanel.instance?.InitNewCharacter();
                NavMenuBarRoute.instance.RouteNextByType(PanelType.CharacterPanel,PanelType.ConfigureNewCharacter);
            }
            
            private void OnRefreshBtnClick(ClickEvent evt)
            {
                RequestCharacterList();
            }

#endregion

#region 滑动列表

            public int CellCount()
            {
                return chararcterListData.Count;
            }
    
            public Type CellAtIndex(int index)
            {
                return typeof(CharacterCell);
            }
    
            public PavScrollViewCellDataBase DataAtIndex(int index)
            {
                if (chararcterListData == null)
                    return null;

                return chararcterListData[index];
            }
            

#endregion

#endregion

            
        }

        public enum CharacterCellType
        {
            Official,
            My,
        }

        public enum CharacterCellBtnType
        {
            Component,
            AnimationSet
        }

        public class CharacterCellData : PavScrollViewCellDataBase
        {
            public CharacterInfo characterData;
        }

        public class CharacterCell : PavScrollViewCell
        {
            public override string AssetPath => "Assets/PicoAvatarAssetPreview/Editors/Views/UxmlWidget/CharacterCell.uxml";
            private VisualElement root;
            private VisualElement hoverMaskRoot;
            private Label name;
            private Button btn1;
            private Button btn2;
            private VisualElement officialBg;
            private VisualElement myBg;
            private VisualElement officialTag;
            private Label officiaTagLabel;
            private VisualElement myTag;
            private Button updateBtn;
            private Button tryOnBtn;
            private Button subMenuBtn;
            private VisualElement icon;
            private VisualElement testIcon;
            private VisualElement publishIcon;
            private VisualElement localIcon;
            //private CharacterCellType cellType;
            private bool isOfficial = false;
            private bool isMyApp = false;
            private bool canUpdate = false;
            private List<CharacterCellBtnType> btnList = new List<CharacterCellBtnType>();

            private WebImage iconImg;
            

            public override void OnInit()
            {
                base.OnInit();
                root = _cellVisualElement.Q("cellRoot");
                hoverMaskRoot = _cellVisualElement.Q("hoverMaskRoot");
                name = _cellVisualElement.Q<Label>("nameText");
                btn1 = _cellVisualElement.Q<Button>("btn1");
                btn2 = _cellVisualElement.Q<Button>("btn2");
                officialBg = _cellVisualElement.Q("officialBg");
                myBg = _cellVisualElement.Q("myBg");
                officialTag = _cellVisualElement.Q("officialTag");
                officiaTagLabel = _cellVisualElement.Q<Label>("officailTagText");
                myTag = _cellVisualElement.Q("myTag");
                updateBtn = _cellVisualElement.Q<Button>("updateBtn");
                tryOnBtn = _cellVisualElement.Q<Button>("tryOnBtn");
                subMenuBtn = _cellVisualElement.Q<Button>("menuBtn");
                testIcon = _cellVisualElement.Q("testIcon");
                publishIcon = _cellVisualElement.Q("publishIcon");
                localIcon = _cellVisualElement.Q("localIcon");
                icon = _cellVisualElement.Q("icon");
                iconImg = new WebImage(icon);
                
                btn1.RegisterCallback<ClickEvent>(@event =>
                {
                    OnButtonClick(0);
                });
                
                btn2.RegisterCallback<ClickEvent>(@event =>
                { 
                    OnButtonClick(1);
                });
                
                updateBtn.RegisterCallback<ClickEvent>(OnUpdateBtnClick);
                tryOnBtn.RegisterCallback<ClickEvent>(OnTryOnBtnClick);
                UIUtils.AddVisualElementHoverMask(hoverMaskRoot, root);
                UIUtils.AddVisualElementHoverMask(btn1);
                UIUtils.AddVisualElementHoverMask(btn2);
                UIUtils.AddVisualElementHoverMask(updateBtn);
            }

            public override void OnDestroy()
            {
                base.OnDestroy();
                iconImg.OnDestroy();
                name = null;
            }

            public override void RefreshCell()
            {
                var data = GetData<CharacterCellData>();
                if (data == null)
                    return;

                UpdateCellType();
                UpdateButton();
                name.text = data.characterData.character.name;
                officialTag.SetActive(isOfficial);
                myTag.SetActive(!isOfficial);
                officialBg.SetActive(isOfficial);
                myBg.SetActive(!isOfficial);
                iconImg.SetTexture(data.characterData.character.cover);
                testIcon.SetActive(data.characterData.character.status == 2); // 测试中
                publishIcon.SetActive(data.characterData.character.status == 3); // 已发布
                localIcon.SetActive(data.characterData.character.status == AvatarAssetPreviewConst.PseudoLocalState); // 已发布

                EditorCoroutineUtility.StartCoroutineOwnerless(UpdateNameTip());
            }

            private IEnumerator UpdateNameTip()
            {
                yield return null;
                var data = GetData<CharacterCellData>();
                if (name == null || data == null)
                    yield break;
                
                var size = name.MeasureTextSize(data.characterData.character.name, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                name.pickingMode = size.x >= name.resolvedStyle.maxWidth.value
                    ? PickingMode.Position
                    : PickingMode.Ignore;
                UIUtils.SetTipText(name, data.characterData.character.name, 400);
            }

            private void UpdateCellType()
            {
                var data = GetData<CharacterCellData>();
                if (data == null)
                    return;
                //isOfficial = data.characterData.app.is_official;
                isOfficial = data.characterData.character.avatar_style == "PicoAvatar3";
                // var loginData = LoginUtils.LoadLoginSetting();
                // isMyApp = !string.IsNullOrEmpty(loginData.appID) && data.characterData.app.pico_app_id == loginData.appID;
                // canUpdate = isMyApp && data.characterData.character.avatar_style != "PicoAvatar3";
            }

            private void UpdateButton()
            {
                var loginData = LoginUtils.LoadLoginSetting();
                btnList.Clear();
                if (isOfficial)
                {
                    if (loginData.canUploadOfficialComponent)
                        btnList.Add(CharacterCellBtnType.Component);
                }
                else
                {
                    //Note: hide component upload in non-offitial app
                    if (loginData.isOfficial)
                    {
                        btnList.Add(CharacterCellBtnType.Component);
                    }
                }
                    
                    
                btnList.Add(CharacterCellBtnType.AnimationSet);

                btn2.SetActive(false);
                btn1.text = GetBtnText(btnList[0]);
                if (btnList.Count == 2)
                {
                    btn2.SetActive(true);
                    btn2.text = GetBtnText(btnList[1]);
                }
                
                updateBtn.SetActive(MainMenuUIManager.instance.PAAB_OPEN && !isOfficial);
                tryOnBtn.SetActive(!MainMenuUIManager.instance.PAAB_OPEN);

                var resultfromPAAB = LoginUtils.LoadLoginSetting();

                // appType 0 : join in our app
                // appType 1 : not in our app
                if (!isOfficial && resultfromPAAB.appType == 1)
                {
                    btn1.SetEnabled(false);
                    btn2.SetEnabled(false);
                }
            }


            private string GetBtnText(CharacterCellBtnType cellBtnType)
            {
                switch (cellBtnType)
                {
                    case CharacterCellBtnType.Component:
                        return "Component";
                    case CharacterCellBtnType.AnimationSet:
                        return "AnimationSet";
                }

                return "";
            }

            private bool CanShowOfficalComponent()
            {
                var data = GetData<CharacterCellData>();
                if (data == null)
                    return false;
                
                var loginData = LoginUtils.LoadLoginSetting();
                if (!string.IsNullOrEmpty(loginData.appID) && data.characterData.app.pico_app_id == loginData.appID)
                    return true;

                return false;
            }
            
            private void OnButtonClick(int index)
            {
                var btnType = btnList[index];
                switch (btnType)
                {
                    case CharacterCellBtnType.Component:
                        OnComponentBtnClick();
                        break;
                    
                    case CharacterCellBtnType.AnimationSet:
                        OnAnimationSetClick();
                        break;
                }
            }

            private void OnComponentBtnClick()
            {
                var data = GetData<CharacterCellData>();
                if (data == null)
                    return;
                
                ComponentListPanel.instance.SetCharacter(data.characterData);
                NavMenuBarRoute.instance.RouteNextByType(PanelType.CharacterPanel,PanelType.ComponentListPanel);
                if (MainMenuUIManager.instance.PAAB_OPEN == false)
                {
                    Debug.LogWarning("Current Character Spec Config: " + data.characterData.character.config);
                    PAAPRuntimeManager manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();
                    if (data.characterData.character.config != "")
                    {
                        // the avatarName is null, it means we can switch the character.
                        if (manager.avatarName == "")
                        {
                            manager.avatarId = data.characterData.character.character_id;
                            manager.fromPresetPanel = false;
                            manager.LoadAvatarFromSpec(data.characterData.character.config);
                        }
                    }
                    else
                    {
                        manager.fromPresetPanel = false;
                        manager.LoadAvatarFromSpec("{\"info\":{\"sex\":\"male\",\"status\":\"Online\",\"tag_list\":[\"MurderMystery\"],\"continent\":\"EU\",\"background\":{\"image\":\"https: //dfsedffe.png\",\"end_color\":[133,182,255],\"start_color\":[148,111,253]},\"avatar_type\":\"preset\"},\"avatar\":{\"body\":{\"version\":1,\"perParams\":[],\"technique\":\"Pico2-Bone\",\"floatIdParams\":[]},\"head\":{\"version\":1,\"perParams\":[],\"technique\":\"Pico2-BS\",\"floatIdParams\":[]},\"skin\":{\"color\":\"\",\"white\":0,\"softening\":0},\"skeleton\":{\"assetId\":\"1807115345356439552\"},\"baseAnimation\":{\"assetId\":\"1807116171483332608\"},\"baseBody\":{\"assetId\":\"1808605820911403008\"},\"assetPins\":[],\"nextWearTimeStamp\":18},\"avatarStyle\":\"PicoCustomAvatar\"}");
                    }                    
                }
            }

            private void OnAnimationSetClick()
            {
                var data = GetData<CharacterCellData>();
                if (data == null)
                    return;
                
                AnimationListPanel.instance.SetCharacter(data.characterData);
                NavMenuBarRoute.instance.RouteNextByPanelName("CharacterPanel", "AnimationListPanel");
                
                if (MainMenuUIManager.instance.PAAB_OPEN == false)
                {
                    Debug.LogWarning("Current Character Spec Config: " + data.characterData.character.config);
                    PAAPRuntimeManager manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();
                    
                    if (data.characterData.character.config != "")
                    {
                        // the avatarName is null, it means we can switch the character.
                        if (manager.avatarName == "")
                        {
                            manager.avatarId = data.characterData.character.character_id;
                            manager.fromPresetPanel = false;
                            manager.LoadAvatarFromSpec(data.characterData.character.config);
                        }
                    }
                    else
                    {
                        manager.fromPresetPanel = false;
                        manager.LoadAvatarFromSpec("{\"info\":{\"sex\":\"male\",\"status\":\"Online\",\"tag_list\":[\"MurderMystery\"],\"continent\":\"EU\",\"background\":{\"image\":\"https: //dfsedffe.png\",\"end_color\":[133,182,255],\"start_color\":[148,111,253]},\"avatar_type\":\"preset\"},\"avatar\":{\"body\":{\"version\":1,\"perParams\":[],\"technique\":\"Pico2-Bone\",\"floatIdParams\":[]},\"head\":{\"version\":1,\"perParams\":[],\"technique\":\"Pico2-BS\",\"floatIdParams\":[]},\"skin\":{\"color\":\"\",\"white\":0,\"softening\":0},\"skeleton\":{\"assetId\":\"1807115345356439552\"},\"baseAnimation\":{\"assetId\":\"1807116171483332608\"},\"baseBody\":{\"assetId\":\"1808605820911403008\"},\"assetPins\":[],\"nextWearTimeStamp\":18},\"avatarStyle\":\"PicoCustomAvatar\"}");
                    }                    
                }
            }
            
            private void OnTryOnBtnClick(ClickEvent @event)
            {
                var data = GetData<CharacterCellData>();
                if (data == null)
                    return;
                
                if (MainMenuUIManager.instance.PAAB_OPEN == false) // preview mode.
                // {
                //     Debug.LogWarning("Current Character Spec Config: " + data.characterData.character.config);
                // }
                // else // editor mode for debug.
                {
                    // to CharacterAndPresetPanel.
                    // to AnimationListPanel first.
                    // then to CharacterAndPresetPanel,and CharacterAndPresetPanel is same as AnimationListPanel.
                    CharacterAndPresetPanel.instance.SetCharacter(data.characterData);
                    NavMenuBarRoute.instance.RouteNextByPanelName("CharacterPanel", "CharacterAndPresetPanel");
                }
            }

            private void OnUpdateBtnClick(ClickEvent @event)
            {
                // if (cellType != CharacterCellType.My)
                //     return;
                
                var data = GetData<CharacterCellData>();
                if (data == null)
                    return;
                
                UpdateCharacterPanel.instance.SetData(data.characterData);
                NavMenuBarRoute.instance.RouteNextByType(PanelType.CharacterPanel, PanelType.UpdateCharacter);
            }
        }
    }
}
#endif