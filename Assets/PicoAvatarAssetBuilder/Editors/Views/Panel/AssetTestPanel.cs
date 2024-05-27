#if UNITY_EDITOR
using System;
using AssemblyCSharp.Assets.AmzAvatar.TestTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pico.Avatar;
using Pico.AvatarAssetBuilder.Protocol;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using CharacterInfo = UnityEngine.CharacterInfo;

namespace Pico.AvatarAssetBuilder
{
    public class AssetTestPanel : PavPanel
    {
        private Button testBtn;
        private Button nextBtn;

        private string sourcePanel;
        private object panelData;
        
        public override string displayName { get => "Pre-Test Avatar"; }
        public override string panelName { get => "AssetTestPanel"; }
        public override string uxmlPathName { get => "Uxml/AssetTestPanel.uxml"; }
            
        private static AssetTestPanel _instance;

        public bool FromSidebar
        {
            get => _fromSidebar;
            set
            {
                _fromSidebar = value;
                RefreshUIButton();
            }
        }

        private bool _fromSidebar;
        public static AssetTestPanel instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Utils.LoadOrCreateAsset<AssetTestPanel>(
                        AssetBuilderConfig.instance.uiDataStorePath + "PanelData/AssetTestPanel.asset");
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 开启资源检查界面
        /// </summary>
        /// <param name="sourcePanel">来源页面名称</param>
        /// <param name="data">发送协议所需数据</param>
        public static void ShowAssetTestPanel(PavPanel sourcePanel, object data)
        {
            //var panel = NavMenuBarRoute.instance.RouteNextByPanelName(sourcePanel.panelName, "AssetTestPanel");
            instance.InitPanel(sourcePanel.panelName, data);
            instance.OnNextBtnClick(null);
        }


        
#region base function
        
        public override void OnShow()
        {
            base.OnShow();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            //
            if(_instance == this)
            {
                _instance = null;
            }
        }
        
        public override void OnHide()
        {
            Debug.Log("exit the Test panel, stop the running scene.");
            base.OnHide();
            if (Application.isPlaying == true)
            {
                string sceneName = SceneManager.GetActiveScene().name;
                if(sceneName.Equals("AssetPreviewV3"))
                    EditorApplication.ExitPlaymode();
                testBtn.text = "Test";
            }
        }
        
