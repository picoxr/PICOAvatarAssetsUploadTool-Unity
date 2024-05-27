#if UNITY_EDITOR
using System;
using System.Collections;
using JetBrains.Annotations;
using Pico.Avatar;
using Pico.AvatarAssetPreview;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class CommonDialogWindow : PavBaseWindow
        {
            public enum CheckStatus
            {
                Right,
                Warning,
                Error,
            }

            public class Message
            {
                public CheckStatus status;
                public string message;

                public Message(CheckStatus status, string message)
                {
                    this.status = status;
                    this.message = message;
                }
            }

            [MenuItem("Window/UI Toolkit/CommonDialogWindow")]
            public static void ShowExample()
            {
                CommonDialogWindow wnd = GetWindow<CommonDialogWindow>();
                wnd.titleContent = new GUIContent("CommonDialogWindow");
            }

            public void CreateGUI()
            {

            }

            private static readonly Vector2 _windowFixedSize = new Vector2(600, 368);
            /// <summary>
            /// 上传资源接口弹窗()
            /// </summary>
            /// <param name="panelData ">type NewCharacterData </param>
            /// <param name="ok"></param>
            /// <param name="cancel"></param>
            public static void ShowCreateOrUpdateModalDialog([NotNull] object panelData, PanelType type)
            {
                if (HasOpenInstances<CommonDialogWindow>())
                {
                    var cdlw = GetWindow<CommonDialogWindow>();
                    if (cdlw.curPanel.panelName.Equals(UploadAssetsPanel.instance.panelName))
                    {
                        cdlw.Focus();
                        return;
                    }
                    cdlw.Close();
                }
                     
                UploadAssetsPanel.instance.PreloadData(panelData, type);
                EditorCoroutineUtility.StartCoroutine(ShowPanel(panelData, type), UploadAssetsPanel.instance);
            }

            static IEnumerator ShowPanel([NotNull] object panelData, PanelType type)
            {
                while (true)
                {
                    yield return null;
                    
                    if (UploadAssetsPanel.instance.WebImageDownloadSucc)
                    {
                        CommonDialogWindow wnd = GetWindow<CommonDialogWindow>(true, "Upload Assets", true);
                        wnd.maxSize = _windowFixedSize;
                        wnd.minSize = _windowFixedSize;
                        wnd.ShowPanel(UploadAssetsPanel.instance, panelData, type);
                        wnd.titleContent = new GUIContent(UploadAssetsPanel.instance.displayName);
                        yield return null;
                        yield return null;
                        wnd.ShowModal();
                        yield break;
                    }
                }
            }

            public static void ShowPopupConfirmDialog(Action success, Action failure = default,
                string content = default, string iconUrl = default,
                string okName = "OK", string cancelName = "Cancel")
            {
                CommonDialogWindow wnd = null;
                if (HasOpenInstances<CommonDialogWindow>())
                {
                    wnd = GetWindow<CommonDialogWindow>();
                    if (wnd.curPanel.panelName.Equals(ConfirmInfoPanel.instance.panelName))
                    {
                        wnd.Focus();
                        return;
                    }
                    wnd.Close();
                    wnd = null;
                }

                EditorCoroutineUtility.StartCoroutine(ShowPanel(success, failure, content,
                    iconUrl, okName, cancelName), ConfirmInfoPanel.instance);
            }

            static IEnumerator ShowPanel(Action success, Action failure = default, 
                string content = default, string iconUrl = default, 
                string okName  ="OK", string cancelName = "Cancel")
            {
                yield return new EditorWaitForSeconds(0.2f); //wait for panel init and render;
                    
                var wnd = GetWindow<CommonDialogWindow>();
                
                wnd.maxSize = _windowFixedSize;
                wnd.minSize = _windowFixedSize;
                if (HasOpenInstances<MainWindow>())
                {
                    MainWindow mainWindow = GetWindow<MainWindow>();
                    wnd.position =new Rect(new Vector2(mainWindow.position.x + mainWindow.position.width/2 - _windowFixedSize.x/2, 
                        mainWindow.position.y + mainWindow.position.height/2 - _windowFixedSize.y/2), _windowFixedSize) ;
                }
                else if (HasOpenInstances<DeveloperUploadToolLoginWindow>())
                {
                    DeveloperUploadToolLoginWindow devWindow = GetWindow<DeveloperUploadToolLoginWindow>();
                    wnd.position =new Rect(new Vector2(devWindow.position.x + devWindow.position.width/2 - _windowFixedSize.x/2, 
                        devWindow.position.y + devWindow.position.height/2 - _windowFixedSize.y/2), _windowFixedSize) ;
                }
                
                wnd.ShowPanel(ConfirmInfoPanel.instance,iconUrl, content, success, failure, okName, cancelName);
                wnd.titleContent = new GUIContent(ConfirmInfoPanel.instance.displayName);
                yield return null;
                yield return null;
                wnd.ShowModal();
            }
            
            public static void ShowCheckPopupDialog(params object[] messages)
            {
                CommonDialogWindow wnd = null;
                if (HasOpenInstances<CommonDialogWindow>())
                {
                    wnd = GetWindow<CommonDialogWindow>();
                    if (wnd.curPanel.panelName.Equals(WarningPanel.instance.panelName))
                    {
                        wnd.Focus();
                        return;
                    }
                    wnd.Close();
                    wnd = null;
                }

                wnd = GetWindow<CommonDialogWindow>();
            
      
                wnd.maxSize = _windowFixedSize;
                wnd.minSize = _windowFixedSize;
                if (HasOpenInstances<MainWindow>())
                {
                    MainWindow mainWindow = GetWindow<MainWindow>();
                    wnd.position = new Rect(new Vector2(mainWindow.position.x + mainWindow.position.width/2 - _windowFixedSize.x/2, 
                        mainWindow.position.y + mainWindow.position.height/2 - _windowFixedSize.y/2), _windowFixedSize) ;
                }
              
                wnd.ShowPanel(WarningPanel.instance, messages);
                wnd.titleContent = new GUIContent(WarningPanel.instance.displayName);
                wnd.ShowPopup();
                wnd.Focus();
            }
            
            public static void ShowMaterialConfigDialog(params object[] messages)
            {
                CommonDialogWindow wnd = null;
                if (HasOpenInstances<CommonDialogWindow>())
                {
                    wnd = GetWindow<CommonDialogWindow>();
                    if (wnd.curPanel.panelName.Equals(MaterialConfigPanel.instance.panelName))
                    {
                        wnd.Focus();
                        return;
                    }
                    wnd.Close();
                    wnd = null;
                }

                wnd = GetWindow<CommonDialogWindow>();
            
      
                wnd.maxSize = _windowFixedSize;
                wnd.minSize = _windowFixedSize;
                if (HasOpenInstances<MainWindow>())
                {
                    MainWindow mainWindow = GetWindow<MainWindow>();
                    wnd.position = new Rect(new Vector2(mainWindow.position.x + mainWindow.position.width/2 - _windowFixedSize.x/2, 
                        mainWindow.position.y + mainWindow.position.height/2 - _windowFixedSize.y/2), _windowFixedSize) ;
                }
              
                wnd.ShowPanel(MaterialConfigPanel.instance, messages);
                wnd.titleContent = new GUIContent(MaterialConfigPanel.instance.displayName);
                wnd.ShowPopup();
                wnd.Focus();
            }
            
            public static void CloseSelf()
            {
                if (!HasOpenInstances<CommonDialogWindow>())
                    return;
                var wnd = GetWindow<CommonDialogWindow>();
                wnd.Close();
            }

            protected void OnDestroy()
            {
                WarningPanel.instance.OnDestroy();
                ConfirmInfoPanel.instance.OnDestroy();
                UploadAssetsPanel.instance.OnDestroy();
                base.OnDestroy();
            }
        }
    }
}

#endif