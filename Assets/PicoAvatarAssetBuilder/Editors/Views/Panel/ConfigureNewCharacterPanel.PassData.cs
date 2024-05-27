#if UNITY_EDITOR
using Pico.AvatarAssetBuilder.Protocol;
using UnityEngine;

namespace Pico.AvatarAssetBuilder
{
    public partial class ConfigureNewCharacterPanel
    {
        private PaabAssetImportSettings skeletonAssetImportSettings;
        private PaabAssetImportSettings animationSetAssetImportSettings;
        private PaabAssetImportSettings basebodyAssetImportSettings;

        private void ClearAssetImportSettings()
        {
            if (skeletonAssetImportSettings != null)
                GameObject.DestroyImmediate(skeletonAssetImportSettings, true);
            skeletonAssetImportSettings = null;
            
            if (animationSetAssetImportSettings != null)
                GameObject.DestroyImmediate(animationSetAssetImportSettings, true);
            animationSetAssetImportSettings = null;
            
            if (basebodyAssetImportSettings != null)
                GameObject.DestroyImmediate(basebodyAssetImportSettings, true);
            basebodyAssetImportSettings = null;
        }

        
        /// <summary>
        /// 骨骼配置界面数据
        /// </summary>
        /// <returns></returns>
        private PaabAssetImportSettings GetSkeletonAssetImportSettings()
        {
            if (skeletonAssetImportSettings == null)
            {
                skeletonAssetImportSettings = ScriptableObject.CreateInstance<PaabAssetImportSettings>();
                skeletonAssetImportSettings.SetAssetTypeName(AssetImportSettingsType.Skeleton);
                skeletonAssetImportSettings.hideFlags = HideFlags.DontSaveInEditor;
            }
            
            if (skeletonAssetImportSettings.basicInfoSetting == null)
            {
                skeletonAssetImportSettings.basicInfoSetting = new PaabBasicInfoImportSetting();
                skeletonAssetImportSettings.basicInfoSetting.characterFolderName = CharacterUtil.NewCharacterTempName;
                skeletonAssetImportSettings.basicInfoSetting.characterId = "";
            }
            skeletonAssetImportSettings.basicInfoSetting.characterName = baseInfoWidget.Name;
            skeletonAssetImportSettings.opType = OperationType.Create;
            
            // 填充骨骼集数据
            if (skeletonAssetImportSettings.settingItems == null)
            {
                var componentSetting = new PaabSkeletonImportSetting();
                skeletonAssetImportSettings.settingItems = new[]
                {
                    componentSetting
                };
            }
            
            return skeletonAssetImportSettings;
        }
        
        /// <summary>
        /// 动画配置界面数据
        /// </summary>
        /// <returns></returns>
        private PaabAssetImportSettings GetAnimationSetImportSettings()
        {
            bool createNew = false;
            if (animationSetAssetImportSettings == null)
            {
                animationSetAssetImportSettings = ScriptableObject.CreateInstance<PaabAssetImportSettings>();
                animationSetAssetImportSettings.SetAssetTypeName(AssetImportSettingsType.AnimationSet);
                animationSetAssetImportSettings.hideFlags = HideFlags.DontSaveInEditor;
            }
            
            if (animationSetAssetImportSettings.basicInfoSetting == null)
            {
                animationSetAssetImportSettings.basicInfoSetting = new PaabBasicInfoImportSetting();
                animationSetAssetImportSettings.basicInfoSetting.characterFolderName = CharacterUtil.NewCharacterTempName;
                animationSetAssetImportSettings.basicInfoSetting.characterId = "";
            }
            animationSetAssetImportSettings.basicInfoSetting.characterName = baseInfoWidget.Name;
            animationSetAssetImportSettings.basicInfoSetting.skeletonAssetName = GetSelectedSkeletonName();
            animationSetAssetImportSettings.opType = OperationType.Create;
            
            
            // 填充动画集数据
            if (animationSetAssetImportSettings.settingItems == null)
            {
                var componentSetting = new PaabAnimationImportSetting();
                componentSetting.isBasicAnimationSet = true;
                animationSetAssetImportSettings.settingItems = new[]
                {
                    componentSetting
                };
            }
            
            return animationSetAssetImportSettings;
        }
        
        /// <summary>
        /// 素体配置数据
        /// </summary>
        /// <returns></returns>
        private PaabAssetImportSettings GetBaseBodyImportSettings()
        {
            if (basebodyAssetImportSettings == null)
            {
                basebodyAssetImportSettings = ScriptableObject.CreateInstance<PaabAssetImportSettings>();
                basebodyAssetImportSettings.SetAssetTypeName(AssetImportSettingsType.BaseBody);
                basebodyAssetImportSettings.hideFlags = HideFlags.DontSaveInEditor;
            }
            
            if (basebodyAssetImportSettings.basicInfoSetting == null)
            {
                basebodyAssetImportSettings.basicInfoSetting = new PaabBasicInfoImportSetting();
                basebodyAssetImportSettings.basicInfoSetting.characterFolderName = CharacterUtil.NewCharacterTempName;
                basebodyAssetImportSettings.basicInfoSetting.characterId = "";
            }
            basebodyAssetImportSettings.basicInfoSetting.characterName = baseInfoWidget.Name;
            basebodyAssetImportSettings.basicInfoSetting.skeletonAssetName = GetSelectedSkeletonName();
            basebodyAssetImportSettings.opType = OperationType.Create;
            
            if (basebodyAssetImportSettings.settingItems == null)
            {
                var componentSetting = new PaabComponentImportSetting();
                componentSetting.componentSource = PaabComponentImportSetting.ComponentSource.Custom;
                componentSetting.componentType = PaabComponentImportSetting.ComponentType.BaseBody;

                basebodyAssetImportSettings.settingItems = new[]
                {
                    componentSetting
                };
            }
            
            return basebodyAssetImportSettings;
        }

        
    }

}
#endif