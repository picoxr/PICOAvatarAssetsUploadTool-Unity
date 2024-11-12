#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Pico.AvatarAssetPreview
{
    /// <summary>
    /// common public req params in header/query
    /// </summary>
    public static class CommonPublicHttpData
    {
        //public static Dictionary<string, string> CommonHeader = new Dictionary<string, string>
        //{
        //    ["x-tt-env"] = GetLoginHeader(),
        //    ["x-avatar-token"] = GetToken(),
        //};

        //public static Dictionary<string, string> CommonQueryParams = new Dictionary<string, string>
        //{
        //    ["req_type"] = "unity_assetbuilder",
        //    ["sdk_version"] = GetSdkVersion(),
        //    ["app_version"] = GetAppVersion(),
        //    ["pico_app_id"] = GetAppid()
        //};

        public static Dictionary<string, string> AddQueryParams()
        {
            return new Dictionary<string, string>
            {
                ["req_type"] = "unity_assetbuilder",
                ["sdk_version"] = GetSdkVersion(),
                ["app_version"] = GetAppVersion(),
                ["pico_app_id"] = GetAppid()
            };
        }

        public static Dictionary<string, string> AddQueryHeaders()
        {
            if (AssetServerProConfig.IsPpe)
            {
                return new Dictionary<string, string>
                {
                    ["x-use-ppe"] = "1",
                    ["x-tt-env"] = GetLoginHeader(),
                    ["x-avatar-token"] = GetToken(),
                };
            }
           else
            {
                return new Dictionary<string, string>
                {
                    //["x-tt-env"] = GetLoginHeader(),
                    ["x-avatar-token"] = GetToken(),
                };
            }                
        }
        public static PaabDeveloperInfoImportSetting LoadLoginSetting()
        {
            return AssetImportManager.instance.assetImportDeveloperData;
        }
        public static string GetToken()
        {
            var result = LoadLoginSetting();
            if (result == null)
                return String.Empty;
            return result.loginToken;
        }
        
        public static string GetAppid()
        {
            var result = LoadLoginSetting();
            if (result == null)
                return String.Empty;
            return result.appID;
        }
		public static string GetSdkVersion()
		{
            return "2.9.1";
		}

        public static string GetAppVersion()
        {
            return "1.1.0";
		}

		public static string GetLoginHeader()
        {
            if (AssetServerProConfig.IsBoe)
            {
                return "";
                //return "boe_pico_avatar_open";
            }
            else if (AssetServerProConfig.IsPpe)
            {
                return "ppe_avatar_hub";
            }
            return "";
        }
    }
}
#endif