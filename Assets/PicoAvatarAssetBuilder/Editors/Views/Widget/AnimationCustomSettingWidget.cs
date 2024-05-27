#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        internal class AnimationCustomSettingWidget : PavWidget
        {
#region Public Properties

            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetBuilder/Editors/Views/"
            public override string uxmlPathName { get => "UxmlWidget/AnimationCustomSettingWidget.uxml"; }

#endregion
            
#region Public Methods
#endregion
            
#region Private Fields
#endregion
        }
    }
}
#endif