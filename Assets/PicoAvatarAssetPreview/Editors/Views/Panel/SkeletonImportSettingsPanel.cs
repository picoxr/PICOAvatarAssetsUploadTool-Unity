#if UNITY_EDITOR
using Pico.Avatar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class SkeletonImportSettingsPanel : AssetImportSettingsPanel
        {
            // Gets singleton instance.
            public static SkeletonImportSettingsPanel instance {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<SkeletonImportSettingsPanel>(
                            AssetBuilderConfig.instance.assetViwerDataAssetsPath + "PanelData/SkeletonImportSettingsPanel.asset");
                    }
                    return _instance;
                }
            }

            // display name of the panel
            public override string displayName { get => "ImportSkeleton"; }
            public override string panelName { get => "ImportSkeleton"; }

            // gets uxml path name. relativ to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetPreview/Editors/Views/"
            public override string uxmlPathName { get => "Uxml/SkeletonImportSettingsPanel.uxml"; }


#region Public Methods

            /**
             * @brief Set new import settings to ui. Derived class SHOULD override the method.
             */
            public override void BindOrUpdateFromData(PaabAssetImportSettings importConfig)
            {
                base.BindOrUpdateFromData(importConfig);
            }

            /**
             * @brief Set new import settings from panel ui. Derived class SHOULD override the method.
             */
            public override void UpdateToData(PaabAssetImportSettings importConfig)
            {
                base.UpdateToData(importConfig);
            }

            /**
             * Notification that the panel will be destroyed. If derived class override the method, MUST invok it.
             */
            public override void OnDestroy()
            {
                base.OnDestroy();
                //
                if(_instance == this)
                {
                    _instance = null;
                }
            }
#endregion


#region Private Fields

            private static SkeletonImportSettingsPanel _instance;

#endregion


#region Protected/Private Mehtods

            // Start is called before the first frame update
            protected override bool BuildUIDOM(VisualElement parent)
            {
                // build sub views.
                AddWidget(new AssetBasicInfoSettingWidget());
                AddWidget(new SkeletonMappingSettingWidget());

                var result = base.BuildUIDOM(parent);
                //TODO: add extra building.

                return result;
            }

            /**
             * @brief Bind ui events. Derived class SHOULD override the method.
             * Invoked from EditorWindowBase.ShowMe after build the ui elements.
             */
            protected override bool BindUIActions()
            {
                if (!base.BindUIActions())
                {
                    return false;
                }
                
                {
                    var btn = this.mainElement.Q<Button>("NextBtn");
                    btn.clicked += () => {
                        if (panelContainer != null)
                        {
                            //
                            this.UpdateToData(curImportSettings);

                            // save context.
                            this.SaveContext();
                            //
                            panelContainer.RemovePanel(this);
                            //
                            // AssetViewerStarter.StartAssetViewer(curImportSettings);
                        }
                    };
                }

                return true;
            }
#endregion
        }
    }
}

#endif