#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.iOS;

namespace Pico.AvatarAssetPreview.Protocol
{
    public class Response<T>
    {
        public string code;
        public string em;
        public string et;
        public T data;
    }
}
#endif