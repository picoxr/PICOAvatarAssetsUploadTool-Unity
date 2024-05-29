#if UNITY_EDITOR
using System;
using Pico.Avatar;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class PICOMenuBar : PavPanel
        {
            public override string displayName { get => "PICOMenuBar"; }
            public override string panelName { get => "PICOMenuBar"; }
            public override string uxmlPathName { get => "Uxml/PICOMenu.uxml"; }


            public static Action<bool> OnGetSvrAppInfo;
            private static PICOMenuBar _instance;
            
            
            const string CharacterMenuButton = "Menu-Character-Button";
            const string TestMenuButton = "Menu-Test-Button";
            const string LogoutButton = "Menu-Logout-Button";
            const string GuideButton = "Menu-Guide-Button";
            
            const string ApplicationName = "Menu-App-Name";
            const string DeveloperName = "Menu-Developer-NickName";
            const string DeveloperThumbnail = "Menu-Developer-Thumbnail"; 
            
            const string ButtonInactiveClass = "black__button";
            const string ButtonActiveClass = "black__button--active";
            
            const string CharacterMenuButtonMask = "Menu-Character-Button-Mask";
            const string TestMenuButtonMask = "Menu-Test-Button-Mask";

            const string CharacterMenuImage = "Menu-Character-Image";
            const string TestMenuImage = "Menu-Test-Image";
            
            // UI Buttons
            private Button _characterMenuButton;
            private Button _testMenuButton;
            private Button _logoutButton;
            private Button _guideButton;
            private Button _appName;
            // UI Labels
          
            private Label _developerName;
            private VisualElement _developerThumbnail;

            private VisualElement _characterButtonMask;
            private VisualElement _testButtonMask;

            private VisualElement _characterImage;
            private VisualElement _testImage;
            private readonly Color _activeStyle = new(1f, 1f, 1f, 1f);
            private readonly Color _unActiveStyle = new(1f, 1f, 1f, 0.4f);
            
            private const string DocsUrl_CN = "https://developer-cn.picoxr.com/document/unity-avatar-uploader/get-started-with-avatar-uploader/";
            private const string DocsUrl_OVERSEA = "https://developer.picoxr.com/document/unity-avatar-uploader/get-started-with-avatar-uploader/";
            
            private WebImage _iconImage;
            public static PICOMenuBar instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<PICOMenuBar>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/PICOMenuBar.asset");
                    }
                    return _instance;
                }
            }
            
            
            protected override bool BuildUIDOM(VisualElement parent) //SetVisualElements and BuildWithUxml
            {
                base.BuildUIDOM(parent);
                if (mainElement != null)
                {
                    _characterMenuButton = mainElement.Q<Button>(CharacterMenuButton);
                    _testMenuButton = mainElement.Q<Button>(TestMenuButton);
                    _logoutButton = mainElement.Q<Button>(LogoutButton);
                    _guideButton = mainElement.Q<Button>(GuideButton);
                    _appName = mainElement.Q<Button>(ApplicationName);
                    _developerName = mainElement.Q<Label>(DeveloperName);
                    _developerThumbnail = mainElement.Q<VisualElement>(DeveloperThumbnail);
                    _characterButtonMask = mainElement.Q(CharacterMenuButtonMask);
                    _testButtonMask = mainElement.Q(TestMenuButtonMask);
                    _characterImage = mainElement.Q(CharacterMenuImage);
                    _testImage = mainElement.Q(TestMenuImage);
                }
                
                return true;
            }

            protected override bool BindUIActions() //RegisterButtonCallbacks
            {
                // register action when each button is clicked
                _characterMenuButton?.RegisterCallback<ClickEvent>(ShowCharacterScreen);
                _testMenuButton?.RegisterCallback<ClickEvent>(ShowTestMenuScreen);
                _logoutButton?.RegisterCallback<ClickEvent>(ClearLoginInfo);
                _guideButton?.RegisterCallback<ClickEvent>(LinkGuideBook);

                if (!LoginUtils.HasBindInfo())
                {
                    var request = AssetServerManager.instance.SvrAppInfo();
                    request.Send(success =>
                    {
                        LoginUtils.SaveAppInfoData(success);
                        InitContent();
                        OnGetSvrAppInfo?.Invoke(true);
                    }, failure =>
                    {
                        InitContent();
                        OnGetSvrAppInfo?.Invoke(false);
                    });
                }
                else
                {
                    InitContent();
                }
            
                return base.BindUIActions();
            }

            void InitContent()
            {
                var setting = LoginUtils.LoadLoginSetting();
                if (setting != null)
                {
                    _appName.text = setting.appName;
                    _developerName.text = setting.userName;
                    _iconImage = new WebImage(_developerThumbnail);
                    _iconImage.ClearTexture();
                    _iconImage.SetActive(false);
                    _iconImage.SetTexture(setting.userThumbnail, ImageFileExtension.PNG);
                    _characterButtonMask.SetActive(true);
                    _testButtonMask.SetActive(false);
                    
                    _testMenuButton.RemoveFromClassList(ButtonActiveClass);
                    _testMenuButton.AddToClassList(ButtonInactiveClass);
                    _characterMenuButton.RemoveFromClassList(ButtonInactiveClass);
                    _characterMenuButton.AddToClassList(ButtonActiveClass);
                    //UIUtils.AddVisualElementHoverMask(_logoutButton);
                    UIUtils.AddVisualElementHoverMask(_guideButton);
                    _iconImage.onTextureLoad += b =>
                    {
                        _iconImage.SetActive(b);
                    };
                }
              
            }

            void ShowCharacterScreen(ClickEvent evt)
            {
                _characterButtonMask.SetActive(true);
                _testButtonMask.SetActive(false);
                ActivateButton(_characterMenuButton);
                _characterImage.style.unityBackgroundImageTintColor = _activeStyle;
                _testImage.style.unityBackgroundImageTintColor = _unActiveStyle;
                NavMenuBarRoute.instance.SwitchChannelByPanelType(PanelType.CharacterPanel);
                var targetPanel = MainMenuUIManager.instance.GetPavPanelByPanelType(PanelType.AssetTestPanel);
                ((AssetTestPanel)targetPanel).FromSidebar = false;
            }

            void ShowTestMenuScreen(ClickEvent evt)
            {
                _characterButtonMask.SetActive(false);
                _testButtonMask.SetActive(true);
                ActivateButton(_testMenuButton);
                _testImage.style.unityBackgroundImageTintColor = _activeStyle;
                _characterImage.style.unityBackgroundImageTintColor = _unActiveStyle;
                NavMenuBarRoute.instance.SwitchChannelByPanelType(PanelType.AssetTestPanel);
                var targetPanel = MainMenuUIManager.instance.GetPavPanelByPanelType(PanelType.AssetTestPanel);
                ((AssetTestPanel)targetPanel).FromSidebar = true;
            }
            
            void ClearLoginInfo(ClickEvent evt)
            {
                CommonDialogWindow.ShowPopupConfirmDialog(() =>
                {
                    LoginUtils.Logout();
                    MainWindow.SwitchWindow();
                }, null, "Are you sure you want to log out?", default, "Log Out", "Cancel");
               
            }
           
            void LinkGuideBook(ClickEvent evt)
            {
                var loginsetting = LoginUtils.LoadLoginSetting();
                if (loginsetting != null)
                {
                    if (loginsetting.serverType == 1)
                    {
                        Application.OpenURL(DocsUrl_CN);
                    }
                    else
                    {
                        Application.OpenURL(DocsUrl_OVERSEA);
                    }
                }
            
            }
            
            void ActivateButton(Button menuButton)
            {
                if (menuButton == null)
                    return;

                HighlightElement(menuButton, ButtonInactiveClass, ButtonActiveClass, this.mainElement);
            }
            
            // toggles between a highlighted/active class and an inactive class
            void HighlightElement(VisualElement visualElem, string inactiveClass, string activeClass, VisualElement root)
            {
                if (visualElem == null)
                    return;

                VisualElement currentSelect = root.Query<VisualElement>(className: activeClass);

                if (currentSelect == visualElem)
                {
                    return;
                }

                // de-highlight whatever is currently active
                currentSelect?.RemoveFromClassList(activeClass);
                currentSelect?.AddToClassList(inactiveClass);

                visualElem.RemoveFromClassList(inactiveClass);
                visualElem.AddToClassList(activeClass);
            }
            
            public override void OnDestroy()
            {
                base.OnDestroy();
                _iconImage = null;
                _instance = null;
            }
        }
    }
}
#endif