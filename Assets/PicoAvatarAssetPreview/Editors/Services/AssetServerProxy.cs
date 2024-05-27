#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Net.Http;
using Pico.AvatarAssetPreview.Protocol;
using UnityEngine;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        /**
         * @brief Proxy for asset server. 
         */
        public class AssetServerProxy
        {
            private WWWForm requestBaseForm
            {
                get
                {
                    WWWForm temp = new WWWForm();
                    temp.AddField("req_type", "unity");
                    temp.AddField("sdk_version", "3.0.0");
                    return temp;
                }
            }

            public AssetServerType DevelopServerType
            {
                get
                {
                    var curDeveloper = AssetImportManager.instance.assetImportDeveloperData;
                    if (curDeveloper == null)
                        return AssetServerType.China;
                    return (AssetServerType)curDeveloper.serverType;
                }
            }

            public string Token
            {
                get
                {
                    var curDeveloper = AssetImportManager.instance.assetImportDeveloperData;
                    if (curDeveloper == null)
                        return "";
                    return curDeveloper.loginToken;
                }
            }

            public string GetRequestFullUrl(string functionURL)
            {
                string url = AssetServerProConfig.GetHttpFullUrl(DevelopServerType, functionURL);
                Debug.Log("GetRequestFullUrl:" + url);
                return url;
            }

            //http request
            public CommonHttpRequest GetLoginAppInfo()
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.SvrPackInfo)
                    .SetHttpMethod(HttpMethod.Get)
                    .AddQueryParam("Content-Type", "application/x-www-form-urlencoded")
                    .Build();
            }

            /// <summary>
            /// Get asset(skeleton、animset、commponent) detail with name asynchronously.
            /// </summary>
            /// <param name="asset_id"></param>
            /// <returns></returns>
            public CommonHttpRequest GetAssetDetailInfo(string asset_id)
            {
                //TODO: get asset information from server， and then download asset 
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.GET_ASSET_DETAIL_GET_URL)
                    .SetHttpMethod(HttpMethod.Get)
                    .AddQueryParam("asset_id", asset_id)
                    .Build();
            }

            /// <summary>
            /// Get asset category list by skeleton_id asynchronously.
            /// </summary>
            /// <param name="skeleton_id"></param>
            /// <param name="need_paging"></param>
            /// <returns></returns>
            public CommonHttpRequest GetCategoryListBySkeleton(string skeleton_id, int need_paging)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.GET_ASSET_DETAIL_GET_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .AddQueryParam("skeleton_id", skeleton_id)
                    .AddQueryParam("need_paging", need_paging)
                    .Build();
            }

            /// <summary>
            /// Get asset AnimationSet list by skeleton_id asynchronously.
            /// </summary>
            /// <param name="skeleton_id"></param>
            /// <param name="need_paging"></param>
            /// <returns></returns>
            public CommonHttpRequest GetBaseAnimationSetListByApp(string skeletonId, string assetStatus, string searchWords)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.POST_BASE_ANIMATION_SET_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .AddPostField("status", assetStatus)
                    .AddPostField("search_word", searchWords)
                    .AddPostField("skeleton_id", skeletonId)
                    .Build();
            }
            
            /// <summary>
            /// Get character custom AnimationSet list by character_id asynchronously.
            /// </summary>
            /// <param name="character_id"></param>
            /// <param name="asset_status"></param>
            /// /// <param name="search_words"></param>
            /// <returns></returns>
            public CommonHttpRequest GetCustomAnimationSetListByCharacter(string characterId, string assetStatus, string searchWords)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.POST_CUSTOM_ANIMATION_SET_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .AddPostField("status", assetStatus)
                    .AddPostField("search_word", searchWords)
                    .AddPostField("character_id", characterId)
                    .Build();
            }
            

            /// <summary>
            /// Get asset Skeleton list asynchronously.
            /// </summary>
            /// <param name="need_paging"></param>
            /// <returns></returns>
            public CommonHttpRequest GetSkeletonListByApp(string assetStatus, string searchWords)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.POST_SKELETON_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .AddPostField("status", assetStatus)
                    .AddPostField("search_word", searchWords)
                    .Build();
            }
            
            
            public CommonHttpRequest GetCharacterListByApp(string assetStatus, string searchWords)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.POST_CHARACTER_LIST_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .AddPostField("status", assetStatus)
                    .AddPostField("search_word", searchWords)
                    .Build();
            }
            
            /// <summary>
            /// Start Download file
            /// </summary>
            /// <param name="url"></param>
            /// <param name="fileName"></param>
            /// <param name="success"></param>
            /// <param name="progress"></param>
            /// <param name="failure"></param>
            public void StartDownloadFile(string url, string fileName, Action<string> success, Action<string, float> progress,
                Action<string, string> failure)
            {
                var request = new CommonHttpRequest.Builder()
                    .SetHttpUrl(url)
                    .SetHttpMethod(HttpMethod.Get)
                    .Build();
                request.StartDownload(fileName, success, progress, failure);
            }
            
            
            public CommonHttpRequest CheckAssetName(string name, long itemType)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.ASSET_NAME_CHECK_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .AddPostField("item_type", itemType)
                    .AddPostField("name", name)
                    .Build();
            }

            public CommonHttpRequest CreateNewCharacter(string json)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.CREATE_CHARACTER_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .SetPostData(json)
                    .Build();
            }
            
            public CommonHttpRequest UpdateCharacter(string json)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.UPDATE_CHARACTER_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .SetPostData(json)
                    .Build();
            }
            
            public CommonHttpRequest GetAssetCategory(string character_id)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.ASSET_CATEGORY_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .AddPostField("character_id", character_id)
                    .Build();
            }
            
            public CommonHttpRequest GetComponentAssetList(string json)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.COMPONENT_ASSET_LIST_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .SetPostData(json)
                    .Build();
            }
            
            public CommonHttpRequest GetAnimaitonSetAssetList(string character_id, string assetStatus, string searchWords)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.ANIMATION_SET_ASSET_LIST_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .AddPostField("status", assetStatus)
                    .AddPostField("search_word", searchWords)
                    .AddPostField("character_id", character_id)
                    .Build();
            }
            
            public CommonHttpRequest GetPresetList(string character_id, string assetStatus, string searchWords)
            {
                string postData = "{\"need_paging\":0,\"page\":0,\"page_size\":0,\"character_id\":\"" + character_id + "\",\"status\":\"\",\"label\":\"\",\"search_word\":\"\"}";
                
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.PRESET_LIST_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .SetPostData(postData)
                    .Build();
            }

            public CommonHttpRequest CreateComponentAsset(string json)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.POST_ASSET_CREATE_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .SetPostData(json)
                    .Build();
            }
            
            public CommonHttpRequest UpdateComponentAsset(string json)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.POST_ASSET_UPDATE_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .SetPostData(json)
                    .Build();
            }
            
            public CommonHttpRequest CreateCustomAnimation(string json)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.POST_CUSTOM_ANIMATIONSET_CREATE_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .SetPostData(json)
                    .Build();
            }
            
            public CommonHttpRequest UpdateCustomAnimation(string json)
            {
                return new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(DevelopServerType))
                    .SetPath(AssetServerProConfig.POST_CUSTOM_ANIMATIONSET_UPDATE_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .SetPostData(json)
                    .Build();
            }
        }
    }
}
#endif