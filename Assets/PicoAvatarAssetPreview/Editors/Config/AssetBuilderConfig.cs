#if UNITY_EDITOR
using UnityEngine;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        /**
         * @breif The class object provides global configuration about the asset builder plugin.
         */
        public class AssetBuilderConfig
        {
            // singleton instance of the class.
            public static AssetBuilderConfig instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = new AssetBuilderConfig();
                        _instance.Initialize();
                    }
                    return _instance;
                }
            }

            // config data asset object.
            public PaabAssetBuilderConfigData configData { get => _configData; }

            // ui data path from  "Assets"
            public string uiDataAssetsPath { get => "Assets/PicoAvatarAssetPreview/Editors/Views/"; }

            public string uiDataStorePath { get => "Assets/PicoAvatarAssetPreview/Models/"; }

            public string uiDataIconPath { get => "Assets/PicoAvatarAssetPreview/Assets/Icon"; }

            public string uiDataEditorConfigPath { get => "Assets/PicoAvatarAssetPreview/Editors/Config/"; }

            public string uiDataAssetUssPath { get => "Assets/PicoAvatarAssetPreview/Assets/Uss/"; }

            // data path for asset viwer. default as "Assets/PicoAvatarAssetPreview/Models"
            public string assetViwerDataAssetsPath
            {
                get
                {
                    if (string.IsNullOrEmpty(_assetViwerDataAssetsPath))
                    {
                        _assetViwerDataAssetsPath = PaabAssetBuilderConfigData.GetStringPreference("assetViwerDataAssetsPath");
                        if (string.IsNullOrEmpty(_assetViwerDataAssetsPath))
                        {
                            _assetViwerDataAssetsPath = "Assets/PicoAvatarAssetPreview/Models/";
                        }
                    }
                    return _assetViwerDataAssetsPath;
                }
                set
                {
                    _assetViwerDataAssetsPath = value.Replace(Application.dataPath, "Assets/");
                    PaabAssetBuilderConfigData.SetStringPreference("assetViwerDataAssetsPath", _assetViwerDataAssetsPath);
                }
            }

            // data path for asset viewer. default as "Assets/PicoAvatarAssetPreview/Scenes/AssetViewer/AssetViewer.unity"
            public string assetViewerScenePath
            {
                get
                {
                    if (string.IsNullOrEmpty(_configData.assetViewerScenePath))
                    {
                        _configData.assetViewerScenePath = PaabAssetBuilderConfigData.GetStringPreference("assetViwerScenePath");
                        if (string.IsNullOrEmpty(_configData.assetViewerScenePath))
                        {
                            _configData.assetViewerScenePath = "Assets/PicoAvatarAssetPreview/Preview/Scenes/AssetPreviewV3.unity";
                        }
                    }
                    return _configData.assetViewerScenePath;
                }
                set
                {
                    _configData.assetViewerScenePath = value.Replace(Application.dataPath, "Assets/");
                    PaabAssetBuilderConfigData.SetStringPreference("assetViwerScenePath", _configData.assetViewerScenePath);
                }
            }


#region Public Methods

            /**
             * @brief Set preference item with key type of string.
             * @param key key in UnityEngine.PlayerPrefs.will be prefixed with "_PAV_BUILDER_".
             */
            public void SetStringPreference(string key, string value)
            {
                PaabAssetBuilderConfigData.SetStringPreference(key, value);
            }

            /**
             * @brief Gets string prefernce.
             * @param key key in UnityEngine.PlayerPrefs.will be prefixed with "_PAV_BUILDER_".
             */
            public string GetStringPreference(string key)
            {
                return PaabAssetBuilderConfigData.GetStringPreference(key);
            }

            /**
             * @brief Set preference item with key type of integer.
             * @param key key in UnityEngine.PlayerPrefs.will be prefixed with "_PAV_BUILDER_".
             */
            public void SetIntPreference(string key, int value)
            {
                PaabAssetBuilderConfigData.SetIntPreference(key, value);
            }
            /**
             * @brief Gets int prefernce.
             * @param key key in UnityEngine.PlayerPrefs.will be prefixed with "_PAV_BUILDER_". 
             */
            public int GetIntPreference(string key)
            {
                return PaabAssetBuilderConfigData.GetIntPreference(key);
            }
#endregion


#region Private Fields

            // singleton instance.
            private static AssetBuilderConfig _instance = null;

            // viewer data path.
            private string _assetViwerDataAssetsPath = null;

            // config data asset object.
            private PaabAssetBuilderConfigData _configData = null;

#endregion


#region Private Methods

            /**
             * @brief Initialize the object. check create asset data.
             */
            private void Initialize()
            {
                if (_configData == null)
                {
                    _configData = Utils.LoadOrCreateAsset<PaabAssetBuilderConfigData>(assetViwerDataAssetsPath + "Data/PaabAssetBuilderConfigData.asset");
                }
            }

#endregion
        }
    }
}

#endif