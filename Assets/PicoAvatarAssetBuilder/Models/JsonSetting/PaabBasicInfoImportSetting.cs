#if UNITY_EDITOR
using Pico.Avatar;
using Pico.AvatarAssetBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        [Serializable]
        public class PaabBasicInfoImportSetting : PaabAssetImportSetting
        {
            // asset import setting type.
            public override AssetImportSettingType settingType { get => AssetImportSettingType.BasicInfo; }

            [SerializeField] 
            public string characterFolderName;
            
            [SerializeField] 
            public string characterName;
            
            [SerializeField] 
            public string characterId;

            // display name that can be changed any time.
            [SerializeField]
            public string assetNickName;

            [SerializeField]
            public string assetIconPath;

            // asset skeleton asset name
            [SerializeField]
            public string skeletonAssetName;

            // asset skeleton asset id
            [SerializeField]
            public string skeletonAssetId;



            // Assets name. should not be modified.
            public string assetName { get => _assetName; }

            // Assets id. should not be modified.
            public string assetId { get => _assetId; }

            /**
             * @brief Sets asset name of the importing asset.
             */
            public void SetAssetName(string assetName_)
            {
                _assetName = assetName_;
            }

            /**
             * @brief Sets asset id of the importing asset.
             */
            public void SetAssetId(string assetId_)
            {
                _assetId = assetId_;
            }

            /**
             * @brief Build from json object. Derived class SHOULD override the method.
             */
            public override void FromJsonObject(Dictionary<string, object> jsonObject)
            {
                _assetName = Utility.GetString(jsonObject, "assetName");
                _assetId = Utility.GetString(jsonObject, "assetId");
                characterFolderName = Utility.GetString(jsonObject, "characterFolderName");
                characterName = Utility.GetString(jsonObject, "characterName");
                characterId = Utility.GetString(jsonObject, "characterId");
                //TODO: add to section?
                assetNickName = Utility.GetString(jsonObject, "assetNickName");
                assetIconPath = Utility.GetString(jsonObject, "assetIconPath");
                skeletonAssetName = Utility.GetString(jsonObject, "skeletonAssetName");
                skeletonAssetId = Utility.GetString(jsonObject, "skeletonAssetId");
            }

            /**
             * @brief Serialize to json object. Derived class SHOULD override the method.
             */
            public override void ToJsonObject(Dictionary<string, object> jsonObject)
            {
                jsonObject["assetName"] = _assetName;
                jsonObject["assetId"] = _assetId;
                jsonObject["characterFolderName"] = characterFolderName;
                jsonObject["characterName"] = characterName;
                jsonObject["characterId"] = characterId;
                //TODO: add to section?
                jsonObject["assetNickName"] = assetNickName;
                jsonObject["assetIconPath"] = assetIconPath;
                jsonObject["skeletonAssetName"] = skeletonAssetName;
                jsonObject["skeletonAssetId"] = skeletonAssetId;
            }

            /**
             * @brief Fill asset json object. Derived class SHOULD override and invoke the method.
             */
            public override void FillAssetMetaProtoJsonObject(Dictionary<string, object> assetMetaProtoJsonObj, PaabAssetImportSettings ownerAssetSettings)
            {
                var pinEditAttrsJsonObj = new Dictionary<string, object>();
                {
                    assetMetaProtoJsonObj["pinEditAttrs"] = pinEditAttrsJsonObj;
                }
            }

            // Assets name. should not be modified.
            [SerializeField]
            private string _assetName;

            // Assets id. should not be modified.
            [SerializeField]
            private string _assetId;
        }
    }
}
#endif