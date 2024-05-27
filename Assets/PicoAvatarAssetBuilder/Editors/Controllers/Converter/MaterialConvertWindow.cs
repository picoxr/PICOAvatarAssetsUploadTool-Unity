#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Pico.Avatar
{
    namespace AvatarAssetBuilder
    {
        public class MaterialConvertWindow : EditorWindow
        {
            static MaterialConvertWindow _myWindow;

            [MenuItem("AvatarSDK/AssetTools/MaterialConvert")]
            public static void ShowWindowMenu()
            {
                if (_myWindow != null)
                {
                    EditorWindow.FocusWindowIfItsOpen<MaterialConvertWindow>();
                    return;
                }

                _myWindow = GetWindow(typeof(MaterialConvertWindow), true) as MaterialConvertWindow;
                _myWindow.titleContent = new GUIContent("Material Convert");
                _myWindow.ShowUtility();
            }

            private void OnDestroy()
            {
                _myWindow = null;
            }

            void OnGUI()
            {
                DrawBaseUI();
            }

            Material sourceMaterial = null;
            Material targetMaterial = null;

            void DrawBaseUI()
            {
                // EditorGUI.BeginChangeCheck();
                {
                    {
                        sourceMaterial = (Material)EditorGUILayout.ObjectField(sourceMaterial, typeof(Material), true);
                        EditorGUILayout.Separator();
                        EditorGUILayout.Separator();
                        targetMaterial = (Material)EditorGUILayout.ObjectField(targetMaterial, typeof(Material), true);
                        EditorGUILayout.Separator();
                        EditorGUILayout.Separator();
                        if (GUILayout.Button("Converter"))
                        {
                            if (sourceMaterial == null)
                            {
                                ShowNotification(new GUIContent("No material selected for convert"));
                            }
                            else
                            {
                                targetMaterial = ConverterMaterial(sourceMaterial, targetMaterial);
                            }
                        }
                    }
                }
                // 分隔符
                EditorGUILayout.Separator();
                EditorGUILayout.Separator();
                EditorGUILayout.Separator();
                EditorGUILayout.Separator();
            }

            // tex MatchNames
            private static List<string> _BaseMapMatchNames = new List<string> { "_MainTex", "_BaseMap", "baseColorTexture" };
            private static List<string> _BumpMapMatchNames = new List<string> { "_BumpMap", "_NormalMap" };

            private static List<string> _MetallicGlossMapMatchNames =
                new List<string> { "_MetallicGlossMap", "metallicRoughnessTexture" };

            // float MatchNames
            private static List<string> _SmoothnessMatchNames = new List<string> { "_Smoothness" };
            private static List<string> _MetallicMatchNames = new List<string> { "_Metallic" };

            // color or vector MatchNames
            private static List<string> _BaseColorMatchNames = new List<string> { "_BaseColor" };

            public static Material ConverterMaterial(Material source, Material targetOfficialMaterial, OfficialShaderTheme theme = OfficialShaderTheme.PicoPBR)
            {
                string sourceMaterialPath = AssetDatabase.GetAssetPath(source);
                string sourceMaterialGuid = AssetDatabase.AssetPathToGUID(sourceMaterialPath);
                string sourceMaterialName = source.name;

                if (theme == OfficialShaderTheme.PicoPBR)
                {
                    targetOfficialMaterial = new Material(Shader.Find("PAV/URP/PicoPBR"));
                    targetOfficialMaterial.name = sourceMaterialName + "_OfficialPBR";
                }
                else if (theme == OfficialShaderTheme.PicoNPR)
                {
                    targetOfficialMaterial = new Material(Shader.Find("PAV/URP/PicoNPR"));
                    targetOfficialMaterial.name = sourceMaterialName + "_OfficialNPR";
                }

                int sourcePropertyCount = ShaderUtil.GetPropertyCount(source.shader);
                for (int i = 0; i < sourcePropertyCount; i++)
                {
                    string propertyName = ShaderUtil.GetPropertyName(source.shader, i);
                    // set Texture.
                    if (ShaderUtil.GetPropertyType(source.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        var tex = source.GetTexture(propertyName) as Texture2D;
                        if (!tex)
                            continue;

                        if (_BaseMapMatchNames.Contains(propertyName))
                        {
                            targetOfficialMaterial.SetTexture("_BaseMap", tex);
                        }
                        else if (_MetallicGlossMapMatchNames.Contains(propertyName))
                        {
                            targetOfficialMaterial.SetTexture("_MetallicGlossMap", tex);
                        }
                        else if (_BumpMapMatchNames.Contains(propertyName))
                        {
                            targetOfficialMaterial.SetTexture("_BumpMap", tex);
                        }
                    }
                    // set float.
                    else if (ShaderUtil.GetPropertyType(source.shader, i) == ShaderUtil.ShaderPropertyType.Float ||
                             ShaderUtil.GetPropertyType(source.shader, i) == ShaderUtil.ShaderPropertyType.Range)
                    {
                        var value = source.GetFloat(propertyName);
                        if (_SmoothnessMatchNames.Contains(propertyName))
                        {
                            targetOfficialMaterial.SetFloat("_Smoothness", value);
                        }
                        else if (_MetallicMatchNames.Contains(propertyName))
                        {
                            targetOfficialMaterial.SetFloat("_Metallic", value);
                        }
                    }
                    // set color.
                    else if (ShaderUtil.GetPropertyType(source.shader, i) == ShaderUtil.ShaderPropertyType.Color)
                    {
                        var value = source.GetColor(propertyName);
                        if (_BaseColorMatchNames.Contains(propertyName))
                        {
                            targetOfficialMaterial.SetColor("_Color", value);
                            targetOfficialMaterial.SetColor("_BaseColor", value);
                        }
                    }
                    // set vector.
                    else if (ShaderUtil.GetPropertyType(source.shader, i) == ShaderUtil.ShaderPropertyType.Vector)
                    {
                    }
                }

                if (sourceMaterialPath == "")
                {
                    var outDir = Application.dataPath + "/OfficialMaterial";
                    if (!Directory.Exists(outDir))
                    {
                        Directory.CreateDirectory(outDir);
                    }
                    sourceMaterialPath = "Assets/OfficialMaterial/Test.mat";
                }
                    
                // save material in sourceMaterial path.
                AssetDatabase.CreateAsset(targetOfficialMaterial, Path.GetDirectoryName(sourceMaterialPath) + "/" + targetOfficialMaterial.name + ".mat");
                return targetOfficialMaterial;
            }
        }
    }
}
#endif