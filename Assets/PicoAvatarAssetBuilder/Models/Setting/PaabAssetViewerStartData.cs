#if UNITY_EDITOR
using Pico.Avatar;
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
        public class PaabAssetViewerStartData : ScriptableObject
        {
            // avatar specification text.
            [SerializeField]
            public string avatarSpec;

            [SerializeField]
            public PaabAssetImportSettings assetImportSettings;
        }
    }
}
#endif