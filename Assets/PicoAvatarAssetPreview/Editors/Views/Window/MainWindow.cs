#if UNITY_EDITOR
using System;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class MainWindow : PavBaseWindow
        {
            // [MenuItem("PICOAvatarUploader/ShowMainWindow")]
            public static void ShowMainWindow()
            {
                
                if (HasOpenInstances<MainWindow>())
                {
                    GetWindow<MainWindow>().Focus();
                    return;
                }
                
                if (HasOpenInstances<DeveloperUploadToolLoginWindow>())
                {
                    GetWindow<DeveloperUploadToolLoginWindow>().Focus();
                    return;
                }

                LoginUtils.IsShow = false;
                MainMenuUIManager.instance.PAAB_OPEN = true;

                if (!LoginUtils.IsLogin())
                {
                    ShowSpecifyWindow(GetWindow<DeveloperUploadToolLoginWindow>(), DeveloperUploadToolLoginPanel.instance);
                }
                else
                {
                    var window = GetWindow<MainWindow>();
                    ShowSpecifyWindow(window, MainPanel.instance);
                }
                LoginUtils.IsShow = true;
            }


            static void ShowSpecifyWindow(PavBaseWindow window, PavPanel panel)
            {
                if (window == null) return;
                window.ShowPanel(panel, false, null);
                window.titleContent = window.curPanel?.panelName == "MainPanel"? new GUIContent("PICOAvatarUploader"):
                    new GUIContent("PICOAvatarUploaderLogin");
                window.Show(true);
                window.Focus();
                window.CurrCortinue = EditorCoroutineUtility.StartCoroutine(ResizeWindow(window), window);
            }

            static IEnumerator ResizeWindow(PavBaseWindow window)
            {
                yield return null;
                if (window == null) yield break;
                var position = window.position;
                position.size = new Vector2(1080, 800);
                window.position = position;
                window.CurrCortinue = null;
            }

            public static void SwitchWindow()
            {
                if (!LoginUtils.IsLogin())
                {
                    DeveloperUploadToolLoginWindow loginWindow = GetWindow<DeveloperUploadToolLoginWindow>();
              
                    if (loginWindow.CurrCortinue != null)
                    {
                        loginWindow.StopCoroutine(loginWindow.CurrCortinue);
                    }
                    ShowSpecifyWindow(loginWindow, DeveloperUploadToolLoginPanel.instance);
                    if (HasOpenInstances<MainWindow>())
                    {
                        GetWindow<MainWindow>().Close();
                    }
                }
                else
                {
                    LoginUtils.IsShow = false;
                    MainWindow mainWindow = GetWindow<MainWindow>();
                    if (mainWindow.CurrCortinue != null)
                    {
                        mainWindow.StopCoroutine(mainWindow.CurrCortinue);
                    }
                 
                    ShowSpecifyWindow(mainWindow, MainPanel.instance);
                    if (HasOpenInstances<DeveloperUploadToolLoginWindow>())
                    {
                        GetWindow<DeveloperUploadToolLoginWindow>().Close();
                    }
                    LoginUtils.IsShow = true;
                }
            }
         
            public static void ShowUploadWindow()
            {
                MainWindow mainWindow = GetWindow<MainWindow>();
                mainWindow.ShowPanel(AssetUploadPanel.instance, false, null);
            }

            public void CreateGUI()
            {
                // if (UnityEditor.EditorApplication.isPlaying)
                //     RestoreWindow();
            }

            private void Awake()
            {
                //init Alog
                ALogFile.Instance.Init(); 
            }

            private new void OnEnable()
            {
                base.OnEnable();
                if (LoginUtils.IsPlatformShow())
                {
                    this.StartCoroutine( CloseSelf());
                }
            }

            IEnumerator CloseSelf()
            {
                yield return null;
                yield return null;
                Close();
            }
            
            protected new void OnDestroy()
            {
                // close Asset Preview V3 scene when destroy mainWindow.
                if (EditorApplication.isPlaying)
                {
                    string sceneName = SceneManager.GetActiveScene().name;
                    if(sceneName.Equals("AssetPreviewV3"))
                        EditorApplication.ExitPlaymode();
                }
                
                CommonDialogWindow.CloseSelf();
                ALogFile.Instance.UnInit();
                if(MainPanel.isValid)
                {
                    RemovePanel(MainPanel.instance);
                }
                else
                {
                    RemovePanel(null);
                }
                
                Clear();
                base.OnDestroy();
            }

            public static void Clear()
            {
                if(MainMenuUIManager.isValid)
                {
                    MainMenuUIManager.instance.Clear();
                }
                if (NavMenuBarRoute.isValid)
                {
                    NavMenuBarRoute.instance.Clear();
                }
            }
            private MainWindow()
            {
                UnityEngine.Debug.Log("MainWindow MainWindow");
            }
            ~MainWindow()
            {
                UnityEngine.Debug.Log("MainWindow ~MainWindow");
            }
           
            protected new void Update()
            {
                base.Update();
            }

            /**
             * @brief notification when playmode changed.
             */ 
            protected override void OnPlaymodeChanged(bool isPlaymode)
            {
                // when return from playmode, should show main panle again.
                if(!isPlaymode)
                {
                   // ShowPanel(MainPanel.instance, false, null);
                }
            }

            private void OnFocus()
            {

            }

            public void RescueContent()
            {
                var copyWindow = Instantiate<MainWindow>(this);
                copyWindow.ShowPanel(MainPanel.instance, false, null);
                copyWindow.minSize = new Vector2(600, 351);
                copyWindow.titleContent = new GUIContent("PicoAvatarAssetPreview");
                copyWindow.Show();
            }

            private void RestoreWindow()
            {
                MainWindow wnd = GetWindow<MainWindow>();
                wnd.ShowPanel(MainPanel.instance, false, null);
                wnd.Show();
                PavPanel.OnPanelStore?.Invoke();
                wnd.titleContent = new GUIContent(MainPanel.instance.panelName);
            }
        }
    }
}
#endif