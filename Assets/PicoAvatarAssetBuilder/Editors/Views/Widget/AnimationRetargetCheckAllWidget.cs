#if UNITY_EDITOR

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        /**
         * @brief widget for check all of animation retarget
         */
        internal class AnimationRetargetCheckAllWidget : PavWidget
        {
            #region Public Properties

            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetBuilder/Editors/Views/"
            public override string uxmlPathName { get => "UxmlWidget/AnimationRetargetCheckAllWidget.uxml"; }

            #endregion
        }
    }
}
#endif