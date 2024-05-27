#if UNITY_EDITOR
using Newtonsoft.Json;
using Pico.AvatarAssetPreview.Protocol;

namespace Pico.AvatarAssetPreview
{
    public class CommonResponse
    {
        [JsonProperty(PropertyName = "code")]
        public int Code;

        [JsonProperty(PropertyName = "em")] // 错误码对应的错误信息（通用信息）
        public string ErrorMsg;

        [JsonProperty(PropertyName = "et")] // 服务端详细报错信息
        public string ErrorServerText;

        [JsonProperty(PropertyName = "data")]
        public object data;
    }
    
    
    public class CommonUploadResponse
    {
        [JsonProperty(PropertyName = "code")]
        public int Code;

        [JsonProperty(PropertyName = "em")] // 错误码对应的错误信息（通用信息）
        public string ErrorMsg;

        [JsonProperty(PropertyName = "et")] // 服务端详细报错信息
        public string ErrorServerText;

        [JsonProperty(PropertyName = "data")]
        public FileInfo FileInfo;
    }
}
#endif