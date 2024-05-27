#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Pico.Avatar;
using Pico.AvatarAssetPreview.Protocol;
using UnityEditor.Searcher;
using UnityEngine;
using FileInfo = Pico.AvatarAssetPreview.Protocol.FileInfo;

namespace Pico.AvatarAssetPreview
{
    public struct AssetDirInfo
    {
        public CharacterBaseAssetType cbat;

        public PaabComponentImportSetting.ComponentType componentType;

        public bool isBasicAnim;

        public AssetDirInfo(CharacterBaseAssetType cbat)
        {
            this.cbat = cbat;
            componentType = PaabComponentImportSetting.ComponentType.Invalid;
            isBasicAnim = false;
        }
        
        public AssetDirInfo(CharacterBaseAssetType cbat, PaabComponentImportSetting.ComponentType componentType)
        {
            this.cbat = cbat;
            this.componentType = componentType;
            isBasicAnim = false;
        }
        
        public AssetDirInfo(CharacterBaseAssetType cbat, bool isBasicAnim)
        {
            this.cbat = cbat;
            componentType = PaabComponentImportSetting.ComponentType.Invalid;
            this.isBasicAnim = isBasicAnim;
        }
    }
    
    public class CharacterUtil
    {
        public const string SkeletonFolderName = "Skeleton";
        public const string AnimationSetFolderName = "AnimationSet";
        public const string BaseBodyFolderName = "BaseBody";
        public const string ComponentFolderName = "Component";

        public const string Lod0ZipName = "0.zip";
        public const string Lod1ZipName = "1.zip";
        public const string Lod2ZipName = "2.zip";

        public const string Lod0ConfigName = "0.config.json";

        public const string NewCharacterTempName = "PAABTempCharactor";
        public const string LocalAvatarCachePath = "LocalAvatarCache";

        public const string BasicAnimationSetFolderName = "Base";
        public const string CustomAnimationSetFolderName = "Custom";

        public const string SpecJsonFileName = "spec.json";

        public const string Official_1_0_MalePrefabPath = "PavOfficial_1_0_MalePrefab";
        public const string Official_1_0_FemalePrefabPath = "PavOfficial_1_0_FemalePrefab";

        public static string CharacterFolderPath => CharacterManager.instance.GetCurrentCharacterPath();

        public static string GetCharacterPathByName(string name)
        {
            return $"{AvatarEnv.cacheSpacePath}/{LocalAvatarCachePath}/{name}";
        }

        public static string GetLodFileName(int lod)
        {
            switch (lod)
            {
                case 0:
                    return Lod0ZipName;
                case 1:
                    return Lod1ZipName;
                case 2:
                    return Lod2ZipName;
            }

            throw new Exception("Invalid lod");
        }
        
