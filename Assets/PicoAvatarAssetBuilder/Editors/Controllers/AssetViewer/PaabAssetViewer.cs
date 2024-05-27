#if UNITY_EDITOR
using Pico.Avatar;
using Pico.AvatarAssetBuilder;
using Pico.Platform.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        /**
         * View for asset both remote and native asset.
         */
        [AddComponentMenu("PicoAvatarAssetBuilder/PaabAssetViewer")]
        public class PaabAssetViewer : MonoBehaviour
        {
            // avatar capabilities used to load avatar.
            public AvatarCapabilities avatarCapabilities { get; set; }

            // avatar specification text.
            [SerializeField]
            public PaabAssetBuilderConfigData assetBuilderConfigData;

            // avatar specification text.
            [SerializeField]
            public PaabAssetViewerStartData viewerStartData;

            // avatar specification text.
            [SerializeField]
            public PaabAssetViewerExitData viewerExitData;

            // temporary field
            public bool exitNow = false;

            /**
             * @brief Constructor.
             */
            public PaabAssetViewer()
            {
                if(avatarCapabilities == null)
                {
                    avatarCapabilities = new AvatarCapabilities();
                    avatarCapabilities.manifestationType = AvatarManifestationType.Full;
                    avatarCapabilities.controlSourceType = ControlSourceType.MainPlayer;
                    avatarCapabilities.bodyCulling = false;
                    avatarCapabilities.recordBodyAnimLevel = RecordBodyAnimLevel.FullBlendShape;
                    avatarCapabilities.enablePlaceHolder = false; //set Enable PlaceHolder.
                    avatarCapabilities.autoStopIK = false; //set automatically stop ik when controller is far,idle,etc.
                    avatarCapabilities.ikMode = AvatarIKMode.FullBody;
                    avatarCapabilities.flags = 0;
                    avatarCapabilities.usage = AvatarCapabilities.Usage.AllowEdit;
                }
            }

            public void Start()
            {
                //
                exitNow = false;

                if (assetBuilderConfigData == null)
                {
#if UNITY_EDITOR
                    assetBuilderConfigData = UnityEditor.AssetDatabase.LoadAssetAtPath<PaabAssetBuilderConfigData>(AssetBuilderConfig.instance.assetViwerDataAssetsPath + "Data/PaabAssetBuilderConfigData.asset");
#endif
                    if (assetBuilderConfigData == null)
                    {
                        UnityEngine.Debug.LogError("Should set viewer datas manully for PaabAssetViewer!");
                        return;
                    }
                }
                if (viewerStartData == null)
                {
                    viewerStartData = assetBuilderConfigData.viewerStartData;
                }
                if (viewerExitData == null)
                {
                    viewerExitData = assetBuilderConfigData.viewerExitData;
                }
                if (viewerStartData == null || viewerExitData == null)
                {
                    UnityEngine.Debug.LogError("Should set viewer datas manully for PaabAssetViewer!");
                    return;
                }

                //
                if (viewerStartData.assetImportSettings != null)
                {
                    var specBuilder = new AvatarSpecificationBuilder();
                    var specText = specBuilder.BuildAssetViewerAvatarSpecText(viewerStartData.assetImportSettings);
                    this.StartCoroutine(LoadViwerAvatar(specText));
                }

#if UNITY_EDITOR

#endif
            }

            public void ExitViewer()
            {
#if UNITY_EDITOR
                if (viewerExitData != null)
                {
                    viewerExitData.result = "Exited";
                    if(viewerStartData != null)
                    {
                        viewerExitData.assetImportSettings = viewerStartData.assetImportSettings;
                    }

                    UnityEditor.EditorUtility.SetDirty(viewerExitData);
                    UnityEditor.AssetDatabase.SaveAssetIfDirty(viewerExitData);
                }
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }


            public void Update()
            {
                if (exitNow)
                {
                    ExitViewer();
                }
            }

            // asset data
            private PaabAssetImportSettings _assetImportSettings;

            // avatar used to view.
            private PicoAvatar _viewerAvatar;

            private void ShowAsset()
            {

            }


            /**
             * @brief Load viewer avatar
             */
            private IEnumerator LoadViwerAvatar(string avatarSpec)
            {
                while (!PicoAvatarManager.canLoadAvatar)
                {
                    yield return null;
                }

                _viewerAvatar = PicoAvatarManager.instance.LoadAvatar(
                     new AvatarLoadContext("__AssetViewer_", "__AssetViewer_", avatarSpec, avatarCapabilities));

            }

#region Build Avatar Specification Text



#endregion
        }
    }
}
#endif