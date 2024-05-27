#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using Pico.Avatar;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class AccessorySelectionWindow : PavBaseWindow
        {
            #region Private Properties
            private static AccessorySelectionWindow _instance;
            public static AccessorySelectionWindow instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = GetWindow<AccessorySelectionWindow>();
                        _instance.titleContent = new GUIContent("AccessorySelectionWindow");
                    }
                    return _instance;
                }
            }

            private static PicoAvatarAccessory _accessoryInterface;
            public static PicoAvatarAccessory accessoryInterface
            {
                get
                {
                    if (_accessoryInterface == null)
                    {
                        _accessoryInterface = new PicoAvatarAccessory();
                    }
                    return _accessoryInterface;
                }
            }
            #endregion

            [MenuItem("PICOAvatarUploader/ShowAccessorySelectionWindow")]
            public static void ShowAccessorySelectionWindow()
            {
                if (HasOpenInstances<AccessorySelectionWindow>())
                {
                    GetWindow<AccessorySelectionWindow>().Focus();
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
                    var window = GetWindow<AccessorySelectionWindow>();
                    ShowSpecifyWindow(window, AccessorySelectionPanel.instance);
                }

                LoginUtils.IsShow = true;
            }

            static void ShowSpecifyWindow(PavBaseWindow window, PavPanel panel)
            {
                if (window == null) { return; }

                window.ShowPanel(panel, false, null);
                window.titleContent = window.curPanel?.panelName == "AccessorySelectionPanel" ?
                     new GUIContent("AccessorySelectionWindow") : new GUIContent("DeveloperUploadToolLogin");
                window.Show(true);
                window.Focus();
                window.CurrCortinue = EditorCoroutineUtility.StartCoroutine(ResizeWindow(window), window);
            }

            static IEnumerator ResizeWindow(PavBaseWindow window)
            {
                yield return null;
                if (window == null) yield break;
                var position = window.position;
                position.size = new Vector2(600, 600);
                window.position = position;
                window.CurrCortinue = null;
            }
        }
    }
}

#endif