        public static void RenameTempCharacterFolderName(string name)
        {
            try
            {
                if (!Directory.Exists(CharacterFolderPath))
                {
                    Debug.LogError($"Can not find character folder : {CharacterFolderPath}");
                    return;
                }
                DirectoryInfo di = new DirectoryInfo(CharacterFolderPath);
                string newPath = Path.Combine(di.Parent.FullName, name);
                if (Directory.Exists(newPath))
                {
                    Debug.LogError($"Remove directory {newPath}");
                    Directory.Delete(newPath);
                }
                Directory.Move(CharacterFolderPath, newPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Rename character folder failed{e.Message}\n{e.StackTrace}");
            }
        }
        
        public static bool CreateCharacterSpecJsonFile(string characterName, string content)
        {
            return true;
            try
            {
                var characterFolderPath = GetCharacterPathByName(characterName);
                if (!Directory.Exists(characterFolderPath))
                    return false;

                var specFilePath = Path.Combine(characterFolderPath, SpecJsonFileName);
                if (File.Exists(specFilePath))
                    File.Delete(specFilePath);

                // 是否要异步 
                File.WriteAllText(specFilePath, content);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Create specJsonFile failed : {e.Message} \n {e.StackTrace}");
                return false;
            }
        }

        public static bool CreateCharacterFolder(string name, bool forceRecreate = true)
        {
            try
            {
                string path = $"{AvatarEnv.cacheSpacePath}/{LocalAvatarCachePath}/{name}";
                if (Directory.Exists(path))
                {
                    if (forceRecreate)
                        Directory.Delete(path, true);
                    else
                        return true;
                }
                
                Directory.CreateDirectory(path);
                // 创建子目录
                Directory.CreateDirectory($"{path}/{SkeletonFolderName}/");
                Directory.CreateDirectory($"{path}/{AnimationSetFolderName}/");
                Directory.CreateDirectory($"{path}/{ComponentFolderName}/");
                Directory.CreateDirectory($"{path}/{BaseBodyFolderName}/");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Create character [{name}] folder failed : {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
        
        public static string LoadAssetConfig(AssetDirInfo assetDirInfo, string assetName)
        {
            string path = GetAssetFullPath(assetDirInfo, assetName, CharacterUtil.Lod0ConfigName);
            if (string.IsNullOrEmpty(path))
                return "";

            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"Load config.json at path [{path}] failed : {e.Message} \n {e.StackTrace}");
                return "";
            }
        }
        
        public static string GetAssetFullPath(AssetDirInfo assetDirInfo, string assetName, string fileName)
        {
            string path = "";
            switch (assetDirInfo.cbat)
            {
                case CharacterBaseAssetType.Skeleton:
                    path =
                        $"{CharacterFolderPath}/{SkeletonFolderName}/{assetName}/{fileName}";
                    break;
                    
                case CharacterBaseAssetType.AnimationSet:
                    path =
                        $"{CharacterFolderPath}/{AnimationSetFolderName}/{(assetDirInfo.isBasicAnim ? BasicAnimationSetFolderName : CustomAnimationSetFolderName)}/{assetName}/{fileName}";
                    break;
                    
                case CharacterBaseAssetType.BaseBody:
                    path =
                        $"{CharacterFolderPath}/{BaseBodyFolderName}/{assetName}/{fileName}";
                    break;
                
                case CharacterBaseAssetType.Component:
                    path =
                        $"{CharacterFolderPath}/{ComponentFolderName}/{assetDirInfo.componentType.ToString()}/{assetName}/{fileName}";
                    break;
            }

            return path;
        }
        
        public static string GetAssetLocalPathWithLod(AssetDirInfo assetDirInfo, string name, int lod)
        {
            return GetAssetFullPath(assetDirInfo, name, CharacterUtil.GetLodFileName(lod));
        }
        
        public static string GetAssetIconLocalPath(AssetDirInfo assetDirInfo, string name)
        {
            return GetAssetFullPath(assetDirInfo, name, $"{name}.png");
        }

        public static NewAssetData CreateNewAssetUploadData(PaabAssetImportSettings settings)
        {
            UpdateAssetData updateAssetData = CreateUpdateAssetUploadData(settings);
            if (updateAssetData == null)
                return null;

            NewAssetData data = new NewAssetData();
            data.name = settings.assetName;
            data.cover = updateAssetData.cover;
            data.show_name = updateAssetData.show_name;
            data.files = updateAssetData.files;
            data.offline_config = updateAssetData.offline_config;

            return data;
        }

        public static UpdateAssetData CreateUpdateAssetUploadData(PaabAssetImportSettings settings)
        {
            UpdateAssetData data = null;
            CharacterBaseAssetType cbat;
            if (settings.assetImportSettingsType == AssetImportSettingsType.Skeleton || settings.assetImportSettingsType == AssetImportSettingsType.AnimationSet)
            {
                bool isBasicAnimationSet = false;
                if (settings.assetImportSettingsType == AssetImportSettingsType.Skeleton)
                    cbat = CharacterBaseAssetType.Skeleton;
                else
                {
                    cbat = CharacterBaseAssetType.AnimationSet;
                    var animationSetImportSetting = settings.GetImportSetting<PaabAnimationImportSetting>(false);
                    isBasicAnimationSet = animationSetImportSetting.isBasicAnimationSet;
                }
                
                var assetConfig = LoadAssetConfig(new AssetDirInfo(cbat, isBasicAnimationSet), settings.assetName);
                if (string.IsNullOrEmpty(assetConfig))
                {
                    Debug.LogError("[CreateNewAssetUploadData] config is null");
                    return null;
                }
                
                var cover = UIUtils.SaveTextureToTmpPathWithAspect(settings.basicInfoSetting.assetIconPath, settings.basicInfoSetting.assetName, UIUtils.DefaultAssetIconAspect);
                if (string.IsNullOrEmpty(cover))
                {
                    Debug.LogError("[CreateNewAssetUploadData] save cover failed");
                    return null;
                }
                
                data = new UpdateAssetData();
                data.cover = cover;
                data.show_name = settings.basicInfoSetting.assetNickName;
                data.files = new List<FileInfo>();
                data.offline_config = assetConfig;

                FileInfo lod0 = new FileInfo();
                lod0.lod = 0;
                lod0.url = GetAssetFullPath(new AssetDirInfo(cbat, isBasicAnimationSet), settings.assetName, GetLodFileName(0));
                data.files.Add(lod0);
            }
            else if (settings.assetImportSettingsType == AssetImportSettingsType.BaseBody || settings.assetImportSettingsType == AssetImportSettingsType.Component)
            {
                var componentImportSetting = settings.GetImportSetting<PaabComponentImportSetting>(false);
                if (componentImportSetting == null)
                    return null;
                
                if (componentImportSetting.componentType == PaabComponentImportSetting.ComponentType.BaseBody)
                    cbat = CharacterBaseAssetType.BaseBody;
                else
                    cbat = CharacterBaseAssetType.Component;
                
                
                var assetConfig = LoadAssetConfig(new AssetDirInfo(cbat, componentImportSetting.componentType), settings.assetName);
                if (string.IsNullOrEmpty(assetConfig))
                {
                    Debug.LogError("[CreateNewAssetUploadData] component config is null");
                    return null;
                }
                
                var cover = UIUtils.SaveTextureToTmpPathWithAspect(settings.basicInfoSetting.assetIconPath, settings.basicInfoSetting.assetName, UIUtils.DefaultAssetIconAspect);
                if (string.IsNullOrEmpty(cover))
                {
                    Debug.LogError("[CreateNewAssetUploadData] save component cover failed");
                    return null;
                }
                
                data = new UpdateAssetData();
                data.cover = cover;
                data.show_name = settings.basicInfoSetting.assetNickName;
                data.files = new List<FileInfo>();
                data.offline_config = assetConfig;

                if (componentImportSetting.lods == null || componentImportSetting.lods.Length == 0)
                {
                    Debug.LogError("[GenCreateCharacterData] Basebody lods is null");
                    return null;
                }
                        
                for (int j = 0; j < componentImportSetting.lods.Length; j++)
                {
                    FileInfo lodFile = new FileInfo();
                    lodFile.lod = componentImportSetting.lods[j].lod;
                    lodFile.url = GetAssetFullPath(new AssetDirInfo(cbat, componentImportSetting.componentType), settings.assetName, CharacterUtil.GetLodFileName(componentImportSetting.lods[j].lod));
                    data.files.Add(lodFile);
                }
            }

            return data;
        }

        public static bool DownloadAsset(AssetDirInfo assetDirInfo, AssetInfo assetInfo)
        {
            if (assetInfo == null)
            {
                Debug.LogError($"Asset is null");
                return false;
            }
            
            if (assetInfo.files == null || assetInfo.files.Count == 0)
            {
                Debug.LogError($"[{assetInfo.name}] asset file count is zero");
                return false;
            }

            bool success = true;
            for (int i = 0; i < assetInfo.files.Count; i++)
            {
                string assetPath = GetAssetLocalPathWithLod(assetDirInfo, assetInfo.name, assetInfo.files[i].lod);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogError($"Get asset [{assetInfo.name}] local path failed");
                    return false;
                }
                
                if (!AssetServerManager.instance.CheckLocalFileVersion(assetPath, assetInfo.files[i].md5))
                {
                    var downloadAssetInfo = assetInfo;
                    // 这是个同步接口
                    AssetServerManager.instance.StartDownloadFile(downloadAssetInfo.files[i].url, assetPath,
                        url =>
                        {
                            Debug.Log($"Download [{url}] success");
                            AssetServerManager.instance.CreateLocalFileVersion(assetPath, downloadAssetInfo.files[i].md5);
                            
                        }, 
                        null, 
                        (s, s1) =>
                        {
                            success = false;
                            Debug.LogError($"Download asset [{assetInfo.name}] file [{assetInfo.files[i].name}] failed : {s} \n {s1}");
                        });
                }
            }

            return success;
        }

        public static bool DownloadAssetIcon(AssetDirInfo assetDirInfo, AssetInfo assetInfo)
        {
            if (assetInfo == null)
            {
                Debug.LogError($"Asset is null");
                return false;
            }
            
            string coverPath = CharacterUtil.GetAssetIconLocalPath(assetDirInfo, assetInfo.name);
            if (string.IsNullOrEmpty(coverPath))
            {
                Debug.LogError($"Get asset icon [{assetInfo.name}] local path failed");
                return false;
            }

            bool success = true;
            // 这是个同步接口
            AssetServerManager.instance.StartDownloadFile(assetInfo.cover, coverPath, null, null, (s, s1) =>
            {
                success = false;
                Debug.LogError($"Download asset [{assetInfo.name}] icon [{assetInfo.cover}] failed : {s} \n {s1}");
            });

            return success;
        }
    }
}
#endif