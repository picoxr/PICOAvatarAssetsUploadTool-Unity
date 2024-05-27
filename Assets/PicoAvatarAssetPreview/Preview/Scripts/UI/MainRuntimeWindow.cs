#if UNITY_EDITOR
using System;
using AssemblyCSharp.Assets.AmzAvatar.TestTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Pico.AvatarAssetPreview;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class MainRuntimeWindow : PavBaseRuntimeWindow
        {
            public static void ShowMainWindow()
            {
                MainMenuUIManager.instance.PAAB_OPEN = false;

                if (!LoginUtils.IsLogin())
                {
                    LoginRuntimeWindow loginRuntimeWindow = LoginRuntimeWindow.instance;
                    loginRuntimeWindow.ShowPanel(DeveloperUploadToolLoginPanel.instance, false, null);
                    if (MainRuntimeWindow.instance.curPanel != null)
                    {
                        MainRuntimeWindow.instance.curPanel.mainElement.style.display = DisplayStyle.None;
                    }
                }
                else
                {
                    var loginInfo = LoginUtils.LoadLoginSetting();
                    PAAPRuntimeManager manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();

                    AppConfigDataInPAAPDemo configData = new AppConfigDataInPAAPDemo();
                    configData.channel = "test";
                    configData.PicoDevelopAppId = "xxx";
                    manager.avatarApp.extraSettings.configString = JsonUtility.ToJson(configData);
                    
                    var configString = manager.avatarApp.extraSettings.configString;
                    if (configString != "")
                    {
                        var configJson = JsonConvert.DeserializeObject<JObject>(configString);
                        if (configJson != null && configJson.TryGetValue("PicoDevelopAppId", out JToken idToken))
                        {
                            var picoDevelopAppId = idToken.ToString();
                            configJson.SelectToken("PicoDevelopAppId").Replace(loginInfo.appID);
                        }

                        configString = configJson.ToString();
                    }

                    manager.avatarApp.extraSettings.configString = configString;
                    
                    MainRuntimeWindow mainRuntimeWindow = instance;
                    mainRuntimeWindow.ShowPanel(MainPanel.instance, false, null);
                    if (LoginRuntimeWindow.instance.curPanel != null)
                    {
                        LoginRuntimeWindow.instance.curPanel.mainElement.style.display = DisplayStyle.None;
                    }
                    
                    bool usingCustomMaterial = manager.avatarApp.renderSettings.useCustomMaterial;
                    manager.SwitchMaterialType(usingCustomMaterial ? MaterialTyple.Custom : MaterialTyple.Official);
                }
                
            }
            
            private static MainRuntimeWindow _instance;
            public static MainRuntimeWindow instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<MainRuntimeWindow>();
                    }
                    return _instance;
                }
            }

            public static void ShowUploadWindow()
            {
                // MainWindow mainWindow = GetWindow<MainWindow>();
                // mainWindow.ShowPanel(AssetUploadPanel.instance, false, null);
            }

            public void CreateGUI()
            {
                if (UnityEditor.EditorApplication.isPlaying)
                    RestoreWindow();
            }

            public static void Clear()
            {
                MainMenuUIManager.instance.Clear();
                NavMenuBarRoute.instance.Clear();
            }

            protected void OnDestroy()
            {
                base.OnDestroy();
                RemovePanel(MainPanel.instance);
            }

            private void RestoreWindow()
            {
                // MainWindow wnd = GetWindow<MainWindow>();
                // wnd.ShowPanel(MainPanel.instance, false, null);
                // wnd.Show();
                PavPanel.OnPanelStore?.Invoke();
                // wnd.titleContent = new GUIContent(MainPanel.instance.displayName);
            }
        }
    }
}
#endif