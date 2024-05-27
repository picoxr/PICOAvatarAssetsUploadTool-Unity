#if UNITY_EDITOR
namespace Pico
{
    namespace AvatarAssetPreview
    {
        public enum PlaceHolderType : byte
        {
            None = 0,
            NavagationBar,
            PICOMenuBar,
            DisPlayArea,
        }

        public enum UploadFileType : byte
        {
            None = 0,
            CharacterBaseCover,
            BaseBody,
            BaseBodyCover,
            Skeleton,
            SkeletonCover,
            AnimationSet,
            AnimationSetCover,
            CharacterThumbnail,
            AnimationSetCustom,
            AnimationSetCustomCover,
            ComponentCustomSet,
            ComponentCustomSetCover,
        }
        
        public enum PanelType : byte
        {
            None = 0,
            TopNavMenuBar,
            MenuBar,
            PICOMenuBar,
            CharacterPanel,
            ConfigureComponent,
            ConfigureNewCharacter,
            SkeletonPanel,
            UpdateCharacter,
            ComponentListPanel,
            AnimationListPanel,
            ConfigureBaseAnimationSet,
            ConfigureCustomAnimationSet,
            UploadDialog,
            UploadSuccess,
            UploadFailure,
            AssetTestPanel,
            CharacterAndPresetPanel
        }
    }
}
#endif