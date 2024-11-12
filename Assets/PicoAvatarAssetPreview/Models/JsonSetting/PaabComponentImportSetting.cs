#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pico.Avatar;
using UnityEngine;
using Object = System.Object;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        [Serializable]
        public class PaabComponentImportSetting : PaabAssetImportSetting
        {
            // asset import setting type.
            public override AssetImportSettingType settingType { get => AssetImportSettingType.Unknown; }

            private const string presetConfigJsonDir = "PicoAvatarAssetPreview/Editors/Config/PresetConfigJson";

            public enum ComponentSource : int
            {
                Official_1_0,
                Custom,
                Official_2_0,
            }

            public enum ComponentType : int
            {
                Invalid = -1,
                BaseBody,
                Head,
                Body,
                Hand,
                Hair,
                ClothTop,
                ClothBottom,
                ClothShoes,
                ClothSocks,
                ClothGloves,
                ClothHood,
                AccessoryHeaddress,
                AccessoryMask,
                AccessoryBracelet,
                AccessoryNecklace,
                AccessoryArmguard,
                AccessoryShoulderknot,
                AccessoryLegRing,
                AccessoryProp,
            }

            public enum BaseBodyType : int
            {
                None = -1,
                Head,
                Body,
            }

            public ComponentSource componentSource = ComponentSource.Official_1_0;
            public ComponentType componentType = ComponentType.BaseBody;
            public string serverCategory;  // 上传资源时使用

            public class RendererItem
            {
                public int baseBodyType;
                public string meshName;
                public string customShader;
                public int verts = 0;
                public int triangles = 0;
            }

            public class LodRenderers
            {
                public int lod;
                public RendererItem[] rendererItems;
            }

            public LodRenderers[] lods;

            public int currentLod = -1; // only used for single lod json serialize

            [Serializable]
            public class ShoeParams
            {
                public float heelHeight = 0.0f;
                public float soleThickness = 0.01f;
            }

            public ShoeParams shoeParams {
                get {
                    if (_shoeParams == null)
                    {
                        _shoeParams = new ShoeParams();
                    }
                    return _shoeParams;
                }
            }
            [SerializeField]
            private ShoeParams _shoeParams = null;

            /**
             * @brief Build from json object. Derived class SHOULD override the method.
             */
            public override void FromJsonObject(Dictionary<string, object> jsonObject)
            {
                // do nothing
            }

            /**
             * @brief Serialize to json object. Derived class SHOULD override the method.
             */
            public override void ToJsonObject(Dictionary<string, object> jsonObject)
            {
                var componentSetting = new Dictionary<string, object>();
                componentSetting["componentSource"] = (int) componentSource;
                componentSetting["componentType"] = (int) componentType;
                componentSetting["serverCategory"] = serverCategory;

                if (componentType == ComponentType.ClothShoes)
                {
                    var shoeParamsObj = new Dictionary<string, object>();
                    shoeParamsObj["heelHeight"] = shoeParams.heelHeight;
                    shoeParamsObj["soleThickness"] = shoeParams.soleThickness;
                    componentSetting["shoeParams"] = shoeParamsObj;
                }

                var qualityInfoConfig = new Dictionary<string, object>();
                
                if (lods != null)
                {
                    for (int i = 0; i < lods.Length; i++)
                    {
                        var vertsInComponent = 0;
                        var trianglesInComponent = 0;
                        
                        var rendererItems = lods[i].rendererItems;
                        
                        for (int j = 0; j < rendererItems.Length; ++j)
                        {
                            vertsInComponent += rendererItems[j].verts;
                            trianglesInComponent += rendererItems[j].triangles;
                        }
                        if (vertsInComponent != 0 && trianglesInComponent != 0)
                        {
                            var qualityLodInfoConfig = new Dictionary<string, object>();
                            qualityLodInfoConfig["verts"] = vertsInComponent;
                            qualityLodInfoConfig["triangles"] = trianglesInComponent;
                            qualityInfoConfig["lod" + i] = qualityLodInfoConfig;
                        }
                    }
                }

                if (lods != null && currentLod >= 0 && currentLod < lods.Length && lods[currentLod] != null)
                {
                    var rendererItems = lods[currentLod].rendererItems;
                    var rendererItemsArray = new Dictionary<string, object>[rendererItems.Length];
                    for (int i = 0; i < rendererItems.Length; ++i)
                    {
                        var item = new Dictionary<string, object>();
                        item["baseBodyType"] = rendererItems[i].baseBodyType;
                        item["meshName"] = rendererItems[i].meshName;
                        item["customShader"] = rendererItems[i].customShader;
                        rendererItemsArray[i] = item;
                    }
                    componentSetting["rendererItems"] = rendererItemsArray;
                }
                jsonObject["componentSetting"] = componentSetting;

                // add to config.json
                var presetConfigPath = Application.dataPath + "/" + presetConfigJsonDir + "/" + componentType.ToString() + ".json";
                if (File.Exists(presetConfigPath))
                {
                    var presetConfigJson = File.ReadAllText(presetConfigPath);
                    var presetConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(presetConfigJson);
                    if (presetConfig != null)
                    {
                        if (componentType == ComponentType.BaseBody)
                        {
                            var baseBodyConfigTemplate = ((JObject)presetConfig["baseBody"]).ToObject<Dictionary<string, object>>();
                            var meshesTemplate = ((JArray)baseBodyConfigTemplate["meshes"]).ToList<object>();
                            var meshHeadTemplate =  ((JObject)meshesTemplate[0]).ToObject<Dictionary<string, object>>();
                            var meshBodyTemplate =  ((JObject)meshesTemplate[1]).ToObject<Dictionary<string, object>>();
                            var headLayerMasks = meshHeadTemplate["meshRegionLayerMasks"];
                            var bodyLayerMasks = meshBodyTemplate["meshRegionLayerMasks"];
                            

                            var baseBodyConfig = new Dictionary<string, object>();
                            var meshes = new List<Dictionary<string, object>>();
                            
                            
                            if (lods != null && currentLod >= 0 && currentLod < lods.Length && lods[currentLod] != null)
                            {
                                var rendererItems = lods[currentLod].rendererItems;
                                for (int i = 0; i < rendererItems.Length; ++i)
                                {
                                    var mesh = new Dictionary<string, object>();
                                    mesh["name"] = rendererItems[i].meshName;
                                    if (rendererItems[i].baseBodyType == (int) BaseBodyType.Head)
                                    {
                                        mesh["meshRegionLayerMasks"] = headLayerMasks;
                                    }
                                    else
                                    {
                                        mesh["meshRegionLayerMasks"] = bodyLayerMasks;
                                    }
                                    meshes.Add(mesh);
                                }
                            }

                            baseBodyConfig["meshes"] = meshes.ToArray();
                            presetConfig["baseBody"] = baseBodyConfig;
                        }
                        presetConfig["qualityInfo"] = qualityInfoConfig;
                        jsonObject.Add("config", presetConfig);
                    }
                }
            }

            /**
             * @brief Fill asset json object. Derived class SHOULD override and invoke the method.
             */
            public override void FillAssetMetaProtoJsonObject(Dictionary<string, object> assetMetaProtoJsonObj, PaabAssetImportSettings ownerAssetSettings)
            {
                // do nothing
            }
        }
    }
}
#endif