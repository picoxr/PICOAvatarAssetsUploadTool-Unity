#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Pico.Avatar;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class AvatarConverter
        {
            public static string converterRootDir { get => "Assets/PicoAvatarAssetBuilder/Editors/Controllers/Converter"; }
            public static string builtinSkeletonPath { get => Application.dataPath + "/PicoAvatarAssetBuilder/Assets/Resources/PavOfficial_1_0_MaleSkeleton.zip"; }
            private static string builtinAnimPath { get => Application.dataPath + "/PicoAvatarAssetBuilder/Assets/Resources/PavOfficial_1_0_MaleAnim.zip"; }
            private static string tempAnimatorControllerPath { get => AssetBuilderConfig.instance.uiDataStorePath + "Data/TempAnimatorController.asset"; }

            public static bool ConvertSkeleton(string skeletonName, GameObject skeletonRoot, string prefabZipPath, string extrasJson, System.Action finishCallback)
            {
                GameObject newRoot = Object.Instantiate(skeletonRoot);
                newRoot.name = skeletonRoot.name;
                RemoveComponents(newRoot);
                newRoot = AddSkinnedCube(newRoot);
                return ConvertGameObjectToZip(newRoot, null, null, skeletonName, prefabZipPath, extrasJson, finishCallback);
            }

            public static bool ConvertAnimationSet(GameObject skeletonRoot, AnimationClip[] clips, string[] clipNames,
                                                   string[] toRetargetClipNames, string skeletonPath,
                                                   string prefabZipPath, System.Action finishCallback)
            {
                var zipFiles = new List<string>();

                // anim dir
                string outDir = new FileInfo(prefabZipPath).DirectoryName;
                string animazDir = outDir + "/anim";
                if (Directory.Exists(animazDir))
                {
                    Directory.Delete(animazDir, true);
                }
                Directory.CreateDirectory(animazDir);

                // retarget
                AnimationRetargeter.RetargetAnimations(toRetargetClipNames, builtinSkeletonPath, builtinAnimPath, skeletonPath, animazDir);

                // animaz
                AnimationConverter.ConvertClipsToAnimaz(skeletonRoot.transform, clips, clipNames, animazDir);
                
                // config
                var animations = new JObject();
                for (int i = 0; i < clipNames.Length; ++i)
                {
                    if (clipNames[i] != null)
                    {
                        var name = clipNames[i];
                        string animazName = name + ".animaz";
                        string animazPath = animazDir + "/" + animazName;
                        if (File.Exists(animazPath))
                        {
                            animations[name] = "anim/" + animazName;
                            zipFiles.Add(animazPath);
                        }
                        else
                        {
                            if (clips[i])
                            {
                                Debug.LogErrorFormat("clip {0} {1} convert failed", i, clipNames[i]);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogErrorFormat("clipName {0} is null", i);
                    }
                }
                var config = new JObject();
                config["animations"] = animations;
                config["md5"] = System.Guid.NewGuid().ToString().Replace("-", "");
                config["version"] = "1.0";
                string configJson = config.ToString();
                string configPath = outDir + "/config.json";
                File.WriteAllText(configPath, configJson);
                zipFiles.Add(configPath);

                // zip
                ZipUtility.Zip(zipFiles.ToArray(), prefabZipPath, null, null, outDir);

                // clean
                Directory.Delete(animazDir, true);

                finishCallback();

                return true;
            }

            public static bool ConvertAvatarComponent(string componentName, GameObject skeletonRoot, SkinnedMeshRenderer[] renderers, Material[] officialMaterials , Material[] customMaterials, string prefabZipPath, string extrasJson, System.Action finishCallback)
            {
                GameObject newRoot = CombineToSameTree(skeletonRoot, renderers);
                return ConvertGameObjectToZip(newRoot, officialMaterials, customMaterials, componentName, prefabZipPath, extrasJson, finishCallback);
            }

            public static GameObject LoadSkeleton(string prefabZipPath)
            {
                var handle = pav_Serialization_LoadSkeleton(prefabZipPath);
                var count = pav_Serialization_GetSkeletonBoneCount(handle);
                if (count == 0)
                {
                    throw new UnityException("load skeleton failed: " + prefabZipPath);
                }

                System.IntPtr[] names = new System.IntPtr[count];
                int[] parentIndices = new int[count];
                Avatar.XForm[] xforms = new Avatar.XForm[count];

                var namesHandle = GCHandle.Alloc(names, GCHandleType.Pinned);
                var parentIndicesHandle = GCHandle.Alloc(parentIndices, GCHandleType.Pinned);
                var xformsHandle = GCHandle.Alloc(xforms, GCHandleType.Pinned);

                pav_Serialization_GetSkeletonBones(handle, namesHandle.AddrOfPinnedObject(), parentIndicesHandle.AddrOfPinnedObject(), xformsHandle.AddrOfPinnedObject());
                pav_Serialization_ReleaseSkeleton(handle);

                namesHandle.Free();
                parentIndicesHandle.Free();
                xformsHandle.Free();

                GameObject[] objs = new GameObject[count];
                for (int i = 0; i < count; ++i)
                {
                    var name = Marshal.PtrToStringUTF8(names[i]);
                    objs[i] = new GameObject(name);
                }

                GameObject root = null;
                for (int i = 0; i < count; ++i)
                {
                    var obj = objs[i];
                    var transform = obj.transform;

                    if (parentIndices[i] < 0)
                    {
                        root = obj;
                    }
                    else
                    {
                        transform.parent = objs[parentIndices[i]].transform;
                    }


                    Vector3 position = xforms[i].position;
                    position.x *= -1;

                    Quaternion orientation = xforms[i].orientation;
                    orientation.y *= -1;
                    orientation.z *= -1;

                    transform.localPosition = position;
                    transform.localRotation = orientation;
                    transform.localScale = xforms[i].scale;
                }

                return root;
            }

            private static bool ConvertGameObjectToZip(GameObject root, Material[] officialMaterials, Material[] customMaterials, string name, string prefabZipPath, string extrasJson, System.Action finishCallback)
            {
                string gltfTempDir;
                string prefabTempDir;
                CreateTempDirs(prefabZipPath, out gltfTempDir, out prefabTempDir);

                AvatarCustomMaterialDataBase.instance.Load();// 这里存储自定义材质信息相关的内容
                AvatarGLTFExporter.ExportConfig config = new AvatarGLTFExporter.ExportConfig();
                config.flipTexCoordY = true;
                config.leftToRightHandSpace = true;
                config.flipFrontFace = true;
                var gltfPath = gltfTempDir + "/" + name + ".gltf";
                bool success = AvatarGLTFExporter.ExportGameObjectToGLTF(root, officialMaterials, customMaterials, gltfPath, config);
                if (success)
                {
                    AvatarCustomMaterialDataBase.instance.Save();
                    AssetDatabase.Refresh();

                    //UnityEditor.EditorUtility.OpenWithDefaultApp(gltfPath);
                    UnityEditor.EditorUtility.DisplayProgressBar("Avatar Converter", "Working, please wait...", 0.0f);
                    ConvertGLTFToPrefab.Convert(gltfPath, prefabTempDir, extrasJson, () => {
                        //CleanTempDirs(gltfTempDir, prefabTempDir);
                        var prefabTempZip = prefabTempDir + ".zip";
                        File.Move(prefabTempZip, prefabZipPath);
                        MessageCenter.instance.PostMessage(() => {
                            UnityEditor.EditorUtility.ClearProgressBar();
                            if (finishCallback != null)
                            {
                                finishCallback.Invoke();
                            }
                        });
                    });
                }
                Object.DestroyImmediate(root);
                return success;
            }

            private static void CreateTempDirs(string prefabZipPath, out string gltfTempDir, out string prefabTempDir)
            {
                if (File.Exists(prefabZipPath))
                {
                    File.Delete(prefabZipPath);
                }
                var prefabDir = new FileInfo(prefabZipPath).DirectoryName;
                if (Directory.Exists(prefabDir))
                {
                    Directory.Delete(prefabDir, true);
                }
                Directory.CreateDirectory(prefabDir);
                gltfTempDir = prefabDir + "/gltf.temp";
                if (Directory.Exists(gltfTempDir))
                {
                    Directory.Delete(gltfTempDir, true);
                }
                Directory.CreateDirectory(gltfTempDir);
                prefabTempDir = prefabDir + "/prefab.temp";
                if (Directory.Exists(prefabTempDir))
                {
                    Directory.Delete(prefabTempDir, true);
                }
                Directory.CreateDirectory(prefabTempDir);
            }

            private static void CleanTempDirs(string gltfTempDir, string prefabTempDir)
            {
                Directory.Delete(gltfTempDir, true);
                Directory.Delete(prefabTempDir, true);
            }

            private static GameObject CombineToSameTree(GameObject skeletonRoot, SkinnedMeshRenderer[] renderers)
            {
                var newRoot = Object.Instantiate(skeletonRoot);
                newRoot.name = skeletonRoot.name;
                RemoveComponents(newRoot);

                newRoot.transform.localPosition = Vector3.zero;
                newRoot.transform.localRotation = Quaternion.identity;
                newRoot.transform.localScale = Vector3.one;

                // remove disabled node in skeleton
                var allNodes = newRoot.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < allNodes.Length; ++i)
                {
                    var node = allNodes[i].gameObject;
                    if (!node.activeInHierarchy)
                    {
                        Object.DestroyImmediate(node);
                    }
                }

                // instantiate renderer obj to new root
                for (int i = 0; i < renderers.Length; ++i)
                {
                    var renderer = Object.Instantiate(renderers[i]);
                    var children = new List<Transform>();
                    for(int j = 0; j < renderer.transform.childCount; ++j)
                    {
                        children.Add(renderer.transform.GetChild(j));
                    }
                    foreach(var child in children)
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                    renderer.gameObject.name = renderers[i].name;
                    renderer.transform.parent = newRoot.transform;
                    renderer.transform.localPosition = Vector3.zero;
                    renderer.transform.localRotation = Quaternion.identity;
                    renderer.transform.localScale = Vector3.one;
                    ReplaceBones(renderer, newRoot);
                }

                RemoveAnimations(newRoot);

                return newRoot;
            }

            // replace skin bones with new skeleton
            private static void ReplaceBones(SkinnedMeshRenderer renderer, GameObject skeletonRoot)
            {
                var bones = renderer.bones;
                for (int i = 0; i < bones.Length; ++i)
                {
                    var bone = bones[i];
                    var newBone = AnimationConverter.FindTransformByName(skeletonRoot, bone.name);
                    if (newBone == null)
                    {
                        throw new UnityException(renderer.gameObject.name + " can not find bone " + bone.name + " in skeleton");
                    }
                    bones[i] = newBone;
                }
                renderer.bones = bones;

                var rootBone = renderer.rootBone;
                if (rootBone)
                {
                    var newBone = AnimationConverter.FindTransformByName(skeletonRoot, rootBone.name);
                    if (newBone == null)
                    {
                        throw new UnityException(renderer.gameObject.name + " can not find bone " + rootBone.name + " in skeleton");
                    }
                    renderer.rootBone = newBone;
                }
            }

            private static GameObject BuildGameObjectForAnimationSetExport(GameObject skeletonRoot, AnimationClip[] clips, string[] clipNames, out AnimationClip[] newClips)
            {
                if (clips.Length == 0)
                {
                    throw new UnityException("clips empty");
                }

                if (clipNames == null && clips.Length != clipNames.Length)
                {
                    throw new UnityException("clip names required");
                }

                GameObject newRoot = Object.Instantiate(skeletonRoot);
                newRoot.name = skeletonRoot.name;
                RemoveComponents(newRoot);
                newRoot = AddSkinnedCube(newRoot);

                bool[] legacy = null;
                newClips = new AnimationClip[clips.Length];
                for (int i = 0; i < clips.Length; ++i)
                {
                    if (clips[i] == null)
                    {
                        continue;
                    }
                    var clip = Object.Instantiate(clips[i]);
                    if (legacy == null)
                    {
                        legacy = new bool[1];
                        legacy[0] = clip.legacy;
                    }
                    else
                    {
                        if (legacy[0] != clip.legacy)
                        {
                            throw new UnityException("clips with different legacy mode");
                        }
                    }
                    clip.name = clipNames[i];
                    newClips[i] = clip;
                }
                if (legacy == null)
                {
                    throw new UnityException("clips empty");
                }

                // add clips into container
                if (legacy[0])
                {
                    var animation = newRoot.AddComponent<Animation>();
                    for (int i = 0; i < newClips.Length; ++i)
                    {
                        if (newClips[i])
                        {
                            animation.AddClip(newClips[i], newClips[i].name);
                        }
                    }
                }
                else
                {
                    var animator = newRoot.AddComponent<Animator>();
                    var editorController = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(tempAnimatorControllerPath);
                    for (int i = 0; i < newClips.Length; ++i)
                    {
                        if (newClips[i])
                        {
                            editorController.AddMotion(newClips[i]);
                        }
                    }
                    animator.runtimeAnimatorController = editorController;
                }

                return newRoot;
            }

            private static void RemoveAnimations(GameObject newRoot)
            {
                // remove clip containers if exist
                var animations = newRoot.GetComponentsInChildren<Animation>();
                for (int i = 0; i < animations.Length; ++i)
                {
                    Object.DestroyImmediate(animations[i]);
                }
                var animators = newRoot.GetComponentsInChildren<Animator>();
                for (int i = 0; i < animators.Length; ++i)
                {
                    Object.DestroyImmediate(animators[i]);
                }
            }

            private static void RemoveRenderers(GameObject newRoot)
            {
                // remove renderers if exist
                var renderers = newRoot.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < renderers.Length; ++i)
                {
                    Object.DestroyImmediate(renderers[i]);
                }
            }

            private static void RemoveComponents(GameObject newRoot)
            {
                RemoveAnimations(newRoot);
                RemoveRenderers(newRoot);
            }

            private static GameObject AddSkinnedCube(GameObject newRoot)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var mesh = Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);
                var material = Object.Instantiate(cube.GetComponent<MeshRenderer>().sharedMaterial);
                Object.DestroyImmediate(cube);

                var bone = newRoot.transform;

                var boneWeights = new BoneWeight[mesh.vertexCount];
                for (int i = 0; i < boneWeights.Length; ++i)
                {
                    var boneWeight = new BoneWeight();
                    boneWeight.boneIndex0 = 0;
                    boneWeight.boneIndex1 = 0;
                    boneWeight.boneIndex2 = 0;
                    boneWeight.boneIndex3 = 0;
                    boneWeight.weight0 = 1;
                    boneWeight.weight1 = 0;
                    boneWeight.weight2 = 0;
                    boneWeight.weight3 = 0;
                    boneWeights[i] = boneWeight;
                }
                mesh.boneWeights = boneWeights;

                var bindposes = new Matrix4x4[1];
                bindposes[0] = bone.worldToLocalMatrix;
                mesh.bindposes = bindposes;

                var bones = new Transform[1];
                bones[0] = bone;

                var cubeObj = new GameObject("ModelRoot");
                newRoot.transform.parent = cubeObj.transform;
                cubeObj.transform.localPosition = Vector3.zero;
                cubeObj.transform.localRotation = Quaternion.identity;
                cubeObj.transform.localScale = Vector3.one;

                var skin = cubeObj.AddComponent<SkinnedMeshRenderer>();
                skin.sharedMesh = mesh;
                skin.bones = bones;
                skin.rootBone = bone;
                skin.sharedMaterial = material;

                return cubeObj;
            }

            [DllImport("effect", CallingConvention = CallingConvention.Cdecl)]
            private static extern System.IntPtr pav_Serialization_LoadSkeleton(string prefabZipPath);
            [DllImport("effect", CallingConvention = CallingConvention.Cdecl)]
            private static extern int pav_Serialization_GetSkeletonBoneCount(System.IntPtr skeletonHandle);
            [DllImport("effect", CallingConvention = CallingConvention.Cdecl)]
            private static extern Avatar.NativeResult pav_Serialization_GetSkeletonBones(System.IntPtr skeletonHandle, System.IntPtr names, System.IntPtr parentIndices, System.IntPtr xforms);
            [DllImport("effect", CallingConvention = CallingConvention.Cdecl)]
            private static extern void pav_Serialization_ReleaseSkeleton(System.IntPtr skeletonHandle);
        }
    }
}

#endif