#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Pico.AvatarAssetBuilder
{
    /// <summary>
    /// common public req params in header/query
    /// </summary>
    public static class CommonPublicHttpData
    {
        public static Dictionary<string, string> CommonHeader = new Dictionary<string, string>
        {
            ["x-tt-env"] = GetLoginHeader(),
            ["x-avatar-token"] = GetToken(),
        };

        public static Dictionary<string, string> CommonQueryParams = new Dictionary<string, string>
        {
            ["req_type"] = "unity_assetbuilder",
            ["sdk_version"] = "0.0.1",
            ["pico_app_id"] = GetAppid()
        };

        public static Dictionary<string, string> AddQueryParams()
        {
            return new Dictionary<string, string>
            {
                ["req_type"] = "unity_assetbuilder",
                ["sdk_version"] = "0.0.1",
                ["pico_app_id"] = GetAppid()
            };
        }

        public static Dictionary<string, string> AddQueryHeaders()
        {
            return new Dictionary<string, string>
            {
                ["x-tt-env"] = GetLoginHeader(),
                ["x-avatar-token"] = GetToken(),
            };
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
        
        public static string GetLoginHeader()
        {
            return AssetServerProConfig.IsBoe ? "boe_pico_avatar_open" : "";
        }
    }
}
#endif