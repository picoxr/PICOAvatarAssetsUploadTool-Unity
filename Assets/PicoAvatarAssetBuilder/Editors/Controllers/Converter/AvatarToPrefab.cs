#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class AvatarToPrefab
        {
            [MenuItem("AvatarSDK/Prefab To GLTF")]
            public static void ExportGLTF()
            {
                if (Selection.activeGameObject == null)
                {
                    EditorUtility.DisplayDialog("Error", "Select a gameobject to save", "OK");
                    return;
                }

                var outDir = Application.dataPath + "/../OutGLTF";
                if (!Directory.Exists(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }
                AvatarGLTFExporter.ExportConfig config = new AvatarGLTFExporter.ExportConfig();
                config.flipTexCoordY = true;
                config.leftToRightHandSpace = true;
                config.flipFrontFace = true;
                bool success = AvatarGLTFExporter.ExportGameObjectToGLTF(Selection.activeGameObject, null, null, outDir + "/test.glb", config);
                if (success)
                {
                    EditorUtility.DisplayDialog("Success", "Export game object to gltf success", "OK");
                }
            }

            [MenuItem("AvatarSDK/Avatar To Prefab")]
            public static void Save()
            {
                if (Selection.activeGameObject == null)
                {
                    EditorUtility.DisplayDialog("Error", "Select an avatar lod gameobject to save", "OK");
                    return;
                }

                var lod = Selection.activeGameObject.GetComponent<Avatar.AvatarLod>();
                if (lod == null)
                {
                    EditorUtility.DisplayDialog("Error", "Select an avatar lod gameobject to save", "OK");
                    return;
                }

                var targetPath = EditorUtility.SaveFilePanelInProject("Avatar To Prefab", lod.name, "prefab", "Enter prefab file to save");
                if (string.IsNullOrEmpty(targetPath))
                {
                    return;
                }

                var assetDir = new FileInfo(targetPath).DirectoryName + "/" + lod.name;
                assetDir = assetDir.Replace("\\", "/");
                if (!Directory.Exists(assetDir))
                {
                    Directory.CreateDirectory(assetDir);
                }
                assetDir = "Assets/" + assetDir.Substring(Application.dataPath.Length + 1);

                var obj = Object.Instantiate(lod.gameObject);

                // remove coms
                {
                    Object.DestroyImmediate(obj.GetComponent<Avatar.AvatarLod>());
                    var coms = obj.GetComponentsInChildren<Avatar.PicoAvatarRenderMesh>();
                    foreach (var c in coms)
                    {
                        Object.DestroyImmediate(c);
                    }
                }

                var renderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();

                // save meshes
                {
                    var meshMap = new Dictionary<int, Mesh>();
                    foreach (var c in renderers)
                    {
                        var mesh = c.sharedMesh;
                        if (mesh)
                        {
                            var id = mesh.GetInstanceID();
                            if (!meshMap.ContainsKey(id))
                            {
                                mesh = Object.Instantiate(mesh);
                                mesh.name = c.name;
                                mesh.RecalculateBounds();
                                meshMap.Add(id, mesh);
                            }
                            c.sharedMesh = meshMap[id];
                        }
                    }
                    foreach (var meshKV in meshMap)
                    {
                        var mesh = meshKV.Value;
                        AssetDatabase.CreateAsset(mesh, assetDir + "/" + mesh.name + ".mesh.asset");
                    }
                }

                // instantiate materials
                var materialMap = new Dictionary<int, Material>();
                {
                    foreach (var c in renderers)
                    {
                        var mats = c.sharedMaterials;
                        for (int i = 0; i < mats.Length; ++i)
                        {
                            var mat = mats[i];
                            if (mat)
                            {
                                var id = mat.GetInstanceID();
                                if (!materialMap.ContainsKey(id))
                                {
                                    mat = Object.Instantiate(mat);
                                    mat.name = c.name + "_" + i;
                                    materialMap.Add(id, mat);
                                }
                                mats[i] = materialMap[id];
                            }
                        }
                        c.sharedMaterials = mats;
                    }
                }

                // save textures
                {
                    var textureMap = new Dictionary<int, Texture2D>();
                    foreach (var matKV in materialMap)
                    {
                        var mat = matKV.Value;
                        var shader = mat.shader;
                        int propertyCount = ShaderUtil.GetPropertyCount(shader);
                        for (int i = 0; i < propertyCount; ++i)
                        {
                            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv &&
                                ShaderUtil.GetTexDim(shader, i) == UnityEngine.Rendering.TextureDimension.Tex2D)
                            {
                                var texName = ShaderUtil.GetPropertyName(shader, i);
                                var tex = mat.GetTexture(texName) as Texture2D;
                                if (tex)
                                {
                                    var id = tex.GetInstanceID();
                                    if (!textureMap.ContainsKey(id))
                                    {
                                        tex = Object.Instantiate(tex);
                                        tex.name = texName + "_" + id;
                                        textureMap.Add(id, tex);
                                    }
                                    mat.SetTexture(texName, textureMap[id]);
                                }
                            }
                        }
                    }
                    foreach (var texKV in textureMap)
                    {
                        var tex = texKV.Value;
                        AssetDatabase.CreateAsset(tex, assetDir + "/" + tex.name + ".texture.asset");
                    }
                }

                // save materials
                foreach (var matKV in materialMap)
                {
                    var mat = matKV.Value;
                    AssetDatabase.CreateAsset(mat, assetDir + "/" + mat.name + ".material.asset");
                }

                bool success;
                PrefabUtility.SaveAsPrefabAsset(obj, targetPath, out success);
                if (success)
                {
                    EditorUtility.DisplayDialog("Success", "Save avatar to prefab asset success", "OK");
                }
                Object.DestroyImmediate(obj);
            }
        }
    }
}

#endif