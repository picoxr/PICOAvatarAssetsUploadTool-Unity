#if UNITY_EDITOR
namespace Pico.AvatarAssetBuilder
{
    public class NetworkFailure
    {
        public readonly NetworkErrorType ErrorType;

        /// <summary>
        /// 错误码
        /// 根据不同的 ErrorType 有不同的含义:
        /// NetworkErrorType.Connection 时统一为 -1;
        /// NetworkErrorType.Protocol 为 http 错误码，比如404，502等；
        /// NetworkErrorType.API 为业务接口错误码，code 字段；
        ///
        /// </summary>
        public readonly int Code;

        /// <summary>
        /// 错误消息
        /// </summary>
        public readonly string ErrorMessage;

        /// <summary>
        /// 服务端详细报错信息
        /// </summary>
        public readonly string ErrorServerText;
        
        /// <summary>
        /// 业务接口状态错误信息, 仅当错误类型为 NetworkErrorType.API 时不为空
        /// </summary>
        public NetworkFailure(NetworkErrorType type, int code, string errorMessage)
        {
            ErrorType = type;
            Code = code;
            ErrorMessage = errorMessage;
        }
        
        /// <summary>
        /// 业务接口状态错误信息, 仅当错误类型为 NetworkErrorType.API 时不为空
        /// </summary>
        public NetworkFailure(CommonResponse response)
        {
            ErrorType = NetworkErrorType.API;
            Code = response.Code;
            ErrorMessage = response.ErrorMsg;
            ErrorServerText = response.ErrorServerText;
        }

        public override string ToString()
        {
            return "error type = " + ErrorType + ", error = " + ErrorMessage + ", code = " + Code +
                   ", server_detail = " + ErrorServerText;
        }
    }
}
#endif