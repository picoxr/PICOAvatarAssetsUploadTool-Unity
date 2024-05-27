#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        /**
         * @brief Widget for AssetImportSetting.
         */ 
        internal class AssetImportSettingWidget : PavWidget
        {
            public AssetImportSettingWidget() : base()
            {
                
            }

            public AssetImportSettingWidget(VisualElement ve) : base(ve)
            {
                
            }
            
            
#region Public Methods

            /**
             * @brief Set new import settings to ui. Derived class SHOULD override the method.
             */
            public virtual void BindOrUpdateFromData(PaabAssetImportSettings importConfig)
            {
                // Derived class should add implementation.
            }

            /**
             * @brief Set new import settings from panel ui. Derived class SHOULD override the method.
             */
            public virtual void UpdateToData(PaabAssetImportSettings importConfig)
            {
                // Derived class should add implementation.
            }

#endregion
        }
    }
}
#endif