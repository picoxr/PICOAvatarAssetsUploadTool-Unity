#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using UnityEditor.EditorTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        /**
         * @brief Manager of AssetImportConfig.
         */
        public class AssetImportManager
        {
#region Public Properties
            //paab save local data from  "Assets" path
            public const string paabAssetDataAssetsPath = "Assets/PicoAvatarAssetBuilder/Editors/Config/";


            // Singleton instance.
            public static AssetImportManager instance
            {
                get
                {
                    if(_instance == null)
                    {
                        _instance = new AssetImportManager();
                    }
                    return _instance;
                }
            }

            // import settings. MUST NOT modify the dictionary outside the class.
            public Dictionary<string, PaabAssetImportSettings> assetImportSettings { get => _assetImportSettings; }

            //developer info
            public PaabDeveloperInfoImportSetting assetImportDeveloperData
            {
                get
                {
                    if (_assetImportDeveloperData == null)
                        _assetImportDeveloperData = LoadNativeSetting<PaabDeveloperInfoImportSetting>();
                    return _assetImportDeveloperData;
                }
                set  => _assetImportDeveloperData = value;
            }
#endregion


#region Public Methods

            /**
             * @brief Gets import config by asset name.
             * @param assetName name of asset to find.
             */
            public PaabAssetImportSettings GetAssetImportSettings(string assetName)
            {
                PaabAssetImportSettings importConfig;
                if(_assetImportSettings.TryGetValue(assetName, out importConfig))
                {
                    return importConfig;
                }
                return null;
            }

            /**
             * @brief add asset import settings.
             */ 
            public bool AddAssetImportSettings(PaabAssetImportSettings settings)
            {
                //
                if(_assetImportSettings.ContainsKey(settings.assetName))
                {
                    return false;
                }
                _assetImportSettings.Add(settings.assetName, settings);
                return true;
            }

            /**
             * @brief remove asset import settings.
             */ 
            public void AddAssetImportSettings(string assetName)
            {
                _assetImportSettings.Remove(assetName);
            }

            /**
             * @brief Helper method to make an asset import settings.
             */ 
            public PaabAssetImportSettings MakeSkeletonSettings()
            {
                var settings = ScriptableObject.CreateInstance<PaabAssetImportSettings>();
                settings.basicInfoSetting = new PaabBasicInfoImportSetting();// ScriptableObject.CreateInstance<PaabBasicInfoImportSetting>();
                settings.basicInfoSetting.SetAssetName("skeleton");
                settings.basicInfoSetting.assetNickName = "skeleton nick name";
                settings.settingItems = new PaabAssetImportSetting[] {
                    new PaabSkeletonImportSetting()//ScriptableObject.CreateInstance<PaabSkeletonImportSetting>()
                };
                return settings;
            }

            public PaabAssetImportSettings MakeAnimationSettings()
            {
                var settings = ScriptableObject.CreateInstance<PaabAssetImportSettings>();
                settings.basicInfoSetting = new PaabBasicInfoImportSetting();
                settings.basicInfoSetting.SetAssetName("Animation");
                settings.basicInfoSetting.assetNickName = "animation nick name";

                var animationImportSetting = new PaabAnimationImportSetting();
                settings.opType = OperationType.Update;
                settings.settingItems = new PaabAssetImportSetting[] {
                    animationImportSetting
                };
                return settings;
            }

#endregion

#region Load native settings
#if UNITY_EDITOR
            public T LoadNativeSetting<T>(string folderPath = null) where T: ScriptableObject
            {
                folderPath = string.IsNullOrEmpty(folderPath) ? paabAssetDataAssetsPath : folderPath;
                string filePath = Path.Combine(folderPath, typeof(T).Name + ".asset");
                var LoginAsset = AssetDatabase.LoadAssetAtPath<T>(filePath);
                if (LoginAsset == null)
                {
                    return null;
                }
                return LoginAsset;
            }
            public void SaveAssetImportSetting<T>(T classdata, string folderPath = null) where T : ScriptableObject
            {
                folderPath = string.IsNullOrEmpty(folderPath) ? paabAssetDataAssetsPath : folderPath;
                string filePath = Path.Combine(folderPath, classdata.GetType().Name + ".asset");
                if (File.Exists(filePath))
                {
                    EditorUtility.SetDirty(classdata);
                    AssetDatabase.SaveAssetIfDirty(classdata);
                    return;
                }

                string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(filePath);

                AssetDatabase.CreateAsset(classdata, assetPathAndName);
                AssetDatabase.SaveAssetIfDirty(classdata);
            }
#endif
#endregion


#region Private Fields

            private static AssetImportManager _instance = null;
            //
            Dictionary<string, PaabAssetImportSettings> _assetImportSettings = new Dictionary<string, PaabAssetImportSettings>();

            private PaabDeveloperInfoImportSetting _assetImportDeveloperData; 
#endregion


#region Private Methods


#endregion
        }
    }
}
#endif