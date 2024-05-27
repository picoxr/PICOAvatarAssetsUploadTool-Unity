#if UNITY_EDITOR
using System.Collections.Generic;
using Newtonsoft.Json;
namespace Pico
{
    namespace AvatarAssetBuilder
    {
        /**
         * @brief The class encapsulates all kinds of helper methods to build avatar specification text.
         */ 
        public class AvatarSpecificationBuilder
        {
            /**
             * @brief Build avatar spec text.
             */
            public string BuildAssetViewerAvatarSpecText(PaabAssetImportSettings assetSettings)
            {
                // prepare import settings.
                Prepare(assetSettings);

                // add the default asset.
                if(assetSettings.assetImportSettingsType != AssetImportSettingsType.Skeleton)
                {
                    AddAsset(assetSettings);
                }

                // return specification text.
                return ResolveAvatarSpecText();
            }

            /**
             * @brief Build specification json object.
             */
            public void Prepare(PaabAssetImportSettings assetSettings)
            {
                InitializeBasicObjects();

                //analyse skeleton.
                TryAddSkeletonAsset(assetSettings);
            }
            
            /**
             * @brief Adds asset to assetPins.
             */ 
            public void AddAsset(PaabAssetImportSettings assetSettings)
            {
                // basic info.
                var assetPinProtoJsonObj = new Dictionary<string, object>();
                FillAssetPinJsonObject(assetPinProtoJsonObj, assetSettings);
                //
                _jo_avatar_assetPins.Add(assetPinProtoJsonObj);

                //switch (assetSettings.assetImportSettingsType)
                //{
                //    case AssetImportSettingsType.Skeleton:
                //        break;
                //    case AssetImportSettingsType.AnimationSet:
                //        break;
                //    case AssetImportSettingsType.Clothes:
                //        break;
                //    case AssetImportSettingsType.Hair:
                //        break;
                //    case AssetImportSettingsType.Shoe:
                //        break;
                //}
            }

            public string ResolveAvatarSpecText()
            {
                //
                ResolveJsonObjects();
                //
                var jsonText = JsonConvert.SerializeObject(_jo_root);
                return jsonText;
            }

            //
            private Dictionary<string, object> _jo_root;
            private Dictionary<string, object> _jo_info;
            private Dictionary<string, object> _jo_avatar;
            private Dictionary<string, object> _jo_avatar_head;
            private Dictionary<string, object> _jo_avatar_body;
            private List<object> _jo_avatar_assetPins;

            /**
             * @brief Initialize basic objects.
             */ 
            private void InitializeBasicObjects()
            {
                _jo_root = new Dictionary<string, object>();
                // "info":{}
                _jo_info = new Dictionary<string, object>();
                // "avatar":{}
                _jo_avatar = new Dictionary<string, object>();

                // "head":{}
                _jo_avatar_head = new Dictionary<string, object>();
                // "body":{}
                _jo_avatar_body = new Dictionary<string, object>();
                // "assetPins":{}
                _jo_avatar_assetPins = new List<object>();
            }

            /**
             * @brief connect json objects.
             */
            private void ResolveJsonObjects()
            {
                // info
                {
                    _jo_root["info"] = _jo_info;
                }

                // avatar
                {
                    _jo_root["avatar"] = _jo_avatar;

                    // head
                    {
                        _jo_avatar["head"] = _jo_avatar_head;
                    }

                    // body
                    {
                        _jo_avatar["body"] = _jo_avatar_body;
                    }

                    // assetPins
                    {
                        _jo_avatar["assetPins"] = _jo_avatar_assetPins.ToArray();
                    }
                }

                // avatar style
                {
                    _jo_root["avatarStyle"] = "PicoAvatar3";
                }
            }

            /**
             * @brief build skeleton assets.
             */ 
            private void TryAddSkeletonAsset(PaabAssetImportSettings assetSettings)
            {
                if (assetSettings.basicInfoSetting != null)
                {
                    string assetId = assetSettings.basicInfoSetting.skeletonAssetId;
                    // if asset is skeleton id, get id from basicInfoSetting
                    if (assetSettings.assetImportSettingsType == AssetImportSettingsType.Skeleton)
                    {
                        assetId = assetSettings.basicInfoSetting.assetId;
                    }
                    //
                    _jo_avatar["skeleton"] = BuildSkeletonJsonObject(assetId);
                }
            }

            private Dictionary<string, object> BuildSkeletonJsonObject(string skeletonAssetId)
            {
                var ret = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(skeletonAssetId))
                {
                    ret["asset"] = skeletonAssetId;
                }
                return ret;
            }

            private Dictionary<string, object> BuildBasicInfoJsonObject(PaabBasicInfoImportSetting basicSetting)
            {
                var ret = new Dictionary<string, object>();
                
                ret["id"] = basicSetting.assetId;


                return ret;
            }
            /**
             * @brief Fill asset json object. Derived class SHOULD override and invoke the method.
             */
            public void FillAssetPinJsonObject(Dictionary<string, object> assetPinJsonObject, PaabAssetImportSettings ownerAssetSettings)
            {
                var assetMetaJsonObj = new Dictionary<string, object>();
                ownerAssetSettings.FillAssetMetaProtoJsonObject(assetMetaJsonObj);

                assetPinJsonObject["assetId"] = ownerAssetSettings.basicInfoSetting.assetId;
                assetPinJsonObject["ownerAssetId"] = "";
                assetPinJsonObject["assetMeta"] = ownerAssetSettings;
                assetPinJsonObject["timeStamp"] = 0;
                assetPinJsonObject["userParams"] = new Dictionary<string, object>();
            }
        }
    }
}
#endif