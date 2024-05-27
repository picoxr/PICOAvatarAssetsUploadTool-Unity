#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;

namespace Pico.AvatarAssetBuilder
{
    public enum AssetServerType
    {
        China = 1,
        Global,
        BOE_China,
        BOE_Global,
    }

    public class AssetServerProConfig
    {
        private static StringBuilder tempStringBuilder = new StringBuilder();

#region config

        private static Dictionary<AssetServerType, string> serverConfigDomain =
            new Dictionary<AssetServerType, string>()
            {
                { AssetServerType.China, "https://avatar.picovr.com" },
                { AssetServerType.Global, "https://avatar-global.picovr.com" },
                { AssetServerType.BOE_China, "https://avatar.picovr.com" },
                { AssetServerType.BOE_Global, "https://avatar-global.picovr.com" },
            };

#endregion

        public const bool IsBoe = false;

#region server url

        //Login
        public const string LocalListernAdress = "http://127.0.0.1:{0}/auth/callback/";
        public const string LoginWeb = "/platform/unity-auth?port={0}";
        public const string CreateOrUpdateCharacterWeb = "/platform/character?orgId={0}&appId={1}&target_user={2}" +
                                                         "&search_word_type=id&search_word={3}";
        public const string CreateOrUpdateComponentWeb = "/platform/asset?orgId={0}&appId={1}&target_user={2}&search_word_type=id" +
                                                         "&search_word={3}&character_id={4}&category_key=__all__";
        public const string CreateOrUpdateCustomAniWeb = "/platform/custom-animation-set?orgId={0}&appId={1}&target_user={2}" +
                                                         "&search_word_type=id&search_word={3}&character_id={4}";
        
        public const string SvrPackInfo = "/platform_api/v2/user/pack_info";

        //Upload
        public const string POST_FILE_UPLOAD_URL = "/platform_api/v2/file/upload";
        public const string POST_SKELETON_CREATE_URL = "/platform_api/v2/asset/skeleton/create";
        public const string POST_ANIMATIONSET_CREATE_URL = "/platform_api/v2/asset/animation_set/create";
        public const string POST_CUSTOM_ANIMATIONSET_CREATE_URL = "/platform_api/v2/asset/animation_set/create_custom";
        public const string POST_ASSET_CREATE_URL = "/platform_api/v2/asset/create";
        public const string POST_CUSTOM_ANIMATIONSET_UPDATE_URL = "/platform_api/v2/asset/animation_set/update_custom";
        public const string POST_ASSET_UPDATE_URL = "/platform_api/v2/asset/update";

        //asset list
        public const string POST_CATEGORY_BY_SKELETON_URL = "/platform_api/v2/category/list_by_skeleton";
        public const string GET_ASSET_DETAIL_GET_URL = "/platform_api/v2/asset/get";
        public const string POST_ANIMATION_SET_URL = "/platform_api/v2/asset/animation_set/list_by_app";
        public const string POST_BASE_ANIMATION_SET_URL = "/platform_api/v2/asset/animation_set/base_list_by_skeleton";
        public const string POST_CUSTOM_ANIMATION_SET_URL = "/platform_api/v2/asset/animation_set/custom_list_by_character";
        public const string POST_SKELETON_URL = "/platform_api/v2/asset/skeleton/list_by_app";
        public const string POST_CHARACTER_LIST_URL = "/platform_api/v2/character/list_by_app";
        public const string ASSET_NAME_CHECK_URL = "/platform_api/v2/item/name_check";
        public const string ASSET_CATEGORY_URL = "/platform_api/v2/category/list_by_character";
        public const string COMPONENT_ASSET_LIST_URL = "/platform_api/v2/asset/list_by_category";
        public const string ANIMATION_SET_ASSET_LIST_URL = "/platform_api/v2/asset/animation_set/custom_list_by_character";
        public const string PRESET_LIST_URL = "/platform_api/v2/preset/list_by_character";

        //Create character
        public const string CREATE_CHARACTER_URL = "/platform_api/v2/character/create";
        public const string UPDATE_CHARACTER_URL = "/platform_api/v2/character/update";


#endregion

#region errorCode

        public const int Result_Code_Success = 0;

#endregion

#region Public Method

        /// <summary>
        /// 获取base url
        /// </summary>
        /// <param name="serverType"></param>
        /// <returns></returns>
        public static string GetDomainByServerType(AssetServerType serverType)
        {
            serverType = UseBoeTransform(serverType);
            string url = "";
            serverConfigDomain.TryGetValue(serverType, out url);
            if (string.IsNullOrEmpty(url))
                return "";
            return url;
        }

        /// <summary>
        /// 获取base url
        /// </summary>
        /// <param name="serverType"></param>
        /// <returns></returns>
        public static string GetDomainByServerType()
        {
            var serverType = LoginUtils.LoadLoginSetting()?.serverType;
            AssetServerType assetServerType;
            #if UNITY_EDITOR
            assetServerType = serverType == 1?AssetServerType.China:AssetServerType.Global;
            #endif
            assetServerType = UseBoeTransform(assetServerType);
            string url = "";
            serverConfigDomain.TryGetValue(assetServerType, out url);
            if (string.IsNullOrEmpty(url))
                return "";
            return url;
        }

        
        /// <summary>
        /// 获取完整的请求url地址
        /// </summary>
        /// <param name="serverType"></param>
        /// <param name="functionURL"></param>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        public static string GetHttpFullUrl(AssetServerType serverType, string functionURL, params object[] queryParams)
        {
            //减少GC
            tempStringBuilder.Clear();

            serverType = UseBoeTransform(serverType);
            string root = GetDomainByServerType(serverType);

            tempStringBuilder.Append(root);
            tempStringBuilder.Append(functionURL);
            //if(queryParams.Length  == 1)
            //{
            //    var paramsMap = (queryParams[0] as Dictionary<string, string>);
            //    if(paramsMap != null)
            //    {
            //        tempStringBuilder.Append("?");
            //        foreach (var item in paramsMap)
            //        {
            //            tempStringBuilder.Append(item.Key);
            //            tempStringBuilder.Append("=");
            //            tempStringBuilder.Append(item.Value);
            //            tempStringBuilder.Append("&");
            //        }
            //    }
            //    tempStringBuilder.Remove(tempStringBuilder.Length - 1, 1);
            //}

            return tempStringBuilder.ToString();
        }

        /// <summary>
        /// 是否使用boe环境
        /// </summary>
        /// <param name="serverType"></param>
        /// <returns></returns>
        static AssetServerType UseBoeTransform(AssetServerType serverType)
        {
            if (IsBoe)
            {
                if (serverType == AssetServerType.China)
                    return AssetServerType.BOE_China;
                if (serverType == AssetServerType.Global)
                    return AssetServerType.BOE_Global;
            }

            return serverType;
        }

#endregion
    }
}
#endif