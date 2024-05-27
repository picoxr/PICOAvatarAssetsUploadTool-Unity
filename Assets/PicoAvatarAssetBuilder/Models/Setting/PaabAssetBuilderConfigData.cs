#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        [Serializable]
        public class PaabAssetBuilderConfigData : ScriptableObject
        {
            //
            public const string PreferenceKeyPrefix = "_PAV_BUILDER_";

            // viewer data path.
            [SerializeField]
            public string assetViwerDataAssetsPath;

            // viewer scene path.
            [SerializeField]
            public string assetViewerScenePath;

            [SerializeField]
            public PaabAssetViewerStartData viewerStartData;

            [SerializeField]
            public PaabAssetViewerExitData viewerExitData;

            [SerializeField]
            public ScriptableObject lastPanelOfAssetImportWindow;

            /**
             * @brief Set preference item with key type of string.
             * @param key key in UnityEngine.PlayerPrefs.will be prefixed with "_PAV_BUILDER_".
             */
            public static void SetStringPreference(string key, string value)
            {
                PlayerPrefs.SetString(PreferenceKeyPrefix + key, value);
            }

            /**
             * @brief Gets string prefernce.
             * @param key key in UnityEngine.PlayerPrefs.will be prefixed with "_PAV_BUILDER_".
             */
            public static string GetStringPreference(string key)
            {
                return PlayerPrefs.GetString(PreferenceKeyPrefix + key);
            }

            /**
             * @brief Set preference item with key type of integer.
             * @param key key in UnityEngine.PlayerPrefs.will be prefixed with "_PAV_BUILDER_".
             */
            public static void SetIntPreference(string key, int value)
            {
                PlayerPrefs.SetInt(PreferenceKeyPrefix + key, value);
            }
            /**
             * @brief Gets int prefernce.
             * @param key key in UnityEngine.PlayerPrefs.will be prefixed with "_PAV_BUILDER_".
             */
            public static int GetIntPreference(string key)
            {
                return PlayerPrefs.GetInt(PreferenceKeyPrefix + key);
            }
        }
    }
}
#endif