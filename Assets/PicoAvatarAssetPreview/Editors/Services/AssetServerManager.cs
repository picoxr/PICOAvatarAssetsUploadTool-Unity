#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Pico.Avatar;
using Pico.AvatarAssetPreview.Protocol;
using UnityEngine;
using UnityEngine.Networking;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        /**
         * @brief The class encapsulates management for asset server. Responsibilites include making connector to remote asset server,
         *      building AssetServerProxy etc.
         */
        public class AssetServerManager
        {
            private static AssetServerManager _instance;

            //remote asset server proxy
            private AssetServerProxy m_AssetServerProxy;

            // singleton instance of the class.
            public static AssetServerManager instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = new AssetServerManager();
                    }

                    return _instance;
                }
            }

            /**
             * @brief Connect WebServer and build session.
             */
            public void ConnectServer(System.Action<AssetServerProxy> callback = null)
            {
                if (m_AssetServerProxy == null)
                {
                    m_AssetServerProxy = new AssetServerProxy();
                }

                if (callback != null)
                    callback.Invoke(m_AssetServerProxy);
            }

#region Login

            /**
             * @brief Open the developer website
             */
            public void OpenBrowserWithHeader(string url, string headerKey, string headerValue)
            {
                UnityWebRequest webRequest = UnityWebRequest.Get(url);
                webRequest.SetRequestHeader(headerKey, headerValue);
#if UNITY_EDITOR || UNITY_STANDALONE
                Application.OpenURL(webRequest.url);
#elif UNITY_WEBGL
                    Application.ExternalEval(string.Format("window.open('{0}','_blank')", webRequest.url));
#endif
            }

            public void OpenDeveloperLoginWebsite(int port)
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                string baseUrl = string.Format(AssetServerProConfig.LoginWeb, port);
                string url = AssetServerProConfig.GetHttpFullUrl(m_AssetServerProxy.DevelopServerType, baseUrl);
                Application.OpenURL(url);
            }

            /**
            * @brief Create Organization the developer website
            */
            public void OpenDeveloperAppWebsite(string targetUrl)
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                string url = AssetServerProConfig.GetHttpFullUrl(m_AssetServerProxy.DevelopServerType,
                    targetUrl);
                Application.OpenURL(url);
            }
            
            /**
            * @brief Request user information.
            */
            public CommonHttpRequest SvrAppInfo()
            {
                if (m_AssetServerProxy == null)
                {
                    m_AssetServerProxy = new AssetServerProxy();
                }
                
                return m_AssetServerProxy.GetLoginAppInfo();;
            }

            public bool CheckShowLoginWindow()
            {
                if (true)
                    return true;
                var loginData = AssetImportManager.instance.assetImportDeveloperData;
                if (loginData == null || !loginData.HasBindAppID)
                    return true;
                return false;
            }

#endregion


