#if UNITY_EDITOR
using System;

namespace Pico.AvatarAssetPreview.Protocol
{
    [Serializable]
    public class FileInfo
    {
        public string key;
        public string name;
        public string file_type;
        public string url;
        public int lod;
        public string md5;
    }
}
#endif