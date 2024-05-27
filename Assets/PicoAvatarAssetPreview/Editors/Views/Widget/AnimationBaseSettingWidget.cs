#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        /**
         * @brief widget for base animation set
         */
        internal class AnimationBaseSettingWidget : PavWidget
        {
#region Public Properties

            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetPreview/Editors/Views/"
            public override string uxmlPathName { get => "UxmlWidget/AnimationBaseSettingWidget.uxml"; }

#endregion
            
#region Public Methods
#endregion
            
#region Private Fields
#endregion
        }
    }
}
#endif