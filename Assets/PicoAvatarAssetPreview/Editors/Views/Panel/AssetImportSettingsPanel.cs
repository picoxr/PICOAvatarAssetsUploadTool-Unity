#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        /**
         * @brief Base class for panel that used to import asset import.
         */
        public class AssetImportSettingsPanel : PavPanel, Pico.AvatarAssetPreview.PaabAssetImportSettings.Listener
        {
#region Public Fields

            // Previous panel used to navigate back .
            [SerializeField]
            public PavPanel previousPanel = null;

            // current AssetImportSettings.
            public PaabAssetImportSettings curImportSettings
            {
                get
                {
                    if (_curImportSettings == null)
                    {
                        Debug.LogError("get curImportSettings is null");
                    }
                    return _curImportSettings;
                }
                set
                {
                    if (value == _curImportSettings)
                    {
                        return;
                    }
                    if (value == null)
                    {
                        Debug.LogError("set curImportSettings is null");
                    }
                    if(_curImportSettings != null)
                    {
                        _curImportSettings.listener = null;
                    }
                    _curImportSettings = value;
                    if(value != null)
                    {
                        // force not be destroyed.
                        value.hideFlags = HideFlags.DontUnloadUnusedAsset| HideFlags.DontSaveInEditor;
                        _curImportSettings.hideFlags = HideFlags.DontUnloadUnusedAsset| HideFlags.DontSaveInEditor;
                        value.listener = this;
                    }
                }
            }

#endregion


#region Public Methods

            /**
             * @brief Show next panel in replace of this panel, and bind data for next panel.
             * @param importSettings AssetImportSetting to bind for next panel. if null use the import settings of this panel object.
             */
            public void ShowNextPanelInPlace(AssetImportSettingsPanel nextPanel, PaabAssetImportSettings importSettings = null)
            {
                // save context.
                this.SaveContext();

                // show next panel.
                this.panelContainer.ShowPanel(nextPanel, true, importSettings);

                // bind data for next panel.
                if (importSettings == null)
                {
                    importSettings = curImportSettings;
                }
                nextPanel.BindOrUpdateFromData(importSettings);
            }

            /**
             * @brief Set new import settings to ui. Derived class SHOULD override the method.
             */
            public virtual void BindOrUpdateFromData(PaabAssetImportSettings importConfig) 
            {
                // current import settings.
                curImportSettings = importConfig;

                foreach (var x in widgets)
                {
                    var settingWidget = x as AssetImportSettingWidget;
                    if(settingWidget != null && settingWidget.visible)
                    {
                        settingWidget.BindOrUpdateFromData(importConfig);
                    }
                }
            }

            /**
             * @brief Set new import settings from panel ui. Derived class SHOULD override the method.
             */
            public virtual void UpdateToData(PaabAssetImportSettings importConfig)
            {
                foreach (var x in widgets)
                {
                    var settingWidget = x as AssetImportSettingWidget;
                    if (settingWidget != null && settingWidget.visible)
                    {
                        settingWidget.UpdateToData(importConfig);
                    }
                }
            }

            /**
             * Notification that the panel will be destroyed. If derived class override the method, MUST invok it.
             */
            public override void OnDestroy()
            {
                base.OnDestroy();
            }

            /**
             * @brief Notification from _curImportSettings when OnDestroy invoked.
             */
            public void OnAssetImportSettingsDestroy(PaabAssetImportSettings importSettings)
            {
                _curImportSettings = null;
            }

            /**
             * @brief dirty and save me
             */
            public override void SaveContext()
            {
                return;
                //1. save the assetSettings to local CurrentPreviewAsset.asset
                //todo _curImportSettings is not null, but cacheptr is 0x0.
                if(_curImportSettings.settingItems.Length > 0)
                {
                    var assetImportSettingPath = AssetDatabase.GetAssetPath(_curImportSettings);
                    if (string.IsNullOrEmpty(assetImportSettingPath))
                    {
                        Utils.ReCreateAssetAt(_curImportSettings, AssetBuilderConfig.instance.assetViwerDataAssetsPath + "Data/CurrentPreviewAsset.asset");
                    }
                }
                //
                base.SaveContext();
            }
#endregion


#region Private Fields

            // current AssetImportSettings.
            [SerializeField]
            private PaabAssetImportSettings _curImportSettings;

#endregion
        }
    }
}
#endif