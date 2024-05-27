#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.iOS;

namespace Pico.AvatarAssetBuilder.Protocol
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