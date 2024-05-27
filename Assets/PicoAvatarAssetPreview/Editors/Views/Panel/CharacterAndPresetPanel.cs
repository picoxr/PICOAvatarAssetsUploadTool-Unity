#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AssemblyCSharp.Assets.AmzAvatar.TestTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pico.Avatar;
using Pico.AvatarAssetPreview.Protocol;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using CharacterInfo = Pico.AvatarAssetPreview.Protocol.CharacterInfo;
using Label = UnityEngine.UIElements.Label;

namespace Pico.AvatarAssetPreview
{
    public class CharacterAndPresetPanel : PavPanel, IPavPanelExtra
    {
        private Button refreshBtn;
        private PavScrollView characterAvatarListSV;
        private PavScrollView presetAvatarListSV;

        private CharacterInfo characterInfo;
        private PresetList presetAvatarList;
        
        private VisualElement assetListLoading;
        private AssetListLoadingWidget assetListLoadingWidget;

        private Label characterAvatarLabel;
        private Label presetAvatarLabel;
        
        public override string displayName
        {
            get
            {
                return "CharacterAndPresetPanel";
            }
        }

        public override string panelName { get => "CharacterAndPresetPanel"; }
        public override string uxmlPathName { get => "Uxml/CharacterAndPresetPanel.uxml"; }
        
        public CharacterInfo CharacterInfo => characterInfo;
        
        private static CharacterAndPresetPanel _instance;
        
        // to store presetCharacterCellData
        private List<CharacterAndPresetCellData> characterAndPresetCellDataList = new List<CharacterAndPresetCellData>();

        public static CharacterAndPresetPanel instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Utils.LoadOrCreateAsset<CharacterAndPresetPanel>(
                        AssetBuilderConfig.instance.uiDataStorePath + "PanelData/CharacterAndPresetPanel.asset");
                }
                return _instance;
            }
        }

        #region base function
        
        public override void OnShow()
        {
            base.OnShow();
            CharacterManager.instance.ClearCurrentCharacter();
            RequestAssetList();
        }

        protected override bool BuildUIDOM(VisualElement parent) //SetVisualElements and BuildWithUxml
        {
            base.BuildUIDOM(parent);

            InitElements();
                
            return true;
        }

        protected override bool BindUIActions() //RegisterButtonCallbacks
        {
            refreshBtn.RegisterCallback<ClickEvent>(OnRefreshBtnClick);
            return base.BindUIActions();
        }
        
        public override void OnHide()
        {
            characterAvatarListSV.OnDestroy();
            presetAvatarListSV.OnDestroy();
            characterInfo = null;
            presetAvatarList = new PresetList();
            characterAndPresetCellDataList = new List<CharacterAndPresetCellData>();
            
            //
            if(_instance == this)
            {
                _instance = null;
            }
        }
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            characterAvatarListSV.OnDestroy();
            presetAvatarListSV.OnDestroy();
            characterInfo = null;
            presetAvatarList = new PresetList();
            characterAndPresetCellDataList = new List<CharacterAndPresetCellData>();
            
            //
            if(_instance == this)
            {
                _instance = null;
            }
        }
        
        public void ClearAssets()
        {
            characterAvatarListSV.ClearAllCell();
        }
        
        
#endregion