        public override void OnUpdate()
        {
            base.OnUpdate();
            if (Application.isPlaying == false)
            {
                testBtn.text = "Test";
            }
            else if (Application.isPlaying == true)
            {
                testBtn.text = "Stop Test";
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
            testBtn.RegisterCallback<ClickEvent>(OnTestBtnClick);
            nextBtn.RegisterCallback<ClickEvent>(OnNextBtnClick);
            FromSidebar = false;
            return base.BindUIActions();
        }
        
#endregion
        
#region 公有函数

        public void InitPanel(string sourcePanel, object data)
        {
            this.sourcePanel = sourcePanel;
            panelData = data;
        }

        public void RefreshUIButton()
        {
            nextBtn.style.display = _fromSidebar ? DisplayStyle.None : DisplayStyle.Flex;
        }

#endregion

#region 私有函数

        private void InitElements()
        {
            testBtn = mainElement.Q<Button>("testBtn");
            nextBtn = mainElement.Q<Button>("nextBtn");
            
            UIUtils.AddVisualElementHoverMask(testBtn);
            UIUtils.AddVisualElementHoverMask(nextBtn);
        }

        private void DoNewCharacterNext()
        {
            // TODO @mingfei 跳转上传弹窗
            if (!string.IsNullOrEmpty(sourcePanel) && Enum.TryParse(sourcePanel, out PanelType type))
            {//第一期先处理创建角色时的上传弹窗
                CommonDialogWindow.ShowCreateOrUpdateModalDialog(panelData, type);
            }
        }
        


#region UI Event

        private void OnTestBtnClick(ClickEvent evt)
        {
            if (Application.isPlaying == false)
            {
                if (FromSidebar)
                {
                    AssetViewerStarter.StartAssetViewer();
                    PAAPRuntimeManager manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();
                    if (manager != null)
                    {
                        //reset store info in manager.
                        manager.avatarSpec = "";
                        manager.avatarName = "";
                        manager.avatarId = "";
                        
                        manager.characterinfo = null;
                        
                        manager.assetPath = "";
                        manager.assetConfig = "";
                        manager.assetName = "";
                        manager.assetBasicInfoSetting = null;
                        manager.assetComponentSetting = null;
                        manager.assetAnimationSetting = null;
                    }
                }
                else
                {
                    // for avatar.
                    string characterSpecLocal = CharacterManager.instance.GetSpecString(); // for local character
                    Protocol.CharacterInfo characterinfo = CharacterManager.instance.GetCurrentCharacter(); //for online character
                    string characterSpecOnline = characterinfo.character.config;
                    
                    // for local component or customAnimation.
                    PaabAssetImportSettings settings = panelData as PaabAssetImportSettings;
                    CharacterData characterSettings = panelData as CharacterData;

                    AssetViewerStarter.StartAssetViewer();
                    PAAPRuntimeManager manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();
                    if (manager != null)
                    {
                        // only use the local character spec.
                        if (characterSpecLocal != "")
                            manager.avatarSpec = characterSpecLocal;
                        else
                            manager.avatarSpec = characterSpecOnline;
                        manager.avatarName = characterinfo.character.name;
                        manager.avatarId = characterinfo.character.character_id;
                       
                        // for Base Animation.
                        if (characterSettings != null && characterSettings.base_animation_set != null)
                        {
                            characterinfo.base_animation_set.offline_config = characterSettings.base_animation_set.offline_config;
                            characterinfo.base_animation_set.name = characterSettings.base_animation_set.name;
                        }
                        manager.characterinfo = characterinfo;

                        // for component or customAnimation.
                        if (settings != null)
                        {
                            string assetSettingString = settings.ToJsonText();
                            var asset_config = "";
                            if (assetSettingString != "")
                            {
                                var configJson = JsonConvert.DeserializeObject<JObject>(assetSettingString);
                                if (configJson != null && configJson.TryGetValue("config", out JToken configToken))
                                {
                                    asset_config = configToken.ToString();
                                }
                            }
                            manager.assetConfig = asset_config;
                            manager.assetName = settings.basicInfoSetting.assetName;
                            manager.assetBasicInfoSetting = settings.basicInfoSetting;
                            manager.assetComponentSetting = settings.GetImportSetting<PaabComponentImportSetting>(false);
                            manager.assetAnimationSetting = settings.GetImportSetting<PaabAnimationImportSetting>(false);
                        }
                        else // no local asset test.
                        {
                            manager.assetConfig = "";
                            manager.assetName = "";
                            manager.assetBasicInfoSetting = null;
                            manager.assetComponentSetting = null;
                            manager.assetAnimationSetting = null;
                        }
                    }
                }
                testBtn.text = "Stop Test";
            }
            else if (Application.isPlaying == true)
            {
                EditorApplication.ExitPlaymode();
                testBtn.text = "Test";
            }
        }

        // 需要补充对应类型点击next的处理逻辑
        private void OnNextBtnClick(ClickEvent evt)
        {
            if (!string.IsNullOrEmpty(sourcePanel) && Enum.TryParse(sourcePanel, out PanelType type))
            {
                CommonDialogWindow.ShowCreateOrUpdateModalDialog(panelData, type);
            }
            else
            {
                Debug.LogError("@@@@@@@@PanelType not found!");
            }
        }
        
#endregion

#endregion

        public void CreateOrUpdateDoneCallback(AssetUploadManager.NewUploadAssetData uploadAssetData)
        {
            var sourceType = uploadAssetData.sourceType;
            switch (sourceType)
            {
                case UploadAssetsPanel.SourceType.CreateSkeleton:
                case UploadAssetsPanel.SourceType.CreateAnimationSet:
                case UploadAssetsPanel.SourceType.CreateBaseBody:
                case UploadAssetsPanel.SourceType.CreateCharacter:
                    CreateCharacter2Server(new CharacterData(uploadAssetData));
                    break;
                case UploadAssetsPanel.SourceType.UpdateSkeleton:
                case UploadAssetsPanel.SourceType.UpdateAnimationSet:
                case UploadAssetsPanel.SourceType.UpdateBaseBody:
                case UploadAssetsPanel.SourceType.UpdateCharacter:
                    UpdateCharacter2Server(new CharacterData(uploadAssetData));
                    break;
                case UploadAssetsPanel.SourceType.CreateComponent:
                    CreateComponent2Server(new AssetSendData(uploadAssetData));
                    break;
                case UploadAssetsPanel.SourceType.UpdateComponent:
                    UpdateComponent2Server(new AssetSendData(uploadAssetData));
                    break;
                case UploadAssetsPanel.SourceType.CreateCustomAnimationSet:
                    CreateAnimation2Server(new AssetSendData(uploadAssetData));
                    break;
                case UploadAssetsPanel.SourceType.UpdateCustomAnimationSet:
                    UpdateAnimation2Server(new AssetSendData(uploadAssetData));
                    break;
                case UploadAssetsPanel.SourceType.CreateHair:
                case UploadAssetsPanel.SourceType.UpdateHair:
                    break;
                case UploadAssetsPanel.SourceType.CreateShoe:
                case UploadAssetsPanel.SourceType.UpdateShoe:
                    break;
                case UploadAssetsPanel.SourceType.CreateClothes:
                case UploadAssetsPanel.SourceType.UpdateClothes:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, null);
            }
        }