#region Get some asset list

            /// <summary>
            /// get asset(skeleton、animset、commponent) detail with name asynchronously.
            /// </summary>
            /// <param name="repo_id"></param>
            /// <returns></returns>
            public CommonHttpRequest GetAssetDetailInfo(string repo_id)
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.GetAssetDetailInfo(repo_id);
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
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }
                m_AssetServerProxy.StartDownloadFile(url, fileName, success, progress, failure);
            }

            /// <summary>
            /// Get asset category list by skeleton_id asynchronously.
            /// </summary>
            /// <param name="skeleton_id"></param>
            /// <param name="need_paging"></param>
            /// <returns></returns>
            public CommonHttpRequest GetCategoryListBySkeleton(string skeleton_id, int need_paging)
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.GetCategoryListBySkeleton(skeleton_id, need_paging);
            }

            /// <summary>
            /// Get asset AnimationSet list by skeleton_id asynchronously.
            /// </summary>
            /// <param name="skeleton_id"></param>
            /// <param name="need_paging"></param>
            /// <returns></returns>
            public CommonHttpRequest GetBaseAnimationSetListByApp(string skeletonId, string assetStatus = "", string searchWords = "")
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.GetBaseAnimationSetListByApp(skeletonId, assetStatus, searchWords);
            }
            
            /// <summary>
            /// Get character custom AnimationSet list by character_id asynchronously.
            /// </summary>
            /// <param name="character_id"></param>
            /// <param name="asset_status"></param>
            /// /// <param name="search_words"></param>
            /// <returns></returns>
            public CommonHttpRequest GetCustomAnimationSetListByCharacter(string characterId, string assetStatus = "", string searchWords = "")
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.GetCustomAnimationSetListByCharacter(characterId, assetStatus, searchWords);
            }

            /// <summary>
            /// Get asset Skeleton list asynchronously.
            /// </summary>
            /// <param name="need_paging"></param>
            /// <returns></returns>
            public CommonHttpRequest GetSkeletonListByApp(string assetStatus = "", string searchWords = "")
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.GetSkeletonListByApp(assetStatus, searchWords);
            }
            
            public CommonHttpRequest GetCharacterListByApp(string assetStatus = "", string searchWords = "")
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.GetCharacterListByApp(assetStatus, searchWords);
            }
            
            public CommonHttpRequest CheckAssetName(string name, long itemType)
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.CheckAssetName(name, itemType);
            }
            
            public CommonHttpRequest CreateNewCharacter(string json)
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.CreateNewCharacter(json);
            }
            
            public CommonHttpRequest UpdateCharacter(string json)
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.UpdateCharacter(json);
            }
            
            public CommonHttpRequest GetAssetCategory(string character_id)
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.GetAssetCategory(character_id);
            }
            
            public CommonHttpRequest GetComponentAssetList(string json)
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.GetComponentAssetList(json);
            }
            
            public CommonHttpRequest GetAnimationSetAssetList(string characterId, string assetStatus = "", string searchWords = "")
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.GetAnimaitonSetAssetList(characterId, assetStatus, searchWords);
            }
            
            public CommonHttpRequest GetPresetList(string characterId, string assetStatus = "", string searchWords = "")
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.GetPresetList(characterId, assetStatus, searchWords);
            }
            
            public CommonHttpRequest CreateComponentAsset(string json)
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.CreateComponentAsset(json);
            }
            
            public CommonHttpRequest UpdateComponentAsset(string json)
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.UpdateComponentAsset(json);
            }
            
            public CommonHttpRequest CreateCustomAnimation(string json)
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.CreateCustomAnimation(json);
            }
            
            public CommonHttpRequest UpdateCustomAnimation(string json)
            {
                if (m_AssetServerProxy == null)
                {
                    ConnectServer();
                }

                return m_AssetServerProxy.UpdateCustomAnimation(json);
            }

            /// <summary>
            /// 检测本地文件版本
            /// </summary>
            /// <param name="md5">最新md5</param>
            /// <returns>本地文件是否是最新版本</returns>
            public bool CheckLocalFileVersion(string file, string md5)
            {
                if (string.IsNullOrEmpty(md5))
                    return false;
                
                try
                {
                    var dir = Path.GetDirectoryName(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(fileName))
                        return false;
                    
                    var versionPath = $"{dir}/version_{fileName}.json";
                    if (!File.Exists(versionPath))
                        return false;
                    
                    string versionStr = File.ReadAllText(versionPath);
                    if (string.IsNullOrEmpty(versionStr))
                        return false;

                    var fileVersion = JsonUtility.FromJson<FileVersion>(versionStr);
                    if (fileVersion == null || fileVersion.md5 != md5)
                        return false;

                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"CheckLocalFileVersion failed : {e.Message}\n{e.StackTrace}");
                    return false;
                }
            }

            /// <summary>
            /// 创建文件对应的version文件
            /// </summary>
            /// <param name="file"></param>
            /// <param name="md5"></param>
            /// <returns></returns>
            public bool CreateLocalFileVersion(string file, string md5)
            {
                try
                {
                    var dir = Path.GetDirectoryName(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(fileName))
                        return false;
                    
                    var versionPath = $"{dir}/version_{fileName}.json";
                    FileVersion version = new FileVersion();
                    version.md5 = md5;
                    File.WriteAllText(versionPath, JsonUtility.ToJson(version));
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"CheckLocalFileVersion failed : {e.Message}\n{e.StackTrace}");
                    return false;
                }
            }

#endregion
        }

        [Serializable]
        public class FileVersion
        {
            public string md5;
        }
    }
}
#endif