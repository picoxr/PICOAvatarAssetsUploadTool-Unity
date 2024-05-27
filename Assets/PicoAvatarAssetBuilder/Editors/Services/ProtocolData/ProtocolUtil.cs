#if UNITY_EDITOR
using UnityEngine;
using Newtonsoft.Json;

namespace Pico.AvatarAssetBuilder.Protocol
{
    public class ProtocolUtil
    {
        public static Response<T> GetResponse<T>(string response) where T : class
        {
            var obj = JsonUtility.FromJson<Response<T>>(response);
            if (obj == null)
            {
                Debug.LogError("Parse response failed");
                return null;
            }

            return obj;
        }
    }
}
#endif