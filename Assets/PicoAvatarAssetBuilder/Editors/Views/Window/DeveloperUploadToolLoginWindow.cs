#if UNITY_EDITOR
using System;
using Pico.AvatarAssetBuilder;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetBuilder
    {

        public class DeveloperUploadToolLoginWindow : PavBaseWindow
        {
            public DeveloperUploadToolLoginWindow loginWindow;
            //[MenuItem("Window/UI Toolkit/DevelopeUploadToolLogin")]
            public static void ShowExample()
            {
                DeveloperUploadToolLoginWindow wnd = GetWindow<DeveloperUploadToolLoginWindow>();
                wnd.loginWindow = wnd;
                wnd.ShowPanel(DeveloperUploadToolLoginPanel.instance, false, null);
                wnd.Show();
                wnd.titleContent = new GUIContent("PICOAvatarUploaderLogin");
            }

            private void Update()
            {
                base.Update();
                if (loginWindow != null)
                {
                    if (loginWindow.position.height < 665)
                    {
                        if (DeveloperUploadToolLoginPanel.instance.ChangeScaleMode(true))
                        {
                            loginWindow.Repaint();
                        }
                    }
                    else if (loginWindow.position.width < 1276)
                    {
                        if (DeveloperUploadToolLoginPanel.instance.ChangeScaleMode(false))
                        {
                            loginWindow.Repaint();
                        }
                    }
                    else
                    {
                        if (DeveloperUploadToolLoginPanel.instance.ChangeScaleMode(true))
                        {
                            loginWindow.Repaint();
                        }
                    }
                }
            }

            public void CreateGUI()
            {
                if (HasOpenInstances<DeveloperUploadToolLoginWindow>())
                {
                    DeveloperUploadToolLoginWindow wnd = GetWindow<DeveloperUploadToolLoginWindow>();
                    wnd.loginWindow = wnd;
                    wnd.RemovePanel(DeveloperUploadToolLoginPanel.instance);
                    wnd.ShowPanel(DeveloperUploadToolLoginPanel.instance, false, null);
                    wnd.Show();
                    wnd.titleContent = new GUIContent("PICOAvatarUploaderLogin");
                }
              
            }
        }
    }
}
#endif