        public void CreateCharacter2Server(CharacterData character)
        {
            //资源上传完成，创建角色 
            var req = AssetServerManager.instance.CreateNewCharacter(JsonConvert.SerializeObject(character));
            
            req.Send(success =>
                {
                    Debug.Log($"Create character success : {success}");
                    var response = ProtocolUtil.GetResponse<Protocol.CharacterInfo>(success);
                    if (response == null || response.data == null)
                    {
                        Debug.LogError("Get response failed");
                        return;
                    }
                    else
                    {
                        AssetUploadManager.instance.newUploadAsset.character_id = response.data.character.character_id;
                        CharacterUtil.RenameTempCharacterFolderName(response.data.character.name);
                        CharacterUtil.CreateCharacterSpecJsonFile(response.data.character.name, response.data.character.config);
                        if (NavMenuBarRoute.isValid)
                            NavMenuBarRoute.instance.RouteNextByPanelName(panelName, PanelType.UploadSuccess.ToString());
                    }
                },
                failure =>
                {
                    Debug.LogError($"Create character failed : {failure}");
                    if (NavMenuBarRoute.isValid)
                    {
                        var failurePanel = NavMenuBarRoute.instance.RouteNextByPanelName(panelName,
                        PanelType.UploadFailure.ToString());
                        if (failurePanel)
                        {
                            ((UploadFailurePanel)failurePanel).FillFailureContent(failure.ErrorServerText);
                        }
                    }
                });

            WaitResponseAndBlockUnity(req.CurrRequest);
        }
        public void UpdateCharacter2Server(CharacterData character)
        {
            var req = AssetServerManager.instance.UpdateCharacter(JsonConvert.SerializeObject(character));
            req.Send(success =>
                {
                    if (NavMenuBarRoute.isValid)
                        NavMenuBarRoute.instance.RouteNextByPanelName(panelName, PanelType.UploadSuccess.ToString());
                    /*var failurePanel =  NavMenuBarRoute.instance.RouteNextByPanelName(panelName, 
                        PanelType.UploadFailure.ToString());
                    if (failurePanel)
                    {
                        ((UploadFailurePanel)failurePanel).FillFailureContent("xxxxxxxxxxxxxx");
                    }*/
                },
                failure =>
                {
                    if (NavMenuBarRoute.isValid)
                    {
                        var failurePanel = NavMenuBarRoute.instance.RouteNextByPanelName(panelName,
                        PanelType.UploadFailure.ToString());
                        if (failurePanel)
                        {
                            ((UploadFailurePanel)failurePanel).FillFailureContent(failure.ErrorServerText);
                        }
                    }
                
                });

            WaitResponseAndBlockUnity(req.CurrRequest);
        }

        public void CreateComponent2Server(AssetSendData assetSendData)
        {
            var req = AssetServerManager.instance.CreateComponentAsset(JsonConvert.SerializeObject(assetSendData));
            req.Send(success =>
                {
                    var jo = JsonConvert.DeserializeObject<JObject>(success);
                    if (jo != null)
                    {
                        AssetUploadManager.instance.newUploadAsset.asset_id = jo["data"]?["asset_info"]?["asset_id"]?.ToString();
                    }
                   
                    if (NavMenuBarRoute.isValid)
                        NavMenuBarRoute.instance.RouteNextByPanelName(panelName, PanelType.UploadSuccess.ToString());
                    /*var failurePanel =  NavMenuBarRoute.instance.RouteNextByPanelName(panelName, 
                        PanelType.UploadFailure.ToString());
                    if (failurePanel)
                    {
                        ((UploadFailurePanel)failurePanel).FillFailureContent("xxxxxxxxxxxxxxxx");
                    }*/
                },
                failure =>
                {
                    if (NavMenuBarRoute.isValid)
                    {
                        var failurePanel = NavMenuBarRoute.instance.RouteNextByPanelName(panelName,
                        PanelType.UploadFailure.ToString());
                        if (failurePanel)
                        {
                            ((UploadFailurePanel)failurePanel).FillFailureContent(failure.ErrorServerText);
                        }
                    }
                });

            WaitResponseAndBlockUnity(req.CurrRequest);
        }
        
