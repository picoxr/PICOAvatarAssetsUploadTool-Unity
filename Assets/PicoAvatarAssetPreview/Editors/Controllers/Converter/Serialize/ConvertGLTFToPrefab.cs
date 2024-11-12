#if UNITY_EDITOR
#define THREAD_WORK

using UnityEngine;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Pico.Avatar;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class ConvertGLTFToPrefab
        {
            private static bool _init = false;
            private static bool _working = false;
            private static string _cpuInfo;

            class ThreadParams
            {
                public string appDataPath;
                public string gltfPath;
                public string prefabDir;
                public string extrasJson;
                public System.Action finishCallback;
            }

            public static void Convert(string gltfPath, string prefabDir, string extrasJson, System.Action finishCallback)
            {
                if (_working)
                {
                    return;
                }

                _working = true;
                _cpuInfo = SystemInfo.processorType.ToLower();

                var parameter = new ThreadParams();
                parameter.appDataPath = Application.dataPath;
                parameter.gltfPath = gltfPath;
                parameter.prefabDir = prefabDir;
                parameter.extrasJson = string.IsNullOrEmpty(extrasJson) ? "" : extrasJson;
                parameter.finishCallback = finishCallback;

#if THREAD_WORK
                var thread = new Thread(Work);
                thread.Start(parameter);
#else
                Work(parameter);
#endif
            }

            private static void Work(object obj)
            {
                var parameter = obj as ThreadParams;

                string dependResDirParent = parameter.appDataPath + "/../Library";
                dependResDirParent = new DirectoryInfo(dependResDirParent).FullName.Replace("\\", "/");
                string dependResDir = dependResDirParent + "/dependRes";

                if (!_init)
                {
                    string dependResZip = parameter.appDataPath + "/../" + AvatarConverter.converterRootDir + "/Serialize/dependRes.zip";
                    dependResZip = new FileInfo(dependResZip).FullName.Replace("\\", "/");
                    
                    bool dirExist = Directory.Exists(dependResDir);
                    // reuse AppSetting's version for dependRes
                    bool versionMatch = false;
                    string versionFilePath = dependResDir + "/version.txt";
                    string versionContent = AppSettings.avatarSdkVersion + "-" + AppSettings.unityPluginCommit + "-" + AppSettings.avatarUploaderVersion;
                    if (File.Exists(versionFilePath))
                    {
                        string content = File.ReadAllText(versionFilePath);
                        versionMatch = content == versionContent;
                    }
                    //
                    bool toUnzip = (!dirExist) || (!versionMatch);
                    if (toUnzip)
                    {
                        ZipUtility.UnzipFile(dependResZip, dependResDirParent);
                        File.WriteAllText(versionFilePath, versionContent);
#if UNITY_EDITOR_OSX
                        SetExeMode(dependResDir + "/astcenc/astcenc-avx2");
                        SetExeMode(dependResDir + "/astcenc/astcenc-neon");
                        SetExeMode(dependResDir + "/astcenc/textureConverter");
#endif
                    }

                    _init = true;
                }
                Debug.Assert(Directory.Exists(dependResDir));

                var gltfPath = parameter.gltfPath.Replace("\\", "/");
                var prefabDir = parameter.prefabDir.Replace("\\", "/");

                bool armNeonForMac = _cpuInfo.StartsWith("apple m");
                pav_Serialization_ConvertGLTFToPrefab(dependResDir, gltfPath, prefabDir, parameter.extrasJson, armNeonForMac);

                var files = Directory.GetFiles(prefabDir, "*.*", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    var targetZip = prefabDir + ".zip";
                    ZipUtility.Zip(files, targetZip, null, null, prefabDir);
                    File.Copy(prefabDir + "/config.json", prefabDir + "/../config.json", true);
                }

                _working = false;

                if (parameter.finishCallback != null)
                {
                    parameter.finishCallback();
                }
            }

            private static void SetExeMode(string path)
            {
                string cmd = $"chmod 777 \"{path}\"";
                using (System.Diagnostics.Process proc = System.Diagnostics.Process.Start("/bin/bash", $"-c \"{cmd}\""))
                {
                    proc.WaitForExit();
                }
            }

            [DllImport("effect", CallingConvention = CallingConvention.Cdecl)]
            private static extern void pav_Serialization_ConvertGLTFToPrefab(string dependResDir, string gltfFilePath, string prefabDirPath, string extrasJson, bool armNeon);
        }
    }
}

#endif