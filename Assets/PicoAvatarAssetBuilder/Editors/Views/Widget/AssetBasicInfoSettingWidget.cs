#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        /**
         * @brief widget for basic information of asset.
         */ 
        internal class AssetBasicInfoSettingWidget : AssetImportSettingWidget
        {
#region Public Properties

            // asset import type.
            public virtual AssetImportSettingType settingType { get => AssetImportSettingType.BasicInfo; }

            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetBuilder/Editors/Views/"
            public override string uxmlPathName { get => "UxmlWidget/AssetBasicInfoSettingWidget.uxml"; }

#endregion


#region Public Methods

            /**
             * @brief Bind ui events. Derived class SHOULD override the method.
             * Invoked from EditorWindowBase.ShowMe after build the ui elements.
             */
            public override bool BindUIActions()
            {
                if(!base.BindUIActions())
                {
                    return false;
                }

                _assetNameTextUI = this.mainElement.Q<TextField>("AssetNameText");
                _assetNickNameTextUI = this.mainElement.Q<TextField>("AssetNickNameText");
                return (_assetNameTextUI != null && _assetNickNameTextUI != null);
            }

            /**
             * @brief Set new import settings to ui. Derived class SHOULD override the method.
             */
            public override void BindOrUpdateFromData(PaabAssetImportSettings importConfig)
            {
                if(_assetNameTextUI != null && _assetNickNameTextUI != null && importConfig != null)
                {
                    //var so = new SerializedObject(importConfig.basicInfoSetting);
                    //this.mainElement.Bind(so); 
                    _assetNameTextUI.value = importConfig.basicInfoSetting.assetName;
                    _assetNickNameTextUI.value = importConfig.basicInfoSetting.assetNickName;
                }
            }

            /**
             * @brief Set new import settings from panel ui. Derived class SHOULD override the method.
             */
            public override void UpdateToData(PaabAssetImportSettings importConfig)
            {
                if (_assetNameTextUI != null && _assetNickNameTextUI != null && importConfig != null)
                {
                    importConfig.basicInfoSetting.SetAssetName(_assetNameTextUI.value);
                    importConfig.basicInfoSetting.assetNickName = _assetNickNameTextUI.value;
                }
            }

            /**
             * @brief Set editable to _assetNameTextUI
             */
            public void SetAssetNameEditable(bool editable = true)
            {
                if (_assetNameTextUI != null)
                {
                    _assetNameTextUI.isReadOnly = !editable;
                    // TODO: change style? since there is no visual difference in these two conditions
                }
            }

#endregion


#region Private Fields

            // ui element
            private TextField _assetNameTextUI;
            private TextField _assetNickNameTextUI;

            //private SerializedObject _basicInfoSettingSO = null;
#endregion
        }
    }
}
#endif