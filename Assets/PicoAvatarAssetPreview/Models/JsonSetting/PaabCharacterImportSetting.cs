#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Pico.Avatar;
using Object = UnityEngine.Object;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        [Serializable]
        public class PaabCharacterImportSetting : PaabAssetImportSetting
        {

            // asset import setting type.
            public override AssetImportSettingType settingType { get => AssetImportSettingType.Character; }

            [SerializeField] public string setType = "none";

            [SerializeField] public string skeletonAssetId = "-1";
            [SerializeField] public string skeletonAssetState = "online"; // "local"
            [SerializeField] public string skeletonAssetPath = "/";

            [SerializeField] public string baseAnimationAssetId = "-1";
            [SerializeField] public string baseAnimationAssetState = "online"; // "local"
            [SerializeField] public string baseAnimationAssetPath = "/";
            
            [SerializeField] public string baseBodyAssetId = "-1";
            [SerializeField] public string baseBodyAssetState = "online"; // "local"
            [SerializeField] public string baseBodyAssetPath = "/";
            
            [SerializeField] public string avatarStyle = "PicoCustomAvatar";

            [SerializeField] public string infoStr = "{\"sex\":\"male\",\"status\":\"Online\",\"continent\":\"EU\",\"background\":{\"image\":\"https: //dfsedffe.png\",\"end_color\":[133,182,255],\"start_color\":[148,111,253]},\"avatar_type\":\"preset\"}";
            [SerializeField] public string bodyStr = "{\"version\":1,\"perParams\":[],\"technique\":\"Pico2-Bone\",\"floatIdParams\":[]}";
            [SerializeField] public string headStr = "{\"version\":1,\"perParams\":[],\"technique\":\"Pico2-BS\",\"floatIdParams\":[]}";
            [SerializeField] public string skinStr = "{\"color\":\"\",\"white\":0,\"softening\":0}";            
            [SerializeField] public string assetPinsStr = "[]";

            public const string AssetState_Online = "Online";
            public const string AssetState_Local = "Local";


            public void setSkeletonAsset(string idOrPath, string state)
            {
                skeletonAssetId = idOrPath;
                skeletonAssetState = state;
                skeletonAssetPath = idOrPath;
            }
            
            public void setBaseAnimationAsset(string idOrPath, string state)
            {
                baseAnimationAssetId = idOrPath;
                baseAnimationAssetState = state;
                baseAnimationAssetPath = idOrPath;
            }
            
            public void setBaseBodyAsset(string idOrPath, string state)
            {
                baseBodyAssetId = idOrPath;
                baseBodyAssetState = state;
                baseBodyAssetPath = idOrPath;
            }
            
            // if asset state is local. set id to -1.
            

            /**
             * @brief Build from json object. Derived class SHOULD override the method.
             */
            public override void FromJsonObject(Dictionary<string, object> jsonObject)
            {
                
                var infoObj = (Dictionary<string, object>)jsonObject.GetValueOrDefault("info", null);
                if (infoObj != null)
                {
                    infoStr = JsonConvert.SerializeObject(infoObj);
                }
                
                var avatarObj = (Dictionary<string, object>)jsonObject.GetValueOrDefault("avatar", null);
                if (avatarObj != null)
                {
                    var bodyObj = (Dictionary<string, object>)avatarObj.GetValueOrDefault("body", null);
                    if (bodyObj != null)
                    {
                        bodyStr = JsonConvert.SerializeObject(bodyObj);
                    }
                    
                    var headObj = (Dictionary<string, object>)avatarObj.GetValueOrDefault("head", null);
                    if (headObj != null)
                    {
                        headStr = JsonConvert.SerializeObject(headObj);
                    }
                    
                    var skinObj = (Dictionary<string, object>)avatarObj.GetValueOrDefault("skin", null);
                    if (skinObj != null)
                    {
                        headStr = JsonConvert.SerializeObject(skinObj);
                    }
                    
                    var assetPinsObj = (Dictionary<string, object>)avatarObj.GetValueOrDefault("assetPins", new List<object>());
                    if (assetPinsObj != null)
                    {
                        assetPinsStr = JsonConvert.SerializeObject(assetPinsObj);
                    }
                    
                    var skeletonObj = (Dictionary<string, object>)avatarObj.GetValueOrDefault("skeleton", null);
                    if (skeletonObj != null)
                    {
                        skeletonAssetId = (string)skeletonObj.GetValueOrDefault("assetId", "");
                        var skeletonMeta = (Dictionary<string, object>)skeletonObj.GetValueOrDefault("assetMeta", null);
                        var state = (string)skeletonMeta.GetValueOrDefault("state", "");
                        if (state != "")
                        {
                            skeletonAssetState = state;
                        }
                        else
                        {
                            skeletonAssetState = "online";
                        }
                        
                        var path = (string)skeletonMeta.GetValueOrDefault("path", "");
                        if (path != "")
                        {
                            skeletonAssetPath = path;
                        }
                        else
                        {
                            skeletonAssetPath = "/";
                        }
                    }
                    
                    var baseAnimationObj = (Dictionary<string, object>)avatarObj.GetValueOrDefault("baseAnimation", null);
                    if (baseAnimationObj != null)
                    {
                        baseAnimationAssetId = (string)baseAnimationObj.GetValueOrDefault("assetId", "");
                        var baseAnimationMeta = (Dictionary<string, object>)baseAnimationObj.GetValueOrDefault("assetMeta", null);
                        var state = (string)baseAnimationMeta.GetValueOrDefault("state", "");
                        if (state != "")
                        {
                            baseAnimationAssetState = state;
                        }
                        else
                        {
                            baseAnimationAssetState = "online";
                        }
                        
                        var path = (string)baseAnimationMeta.GetValueOrDefault("path", "");
                        if (path != "")
                        {
                            baseAnimationAssetPath = path;
                        }
                        else
                        {
                            baseAnimationAssetPath = "/";
                        }
                    }
                    
                    var baseBodyObj = (Dictionary<string, object>)avatarObj.GetValueOrDefault("baseBody", null);
                    if (baseBodyObj != null)
                    {
                        baseBodyAssetId = (string)baseBodyObj.GetValueOrDefault("assetId", "");
                        var baseBodyMeta = (Dictionary<string, object>)baseBodyObj.GetValueOrDefault("assetMeta", null);
                        var state = (string)baseBodyMeta.GetValueOrDefault("state", "");
                        if (state != "")
                        {
                            baseBodyAssetState = state;
                        }
                        else
                        {
                            baseBodyAssetState = "online";
                        }
                        
                        var path = (string)baseBodyMeta.GetValueOrDefault("path", "");
                        if (path != "")
                        {
                            baseBodyAssetPath = path;
                        }
                        else
                        {
                            baseBodyAssetPath = "online";
                        }
                    }

                }
                
                avatarStyle = (string)jsonObject.GetValueOrDefault("avatarStyle", "");
            }

            /**
             * @brief Serialize to json object. Derived class SHOULD override the method.
             */
            public override void ToJsonObject(Dictionary<string, object> jsonObject)
            {
                //"info"
                var info = new Dictionary<string, object>();
                info = JsonConvert.DeserializeObject<Dictionary<string, object>>(infoStr);
                jsonObject["info"] = info;
                
                //"avatar"
                var avatar = new Dictionary<string, object>();
                avatar["body"] = JsonConvert.DeserializeObject<Dictionary<string, object>>(bodyStr);
                avatar["head"] = JsonConvert.DeserializeObject<Dictionary<string, object>>(headStr);
                avatar["skin"] = JsonConvert.DeserializeObject<Dictionary<string, object>>(skinStr);
                avatar["assetPins"] = JsonConvert.DeserializeObject<List<object>>(assetPinsStr);
                if (avatar["assetPins"] == null)
                {
                    avatar["assetPins"] = new List<object>();
                }
                
                // avatar skeleton
                var skeletonItem = new Dictionary<string, object>();
                skeletonItem["assetId"] = skeletonAssetId;
                var skeletonMeta = new Dictionary<string, object>();
                skeletonItem["assetMeta"] = skeletonMeta;
                skeletonMeta["state"] = skeletonAssetState;
                skeletonMeta["path"] = skeletonAssetPath;
                avatar["skeleton"] = skeletonItem;
                
                // avatar baseAnimation
                var baseAnimationItem = new Dictionary<string, object>();
                baseAnimationItem["assetId"] = baseAnimationAssetId;
                var baseAnimationMeta = new Dictionary<string, object>();
                baseAnimationItem["assetMeta"] = baseAnimationMeta;
                baseAnimationMeta["state"] = baseAnimationAssetState;
                baseAnimationMeta["path"] = baseAnimationAssetPath;
                avatar["baseAnimation"] = baseAnimationItem;
                
                // avatar baseBody
                var baseBodyItem = new Dictionary<string, object>();
                baseBodyItem["assetId"] = baseBodyAssetId;
                var baseBodyMeta = new Dictionary<string, object>();
                baseBodyItem["assetMeta"] = baseBodyMeta;
                baseBodyMeta["state"] = baseBodyAssetState;
                baseBodyMeta["path"] = baseBodyAssetPath;
                avatar["baseBody"] = baseBodyItem;
                
                jsonObject["avatar"] =  avatar;
                
                //"avatarStyle"
                jsonObject["avatarStyle"] = avatarStyle;
                
            }
        }
    }
}
#endif