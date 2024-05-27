#if UNITY_EDITOR
namespace Pico.AvatarAssetBuilder
{
    public enum NetworkErrorType
    {
        /// <summary>
        /// 网络连接异常，比如无网络，或连接超时等
        /// </summary>
        Connection = 0,

        /// <summary>
        ///  协议错误，比如404，502错误
        /// </summary>
        Protocol = 1,

        /// <summary>
        /// 业务接口错误，返回 code 非 0 的情况
        /// </summary>
        API = 2
    }
}
#endif