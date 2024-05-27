#if UNITY_EDITOR
using Pico.Avatar;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class Utils
        {
            /**
             * @brief Delete asset and create new asset file.
             */ 
            public static void ReCreateAssetAt(UnityEngine.Object obj, string assetPathName)
            {
                if (obj == null)
                {
                    Debug.LogError("ReCreateAssetAt : obj is null");
                    return;
                }
                if(AssetDatabase.LoadAssetAtPath<PaabAssetImportSettings>(assetPathName) != null)
                {
                    AssetDatabase.DeleteAsset(assetPathName);
                }
                if (MainMenuUIManager.instance.PAAB_OPEN == true)
                {
                    AssetDatabase.CreateAsset(obj, assetPathName);
                }
            }


            public static void CheckDirectory(string assetPath)
            {
                string dirPath;
                // check path root.
                if (assetPath.LastIndexOf(".") > assetPath.LastIndexOf("/"))
                {
                    dirPath = assetPath.Substring(0, assetPath.LastIndexOf("/"));
                }
                else
                {
                    dirPath = assetPath;
                }

                if (!System.IO.Directory.Exists(dirPath))
                {
                    int startIndex = assetPath.IndexOf(Application.dataPath);
                    if (startIndex == 0)
                    {
                        dirPath = dirPath.Replace(Application.dataPath, "Assets");
                    }

                    var dirNames = dirPath.Split('/');
                    string parentPathName = "Assets";
                    for (int i = 1; i < dirNames.Length; ++i)
                    {
                        string newPathName = parentPathName + "/" + dirNames[i];
                        if (!System.IO.Directory.Exists(newPathName))
                        {
                            UnityEditor.AssetDatabase.CreateFolder(parentPathName, dirNames[i]);
                        }

                        parentPathName = newPathName;
                    }
                }
            }

            /**
             * @brief Load asset at asset path. if failed, create new one and save to the path.
             */
            public static T LoadOrCreateAsset<T>(string assetPathName) where T : ScriptableObject
            {
                var assetObj = AssetDatabase.LoadAssetAtPath<T>(assetPathName);
                if (assetObj == null)
                {
                    assetObj = ScriptableObject.CreateInstance<T>();
                    CheckDirectory(assetPathName);
                    AssetDatabase.CreateAsset(assetObj, assetPathName);
                    EditorUtility.SetDirty(assetObj);
                    AssetDatabase.SaveAssetIfDirty(assetObj);
                }

                return assetObj;
            }
        }
    }
}
#endif