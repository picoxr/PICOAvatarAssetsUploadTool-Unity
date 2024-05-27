#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Pico.AvatarAssetPreview;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class LoginRuntimeWindow : PavBaseRuntimeWindow
        {
            public static void ShowMainWindow()
            {
                MainMenuUIManager.instance.PAAB_OPEN = false;
                LoginRuntimeWindow LoginRuntimeWindow = instance;
                LoginRuntimeWindow.ShowPanel(DeveloperUploadToolLoginPanel.instance, false, null);
            }
            
            private static LoginRuntimeWindow _instance;
            public static LoginRuntimeWindow instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<LoginRuntimeWindow>();
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
                RemovePanel(DeveloperUploadToolLoginPanel.instance);
                Clear();
            }

            private void RestoreWindow()
            {
                // MainWindow wnd = GetWindow<MainWindow>();
                // wnd.ShowPanel(DeveloperUploadToolLoginPanel.instance, false, null);
                // wnd.Show();
                PavPanel.OnPanelStore?.Invoke();
                // wnd.titleContent = new GUIContent(DeveloperUploadToolLoginPanel.instance.displayName);
            }
        }
    }
}
#endif