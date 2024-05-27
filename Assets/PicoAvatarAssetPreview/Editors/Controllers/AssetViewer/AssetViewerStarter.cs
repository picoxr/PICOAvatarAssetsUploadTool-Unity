#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        /**
         * @brief the class start an asset viewer.
         */ 
        public class AssetViewerStarter
        {
            /**
             * @brief start asset viewer, without param. 
             */
            public static void StartAssetViewer()
            {
                PreEnterPreview();

                if (EditorApplication.isPlaying)
                {
                    SceneManager.LoadSceneAsync("AssetPreviewV3");
                }
                else
                {
                    EditorSceneManager.OpenScene(AssetBuilderConfig.instance.assetViewerScenePath);
                    EditorApplication.EnterPlaymode();
                }
            }
            
            /**
             * @brief start asset viewer. Invoked in edit mode.
             */ 
            public static void StartAssetViewer(PaabAssetImportSettings assetSettings)
            {
                PreEnterPreview();

                //1. save the assetSettings to local CurrentPreviewAsset.asset
                var assetImportSettingPath = AssetDatabase.GetAssetPath(assetSettings);
                if (string.IsNullOrEmpty(assetImportSettingPath))
                {
                    throw new System.Exception("assetSettings need saved to any file as asset.");
                    //Utils.ReCreateAssetAt(assetSettings, AssetBuilderConfig.instance.assetViwerDataAssetsPath + "CurrentPreviewAsset.asset");
                }

                // 2. save start data. "Assets/PicoAvatarAssetPreview/Models/Data/AssetViewerStartData.asset"
                var viewerStartData = Utils.LoadOrCreateAsset<PaabAssetViewerStartData>(AssetBuilderConfig.instance.assetViwerDataAssetsPath + "Data/PaabAssetViewerStartData.asset");
                viewerStartData.assetImportSettings = assetSettings;

                // 3. save exit data
                var viewerExitData = Utils.LoadOrCreateAsset<PaabAssetViewerExitData>(AssetBuilderConfig.instance.assetViwerDataAssetsPath + "Data/PaabAssetViewerExitData.asset");

                // 4. set start/exist data to config data.
                AssetBuilderConfig.instance.configData.viewerStartData = viewerStartData;
                AssetBuilderConfig.instance.configData.viewerExitData = viewerExitData;
                AssetBuilderConfig.instance.configData.assetViwerDataAssetsPath = AssetBuilderConfig.instance.assetViwerDataAssetsPath;
                
                // 5. save related asset files.
                EditorUtility.SetDirty(viewerStartData);
                AssetDatabase.SaveAssetIfDirty(viewerStartData);
                EditorUtility.SetDirty(viewerExitData);
                AssetDatabase.SaveAssetIfDirty(viewerExitData);
                EditorUtility.SetDirty(AssetBuilderConfig.instance.configData);
                AssetDatabase.SaveAssetIfDirty(AssetBuilderConfig.instance.configData);

                // 6. save all
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 7. start enter viewer in playing mode. "Assets/PicoAvatarAssetPreview/Scenes/AssetViewer/AssetViewer.unity"
                EditorSceneManager.OpenScene(AssetBuilderConfig.instance.assetViewerScenePath);
                EditorApplication.isPlaying = true;
            }

            private static string sceneCacheDir { get => "Assets/AssetViewerSceneCache"; }
            private static string savedSceneInfoPath { get => Application.dataPath + "/../" + sceneCacheDir + "/SavedScene.txt"; }

            private static JArray GamesObjectsToJArray(GameObject[] objs)
            {
                if (objs != null)
                {
                    var objArray = new JArray();
                    List<GameObject> rootGameObjects = null;
                    for (int i = 0; i < objs.Length; ++i)
                    {
                        var obj = objs[i];
                        var objInfo = new JObject();
                        if (obj != null)
                        {
                            if (rootGameObjects == null)
                            {
                                rootGameObjects = new List<GameObject>();
                                rootGameObjects.AddRange(obj.transform.root.gameObject.scene.GetRootGameObjects());
                            }
                            int rootIndex = rootGameObjects.IndexOf(obj.transform.root.gameObject);
                            var path = AnimationUtility.CalculateTransformPath(obj.transform, obj.transform.root);
                            objInfo["rootIndex"] = rootIndex;
                            objInfo["path"] = path;
                        }
                        else
                        {
                            objInfo["rootIndex"] = -1;
                            objInfo["path"] = "";
                        }
                        objArray.Add(objInfo);
                    }
                    return objArray;
                }
                return null;
            }

            private static GameObject[] JArrayToGamesObjects(Scene scene, JArray objArray)
            {
                if (objArray != null)
                {
                    List<GameObject> objs = new List<GameObject>();
                    var rootGameObjects = scene.GetRootGameObjects();
                    for (int i = 0; i < objArray.Count; ++i)
                    {
                        var objInfo = objArray[i];
                        int rootIndex = (int) objInfo["rootIndex"];
                        string path = (string) objInfo["path"];
                        if (rootIndex >= 0)
                        {
                            var obj = rootGameObjects[rootIndex].transform.Find(path).gameObject;
                            objs.Add(obj);
                        }
                        else
                        {
                            objs.Add(null);
                        }
                    }
                    return objs.ToArray();
                }
                return null;
            }

            private static void PreEnterPreview()
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

                var savedSceneInfo = new JObject();

                bool needSave = false;
                var objs = GamesObjectsToJArray(ConfigureComponentPanel.instance.OnPreEnterPreview());
                if (objs != null)
                {
                    savedSceneInfo["configureComponentPanelGameObjects"] = objs;
                    needSave = true;
                }

                objs = GamesObjectsToJArray(SkeletonPanel.instance.OnPreEnterPreview());
                if (objs != null)
                {
                    savedSceneInfo["skeletonPanelGameObjects"] = objs;
                    needSave = true;
                }

                // save scene
                if (needSave)
                {
                    int sceneCount = EditorSceneManager.sceneCount;
                    if (sceneCount > 0)
                    {
                        var dir = new FileInfo(savedSceneInfoPath).DirectoryName;
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        var scene = EditorSceneManager.GetSceneAt(0);
                        string sceneName = System.Guid.NewGuid().ToString().ToLower().Replace("-", "");
                        EditorSceneManager.SaveScene(scene, sceneCacheDir + "/" + sceneName + ".unity", true);

                        savedSceneInfo["sceneName"] = sceneName;

                        File.WriteAllText(savedSceneInfoPath, savedSceneInfo.ToString());
                    }
                }
            }

            private static void OnPlayModeStateChanged(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    if (File.Exists(savedSceneInfoPath))
                    {
                        string savedSceneInfoText = File.ReadAllText(savedSceneInfoPath);
                        var savedSceneInfo = JObject.Parse(savedSceneInfoText);
                        var sceneName = (string) savedSceneInfo["sceneName"];
                        var scene = EditorSceneManager.OpenScene(sceneCacheDir + "/" + sceneName + ".unity", OpenSceneMode.Single);
                        File.Delete(savedSceneInfoPath);

                        if (savedSceneInfo.ContainsKey("configureComponentPanelGameObjects"))
                        {
                            var configureComponentPanelGameObjects = (JArray) savedSceneInfo["configureComponentPanelGameObjects"];
                            GameObject[] objs = JArrayToGamesObjects(scene, configureComponentPanelGameObjects);
                            ConfigureComponentPanel.instance.OnPostExitPreview(objs);
                        }

                        if (savedSceneInfo.ContainsKey("skeletonPanelGameObjects"))
                        {
                            var skeletonPanelGameObjects = (JArray) savedSceneInfo["skeletonPanelGameObjects"];
                            GameObject[] objs = JArrayToGamesObjects(scene, skeletonPanelGameObjects);
                            SkeletonPanel.instance.OnPostExitPreview(objs);
                        }
                    }
                    EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                }
            }
        }
    }
}
#endif