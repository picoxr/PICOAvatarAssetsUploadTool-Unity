#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Pico.Avatar;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class AvatarGLTFExporter
        {
            public class ExportConfig
            {
                public bool flipTexCoordY = false;
                public bool leftToRightHandSpace = false;
                public bool leftToRightHandSpaceRule2 = true; // used for effect sdk conversion compatible
                public bool flipFrontFace = false;
                public bool mapMetallicTexChannels = false;
            }

            class PrimitiveMesh
            {
                public JObject attributes = new JObject();
                public List<int> indices = new List<int>();
                public JArray targets = null;
                public JObject extras = null;
            }
            
            public static string ShaderThemeString = "_ShaderTheme";

            class Context
            {
                public string dir;
                public bool glb = false;
                public ExportConfig config = new ExportConfig();
                public JObject gltf = new JObject();
                public JArray meshes = new JArray();
                public JArray skins = new JArray();
                public JArray accessors = new JArray();
                public JArray bufferViews = new JArray();
                public JArray materials = new JArray();
                public JArray textures = new JArray();
                public JArray samplers = new JArray();
                public JArray images = new JArray();
                public JArray animations = new JArray();
                public BinaryWriter streamWriter = new BinaryWriter(new MemoryStream());
                public byte[] bufferPadding = new byte[64]; // max padding size is sizeof(Matrix4x4) = 64
                public Dictionary<int, int> objIndices = new Dictionary<int, int>();
                public Dictionary<int, PrimitiveMesh> primitiveMeshes = new Dictionary<int, PrimitiveMesh>();
                public Dictionary<ulong , int> matIndices = new Dictionary<ulong, int>();
                public Dictionary<int, int> texIndices = new Dictionary<int, int>();
            }


            delegate void Definiation_glTF_Callback(Context context, JObject material, Material umaterial);

            public static bool ExportGameObjectToGLTF(GameObject obj, Material[] officialMaterials, Material[] customMaterials, string gltfPath, ExportConfig config = null)
            {
                if (!gltfPath.EndsWith(".gltf") && !gltfPath.EndsWith(".glb"))
                {
                    return false;
                }

                var context = new Context();
                context.dir = new FileInfo(gltfPath).DirectoryName;
                context.glb = gltfPath.EndsWith(".glb");
                if (config != null)
                {
                    context.config = config;
                }

                ExportAsset(context);
                ExportNodes(context, obj, officialMaterials, customMaterials);
                ExportScene(context, obj);
                ExportAnimations(context, obj);
                if (context.meshes.Count > 0)
                {
                    context.gltf["meshes"] = context.meshes;
                }
                if (context.skins.Count > 0)
                {
                    context.gltf["skins"] = context.skins;
                }
                if (context.accessors.Count > 0)
                {
                    context.gltf["accessors"] = context.accessors;
                }
                if (context.bufferViews.Count > 0)
                {
                    context.gltf["bufferViews"] = context.bufferViews;
                }
                if (context.materials.Count > 0)
                {
                    context.gltf["materials"] = context.materials;
                }
                if (context.textures.Count > 0)
                {
                    context.gltf["textures"] = context.textures;
                }
                if (context.samplers.Count > 0)
                {
                    context.gltf["samplers"] = context.samplers;
                }
                if (context.images.Count > 0)
                {
                    context.gltf["images"] = context.images;
                }
                if (context.animations.Count > 0)
                {
                    context.gltf["animations"] = context.animations;
                }

                if (context.glb)
                {
                    WriteGlb(gltfPath, context);
                }
                else
                {
                    WriteBuffer(context);
                    File.WriteAllText(gltfPath, context.gltf.ToString());
                }

                context.streamWriter.BaseStream.Dispose();
                context.streamWriter.Dispose();

                return true;
            }

            private static void ExportAsset(Context context)
            {
                var asset = new JObject();
                asset["version"] = "2.0";
                asset["generator"] = "Unity GLTF Exporter";
                asset["copyright"] = "Pico Avatar GLTF Exporter";
                context.gltf["asset"] = asset;
            }

            private static void ExportNodes(Context context, GameObject root, Material[] officialMaterials, Material[] customMaterials)
            {
                var objArray = new List<GameObject>();
                AddObj(objArray, context.objIndices, root);

                var nodes = new JArray();
                int materialIndex = 0;
                for (int i = 0; i < objArray.Count; ++i)
                {
                    var obj = objArray[i];
                    var meshRenderer = obj.GetComponent<MeshRenderer>();
                    var skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
                    Mesh mesh = null;
                    Renderer renderer = null;

                    var transform = obj.transform;
                    var childCount = transform.childCount;
                    var node = new JObject();
                    if (childCount > 0)
                    {
                        var children = new JArray();
                        for (int j = 0; j < childCount; ++j)
                        {
                            var child = transform.GetChild(j);
                            children.Add(context.objIndices[child.gameObject.GetInstanceID()]);
                        }
                        node["children"] = children;
                    }
                    if (obj.activeInHierarchy && meshRenderer && meshRenderer.enabled)
                    {
                        mesh = obj.GetComponent<MeshFilter>().sharedMesh;
                        renderer = meshRenderer;
                    }
                    if (obj.activeInHierarchy && skinnedMeshRenderer && skinnedMeshRenderer.enabled)
                    {
                        mesh = skinnedMeshRenderer.sharedMesh;
                        renderer = skinnedMeshRenderer;
                        if (skinnedMeshRenderer.bones.Length > 0)
                        {
                            node["skin"] = AddSkin(context, mesh, skinnedMeshRenderer);
                        }
                    }
                    if (mesh)
                    {
                        Material officialMaterial = officialMaterials != null ? officialMaterials[materialIndex] : null;
                        Material customMaterial = customMaterials != null ? customMaterials[materialIndex] : null;

                        node["mesh"] = AddMesh(context, mesh, renderer, officialMaterial, customMaterial);

                        ++materialIndex;
                    }
                    var localPosition = transform.localPosition;
                    var localRotation = transform.localRotation;
                    var localScale = transform.localScale;
                    if (context.config.leftToRightHandSpace)
                    {
                        if (context.config.leftToRightHandSpaceRule2)
                        {
                            localPosition.x *= -1.0f;
                            localRotation.y *= -1.0f;
                            localRotation.z *= -1.0f;
                        }
                        else
                        {
                            Matrix4x4 matrix = Matrix4x4.TRS(localPosition, localRotation, localScale);
                            matrix = MatrixLeftToRightSpace(matrix);
                            localPosition = matrix.GetColumn(3);
                            localRotation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
                            localScale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);
                        }
                    }
                    node["translation"] = new JArray(localPosition.x, localPosition.y, localPosition.z);
                    node["rotation"] = new JArray(localRotation.x, localRotation.y, localRotation.z, localRotation.w);
                    node["scale"] = new JArray(localScale.x, localScale.y, localScale.z);
                    node["name"] = mesh ? "mesh_" + obj.name : obj.name;
                    nodes.Add(node);
                }
                context.gltf["nodes"] = nodes;
            }

            private static void ExportScene(Context context, GameObject root)
            {
                var nodes = new JArray();
                nodes.Add(0);
                var scene = new JObject();
                scene["name"] = root.name;
                scene["nodes"] = nodes;
                var scenes = new JArray();
                scenes.Add(scene);
                context.gltf["scenes"] = scenes;
                context.gltf["scene"] = 0;
            }

            private static void ExportAnimations(Context context, GameObject root)
            {
                var clips = new Dictionary<int, AnimationClip>();
                var animations = root.GetComponentsInChildren<Animation>();
                for (int i = 0; i < animations.Length; ++i)
                {
                    var animation = animations[i];
                    foreach (AnimationState state in animation)
                    {
                        var clip = state.clip;
                        if (clip)
                        {
                            if (!clips.ContainsKey(clip.GetInstanceID()))
                            {
                                clips.Add(clip.GetInstanceID(), clip);
                                AddClip(context, clip, animation.transform);
                            }
                        }
                    }
                }

                var animators = root.GetComponentsInChildren<Animator>();
                for (int i = 0; i < animators.Length; ++i)
                {
                    var animator = animators[i];
                    var controller = animator.runtimeAnimatorController;
                    if (controller)
                    {
                        var animationClips = controller.animationClips;
                        for (int j = 0; j < animationClips.Length; ++j)
                        {
                            var clip = animationClips[j];
                            if (clip)
                            {
                                if (!clips.ContainsKey(clip.GetInstanceID()))
                                {
                                    clips.Add(clip.GetInstanceID(), clip);
                                    AddClip(context, clip, animator.transform);
                                }
                            }
                        }
                    }
                }
            }

            enum ChannelPath
            {
                None,
                Translation,
                Rotation,
                Scale,
                Weights,
                EulerAnglesRaw,
            }

            class ClipCurve
            {
                public AnimationCurve[] curves;
                public List<AnimationCurve> blendShapeCurves;
                public string path;
                public ChannelPath channePath;
                public GameObject target;
                public EditorCurveBinding binding;
                public float[] blendShapeFrameWeights;
            }

            private static void AddClip(Context context, AnimationClip clip, Transform target)
            {
                var clipName = clip.name;
                clip = Object.Instantiate(clip);
                clip.name = clipName;
                var curveMap = new Dictionary<string, ClipCurve>();
                var curveBindings = AnimationUtility.GetCurveBindings(clip);
                for (int i = 0; i < curveBindings.Length; ++i)
                {
                    var binding = curveBindings[i];
                    string path = binding.path;
                    string propertyName = binding.propertyName;

                    bool ignoreProperty = false;
                    int curveIndex = -1;
                    int curveCount = 0;
                    ChannelPath channePath = ChannelPath.None;
                    switch (propertyName)
                    {
                        case "m_LocalPosition.x":
                            curveIndex = 0;
                            break;
                        case "m_LocalPosition.y":
                            curveIndex = 1;
                            break;
                        case "m_LocalPosition.z":
                            curveIndex = 2;
                            break;

                        case "m_LocalRotation.x":
                            curveIndex = 0;
                            break;
                        case "m_LocalRotation.y":
                            curveIndex = 1;
                            break;
                        case "m_LocalRotation.z":
                            curveIndex = 2;
                            break;
                        case "m_LocalRotation.w":
                            curveIndex = 3;
                            break;

                        case "m_LocalScale.x":
                            curveIndex = 0;
                            break;
                        case "m_LocalScale.y":
                            curveIndex = 1;
                            break;
                        case "m_LocalScale.z":
                            curveIndex = 2;
                            break;

                        case "localEulerAnglesRaw.x":
                            curveIndex = 0;
                            break;
                        case "localEulerAnglesRaw.y":
                            curveIndex = 1;
                            break;
                        case "localEulerAnglesRaw.z":
                            curveIndex = 2;
                            break;

                        default:
                            if (propertyName.StartsWith("blendShape."))
                            {
                                channePath = ChannelPath.Weights;
                            }
                            else
                            {
                                ignoreProperty = true;
                            }
                            break;
                    }
                    if (ignoreProperty)
                    {
                        Debug.LogWarning("ignore Property:" + propertyName);
                        continue;
                    }

                    if (propertyName.StartsWith("m_LocalPosition."))
                    {
                        curveCount = 3;
                        channePath = ChannelPath.Translation;
                    }
                    else if (propertyName.StartsWith("m_LocalRotation."))
                    {
                        curveCount = 4;
                        channePath = ChannelPath.Rotation;
                    }
                    else if (propertyName.StartsWith("m_LocalScale."))
                    {
                        curveCount = 3;
                        channePath = ChannelPath.Scale;
                    }
                    else if (propertyName.StartsWith("localEulerAnglesRaw."))
                    {
                        curveCount = 3;
                        channePath = ChannelPath.EulerAnglesRaw;
                    }

                    // Transform curveTarget = target.Find(path); // find by path
                    var targetName = path.Substring(path.LastIndexOf('/') + 1);
                    Transform curveTarget = Avatar.AnimationConverter.FindTransformByName(target.gameObject, targetName); // find by name
                    if (curveTarget == null && channePath != ChannelPath.Weights)
                    {
                        Debug.LogError("can not find curve target: " + path);
                        continue;
                    }

                    float blendShapeFrameWeight = 1.0f;
                    if (channePath == ChannelPath.Weights && curveTarget)
                    {
                        var blendShapeName = propertyName.Substring("blendShape.".Length);
                        var skin = curveTarget.GetComponent<SkinnedMeshRenderer>();
                        if (skin == null || skin.sharedMesh == null)
                        {
                            Debug.LogError("can not find curve target skinned mesh: " + path);
                        }
                        var mesh = skin.sharedMesh;
                        curveCount = mesh.blendShapeCount;
                        curveIndex = mesh.GetBlendShapeIndex(blendShapeName);
                        blendShapeFrameWeight = mesh.GetBlendShapeFrameWeight(curveIndex, 0);
                    }

                    ClipCurve clipCurve;
                    var curveKey = path + channePath.ToString();
                    if (!curveMap.TryGetValue(curveKey, out clipCurve))
                    {
                        clipCurve = new ClipCurve();
                        if (curveTarget)
                        {
                            clipCurve.curves = new AnimationCurve[curveCount];
                            clipCurve.path = path;
                            clipCurve.channePath = channePath;
                            clipCurve.target = curveTarget.gameObject;
                            clipCurve.binding = binding;
                            if (channePath == ChannelPath.Weights)
                            {
                                clipCurve.blendShapeFrameWeights = new float[curveCount];
                            }
                        }
                        else
                        {
                            clipCurve.blendShapeCurves = new List<AnimationCurve>();
                            clipCurve.path = path;
                            clipCurve.channePath = channePath;
                            clipCurve.target = null;
                            clipCurve.binding = binding;
                            clipCurve.blendShapeFrameWeights = null;
                        }
                        curveMap.Add(curveKey, clipCurve);
                    }

                    if (clipCurve.curves != null)
                    {
                        clipCurve.curves[curveIndex] = AnimationUtility.GetEditorCurve(clip, binding);
                        if (channePath == ChannelPath.Weights)
                        {
                            clipCurve.blendShapeFrameWeights[curveIndex] = blendShapeFrameWeight;
                        }
                    }
                    else if (clipCurve.blendShapeCurves != null)
                    {
                        clipCurve.blendShapeCurves.Add(AnimationUtility.GetEditorCurve(clip, binding));
                    }
                }

                // remap localEulerAnglesRaw curves to m_LocalRotation
                foreach (var pair in curveMap)
                {
                    var clipCurve = pair.Value;
                    if (clipCurve.channePath == ChannelPath.EulerAnglesRaw)
                    {
                        var newBinding = clipCurve.binding;
                        newBinding.propertyName = "localEulerAnglesRaw.x";
                        AnimationUtility.SetEditorCurve(clip, newBinding, null);
                        newBinding.propertyName = "localEulerAnglesRaw.y";
                        AnimationUtility.SetEditorCurve(clip, newBinding, null);
                        newBinding.propertyName = "localEulerAnglesRaw.z";
                        AnimationUtility.SetEditorCurve(clip, newBinding, null);

                        newBinding.propertyName = "localEulerAngles.x";
                        AnimationUtility.SetEditorCurve(clip, newBinding, clipCurve.curves[0]);
                        newBinding.propertyName = "localEulerAngles.y";
                        AnimationUtility.SetEditorCurve(clip, newBinding, clipCurve.curves[1]);
                        newBinding.propertyName = "localEulerAngles.z";
                        AnimationUtility.SetEditorCurve(clip, newBinding, clipCurve.curves[2]);

                        clipCurve.channePath = ChannelPath.Rotation;
                        clipCurve.curves = new AnimationCurve[4];
                        newBinding.propertyName = "m_LocalRotation.x";
                        clipCurve.curves[0] = AnimationUtility.GetEditorCurve(clip, newBinding);
                        newBinding.propertyName = "m_LocalRotation.y";
                        clipCurve.curves[1] = AnimationUtility.GetEditorCurve(clip, newBinding);
                        newBinding.propertyName = "m_LocalRotation.z";
                        clipCurve.curves[2] = AnimationUtility.GetEditorCurve(clip, newBinding);
                        newBinding.propertyName = "m_LocalRotation.w";
                        clipCurve.curves[3] = AnimationUtility.GetEditorCurve(clip, newBinding);
                    }
                }

                var channels = new JArray();
                var samplers = new JArray();

                foreach (var pair in curveMap)
                {
                    var clipCurve = pair.Value;
                    var curves = clipCurve.curves;
                    if (curves == null)
                    {
                        curves = clipCurve.blendShapeCurves.ToArray();
                    }
                    int curveCount = curves.Length;
                    List<float> times = new List<float>();
                    float timeMin = float.MaxValue;
                    float timeMax = float.MinValue;
                    for (int i = 0; i < curveCount; ++i)
                    {
                        var curve = curves[i];
                        if (curve == null)
                        {
                            if (clipCurve.channePath == ChannelPath.Weights)
                            {
                                continue;
                            }
                            else
                            {
                                throw new UnityException("miss curve at path: " + clipCurve.path + " with channel: " + clipCurve.channePath + " index: " + i);
                            }
                        }
                        var keyFrames = curve.keys;
                        int frameCount = keyFrames.Length;
                        for (int j = 0; j < frameCount; ++j)
                        {
                            var frame = keyFrames[j];
                            float time = frame.time;
                            if (times.Count == 0)
                            {
                                times.Add(time);
                            }
                            else
                            {
                                for (int k = 0; k < times.Count; ++k)
                                {
                                    if (Mathf.Abs(time - times[k]) < 0.000001f)
                                    {
                                        break;
                                    }
                                    else if (time < times[k])
                                    {
                                        times.Insert(k, time);
                                        break;
                                    }
                                    else if (k == times.Count - 1)
                                    {
                                        times.Add(time);
                                    }
                                }
                            }
                        }
                        //if (times.Count == 0)
                        //{
                        //    for (int j = 0; j < frameCount; ++j)
                        //    {
                        //        times.Add(keyFrames[j].time);
                        //    }
                        //}
                    }
                    float[] values = new float[times.Count * curveCount * 3];

                    for (int i = 0; i < times.Count; ++i)
                    {
                        var time = times[i];
                        for (int j = 0; j < curveCount; ++j)
                        {
                            var curve = curves[j];
                            if (curve == null)
                            {
                                continue;
                            }

                            var keyFrames = curve.keys;
                            if (i >= keyFrames.Length || Mathf.Abs(keyFrames[i].time - time) > 0.000001f)
                            {
                                curve.AddKey(time, curve.Evaluate(time));
                            }
                        }
                    }

                    for (int i = 0; i < times.Count; ++i)
                    {
                        var time = times[i];
                        timeMin = Mathf.Min(timeMin, time);
                        timeMax = Mathf.Max(timeMax, time);

                        for (int j = 0; j < curveCount; ++j)
                        {
                            var curve = curves[j];
                            if (curve == null)
                            {
                                continue;
                            }
                            var keyFrames = curve.keys;
                            var frame = keyFrames[i];
                            float value = frame.value;
                            if (clipCurve.channePath == ChannelPath.Weights)
                            {
                                float blendShapeFrameWeight = 1.0f;
                                if (clipCurve.blendShapeFrameWeights != null)
                                {
                                    blendShapeFrameWeight = clipCurve.blendShapeFrameWeights[j];
                                }

                                values[i * curveCount * 3 + 0 * curveCount + j] = frame.inWeight;
                                values[i * curveCount * 3 + 1 * curveCount + j] = value / blendShapeFrameWeight;
                                values[i * curveCount * 3 + 2 * curveCount + j] = frame.outWeight;
                            }
                            else
                            {
                                values[i * curveCount * 3 + 0 * curveCount + j] = frame.inTangent;
                                values[i * curveCount * 3 + 1 * curveCount + j] = value;
                                values[i * curveCount * 3 + 2 * curveCount + j] = frame.outTangent;
                            }
                        }
                    }

                    if (times.Count > 0 && values.Length > 0)
                    {
                        var sampler = new JObject();
                        sampler["input"] = AddBuffer(context, ToBytes(times.ToArray()), times.Count, ComponentType.FLOAT, AccessorType.SCALAR, BufferTarget.NONE, false, new float[1] { timeMin }, new float[1] { timeMax });
                        sampler["interpolation"] = "CUBICSPLINE";
                        AccessorType type = AccessorType.SCALAR;
                        int count = 0;
                        switch (clipCurve.channePath)
                        {
                            case ChannelPath.Translation:
                                type = AccessorType.VEC3;
                                count = values.Length / curveCount;
                                break;
                            case ChannelPath.Rotation:
                                type = AccessorType.VEC4;
                                count = values.Length / curveCount;
                                break;
                            case ChannelPath.Scale:
                                type = AccessorType.VEC3;
                                count = values.Length / curveCount;
                                break;
                            case ChannelPath.Weights:
                                type = AccessorType.SCALAR;
                                count = values.Length;
                                break;
                        }

                        if (context.config.leftToRightHandSpace)
                        {
                            if (context.config.leftToRightHandSpaceRule2)
                            {
                                if (clipCurve.channePath == ChannelPath.Translation)
                                {
                                    for (int i = 0; i < values.Length; i += 3)
                                    {
                                        values[i + 0] *= -1.0f;
                                    }
                                }
                                else if (clipCurve.channePath == ChannelPath.Rotation)
                                {
                                    for (int i = 0; i < values.Length; i += 4)
                                    {
                                        values[i + 1] *= -1.0f;
                                        values[i + 2] *= -1.0f;
                                    }
                                }
                            }
                            else
                            {
                                if (clipCurve.channePath == ChannelPath.Translation)
                                {
                                    for (int i = 0; i < values.Length; i += 3)
                                    {
                                        values[i + 2] *= -1.0f;
                                    }
                                }
                                else if (clipCurve.channePath == ChannelPath.Rotation)
                                {
                                    for (int i = 0; i < values.Length; i += 4)
                                    {
                                        values[i + 0] *= -1.0f;
                                        values[i + 1] *= -1.0f;
                                    }
                                }
                            }
                        }

                        sampler["output"] = AddBuffer(context, ToBytes(values), count, ComponentType.FLOAT, type);

                        var channelTarget = new JObject();
                        if (clipCurve.target)
                        {
                            var id = clipCurve.target.GetInstanceID();
                            Debug.Assert(context.objIndices.ContainsKey(id));
                            channelTarget["node"] = context.objIndices[id];
                        }
                        else
                        {
                            channelTarget["node"] = 0;
                        }
                        channelTarget["path"] = clipCurve.channePath.ToString().ToLower();

                        var channel = new JObject();
                        channel["sampler"] = samplers.Count;
                        channel["target"] = channelTarget;

                        channels.Add(channel);
                        samplers.Add(sampler);
                    }
                }

                var animation = new JObject();
                animation["name"] = clip.name;
                animation["channels"] = channels;
                animation["samplers"] = samplers;
                context.animations.Add(animation);
            }

            private static void AddObj(List<GameObject> objArray, Dictionary<int, int> objIndices, GameObject obj)
            {
                objIndices.Add(obj.GetInstanceID(), objArray.Count);
                objArray.Add(obj);
                var transform = obj.transform;
                var childCount = transform.childCount;
                for (int i = 0; i < childCount; ++i)
                {
                    var child = transform.GetChild(i);
                    AddObj(objArray, objIndices, child.gameObject);
                }
            }

            private static int AddSkin(Context context, Mesh mesh, SkinnedMeshRenderer skinnedMeshRenderer)
            {
                int index = context.skins.Count;
                var skin = new JObject();

                var bindposes = mesh.bindposes;
                var inverseBindMatrices = new float[bindposes.Length * 16];
                for (int i = 0; i < bindposes.Length; ++i)
                {
                    var matrix = bindposes[i];
                    if (context.config.leftToRightHandSpace)
                    {
                        if (context.config.leftToRightHandSpaceRule2)
                        {
                            var localPosition = matrix.GetColumn(3);
                            var localRotation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
                            var localScale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);
                            localPosition.x *= -1.0f;
                            localRotation.y *= -1.0f;
                            localRotation.z *= -1.0f;
                            matrix = Matrix4x4.TRS(localPosition, localRotation, localScale);
                        }
                        else
                        {
                            matrix = MatrixLeftToRightSpace(matrix);
                        }
                    }
                    for (int j = 0; j < 16; ++j)
                    {
                        inverseBindMatrices[i * 16 + j] = matrix[j % 4, j / 4];
                    }
                }
                skin["inverseBindMatrices"] = AddBuffer(context, ToBytes(inverseBindMatrices), bindposes.Length, ComponentType.FLOAT, AccessorType.MAT4);

                var joints = new JArray();
                var bones = skinnedMeshRenderer.bones;
                for (int i = 0; i < bones.Length; ++i)
                {
                    var id = bones[i].gameObject.GetInstanceID();
                    Debug.Assert(context.objIndices.ContainsKey(id));
                    joints.Add(context.objIndices[id]);
                }
                skin["joints"] = joints;

                // rootBone obj maybe not the closest common root
                //if (skinnedMeshRenderer.rootBone)
                //{
                //    var id = skinnedMeshRenderer.rootBone.gameObject.GetInstanceID();
                //    Debug.Assert(context.objIndices.ContainsKey(id));
                //    skin["skeleton"] = context.objIndices[id];
                //}

                // find closest common root as skeleton
                //{
                //    int rootIndex = 0;
                //    List<Transform> parentList = new List<Transform>();
                //    for (int i = 0; i < bones.Length; ++i)
                //    {
                //        var t = bones[i].transform;
                //        if (i == 0)
                //        {
                //            while (t)
                //            {
                //                parentList.Add(t);
                //                t = t.parent;
                //            }
                //        }
                //        else
                //        {
                //            for (int j = rootIndex; j < parentList.Count; ++j)
                //            {
                //                if (!t.IsChildOf(parentList[j]))
                //                {
                //                    if (j + 1 < parentList.Count)
                //                    {
                //                        rootIndex = j + 1;
                //                    }
                //                    else
                //                    {
                //                        rootIndex = -1;
                //                        Debug.Assert(false);
                //                    }
                //                }
                //                else
                //                {
                //                    break;
                //                }
                //            }

                //        }
                //    }

                //    var id = parentList[rootIndex].gameObject.GetInstanceID();
                //    Debug.Assert(context.objIndices.ContainsKey(id));
                //    skin["skeleton"] = context.objIndices[id];
                //}

                context.skins.Add(skin);
                return index;
            }

            private static int AddMesh(Context context, Mesh umesh, Renderer renderer, Material officialMaterial, Material customMaterial)
            {
                int index = context.meshes.Count;
                var mesh = new JObject();
                mesh["name"] = umesh.name;

                PrimitiveMesh primitiveMesh = AddPrimitiveMesh(context, umesh);

                var materials = renderer.sharedMaterials;
                var primitives = new JArray();
                for (int i = 0; i < umesh.subMeshCount; ++i)
                {
                    var primitive = new JObject();
                    primitive["attributes"] = primitiveMesh.attributes;
                    primitive["indices"] = primitiveMesh.indices[i];
                    if (primitiveMesh.targets != null)
                    {
                        primitive["targets"] = primitiveMesh.targets;
                    }
                    if (i < materials.Length && materials[i])
                    {
                        // if current render have 
                        if (officialMaterial != null || customMaterial != null)
                        {
                            primitive["material"] = AddMaterial(context, officialMaterial, customMaterial);
                        }
                        else
                        {
                            primitive["material"] = AddMaterial(context, materials[i]);
                        }

                    }
                    primitives.Add(primitive);
                }

                if (primitiveMesh.extras != null)
                {
                    mesh["extras"] = primitiveMesh.extras;
                }

                mesh["primitives"] = primitives;
                context.meshes.Add(mesh);
                return index;
            }

            private static byte[] ToBytes(ushort[] data)
            {
                byte[] bytes = new byte[data.Length * sizeof(ushort)];
                System.Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
                return bytes;
            }

            private static byte[] ToBytes(float[] data)
            {
                byte[] bytes = new byte[data.Length * sizeof(float)];
                System.Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
                return bytes;
            }

            private static Matrix4x4 MatrixLeftToRightSpace(Matrix4x4 matrix)
            {
                matrix.m20 = -matrix.m20;
                matrix.m21 = -matrix.m21;
                matrix.m23 = -matrix.m23;
                matrix.m02 = -matrix.m02;
                matrix.m12 = -matrix.m12;
                matrix.m32 = -matrix.m32;
                return matrix;
            }

            private static Quaternion QuaternionLeftToRightSpace(Quaternion quat)
            {
                quat.x = -quat.x;
                quat.y = -quat.y;
                return quat;
            }

            private static void CalcVerticesMinMax(Context context, ref NativeArray<Vector3> vertices, out float[] min, out float[] max)
            {
                var boundsMin = Vector3.one * float.MaxValue;
                var boundsMax = Vector3.one * float.MinValue;
                for (int i = 0; i < vertices.Length; ++i)
                {
                    var v = vertices[i];
                    if (context.config.leftToRightHandSpace)
                    {
                        if (context.config.leftToRightHandSpaceRule2)
                        {
                            v.x = -v.x;
                        }
                        else
                        {
                            v.z = -v.z;
                        }
                        vertices[i] = v;
                    }
                    boundsMin = Vector3.Min(boundsMin, v);
                    boundsMax = Vector3.Max(boundsMax, v);
                }
                min = new float[3] { boundsMin.x, boundsMin.y, boundsMin.z };
                max = new float[3] { boundsMax.x, boundsMax.y, boundsMax.z };
            }

            private static void VerticesSpaceConvert(Context context, ref NativeArray<Vector3> vertices)
            {
                if (context.config.leftToRightHandSpace)
                {
                    for (int i = 0; i < vertices.Length; ++i)
                    {
                        var v = vertices[i];
                        if (context.config.leftToRightHandSpaceRule2)
                        {
                            v.x = -v.x;
                        }
                        else
                        {
                            v.z = -v.z;
                        }
                        vertices[i] = v;
                    }
                }
            }

            private static PrimitiveMesh AddPrimitiveMesh(Context context, Mesh mesh)
            {
                PrimitiveMesh primitiveMesh;
                if (context.primitiveMeshes.TryGetValue(mesh.GetInstanceID(), out primitiveMesh))
                {
                    return primitiveMesh;
                }

                primitiveMesh = new PrimitiveMesh();
                context.primitiveMeshes.Add(mesh.GetInstanceID(), primitiveMesh);

                var meshDatas = Mesh.AcquireReadOnlyMeshData(mesh);
                var meshData = meshDatas[0];
                int vertexCount = mesh.vertexCount;
                bool hasBoneWeights = false;
                var attrs = mesh.GetVertexAttributes();
                for (int i = 0; i < attrs.Length; ++i)
                {
                    var attr = attrs[i];
                    switch (attr.attribute)
                    {
                        case UnityEngine.Rendering.VertexAttribute.Position:
                            var vertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                            meshData.GetVertices(vertices);
                            float[] min, max;
                            CalcVerticesMinMax(context, ref vertices, out min, out max);
                            primitiveMesh.attributes["POSITION"] = AddBuffer(context, vertices.Reinterpret<byte>(sizeof(float) * 3).ToArray(), vertexCount, ComponentType.FLOAT, AccessorType.VEC3, BufferTarget.ARRAY_BUFFER, false, min, max);
                            vertices.Dispose();
                            break;
                        case UnityEngine.Rendering.VertexAttribute.Normal:
                            var normals = new NativeArray<Vector3>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                            meshData.GetNormals(normals);
                            VerticesSpaceConvert(context, ref normals);
                            primitiveMesh.attributes["NORMAL"] = AddBuffer(context, normals.Reinterpret<byte>(sizeof(float) * 3).ToArray(), vertexCount, ComponentType.FLOAT, AccessorType.VEC3, BufferTarget.ARRAY_BUFFER);
                            normals.Dispose();
                            break;
                        case UnityEngine.Rendering.VertexAttribute.Tangent:
                            var tangents = new NativeArray<Vector4>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                            meshData.GetTangents(tangents);
                            // fix tangent w
                            for (int j = 0; j < vertexCount; ++j)
                            {
                                var t = tangents[j];
                                if (context.config.leftToRightHandSpace)
                                {
                                    if (context.config.leftToRightHandSpaceRule2)
                                    {
                                        t.x = -t.x;
                                    }
                                    else
                                    {
                                        t.z = -t.z;
                                    }
                                }
                                if (Mathf.Abs(t.w) < 0.000001f)
                                {
                                    t.w = 1.0f;
                                }
                                tangents[j] = t;
                            }
                            primitiveMesh.attributes["TANGENT"] = AddBuffer(context, tangents.Reinterpret<byte>(sizeof(float) * 4).ToArray(), vertexCount, ComponentType.FLOAT, AccessorType.VEC4, BufferTarget.ARRAY_BUFFER);
                            tangents.Dispose();
                            break;
                        case UnityEngine.Rendering.VertexAttribute.Color:
                            var colors32 = new NativeArray<Color32>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                            meshData.GetColors(colors32);
                            primitiveMesh.attributes["COLOR_0"] = AddBuffer(context, colors32.Reinterpret<byte>(sizeof(byte) * 4).ToArray(), vertexCount, ComponentType.UNSIGNED_BYTE, AccessorType.VEC4, BufferTarget.ARRAY_BUFFER, true);
                            colors32.Dispose();
                            break;
                        case UnityEngine.Rendering.VertexAttribute.BlendWeight:
                        case UnityEngine.Rendering.VertexAttribute.BlendIndices:
                            hasBoneWeights = true;
                            break;
                        default:
                            break;
                    }
                    if (attr.attribute >= UnityEngine.Rendering.VertexAttribute.TexCoord0 &&
                        attr.attribute <= UnityEngine.Rendering.VertexAttribute.TexCoord7)
                    {
                        int channel = attr.attribute - UnityEngine.Rendering.VertexAttribute.TexCoord0;
                        var uvs = new NativeArray<Vector2>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                        meshData.GetUVs(channel, uvs);
                        if (context.config.flipTexCoordY)
                        {
                            for (int j = 0; j < vertexCount; ++j)
                            {
                                var uv = uvs[j];
                                uvs[j] = new Vector2(uv.x, 1.0f - uv.y);
                            }
                        }
                        primitiveMesh.attributes["TEXCOORD_" + channel] = AddBuffer(context, uvs.Reinterpret<byte>(sizeof(float) * 2).ToArray(), vertexCount, ComponentType.FLOAT, AccessorType.VEC2, BufferTarget.ARRAY_BUFFER);
                        uvs.Dispose();
                    }
                }

                if (hasBoneWeights)
                {
                    var boneCounts = mesh.GetBonesPerVertex();
                    var boneWeights = mesh.GetAllBoneWeights();
                    int weightIndex = 0;
                    List<ushort[]> joints = new List<ushort[]>();
                    List<float[]> weights = new List<float[]>();
                    System.Func<int, ushort[]> getJoints = (int index) => {
                        while (index >= joints.Count)
                        {
                            joints.Add(new ushort[vertexCount * 4]);
                        }
                        return joints[index];
                    };
                    System.Func<int, float[]> getWeights = (int index) => {
                        while (index >= weights.Count)
                        {
                            weights.Add(new float[vertexCount * 4]);
                        }
                        return weights[index];
                    };
                    for (int i = 0; i < vertexCount; ++i)
                    {
                        int count = boneCounts[i];
                        if (count > 0)
                        {
                            for (int j = 0; j < count; ++j)
                            {
                                int index = j / 4;
                                var indexJoints = getJoints(index);
                                var indexWeights = getWeights(index);
                                var boneWeight = boneWeights[weightIndex + j];
                                indexJoints[i * 4 + j % 4] = (ushort)boneWeight.boneIndex;
                                indexWeights[i * 4 + j % 4] = boneWeight.weight;
                            }
                            weightIndex += count;
                        }
                        else
                        {
                            getJoints(0);
                            getWeights(0);
                        }
                    }
                    for (int i = 0; i < joints.Count; ++i)
                    {
                        primitiveMesh.attributes["JOINTS_" + i] = AddBuffer(context, ToBytes(joints[i]), vertexCount, ComponentType.UNSIGNED_SHORT, AccessorType.VEC4, BufferTarget.ARRAY_BUFFER);
                        primitiveMesh.attributes["WEIGHTS_" + i] = AddBuffer(context, ToBytes(weights[i]), vertexCount, ComponentType.FLOAT, AccessorType.VEC4, BufferTarget.ARRAY_BUFFER);
                    }
                }

                var indexBuffer = mesh.GetIndexBuffer();
                var indexStride = indexBuffer.stride;
                Debug.Assert(indexStride == 2 || indexStride == 4);
                var indexBytes = new byte[indexBuffer.count * indexStride];
                indexBuffer.GetData(indexBytes);
                for (int i = 0; i < mesh.subMeshCount; ++i)
                {
                    var indexCount = (int)mesh.GetIndexCount(i);
                    var startByteOffset = (int)mesh.GetIndexStart(i) * indexStride;
                    var countByteLength = indexCount * indexStride;
                    var indices = new byte[countByteLength];
                    System.Buffer.BlockCopy(indexBytes, startByteOffset, indices, 0, countByteLength);
                    if (context.config.flipFrontFace)
                    {
                        if (indexStride == 2)
                        {
                            var indices16 = new ushort[countByteLength / indexStride];
                            System.Buffer.BlockCopy(indices, 0, indices16, 0, countByteLength);
                            for (int j = 0; j < indexCount; j += 3)
                            {
                                var temp = indices16[j + 0];
                                indices16[j + 0] = indices16[j + 1];
                                indices16[j + 1] = temp;
                            }
                            System.Buffer.BlockCopy(indices16, 0, indices, 0, countByteLength);
                        }
                        else
                        {
                            var indices32 = new uint[countByteLength / indexStride];
                            System.Buffer.BlockCopy(indices, 0, indices32, 0, countByteLength);
                            for (int j = 0; j < indexCount; j += 3)
                            {
                                var temp = indices32[j + 0];
                                indices32[j + 0] = indices32[j + 1];
                                indices32[j + 1] = temp;
                            }
                            System.Buffer.BlockCopy(indices32, 0, indices, 0, countByteLength);
                        }
                    }
                    int indexBufferIndex = AddBuffer(context, indices, indexCount, indexStride == 4 ? ComponentType.UNSIGNED_INT : ComponentType.UNSIGNED_SHORT, AccessorType.SCALAR, BufferTarget.ELEMENT_ARRAY_BUFFER);
                    primitiveMesh.indices.Add(indexBufferIndex);
                }
                indexBuffer.Dispose();

                if (mesh.blendShapeCount > 0)
                {
                    var targets = new JArray();
                    var extras = new JObject();
                    var targetNames = new JArray();
                    for (int i = 0; i < mesh.blendShapeCount; ++i)
                    {
                        var vertices = new Vector3[vertexCount];
                        var normals = new Vector3[vertexCount];
                        var tangents = new Vector3[vertexCount];
                        var name = mesh.GetBlendShapeName(i);
                        mesh.GetBlendShapeFrameVertices(i, 0, vertices, normals, tangents);

                        var target = new JObject();

                        var verticesNative = new NativeArray<Vector3>(vertices, Allocator.Temp);
                        float[] min, max;
                        CalcVerticesMinMax(context, ref verticesNative, out min, out max);
                        target["POSITION"] = AddBuffer(context, verticesNative.Reinterpret<byte>(sizeof(float) * 3).ToArray(), vertexCount, ComponentType.FLOAT, AccessorType.VEC3, BufferTarget.ARRAY_BUFFER, false, min, max);
                        verticesNative.Dispose();

                        if (primitiveMesh.attributes.Property("NORMAL") != null)
                        {
                            var normalsNative = new NativeArray<Vector3>(normals, Allocator.Temp);
                            VerticesSpaceConvert(context, ref normalsNative);
                            target["NORMAL"] = AddBuffer(context, normalsNative.Reinterpret<byte>(sizeof(float) * 3).ToArray(), vertexCount, ComponentType.FLOAT, AccessorType.VEC3, BufferTarget.ARRAY_BUFFER);
                            normalsNative.Dispose();
                        }

                        if (primitiveMesh.attributes.Property("TANGENT") != null)
                        {
                            var tangentsNative = new NativeArray<Vector3>(tangents, Allocator.Temp);
                            VerticesSpaceConvert(context, ref tangentsNative);
                            target["TANGENT"] = AddBuffer(context, tangentsNative.Reinterpret<byte>(sizeof(float) * 3).ToArray(), vertexCount, ComponentType.FLOAT, AccessorType.VEC3, BufferTarget.ARRAY_BUFFER);
                            tangentsNative.Dispose();
                        }

                        targets.Add(target);
                        targetNames.Add(name);
                    }
                    extras["targetNames"] = targetNames;
                    primitiveMesh.targets = targets;
                    primitiveMesh.extras = extras;
                }

                meshDatas.Dispose();

                return primitiveMesh;
            }


            private static JObject AddExtraCustomPropertiesNode(Context context, Material umaterial)
            {
                var customProperties = new JObject();
                var customFloats = new JObject();
                var customVec4s = new JObject();
                var customTexs = new JObject();
                var customInts = new JObject();
               
                // Parser material Type.
                OfficialShaderTheme shaderTheme = ConfigureComponentPanel.getOfficialShaderThemeByName(umaterial.shader.name);
                if (!customFloats.ContainsKey(ShaderThemeString))
                {
                    customFloats.Add(ShaderThemeString, (int)shaderTheme);
                }
                
                var shader = umaterial.shader;
                var propertyCount = shader.GetPropertyCount();
                for (int i = 0; i < propertyCount; ++i)
                {
                    var name = shader.GetPropertyName(i);
                    var type = shader.GetPropertyType(i);
                    switch (type)
                    {
                        case UnityEngine.Rendering.ShaderPropertyType.Float:
                        case UnityEngine.Rendering.ShaderPropertyType.Range:
                            {
                                var value = umaterial.GetFloat(name);
                                if (!customFloats.ContainsKey(name))
                                {
                                    customFloats.Add(name, value);
                                }
                                else
                                {
                                    Debug.LogWarning("Shader has property with same name: " + name);
                                }
                                break;
                            }
                        case UnityEngine.Rendering.ShaderPropertyType.Color:
                            {
                                var value = umaterial.GetColor(name);
                                if (!customVec4s.ContainsKey(name))
                                {
                                    customVec4s.Add(name, new JArray(value.r, value.g, value.b, value.a));
                                }
                                else
                                {
                                    Debug.LogWarning("Shader has property with same name: " + name);
                                }
                                break;
                            }
                        case UnityEngine.Rendering.ShaderPropertyType.Vector:
                            {
                                var value = umaterial.GetVector(name);
                                if (!customVec4s.ContainsKey(name))
                                {
                                    customVec4s.Add(name, new JArray(value.x, value.y, value.z, value.w));
                                }
                                else
                                {
                                    Debug.LogWarning("Shader has property with same name: " + name);
                                }
                                break;
                            }
                        case UnityEngine.Rendering.ShaderPropertyType.Texture:
                            {
                                var value = umaterial.GetTexture(name);
                                if (!customTexs.ContainsKey(name))
                                {
                                    if (value && value is Texture2D)
                                    {
                                        string suffix = "";
                                        if (name.Equals("_ColorRegionMap")
                                         || name.Equals("_BodyMaskMap"))
                                        {
                                            suffix = name;
                                        }
                                        var texIndex = AddTexture(context, value as Texture2D, autoChecksRGB: true, null, suffix);
                                        customTexs.Add(name, texIndex);
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning("Shader has property with same name: " + name);
                                }
                                break;
                            }
                        case UnityEngine.Rendering.ShaderPropertyType.Int:
                            {
                                var value = umaterial.GetInt(name);
                                if (!customInts.ContainsKey(name))
                                {
                                    customInts.Add(name, value);
                                }
                                else
                                {
                                    Debug.LogWarning("Shader has property with same name: " + name);
                                }
                                break;
                            }
                        default:
                            Debug.Assert(false);
                            break;
                    }
                }
                if (customFloats.Count > 0)
                {
                    customProperties["Float"] = customFloats;
                }
                if (customVec4s.Count > 0)
                {
                    customProperties["Vec4"] = customVec4s;
                }
                if (customTexs.Count > 0)
                {
                    customProperties["Tex"] = customTexs;
                }
                if (customInts.Count > 0)
                {
                    customProperties["Int"] = customInts;
                }

                return customProperties;
            }
            
            private static JObject AddExtraCustomMaterialNode(Material umaterial)
            {
                var customProperties = new JObject();
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(umaterial, out string guid, out long localId))
                {
                    customProperties["Guid"] = guid;

                    var customMaterialDB = AvatarCustomMaterialDataBase.instance;

                    if (customMaterialDB != null)
                    {
                        customMaterialDB.Add(guid, umaterial);
                    }
                }
                return customProperties;
            }

            private static int AddCustomMaterial(Context context, Material officialMaterial, Material customMaterial, Definiation_glTF_Callback func = null)
            {
                int index;

                ulong umID = (ulong)(officialMaterial != null ? officialMaterial.GetInstanceID() : 0);
                ulong cmID = (ulong)(customMaterial != null ? customMaterial.GetInstanceID() : 0);

                ulong combineID = ((umID & 0xFFFFFFFF)  << 32) | (cmID & 0xFFFFFFFF);

                if (context.matIndices.TryGetValue(combineID, out index))
                {
                    return index;
                }

                index = context.materials.Count;
                context.matIndices.Add(combineID, index);
                var materialNode = new JObject();
                materialNode["name"] = officialMaterial.name;

                // extras
                var extrasNode = new JObject();
                
                if (officialMaterial)
				{
                    extrasNode["customProperties"] = AddExtraCustomPropertiesNode(context, officialMaterial); // store the officialMaterial properties.
                }
                
                if (customMaterial)
                {
                    extrasNode["customMaterial"] = AddExtraCustomMaterialNode(customMaterial); //store the customMaterial Guid.
                }
 
                materialNode["extras"] = extrasNode;

                if (func != null)
                {
                    func(context, materialNode, officialMaterial);
                } 

                context.materials.Add(materialNode);
                return index;
            }

            // Parse the official material information and put it into the gltf material.
            // Support the online preview of gltf. So there are only basic attributes.
            static void OfficialMaterialDefined(Context context, JObject materialNode, Material umaterial)
            {
                var pbrMetallicRoughness = new JObject();
                {
                    var mainColor = umaterial.GetMainColor();
                    pbrMetallicRoughness["baseColorFactor"] = new JArray(mainColor.r, mainColor.g, mainColor.b, mainColor.a);
                    var mainTexture = umaterial.GetMainTexture();
                    if (mainTexture && mainTexture is Texture2D)
                    {
                        pbrMetallicRoughness["baseColorTexture"] = AddTexture(context, mainTexture as Texture2D, autoChecksRGB: true);
                    }
                    if (umaterial.HasFloat("_Metallic"))
                    {
                        pbrMetallicRoughness["metallicFactor"] = umaterial.GetFloat("_Metallic");
                    }
                    if (umaterial.HasFloat("_Smoothness"))
                    {
                        pbrMetallicRoughness["roughnessFactor"] = 1.0f - umaterial.GetFloat("_Smoothness");
                    }
                    if (umaterial.HasTexture("_MetallicGlossMap"))
                    {
                        var metallicRoughnessTexture = umaterial.GetTexture("_MetallicGlossMap");
                        if (metallicRoughnessTexture && metallicRoughnessTexture is Texture2D)
                        {
                            // R:metallic A:smoothness -> G:roughness B:metallic
                            Shader convertShader = null;
                            if (context.config.mapMetallicTexChannels)
                            {
                                convertShader = AssetDatabase.LoadAssetAtPath<Shader>(AvatarConverter.converterRootDir + "/GLTF/MetallicMap.shader");
                            }
                            pbrMetallicRoughness["metallicRoughnessTexture"] = AddTexture(context, metallicRoughnessTexture as Texture2D, autoChecksRGB: true, convertShader);
                        }
                    }
                }
                materialNode["pbrMetallicRoughness"] = pbrMetallicRoughness;

                if (umaterial.HasTexture("_BumpMap"))
                {
                    var normalTexture = umaterial.GetTexture("_BumpMap");
                    if (normalTexture && normalTexture is Texture2D)
                    {
                        Shader convertShader = null;
                        var path = AssetDatabase.GetAssetPath(normalTexture);
                        if (!string.IsNullOrEmpty(path))
                        {
                            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                            // DXT5nm / BC5 -> RGB
                            if (importer && importer.textureType == TextureImporterType.NormalMap)
                            {
                                convertShader = AssetDatabase.LoadAssetAtPath<Shader>(AvatarConverter.converterRootDir + "/GLTF/UnpackNormal.shader");
                            }
                        }
                        if (convertShader == null)
                        {
                            convertShader = AssetDatabase.LoadAssetAtPath<Shader>(AvatarConverter.converterRootDir + "/GLTF/LinearTransfer.shader");
                        }
                        materialNode["normalTexture"] = AddTexture(context, normalTexture as Texture2D, autoChecksRGB: false, convertShader);
                    }
                }

                if (umaterial.HasFloat("_SrcBlend") &&
                    Mathf.RoundToInt(umaterial.GetFloat("_SrcBlend")) == (int)UnityEngine.Rendering.BlendMode.SrcAlpha &&
                    umaterial.HasFloat("_DstBlend") &&
                    Mathf.RoundToInt(umaterial.GetFloat("_DstBlend")) == (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha)
                {
                    materialNode["alphaMode"] = "BLEND";
                }
                else if (umaterial.IsKeywordEnabled("_ALPHABLEND_ON"))
                {
                    materialNode["alphaMode"] = "BLEND";
                }
                else if (umaterial.IsKeywordEnabled("_ALPHATEST_ON"))
                {
                    materialNode["alphaMode"] = "MASK";
                    if (umaterial.HasFloat("_Cutoff"))
                    {
                        materialNode["alphaCutoff"] = umaterial.GetFloat("_Cutoff");
                    }
                }
                if (umaterial.HasFloat("_Cull") &&
                    Mathf.RoundToInt(umaterial.GetFloat("_Cull")) == (int)UnityEngine.Rendering.CullMode.Off)
                {
                    materialNode["doubleSided"] = true;
                }
            }

            private static int AddMaterial(Context context, Material officialMaterial, Material customMaterial = null)
			{
                return AddCustomMaterial(context, officialMaterial, customMaterial, OfficialMaterialDefined);
            }
            enum FilterMode
            {
                NONE = -1,
                NEAREST = 9728,
                LINEAR = 9729,
                NEAREST_MIPMAP_NEAREST = 9984,
                LINEAR_MIPMAP_NEAREST = 9985,
                NEAREST_MIPMAP_LINEAR = 9986,
                LINEAR_MIPMAP_LINEAR = 9987,
            }

            enum WrapMode
            {
                NONE = -1,
                CLAMP_TO_EDGE = 33071,
                MIRRORED_REPEAT = 33648,
                REPEAT = 10497,
            }

            private static JObject AddTexture(Context context, Texture2D utexture, bool autoChecksRGB, Shader convertShader = null, string suffix = "")
            {
                bool sRGB = false;
                if (autoChecksRGB)
                {
                    var path = AssetDatabase.GetAssetPath(utexture);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                        sRGB = importer.sRGBTexture;
                    }
                }

                var info = new JObject();
                int index;
                if (context.texIndices.TryGetValue(utexture.GetInstanceID(), out index))
                {
                    info["index"] = index;
                    return info;
                }

                index = context.textures.Count;
                context.texIndices.Add(utexture.GetInstanceID(), index);

                var texture = new JObject();
                texture["name"] = utexture.name;
                texture["sampler"] = index;
                texture["source"] = index;
                context.textures.Add(texture);

                var sampler = new JObject();
                FilterMode filterMode = FilterMode.NONE;
                switch (utexture.filterMode)
                {
                    case UnityEngine.FilterMode.Point:
                        filterMode = FilterMode.NEAREST;
                        break;
                    case UnityEngine.FilterMode.Bilinear:
                    case UnityEngine.FilterMode.Trilinear:
                        filterMode = FilterMode.LINEAR;
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
                WrapMode wrapMode = WrapMode.NONE;
                switch (utexture.wrapMode)
                {
                    case TextureWrapMode.Repeat:
                        wrapMode = WrapMode.REPEAT;
                        break;
                    case TextureWrapMode.Clamp:
                        wrapMode = WrapMode.CLAMP_TO_EDGE;
                        break;
                    case TextureWrapMode.Mirror:
                    case TextureWrapMode.MirrorOnce:
                        wrapMode = WrapMode.MIRRORED_REPEAT;
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
                sampler["magFilter"] = (int) filterMode;
                sampler["minFilter"] = (int) filterMode;
                sampler["wrapS"] = (int) wrapMode;
                sampler["wrapT"] = (int) wrapMode;
                context.samplers.Add(sampler);

                var imageData = ExportPNG(utexture, convertShader);
                
                var image = new JObject();
                image["name"] = utexture.name;

                if (context.glb)
                {
                    image["bufferView"] = AddBuffer(context, imageData, imageData.Length, ComponentType.BYTE, AccessorType.SCALAR);
                    image["mimeType"] = "image/png";
                }
                else
                {
                    string sRGBName = sRGB ? "_sRGB" : "_linear";

                    var imageUri = index.ToString() + "_" + "image" + suffix + sRGBName + ".png";
                    File.WriteAllBytes(context.dir + "/" + imageUri, imageData);
                    image["uri"] = imageUri;
                }
                
                context.images.Add(image);

                info["index"] = index;
                return info;
            }

            private static byte[] ExportPNG(Texture2D texture, Shader convertShader)
            {
                RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                if (convertShader)
                {
                    Graphics.Blit(texture, rt, new Material(convertShader));
                }
                else
                {
                    Graphics.Blit(texture, rt);
                }

                var rtOld = RenderTexture.active;
                RenderTexture.active = rt;

                Texture2D outTex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                outTex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);

                RenderTexture.active = rtOld;
                RenderTexture.ReleaseTemporary(rt);

                return outTex.EncodeToPNG();
            }

            enum ComponentType
            {
                BYTE = 5120,
                UNSIGNED_BYTE = 5121,
                SHORT = 5122,
                UNSIGNED_SHORT = 5123,
                UNSIGNED_INT = 5125,
                FLOAT = 5126,
            }

            enum AccessorType
            {
                SCALAR,
                VEC2,
                VEC3,
                VEC4,
                MAT2,
                MAT3,
                MAT4,
            }

            enum BufferTarget
            {
                NONE = -1,
                ARRAY_BUFFER = 34962,
                ELEMENT_ARRAY_BUFFER = 34963,
            }

            private static int AddBuffer(Context context, byte[] buffer, int count, ComponentType componentType, AccessorType accessorType, BufferTarget bufferTarget = BufferTarget.NONE, bool normalized = false, float[] min = null, float[] max = null)
            {
                int alignment = 0;
                switch (componentType)
                {
                    case ComponentType.BYTE:
                    case ComponentType.UNSIGNED_BYTE:
                        alignment = 1;
                        break;
                    case ComponentType.SHORT:
                    case ComponentType.UNSIGNED_SHORT:
                        alignment = 2;
                        break;
                    case ComponentType.UNSIGNED_INT:
                    case ComponentType.FLOAT:
                        alignment = 4;
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
                switch (accessorType)
                {
                    case AccessorType.SCALAR:
                        alignment *= 1;
                        break;
                    case AccessorType.VEC2:
                        alignment *= 2;
                        break;
                    case AccessorType.VEC3:
                        alignment *= 3;
                        break;
                    case AccessorType.VEC4:
                        alignment *= 4;
                        break;
                    case AccessorType.MAT2:
                        alignment *= 4;
                        break;
                    case AccessorType.MAT3:
                        alignment *= 9;
                        break;
                    case AccessorType.MAT4:
                        alignment *= 16;
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
                int byteOffset = (int) context.streamWriter.BaseStream.Position;
                if (byteOffset % alignment != 0)
                {
                    int pad = alignment - (byteOffset % alignment);
                    context.streamWriter.Write(context.bufferPadding, 0, pad);
                    byteOffset += pad;
                }
                context.streamWriter.Write(buffer);

                int bufferViewIndex = context.bufferViews.Count;
                var bufferView = new JObject();
                bufferView["buffer"] = 0;
                bufferView["byteOffset"] = byteOffset;
                bufferView["byteLength"] = buffer.Length;
                if (bufferTarget != BufferTarget.NONE)
                {
                    bufferView["target"] = (int) bufferTarget;
                }
                context.bufferViews.Add(bufferView);

                int accessorIndex = context.accessors.Count;
                var accessor = new JObject();
                accessor["bufferView"] = bufferViewIndex;
                accessor["componentType"] = (int) componentType;
                accessor["count"] = count;
                accessor["type"] = accessorType.ToString();
                if (normalized)
                {
                    accessor["normalized"] = true;
                }

                // required for animation input and vertex position attribute
                if (min != null)
                {
                    accessor["min"] = new JArray(min);
                }
                if (max != null)
                {
                    accessor["max"] = new JArray(max);
                }

                context.accessors.Add(accessor);

                return accessorIndex;
            }

            private static void WriteBuffer(Context context)
            {
                var stream = context.streamWriter.BaseStream as MemoryStream;
                var data = stream.ToArray();
                if (data.Length == 0)
                {
                    return;
                }

                var uri = "0.bin";
                File.WriteAllBytes(context.dir + "/" + uri, data);

                var buffers = new JArray();
                var buffer = new JObject();
                buffer["uri"] = uri;
                buffer["byteLength"] = data.Length;
                buffers.Add(buffer);
                context.gltf["buffers"] = buffers;
            }

            private static void WriteGlb(string path, Context context)
            {
                const uint alignment = 4;

                const uint magic = 0x46546C67;
                const uint version = 2;
                uint length = 0;

                uint jsonChunkLength = 0;
                const uint jsonChunkType = 0x4E4F534A;
                byte[] jsonChunkData = null;
                byte[] jsonChunkPadding = null;

                uint binChunkLength = 0;
                const uint binChunkType = 0x004E4942;
                byte[] binChunkData = null;
                byte[] binChunkPadding = null;

                // buffer
                var stream = context.streamWriter.BaseStream as MemoryStream;
                binChunkData = stream.ToArray();

                if (binChunkData.Length > 0)
                {
                    var buffers = new JArray();
                    var buffer = new JObject();
                    buffer["byteLength"] = binChunkData.Length;
                    buffers.Add(buffer);
                    context.gltf["buffers"] = buffers;
                }

                binChunkLength = (uint) binChunkData.Length;
                if (binChunkLength % alignment != 0)
                {
                    var pad = alignment - (binChunkLength % alignment);
                    binChunkPadding = new byte[pad];
                    for (int i = 0; i < pad; ++i)
                    {
                        binChunkPadding[i] = 0x00;
                    }
                    binChunkLength += pad;
                }

                // json
                var json = context.gltf.ToString();
                jsonChunkData = System.Text.Encoding.UTF8.GetBytes(json);
                jsonChunkLength = (uint) jsonChunkData.Length;

                if (jsonChunkLength % alignment != 0)
                {
                    var pad = alignment - (jsonChunkLength % alignment);
                    jsonChunkPadding = new byte[pad];
                    for (int i = 0; i < pad; ++i)
                    {
                        jsonChunkPadding[i] = 0x20;
                    }
                    jsonChunkLength += pad;
                }

                length = 12 + 8 + jsonChunkLength + 8 + binChunkLength;

                // write
                var writer = new BinaryWriter(new MemoryStream());
                writer.Write(magic);
                writer.Write(version);
                writer.Write(length);
                writer.Write(jsonChunkLength);
                writer.Write(jsonChunkType);
                writer.Write(jsonChunkData);
                if (jsonChunkPadding != null)
                {
                    writer.Write(jsonChunkPadding);
                }
                writer.Write(binChunkLength);
                writer.Write(binChunkType);
                writer.Write(binChunkData);
                if (binChunkPadding != null)
                {
                    writer.Write(binChunkPadding);
                }
                
                var glb = (writer.BaseStream as MemoryStream).ToArray();
                File.WriteAllBytes(path, glb);

                writer.BaseStream.Dispose();
                writer.Dispose();
            }
        }

        static class ExtensionMethods
        {
            public static Color GetMainColor(this Material material)
            {
                if (material)
                {
                    var shader = material.shader;
                    if (shader)
                    {
                        int propertyCount = shader.GetPropertyCount();
                        for (int i = 0; i < propertyCount; ++i)
                        {
                            var flags = shader.GetPropertyFlags(i);
                            if (flags == UnityEngine.Rendering.ShaderPropertyFlags.MainColor)
                            {
                                return material.GetColor(shader.GetPropertyNameId(i));
                            }
                        }
                        if (material.HasColor("_Color"))
                        {
                            return material.color;
                        }
                    }
                }
                return Color.white;
            }

            public static Texture GetMainTexture(this Material material)
            {
                if (material)
                {
                    var shader = material.shader;
                    if (shader)
                    {
                        int propertyCount = shader.GetPropertyCount();
                        for (int i = 0; i < propertyCount; ++i)
                        {
                            var flags = shader.GetPropertyFlags(i);
                            if (flags == UnityEngine.Rendering.ShaderPropertyFlags.MainTexture)
                            {
                                return material.GetTexture(shader.GetPropertyNameId(i));
                            }
                        }
                        if (material.HasColor("_MainTex"))
                        {
                            return material.mainTexture;
                        }
                    }
                }
                return null;
            }
        }
    }
}

#endif