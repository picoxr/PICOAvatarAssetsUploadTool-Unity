#if UNITY_EDITOR
using Pico.Avatar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        /**
         * @brief Panel window.
         */
        public class AssetImportWindow : PavBaseWindow
        {
            /**
             * Show panel window thith panel.
             */
            public static void ShowWithPanel(AssetImportSettingsPanel panel, bool asNextPanel,
                PaabAssetImportSettings assetImportSettings = null)
            {
                if(panel == null)
                {
                    throw new System.Exception("Bad Parameter!");
                }

                var wnd = GetWindow<AssetImportWindow>();
                wnd.ShowPanel(panel, asNextPanel, assetImportSettings);
                wnd.Show();
                wnd.titleContent = new GUIContent(panel.displayName);
            }
            
            void Update()
            {
                // if not playing and editor mode, reload data.
                if (curPanel == null &&
                    !EditorApplication.isPlaying && AssetBuilderConfig.instance.configData.lastPanelOfAssetImportWindow != null)
                {
                    var panel = AssetBuilderConfig.instance.configData.lastPanelOfAssetImportWindow as AssetImportSettingsPanel;
                    if (panel != null)
                    {
                        this.ShowPanel(panel, false, panel.curImportSettings);
                    }
                }

                base.Update();
            }

            protected void OnGUI()
            {
                // if not playing and editor mode, reload data.
                if (EditorApplication.isPlaying)
                {
                    EditorGUILayout.HelpBox("Do not close the window!", MessageType.Warning);

                    // show stop and exit button.
                    if(GUILayout.Button("Exit Asset Viewer!"))
                    {
                        EditorApplication.isPlaying = false;
                    }
                }
            }

#region For Single panel mode.

            /**
             * @brief Show with single Panel.
             * @param asNextPanel whether show as next panel and can go back from previous panel.
             */
            public override void ShowPanel(PavPanel panel, bool asNextPanel, UnityEngine.Object dataObj)
            {
                var assetImportSettingsPanel = panel as AssetImportSettingsPanel;
                if(assetImportSettingsPanel == null)
                {
                    throw new System.Exception("Bad Program!");
                }

                // keep track of previous panel.
                if (asNextPanel)
                {
                    assetImportSettingsPanel.previousPanel = curPanel as AssetImportSettingsPanel;
                }

                //
                base.ShowPanel(panel, asNextPanel, dataObj);

                // bind asset import settings.
                assetImportSettingsPanel.BindOrUpdateFromData(dataObj as PaabAssetImportSettings);

                // keep track the last panel.
                AssetBuilderConfig.instance.configData.lastPanelOfAssetImportWindow = curPanel;
            }
            /**
             * @brief Show with single Panel.
             * @param asNextPanel whether show as next panel and can go back from previous panel.
             */
            public override void ShowPreviousPanel()
            {
                var assetImportSettingsPanel = curPanel as AssetImportSettingsPanel;
                if (assetImportSettingsPanel != null && assetImportSettingsPanel.previousPanel != null)
                {
                    ShowPanel(assetImportSettingsPanel.previousPanel, false, null);
                }
            }

#endregion
        }
    }
}
#endif