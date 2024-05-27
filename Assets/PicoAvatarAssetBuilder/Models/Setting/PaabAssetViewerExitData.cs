#if UNITY_EDITOR
using Pico.Avatar;
using Pico.AvatarAssetBuilder;
using Pico.Platform.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        /**
         * @brief 
         */
        [Serializable]
        public class PaabAssetViewerExitData : ScriptableObject
        {
            // avatar specification text.
            [SerializeField]
            public string result;

            [SerializeField]
            public PaabAssetImportSettings assetImportSettings;
        }
    }
}
#endif