        public void UpdateComponent2Server(AssetSendData assetSendData)
        {
            var req = AssetServerManager.instance.UpdateComponentAsset(JsonConvert.SerializeObject(assetSendData));
            req.Send(success =>
                {
                    if (NavMenuBarRoute.isValid)
                        NavMenuBarRoute.instance.RouteNextByPanelName(panelName, PanelType.UploadSuccess.ToString());
                },
                failure =>
                {
                    if (NavMenuBarRoute.isValid)
                    {
                        var failurePanel = NavMenuBarRoute.instance.RouteNextByPanelName(panelName,
                        PanelType.UploadFailure.ToString());
                        if (failurePanel)
                        {
                            ((UploadFailurePanel)failurePanel).FillFailureContent(failure.ErrorServerText);
                        }
                    }
                });

            WaitResponseAndBlockUnity(req.CurrRequest);
        }
        
        public void CreateAnimation2Server(AssetSendData assetSendData)
        {
            var req = AssetServerManager.instance.CreateCustomAnimation(JsonConvert.SerializeObject(assetSendData));
            req.Send(success =>
                {
                    var data = JsonConvert.DeserializeObject<JObject>(success);
                    if (data != null)
                    {
                        AssetUploadManager.instance.newUploadAsset.asset_id = data["asset_id"]?.ToString();
                    }

                    if (NavMenuBarRoute.isValid)
                        NavMenuBarRoute.instance.RouteNextByPanelName(panelName, PanelType.UploadSuccess.ToString());
                },
                failure =>
                {
                    if (NavMenuBarRoute.isValid)
                    {
                        var failurePanel = NavMenuBarRoute.instance.RouteNextByPanelName(panelName,
                        PanelType.UploadFailure.ToString());
                        if (failurePanel)
                        {
                            ((UploadFailurePanel)failurePanel).FillFailureContent(failure.ErrorServerText);
                        }
                    }
                });

            WaitResponseAndBlockUnity(req.CurrRequest);
        }
        
        public void UpdateAnimation2Server(AssetSendData assetSendData)
        {
            var req = AssetServerManager.instance.UpdateCustomAnimation(JsonConvert.SerializeObject(assetSendData));
            req.Send(success =>
                {
                    if (NavMenuBarRoute.isValid)
                        NavMenuBarRoute.instance.RouteNextByPanelName(panelName, PanelType.UploadSuccess.ToString());
                },
                failure =>
                {
                    if (NavMenuBarRoute.isValid)
                    {
                        var failurePanel = NavMenuBarRoute.instance.RouteNextByPanelName(panelName,
                        PanelType.UploadFailure.ToString());
                        if (failurePanel)
                        {
                            ((UploadFailurePanel)failurePanel).FillFailureContent(failure.ErrorServerText);
                        }
                    }
                });
            
            WaitResponseAndBlockUnity(req.CurrRequest);
        }

        private void WaitResponseAndBlockUnity(UnityWebRequest req)
        {
            DateTime start = DateTime.Now;
            // 直接卡主得了
            while (req != null && !req.isDone)
            {
                if ((DateTime.Now - start).TotalMinutes > 10)
                {
                    Debug.LogError("Time out");
                    break;
                }
            }
        }
    }

  
    [Serializable]
    public class AssetSendData
    {
        public string character_id;
        public string category_id;
        public string asset_id;
        public NewAssetData asset;

        public AssetSendData(PaabAssetImportSettings importSettings)
        {
            character_id = importSettings.basicInfoSetting.characterId;
            if (importSettings.assetImportSettingsType == AssetImportSettingsType.Component)
            {
                PaabComponentImportSetting componentImportSetting =
                    importSettings.GetImportSetting<PaabComponentImportSetting>(false);
                category_id =
                    ComponentListPanel.ParseComponentTypeToComponentKey(componentImportSetting.componentType); //componentImportSetting.serverCategory;
            }
            else
                category_id = null;
            
            asset_id = importSettings.basicInfoSetting.assetId;
            asset = CharacterUtil.CreateNewAssetUploadData(importSettings);
        }
        
        public AssetSendData(AssetUploadManager.NewUploadAssetData uploadAssetData)
        {
            character_id = uploadAssetData.character_id;
            category_id = uploadAssetData.category_id;
            asset_id = uploadAssetData.asset_id;
            asset = uploadAssetData?.custom_animation_set_asset ?? uploadAssetData.component_asset;
        }
    }
}
#endif