#region 私有函数

        // 这里需要通过角色id获取得到预设形象列表
        private void RequestAssetList()
        {
            if (characterInfo == null)
                return;

            var characterId = characterInfo.character.character_id;
            if (characterId == "")
                return;
            var request = AssetServerManager.instance.GetPresetList(characterId);
            assetListLoadingWidget.ShowLoading();
            request.Send(success =>
                {
                    assetListLoadingWidget.HideLoading();

                    if (characterInfo == null || characterId != characterInfo.character.character_id)
                        return;
                    //这里不用AssetList，用一个新的数据结构看看
                    var response = ProtocolUtil.GetResponse<PresetList>(success);
                    if (response == null || response.data == null)
                    {
                        Debug.LogError("Get response failed");
                    }
                    else
                    {
                        presetAvatarList = response.data;
                        
                        for (int i = 0; i < presetAvatarList.count; i++)
                        {
                            characterAndPresetCellDataList.Add(new CharacterAndPresetCellData()
                            {
                                name = presetAvatarList.presets[i].preset.show_name,
                                specJson = presetAvatarList.presets[i].preset.config,
                                iconImage = presetAvatarList.presets[i].preset.cover,
                                id = characterId,
                                showid = presetAvatarList.presets[i].preset.preset_id,
                            });
                        }
                        characterAvatarListSV.Refresh();
                        presetAvatarListSV.Refresh();
                    }
                },
                failure =>
                {
                    assetListLoadingWidget.HideLoading();
                    Debug.Log("RequestAnimationSetAssetList...failed : " + failure.ToString());
                });
        }
        
        private void InitElements()
        {
         
            characterAvatarLabel = mainElement.Q<Label>("characterAvatarLabel");
            characterAvatarLabel.style.color = new StyleColor(new Color(210, 210, 210, 1.0f));
            presetAvatarLabel = mainElement.Q<Label>("presetAvatarLabel");
            presetAvatarLabel.style.color = new StyleColor(new Color(210, 210, 210, 1.0f));

            refreshBtn = mainElement.Q<Button>("refreshBtn");
            
            var characterListSvInUxml = mainElement.Q<ScrollView>("characterAvatarList");
            characterAvatarListSV = new PavScrollView(this, characterListSvInUxml);

            var presetListSvInUxml = mainElement.Q<ScrollView>("presetAvatarList");
            presetAvatarListSV = new PavScrollView(this, presetListSvInUxml);
            presetAvatarListSV.Direction = ScrollViewDirection.Vertical;
            presetAvatarListSV.WrapMode = Wrap.NoWrap;
            
            assetListLoading = mainElement.Q("AssetListLoading");
            assetListLoadingWidget = new AssetListLoadingWidget(assetListLoading);
            AddWidget(assetListLoadingWidget);
            assetListLoadingWidget.ShowWidget();

            characterAvatarListSV.CellCount = () => CellCount(characterAvatarListSV);
            characterAvatarListSV.CellAtIndex = (index) => CellAtIndex(characterAvatarListSV, index);
            characterAvatarListSV.DataAtIndex = (index) => DataAtIndex(characterAvatarListSV, index);
            
            presetAvatarListSV.CellCount = () => CellCount(presetAvatarListSV);
            presetAvatarListSV.CellAtIndex = (index) => CellAtIndex(presetAvatarListSV, index);
            presetAvatarListSV.DataAtIndex = (index) => DataAtIndex(presetAvatarListSV, index);
            
            UIUtils.AddVisualElementHoverMask(refreshBtn, refreshBtn);
        }

        public void SetCharacter(CharacterInfo data)
        {
            if (data == null)
            {
                Debug.LogError("[AnimationListPanel] Character is null");
                return;
            }

            characterInfo = data;
        }
        
        private int CellCount(PavScrollView scrollView)
        {
            if (scrollView == characterAvatarListSV)
            {
                if (characterInfo == null)
                    return 0;
                return 1;
            }

            if (scrollView == presetAvatarListSV)
                return presetAvatarList.count;
            
            return 0;
        }
        
        private Type CellAtIndex(PavScrollView scrollView, int index)
        {
            if (scrollView == characterAvatarListSV)
                return typeof(CharacterAndPresetCell);

            if (scrollView == presetAvatarListSV)
                return typeof(CharacterAndPresetCell);
            
            return null;
        }

        private PavScrollViewCellDataBase DataAtIndex(PavScrollView scrollView, int index)
        {
            if (scrollView == characterAvatarListSV)
            {
                if (characterInfo == null)
                    return null;
                
                return new CharacterAndPresetCellData()
                {
                    name = characterInfo.character.show_name,
                    specJson = characterInfo.character.config,
                    iconImage = characterInfo.character.cover,
                    id = characterInfo.character.character_id,
                    showid = characterInfo.character.character_id,
                };
            }

            if (scrollView == presetAvatarListSV)
                return characterAndPresetCellDataList[index];

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
    
    public class CharacterAndPresetCellData : PavScrollViewCellDataBase
    {
        public string name;
        public string specJson;
        public string iconImage;
        public string id;
        public string showid;
    }
    
    public class CharacterAndPresetCell : PavScrollViewCell
    {
        private Button playBtn;
        private VisualElement iconVe;
        private Label nameLabel;
        private Label idLabel;
        
        private WebImage iconImg;
        public override string AssetPath => "Assets/PicoAvatarAssetPreview/Editors/Views/UxmlWidget/CharacterAndPresetCell.uxml";

        public override void OnInit()
        {
            base.OnInit();
            playBtn = _cellVisualElement.Q<Button>("btn");
            playBtn.RegisterCallback<ClickEvent>(OnPlayBtnClick);
            UIUtils.AddVisualElementHoverMask(playBtn);
            
            iconVe = _cellVisualElement.Q<VisualElement>("icon");
            nameLabel = _cellVisualElement.Q<Label>("name_label");
            idLabel = _cellVisualElement.Q<Label>("id_label");
            
        }

        public override void RefreshCell()
        {
            var data = GetData<CharacterAndPresetCellData>();
            if (data == null)
                return;

            // playBtn.text = data.name;
            
            iconImg = new WebImage(iconVe);
            iconImg.SetTexture(data.iconImage);

            nameLabel.text = data.name;
            idLabel.text = data.showid;
        }

        private void OnPlayBtnClick(ClickEvent @event)
        {
            var data = GetData<CharacterAndPresetCellData>();
            if (data == null)
                return;
            
            if (MainMenuUIManager.instance.PAAB_OPEN == false) // in preview mode.
            {
                Debug.LogWarning("Current Character Spec Config: " + data.specJson);
                PAAPRuntimeManager manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();
                // the avatarName is null, it means we can switch the character.
                if (data.specJson != "")
                {
                    if (manager.avatarName == "")
                    {
                        manager.avatarId = data.id;
                        manager.fromPresetPanel = true;
                        manager.LoadAvatarFromSpec(data.specJson);
                    }
                }
            }
        }
    }
    
}
#endif