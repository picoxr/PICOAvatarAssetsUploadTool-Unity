#if UNITY_EDITOR
using Pico.Avatar;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        /** 
         * import Asset type.
         * AssetType, eg: Skeleton Asset | AnimationSet Asset | Clothes Asset | Hair Asset | Shoe Asset.
         * Corresponds to the type of asset on the server.
         */
        public enum AssetImportSettingsType : int
        {
            Unknown = -1,
            Skeleton = 0,
            AnimationSet = 1,
            Clothes = 2,        // eg: Combined by BasicInfo | Mesh | Material
            Hair = 3,
            Shoe = 4,
            Character = 5,
            BaseBody,
            Component,
        }

        /** 
         * Sub Setting type.
         */
        public enum AssetImportSettingType : int
        {
            Unknown = -1,
            DeveloperInfo = 0,
            BasicInfo = 1,
            Skeleton = 2,
            Animation = 3,
            Mesh = 4,
            Material = 5,
            Heel = 6,
            Character = 7,
        }

        /**
         * @brief element in AssetImportSettings and provides part information used to import an asset.
         */
        [Serializable]
        public class PaabAssetImportSetting
        {
            // asset import type.
            public virtual AssetImportSettingType settingType { get => AssetImportSettingType.Unknown; }

            /**
             * @brief Build from json object. Derived class SHOULD override and invoke the method.
             */
            public virtual void FromJsonObject(Dictionary<string, object> jsonObject)
            {
            }

            /**
             * @brief Serialize to json object. Derived class SHOULD override and invoke the method.
             */
            public virtual void ToJsonObject(Dictionary<string, object> jsonObject)
            {
            }

            /**
             * @brief Fill asset meta proto json object. Derived class SHOULD override and invoke the method.
             */
            public virtual void FillAssetMetaProtoJsonObject(Dictionary<string, object> assetMetaProtoJsonObj, PaabAssetImportSettings ownerAssetSettings)
            {
                
            }
        }
    }
}
#endif