#if UNITY_EDITOR

namespace Pico
{
    namespace AvatarAssetPreview
    {
        /**
         * @brief widget for check all of animation retarget
         */
        internal class AnimationRetargetCheckAllWidget : PavWidget
        {
            #region Public Properties

            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetPreview/Editors/Views/"
            public override string uxmlPathName { get => "UxmlWidget/AnimationRetargetCheckAllWidget.uxml"; }

            #endregion
        }
    }
}
#endif