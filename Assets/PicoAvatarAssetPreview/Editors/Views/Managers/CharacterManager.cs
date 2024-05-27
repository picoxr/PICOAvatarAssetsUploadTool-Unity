#if UNITY_EDITOR
using Pico.Avatar;
using Pico.AvatarAssetPreview.Protocol;
using UnityEngine;
using CharacterInfo = Pico.AvatarAssetPreview.Protocol.CharacterInfo;

namespace Pico.AvatarAssetPreview
{
    public class CharacterManager
    {
        private PaabAssetImportSettings curCharacterImportSettings;
        private PaabCharacterImportSetting curCharacterSetting;
        public CharacterInfo characterInfo;
        private string characterPath;
        


        public void SetCurrentCharacter(CharacterInfo info, OperationType operationType)
        {
            ClearCurrentCharacter();
            if (curCharacterImportSettings == null)
            {
                curCharacterImportSettings = GetCharacterImportSettings(operationType);
                curCharacterSetting = curCharacterImportSettings.GetImportSetting<PaabCharacterImportSetting>(true);
                curCharacterImportSettings.hideFlags = HideFlags.DontSaveInEditor;
            }

            if (info == null && operationType == OperationType.Create)
            {
                info = new CharacterInfo();
                info.character = new CharacterBaseInfo();
                info.character.name = CharacterUtil.NewCharacterTempName;
                info.app = new CharacterApp();
                info.base_animation_set = new AssetInfo();
                info.base_body = new AssetInfo();
                info.skeleton = new AssetInfo();
            }
            else
            {
                curCharacterImportSettings.FromJsonText(info.character.config);
            }
            
            characterInfo = info;
            if (characterInfo == null)
            {
                Debug.LogError("[CharacterManager] Character info is null");
                return;
            }
            
            // if (operationType == OperationType.Create)
            //     CharacterUtil.CreateCharacterSpecJsonFile(characterInfo.character.name, curCharacterImportSettings.ToJsonText());

            SetCurrentCharacterPath(info);
            //characterPath = CharacterUtil.GetCharacterPathByName(info.character.name);
        }

        public void SetCurrentCharacterPath(CharacterInfo info)
        {
            if (info == null)
                return;
            
            characterPath = CharacterUtil.GetCharacterPathByName(info.character.name);
        }

        public string GetRelativeCharacterPath()
        {
            return characterPath.Replace($"{AvatarEnv.cacheSpacePath}/", "");
        }

        public void ClearCurrentCharacter()
        {
            Debug.LogWarning("ClearCurrentCharacter");
            if (curCharacterImportSettings != null)
                GameObject.DestroyImmediate(curCharacterImportSettings, true);
            
            curCharacterImportSettings = null;
            curCharacterSetting = null;
            characterInfo = null;
            characterPath = "";
        }

        public void SetAssetLoadSource(string source, string idOrPath, CharacterBaseAssetType assetType)
        {
            if (curCharacterSetting == null)
            {
                Debug.LogError("Check failed, the curCharacterSetting is null");
                return;
            }

            bool isLocal = source == PaabCharacterImportSetting.AssetState_Local;
            var path = GetRelativeCharacterPath();
            switch (assetType)
            {
                case CharacterBaseAssetType.Skeleton:
                    if (isLocal)
                        idOrPath = $"{path}/{CharacterUtil.SkeletonFolderName}/{idOrPath}";
                    curCharacterSetting.setSkeletonAsset(idOrPath, source);
                    break;
                case CharacterBaseAssetType.AnimationSet:
                    if (isLocal)
                        idOrPath = $"{path}/{CharacterUtil.AnimationSetFolderName}/{CharacterUtil.BasicAnimationSetFolderName}/{idOrPath}";
                    curCharacterSetting.setBaseAnimationAsset(idOrPath, source);
                    break;
                case CharacterBaseAssetType.BaseBody:
                    if (isLocal)
                        idOrPath = $"{path}/{CharacterUtil.BaseBodyFolderName}/{idOrPath}";
                    curCharacterSetting.setBaseBodyAsset(idOrPath, source);
                    break;
            }

            CharacterUtil.CreateCharacterSpecJsonFile(characterInfo.character.name, curCharacterImportSettings.ToJsonText());
        }

        public CharacterInfo GetCurrentCharacter()
        {
            return characterInfo;
        }
        
        public string GetSpecString()
        {
            if (!Check())
            {
                Debug.LogError("[CharacterManager] Check failed!!");
                return "";
            }

            return curCharacterImportSettings.ToJsonText();
        }


        private bool Check()
        {
            // Todo. the curCharacterImportSettings is not null, but the cachePtr is 0x0.
            if (curCharacterImportSettings.settingItems.Length == 0 )
                return false;

            if (curCharacterSetting.skeletonAssetId == "-1" ||
                curCharacterSetting.baseAnimationAssetId == "-1" ||
                curCharacterSetting.baseBodyAssetId == "-1")
                return false;

            return true;
        }
        
        private PaabAssetImportSettings GetCharacterImportSettings(OperationType operationType)
        {
            var characterImportSettings = ScriptableObject.CreateInstance<PaabAssetImportSettings>();
            characterImportSettings.SetAssetTypeName(AssetImportSettingsType.Character);
            
            // if (characterImportSettings.basicInfoSetting == null)
            // {
            //     characterImportSettings.basicInfoSetting = new PaabBasicInfoImportSetting();
            //     characterImportSettings.basicInfoSetting.characterName = CharacterUtil.NewCharacterTempName;
            // }

            characterImportSettings.opType = operationType;
            
            if (characterImportSettings.settingItems == null)
            {
                var setting = new PaabCharacterImportSetting();
                characterImportSettings.settingItems = new[]
                {
                    setting
                };
            }
            
            return characterImportSettings;
        }

        public string GetCurrentCharacterPath()
        {
            return characterPath;
        }
        
        
        
        private static CharacterManager _instance;
        public static CharacterManager instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CharacterManager();

                return _instance;
            }
        }
        // whether CharacterManager has been created.
        public static bool isValid => _instance != null;
    }
}
#endif