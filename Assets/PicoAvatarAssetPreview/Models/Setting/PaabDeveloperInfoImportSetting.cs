#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pico.Avatar;
using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        [Serializable]
        public class PaabDeveloperInfoImportSetting : ScriptableObject
        {
            // asset import setting type.
            //public override AssetImportSettingType settingType { get => AssetImportSettingType.DeveloperInfo; }

            [SerializeField]
            public string loginToken;
            [SerializeField]
            public string saveTime;
            [SerializeField]
            public string appPackInfoServerJson;
            // server user key start
            [SerializeField]
            public string userName;
            [SerializeField]
            public string userID;
            [SerializeField]
            public string userThumbnail;
            //server user key end
            // server app key start
            [SerializeField]
            public string orzID;
            [SerializeField]
            public string appID;
            [SerializeField]
            public string appName;
            [SerializeField]
            public bool isOfficial;
            [SerializeField]
            public int appType;
            [SerializeField] 
            public bool canUploadOfficialComponent;
			//server app key end
			[SerializeField]
            public int serverType;

			public Dictionary<string, string> packetContent = new Dictionary<string, string>();

            public bool HasToken
            {
                get
                {
                    return string.IsNullOrEmpty(loginToken);
                }
            }
            public bool HasBindAppID
            {
                get
                {
                    return !string.IsNullOrEmpty(appPackInfoServerJson) && !string.IsNullOrEmpty(appID);
                }
            }
            
            
            public bool HasBindInfo => !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(appName);

            void ResetAppInfo()
            {
                Debug.Log("ResetAppInfo");
                this.appPackInfoServerJson = "";
                appID = String.Empty;
                appName = String.Empty;
            }


            /**
            * @brief Build from json object. Derived class SHOULD override the method.
            */
            public void FromJsonObject(JObject jsonObject)
            {
                //user info
                var userInfo = jsonObject.Value<JObject>("user");
                if (userInfo == null) return;
                userID = userInfo.Value<string>("id");
                userName = userInfo.Value<string>("name");
                userThumbnail = userInfo.Value<string>("avatar_url");
                
                // app info
                var orgPacks = jsonObject.Value<JArray>("org_packs");
                if (orgPacks == null || orgPacks.Count == 0)
                {
                    //未绑定应用
                    return;
                }

              
                var appInfo = jsonObject.Value<JObject>("app");

                if (appInfo == null) return;
                this.appID = appInfo.Value<string>("pico_app_id");
                this.appName = appInfo.Value<string>("name");
                this.isOfficial = appInfo.Value<bool>("is_official");
                this.appType = appInfo.Value<int>("app_type");//应用的形象准入模式
                this.canUploadOfficialComponent = appInfo.Value<bool>("upload_official_component");

                foreach (var packInfo in orgPacks)
                {
                    if (packInfo?["apps"] != null)
                    {
                        foreach (var info in packInfo["apps"])
                        {
                            if (info["pico_app_id"]?.ToString() == appID)
                            {
                                orzID = packInfo?["org"]?["org_id"]?.ToString();
                            }
                        }
                    }
                }
            }

            /**
             * @brief Serialize to json object. Derived class SHOULD override the method.
             */
            public void ToJsonObject(Dictionary<string, object> jsonObject)
            {
                //TODO: add to section?
            }

            public void InitLoginToken(string token, string appid, string saveTime)
            {
                loginToken = token;
                this.saveTime = saveTime;
                appID = appid;
            }
            public void InitServerType(int serverType)
            {
                this.serverType = serverType;
            }
            public void InitAppInfo(string appInfoJson)
            {
                this.appPackInfoServerJson = appInfoJson;
                var info = JsonConvert.DeserializeObject<JObject>(appPackInfoServerJson);
                if (info!.Value<int>("code") == 0)
                {
                    this.FromJsonObject(info.Value<JObject>("data"));
                }
             
            }
            public void Logout()
            {
                ResetAppInfo();
                loginToken = String.Empty;
                userName = String.Empty;
            }
        }
    }
}
#endif