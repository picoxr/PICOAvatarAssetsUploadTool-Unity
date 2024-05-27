#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;
using Pico.AvatarAssetBuilder.Protocol;
using UnityEditor;
using UnityEngine;
using File = UnityEngine.Windows.File;
using FileInfo = Pico.AvatarAssetBuilder.Protocol.FileInfo;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        /**
         * @brief Upload(new/update) asset manager
         */
        public class AssetUploadManager
        {
            private static AssetUploadManager _instance = null;
            private CharacterData _panelData;
            public NewUploadAssetData newUploadAsset { get; private set; }

            private Dictionary<UploadFileType, bool> _uploadDone = new();

            // Singleton instance.
            public static AssetUploadManager instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = new AssetUploadManager();
                    }

                    return _instance;
                }
            }

            [Serializable]
            public class NewUploadAssetData
            {
                public UploadAssetsPanel.SourceType sourceType;
                public string character_id;
                public CharacterBase character_base;
                public string skeleton_id;
                public string base_animation_set_id;
                public NewAssetData skeleton;
                public NewAssetData base_animation_set;
                public NewAssetData base_body;

                public string category_id;
                public string asset_id;
                public NewAssetData component_asset;
                public NewAssetData custom_animation_set_asset;

                public NewUploadAssetData()
                {
                }

                public NewUploadAssetData(CharacterData characterData)
                {
                    CopyFrom(characterData);
                }

                public void CopyFrom(CharacterData characterData)
                {
                    character_id = characterData.character_id;
                    character_base = characterData.character_base;
                    skeleton_id = characterData.skeleton_id;
                    base_animation_set_id = characterData.base_animation_set_id;
                    skeleton = characterData.skeleton;
                    base_animation_set = characterData.base_animation_set;
                    base_body = characterData.base_body;
                }

                public NewAssetData CopyFrom(PaabAssetImportSettings importSettings)
                {
                    character_id = importSettings.basicInfoSetting.characterId;
                    if (importSettings.assetImportSettingsType == AssetImportSettingsType.Component)
                    {
                        PaabComponentImportSetting componentImportSetting =
                            importSettings.GetImportSetting<PaabComponentImportSetting>(false);
                        category_id = ComponentListPanel.ParseComponentTypeToComponentKey(componentImportSetting.componentType);
                    }
                    else
                        category_id = null;

                    asset_id = importSettings.basicInfoSetting.assetId;
                    return CharacterUtil.CreateNewAssetUploadData(importSettings);
                }
            }

            /**
             *  资产上传涉及到以下4个接口
             *  TODO：
             *  1. 角色维度：
             *    【新建】：上传骨架、上传动画集、上传素体 ： /platform_api/v2/character/create
             *    【更新】：骨架、动画集、素体 ：  /platform_api/v2/character/update
             *
             *  2. 创建/更新 custom动画：  /platform_api/v2/asset/animation_set/create
             *  3. 创建/更新 部件：  /platform_api/v2/asset/create
             */
            /**
             * @brief upload asset file asynchronously.
             */
            public void UploadAssetFile(string repo_id, System.Action<PaabAssetImportSettings> callback)
            {
                // //获取本地文件
                // string outputDirectoy = AvatarEnv.cacheSpacePath + "/AvatarCacheLocal/" + "1111/";
                //
                // if (!System.IO.Directory.Exists(outputDirectoy))
                // {
                //     System.IO.Directory.CreateDirectory(outputDirectoy);
                // }
                // Debug.Log(outputDirectoy);

                string filepath = Application.streamingAssetsPath + "/2222.log.zip";
                Debug.Log("path= " + filepath);

                //upload zip or image, recieve the url
                var request = new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(AssetServerType.BOE_China))
                    .SetPath(AssetServerProConfig.POST_FILE_UPLOAD_URL)
                    .SetHttpMethod(HttpMethod.Post)
                    .SetContentType(ContentType.Multipart)
                    .SetUploadFilePath(filepath)
                    .Build();

                request.Send(success =>
                {
                    Debug.Log("upload...success= " + success.ToString());
                    callback.Invoke(null);
                }, failure => { Debug.Log("upload...success= " + failure.ToString()); });
            }

            /**
             *  资产上传涉及到以下4个接口
             *  TODO：
             *  1. 角色维度：
             *    【新建】：上传骨架、上传动画集、上传素体 ： /platform_api/v2/character/create
             *    【更新】：骨架、动画集、素体 ：  /platform_api/v2/character/update
             *
             *  2. 创建/更新 custom动画：  /platform_api/v2/asset/animation_set/create
             *  3. 创建/更新 部件：  /platform_api/v2/asset/create
             */
            /**
             * @brief upload asset file asynchronously.
             */
            public void CreateOrUpdateCharacter(CharacterData characterData, UploadAssetsPanel.SourceType sourceType)
            {
                newUploadAsset = new NewUploadAssetData(characterData);
                newUploadAsset.sourceType = sourceType;
                UpdateCharacterUploadStatus(newUploadAsset);
            }

            public void UpdateCharacterUploadStatus(NewUploadAssetData data)
            {
                foreach (UploadFileType type in Enum.GetValues(typeof(UploadFileType)))
                {
                    switch (type)
                    {
                        case UploadFileType.None:
                            break;
                        case UploadFileType.CharacterBaseCover:
                            _uploadDone[UploadFileType.CharacterBaseCover] = data?.character_base == null;
                            if (!_uploadDone[UploadFileType.CharacterBaseCover])
                                UploadSingleCoverRes(UploadFileType.CharacterBaseCover, data.character_base);
                            break;
                        case UploadFileType.BaseBody:
                            _uploadDone[UploadFileType.BaseBody] = data?.base_body?.files == null;
                            if (!_uploadDone[UploadFileType.BaseBody])
                                UploadSingleZipRes(UploadFileType.BaseBody, data.base_body.files,
                                    data.base_body.files.Count);
                            break;
                        case UploadFileType.BaseBodyCover:
                            _uploadDone[UploadFileType.BaseBodyCover] = data?.base_body == null;
                            if (!_uploadDone[UploadFileType.BaseBodyCover])
                                UploadSingleCoverRes(UploadFileType.BaseBodyCover, data.base_body);
                            break;
                        case UploadFileType.Skeleton:
                            _uploadDone[UploadFileType.Skeleton] = data?.skeleton?.files == null;
                            if (!_uploadDone[UploadFileType.Skeleton])
                                UploadSingleZipRes(UploadFileType.Skeleton, data.skeleton.files,
                                    data.skeleton.files.Count);
                            break;
                        case UploadFileType.SkeletonCover:
                            _uploadDone[UploadFileType.SkeletonCover] = data?.skeleton == null;
                            if (!_uploadDone[UploadFileType.SkeletonCover])
                                UploadSingleCoverRes(UploadFileType.SkeletonCover, data.skeleton);
                            break;
                        case UploadFileType.AnimationSet:
                            _uploadDone[UploadFileType.AnimationSet] = data?.base_animation_set?.files == null;
                            if (!_uploadDone[UploadFileType.AnimationSet])
                                UploadSingleZipRes(UploadFileType.AnimationSet, data.base_animation_set.files,
                                    data.base_animation_set.files.Count);
                            break;
                        case UploadFileType.AnimationSetCover:
                            _uploadDone[UploadFileType.AnimationSetCover] = data?.base_animation_set == null;
                            if (!_uploadDone[UploadFileType.AnimationSetCover])
                                UploadSingleCoverRes(UploadFileType.AnimationSetCover, data.base_animation_set);
                            break;
                        case UploadFileType.CharacterThumbnail:
                            break;
                        case UploadFileType.AnimationSetCustom:
                            _uploadDone[UploadFileType.AnimationSetCustom] =
                                data?.custom_animation_set_asset?.files == null;
                            if (!_uploadDone[UploadFileType.AnimationSetCustom])
                                UploadSingleZipRes(UploadFileType.AnimationSetCustom,
                                    data.custom_animation_set_asset.files,
                                    data.custom_animation_set_asset.files.Count);
                            break;
                        case UploadFileType.AnimationSetCustomCover:
                            _uploadDone[UploadFileType.AnimationSetCustomCover] =
                                data?.custom_animation_set_asset == null;
                            if (!_uploadDone[UploadFileType.AnimationSetCustomCover])
                                UploadSingleCoverRes(UploadFileType.AnimationSetCustomCover,
                                    data.custom_animation_set_asset);
                            break;
                        case UploadFileType.ComponentCustomSet:
                            _uploadDone[UploadFileType.ComponentCustomSet] = data?.component_asset?.files == null;
                            if (!_uploadDone[UploadFileType.ComponentCustomSet])
                                UploadSingleZipRes(UploadFileType.ComponentCustomSet, data.component_asset.files,
                                    data.component_asset.files.Count);
                            break;
                        case UploadFileType.ComponentCustomSetCover:
                            _uploadDone[UploadFileType.ComponentCustomSetCover] = data?.component_asset == null;
                            if (!_uploadDone[UploadFileType.ComponentCustomSetCover])
                                UploadSingleCoverRes(UploadFileType.ComponentCustomSetCover, data.component_asset);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                //final check,if no res upload
                UploadCompleted(UploadFileType.None);
            }

            public void UploadCompleted(UploadFileType type)
            {
                _uploadDone[type] = true;
                bool allDone = true;
                foreach (var flag in _uploadDone)
                {
                    allDone &= flag.Value;
                }

                if (allDone)
                {
                    AssetTestPanel.instance.CreateOrUpdateDoneCallback(newUploadAsset);
                }
            }


            private void UploadSingleCoverRes(UploadFileType fileType, object originData)
            {
                string filepath = String.Empty;
                if (fileType == UploadFileType.CharacterBaseCover)
                {
                    filepath = ((CharacterBase)originData).cover;
                }
                else
                {
                    filepath = ((NewAssetData)originData).cover;
                }

                Debug.Log($"@@@Upload:{filepath} Start~");
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                filepath = filepath?.Replace("file:///", "/");
#endif
                if (!string.IsNullOrEmpty(filepath) && File.Exists(filepath))
                {
                    //upload zip or image, recieve the url
                    var commonHttpRequest = new CommonHttpRequest.Builder()
                        .SetHostForEditor(AssetServerProConfig.GetDomainByServerType())
                        .SetPath(AssetServerProConfig.POST_FILE_UPLOAD_URL)
                        .SetHttpMethod(HttpMethod.Post)
                        .SetContentType(ContentType.Multipart)
                        .SetUploadFilePath(filepath)
                        .Build();

                    void UploadLoop()
                    {
                        if (commonHttpRequest?.CurrRequest?.isDone == false)
                        {
                            while (true)
                            {
                                if (EditorUtility.DisplayCancelableProgressBar("UploadCover", "Progress",
                                        commonHttpRequest.CurrRequest.uploadProgress)
                                    || commonHttpRequest.CurrRequest == null
                                    || commonHttpRequest.CurrRequest.isDone)
                                    break;
                                Thread.Sleep(1000);
                            }

                            EditorApplication.update -= UploadLoop;
                            EditorUtility.ClearProgressBar();
                        }
                    }

                    EditorApplication.update += UploadLoop;
                    commonHttpRequest.Send(null, success =>
                    {
                        //EditorApplication.update -= UploadLoop;

                        var result = JsonConvert.DeserializeObject<CommonUploadResponse>(success);
                        if (result != null)
                        {
                            if (fileType == UploadFileType.CharacterBaseCover)
                            {
                                ((CharacterBase)originData).cover = result.FileInfo.url;
                                Debug.Log($"@@@Upload:{((CharacterBase)originData).cover} Success~");
                            }
                            else
                            {
                                ((NewAssetData)originData).cover = result.FileInfo.url;
                                Debug.Log($"@@@Upload:{((NewAssetData)originData).cover} Success~");
                            }

                            if (commonHttpRequest.CurrRequest != null) commonHttpRequest.CurrRequest.Dispose();
                            commonHttpRequest.CurrRequest = null;
                            UploadCompleted(fileType);
                        }
                    }, failure =>
                    {
                        //EditorApplication.update -= UploadLoop;
                        if (commonHttpRequest.CurrRequest != null) commonHttpRequest.CurrRequest.Dispose();
                        commonHttpRequest.CurrRequest = null;
                        
                        if (NavMenuBarRoute.isValid)
                        {
                            var currPanel = MainMenuUIManager.instance.CurrentPanel;
                            if (currPanel != null && !currPanel.panelName.Equals(PanelType.UploadFailure.ToString()))
                            {
                                var failurePanel = NavMenuBarRoute.instance.RouteNextByPanelName(currPanel.panelName,
                                    PanelType.UploadFailure.ToString());
                                if (failurePanel)
                                {
                                    ((UploadFailurePanel)failurePanel).FillFailureContent(failure.ErrorServerText);
                                }
                            }
                        }
                        
                        Debug.Log("@@@@@@@@@@@@@upload...failed= " + failure.ToString());
                    });
                }
                else
                {
                    _uploadDone[fileType] = true;
                }
            }

            private void UploadSingleZipRes(UploadFileType fileType, List<FileInfo> assetsName, int Count)
            {
                if (Count == 0)
                {
                    UploadCompleted(fileType);
                    return;
                }

                string uploadName = $"Upload {fileType}";
                var data = assetsName[Count - 1];
                string filepath = data.url;
                Debug.Log($"@@@Upload:{filepath} Start~");
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                filepath = filepath?.Replace("file:///", "/");
#endif
                if (!string.IsNullOrEmpty(filepath) && File.Exists(filepath))
                {
                    //upload zip or image, recieve the url
                    var commonHttpRequest = new CommonHttpRequest.Builder()
                        .SetHostForEditor(AssetServerProConfig.GetDomainByServerType())
                        .SetPath(AssetServerProConfig.POST_FILE_UPLOAD_URL)
                        .SetHttpMethod(HttpMethod.Post)
                        .SetContentType(ContentType.Multipart)
                        .SetUploadFilePath(filepath)
                        .Build();

                    void UploadLoop()
                    {
                        if (commonHttpRequest?.CurrRequest?.isDone == false)
                        {
                            while (true)
                            {
                                if (EditorUtility.DisplayCancelableProgressBar(uploadName, "Progress",
                                        commonHttpRequest.CurrRequest.uploadProgress)
                                    || commonHttpRequest.CurrRequest == null
                                    || commonHttpRequest.CurrRequest.isDone)
                                    break;
                                Thread.Sleep(1000);
                            }

                            EditorApplication.update -= UploadLoop;
                            EditorUtility.ClearProgressBar();
                        }
                    }

                    EditorApplication.update += UploadLoop;
                    commonHttpRequest.Send(null, success =>
                    {
                        //EditorApplication.update -= UploadLoop;

                        var result = JsonConvert.DeserializeObject<CommonUploadResponse>(success);
                        if (result != null)
                        {
                            Debug.Log("@@@@@@@@@@@@@@@upload...success= " + result.FileInfo.url);
                            data.url = result.FileInfo.url;
                            data.key = result.FileInfo.key;
                            data.md5 = result.FileInfo.md5;
                            data.name = result.FileInfo.name;
                            data.file_type = result.FileInfo.file_type;
                            data.lod = result.FileInfo.lod;
                        }

                        if (commonHttpRequest.CurrRequest != null) commonHttpRequest.CurrRequest.Dispose();
                        commonHttpRequest.CurrRequest = null;
                        UploadSingleZipRes(fileType, assetsName, Count - 1);
                    }, failure =>
                    {
                        //EditorApplication.update -= UploadLoop;
                        if (commonHttpRequest.CurrRequest != null) commonHttpRequest.CurrRequest.Dispose();
                        commonHttpRequest.CurrRequest = null;
                        if (NavMenuBarRoute.isValid)
                        {
                            var currPanel = MainMenuUIManager.instance.CurrentPanel;
                            if (currPanel != null && !currPanel.panelName.Equals(PanelType.UploadFailure.ToString()))
                            {
                                var failurePanel = NavMenuBarRoute.instance.RouteNextByPanelName(currPanel.panelName,
                                    PanelType.UploadFailure.ToString());
                                if (failurePanel)
                                {
                                    ((UploadFailurePanel)failurePanel).FillFailureContent(failure.ErrorServerText);
                                }
                            }
                        }
                        Debug.Log("@@@@@@@@@@@@@upload...failed= " + failure.ToString());
                    });
                }
                else
                {
                    _uploadDone[fileType] = true;
                }
            }

            public void CreateOrUpdateCharacterPart(PaabAssetImportSettings setting,
                UploadAssetsPanel.SourceType sourceType)
            {
                newUploadAsset = new NewUploadAssetData();
                newUploadAsset.character_id = setting.basicInfoSetting.characterId;
                newUploadAsset.sourceType = sourceType;
                newUploadAsset.asset_id = setting.basicInfoSetting.assetId;
                if (setting.assetImportSettingsType == AssetImportSettingsType.Component)
                {
                    PaabComponentImportSetting componentImportSetting =
                        setting.GetImportSetting<PaabComponentImportSetting>(false);
                    newUploadAsset.category_id =
                        ComponentListPanel.ParseComponentTypeToComponentKey(componentImportSetting.componentType);
                    newUploadAsset.component_asset = CharacterUtil.CreateNewAssetUploadData(setting);
                }
                else
                {
                    newUploadAsset.category_id = null;
                    newUploadAsset.custom_animation_set_asset = CharacterUtil.CreateNewAssetUploadData(setting);
                }

                UpdateCharacterUploadStatus(newUploadAsset);
            }

            /**
            * @brief create or update asset(skeleton) asynchronously.
            */
            public void CreateOrUpdateSkeleton(string assetName)
            {
                var request = new CommonHttpRequest.Builder()
                    .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(AssetServerType.China))
                    .SetPath("/api/v1/avatar/editor/tag_list")
                    .SetHttpMethod(HttpMethod.Get)
                    .AddQueryParam("gender", 1)
                    .AddQueryParam("version_name", "1.0.1")
                    .AddHeader("auth",
                        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHBpcmVJbiI6IjE2NjUzMDUxMDYiLCJzaWciOiJsaWdodHBhY2thZ2UiLCJ0b2tlblR5cGUiOiIwIiwidXNlcklEIjoiMTM4MDYyNTIxMDcyMjY2MDM1MiIsInZlcnNpb24iOiIwLjAuMSJ9.UICsngQqhE6n_Uul-bC3XLZYnaxE48tBH4ax9qeniFQ")
                    .AddHeader("locale", "zh-Hans-CN")
                    .Build();

                request.Send(success => { Debug.Log("CreateOrUpdateSkeleton...success= " + success.ToString()); },
                    failure => { Debug.Log("CreateOrUpdateSkeleton...success= " + failure.ToString()); });
            }

            // /**
            // * @brief create or update asset(animationset) asynchronously.
            // */
            // public CommonHttpRequest CreateOrUpdateAnimSet(string assetName)
            // {
            //     return new CommonHttpRequest.Builder()
            //         .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(AssetServerType.BOE_China))
            //         .SetPath(AssetServerProConfig.POST_ANIMATIONSET_CREATE_URL)
            //         .SetHttpMethod(HttpMethod.Post)
            //         .Build();
            // }
            //
            // /**
            // * @brief create or update asset(commponent) asynchronously.
            // */
            // public CommonHttpRequest CreateOrUpdateCommponet(string assetName)
            // {
            //     return new CommonHttpRequest.Builder()
            //         .SetHostForEditor(AssetServerProConfig.GetDomainByServerType(AssetServerType.BOE_China))
            //         .SetPath(AssetServerProConfig.POST_ASSET_CREATE_URL)
            //         .SetHttpMethod(HttpMethod.Post)
            //         .Build();
            // }
        }
    }
}
#endif