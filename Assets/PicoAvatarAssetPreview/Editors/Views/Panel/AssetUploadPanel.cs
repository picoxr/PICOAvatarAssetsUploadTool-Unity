#if UNITY_EDITOR
using Pico.Avatar;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;


namespace Pico
{
    namespace AvatarAssetPreview
    {
        /**
        * @brief upload asset to server
        */
        public class AssetUploadPanel : AssetImportSettingsPanel
        {

            // Gets singleton instance.
            public static AssetUploadPanel instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<AssetUploadPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/AssetUploadPanel.asset");
                    }
                    return _instance;
                }
            }

#region Private Fields

            private static AssetUploadPanel _instance;

#endregion

            // display name of the panel
            public override string displayName { get => "UploadAsset"; }
            public override string panelName { get => "UploadAsset"; }
            // gets uxml path name. relativ to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetPreview/Editors/Views/"
            public override string uxmlPathName { get => "Uxml/AssetUploadPanel.uxml"; }


            public override void DetachFromDOM()
            {
                base.DetachFromDOM();
            }

            protected override bool BindUIActions()
            {
                // start upload 
                {
                    var btn = this.mainElement.Q<Button>("UploadResultBackBtn");
                    btn.clicked += () =>
                    {
                        Debug.Log("upload assets...");
                        
                        //todo: upload file list, and then build create or update request params url
                        AssetUploadManager.instance.UploadAssetFile("test_image", callback =>
                        {
                            btn.text = "dasdadas";
                        });
                        
                        
                        //test download file 
                        string outputDirectoy = AvatarEnv.cacheSpacePath + "/AvatarCacheLocal/";
                        if (!Directory.Exists(outputDirectoy))
                        {
                            Directory.CreateDirectory(outputDirectoy);
                        }
                        Debug.Log(outputDirectoy);
                        string fileName = outputDirectoy + "md5xxxx.zip";

                        var url =
                            "https://lf26-effectcdn-tos.byteeffecttos.com/obj/ic-material-resource/b975c0b027d421f7c6762ac60a0bd832";

                        AssetServerManager.instance.StartDownloadFile(url, fileName, url =>
                        {
                            Debug.Log("download... finish...");
                        }, (url, progress) =>
                        {
                            Debug.Log("download...progress = " + progress);
                        }, (url, failure) =>
                        {
                            Debug.LogError(failure.ToString());
                        });
                    };
                }

                {
                    var btn = this.mainElement.Q<Button>("NextBtn");
                    btn.clicked += () => {
                        if (panelContainer != null)
                        {
                            //
                            this.UpdateToData(curImportSettings);

                            // save context.
                            this.SaveContext();
                            //
                            panelContainer.RemovePanel(this);
                            //
                            // AssetViewerStarter.StartAssetViewer(curImportSettings);
                        }
                    };
                }
                
                return base.BindUIActions();
            }

            protected override bool BuildUIDOM(VisualElement parent)
            {
                return base.BuildUIDOM(parent);
            }

            public override void OnDestroy()
            {
                base.OnDestroy();
                if (_instance == this)
                {
                    _instance = null;
                }
            }

            public override void OnEnable()
            {
                base.OnEnable();
            }

            public override void OnRemove()
            {
                base.OnRemove();
            }

            public override void OnUpdate()
            {

                base.OnUpdate();
            }

        }
    }
}

#endif