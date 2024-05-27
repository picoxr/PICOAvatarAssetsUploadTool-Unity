#if UNITY_EDITOR
namespace Pico.AvatarAssetBuilder
{
    public enum ALogLevel : uint
    {
        All = 0,
        Verbose = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Fatal = 5,
        None = 999
    }

    public enum ALogMode : uint
    {
        Speed = 0,
        Safe = 1
    }

    public enum ALogCompressMode : byte
    {
        NONE = 0,
        ZLIB = 1,
        ZSTD = 2,
    }

    public enum ALogCryptMode : byte
    {
        NONE = 0,
        TEA16 = 1,
        TEA32 = 2,
        TEA64 = 3
    }

    public enum ALogTransCryptMode : byte
    {
        NONE = 0,
        SECP256K1 = 1,
        SECP256R1 = 2
    }

    public enum ALogTimeFormat : uint
    {
        RAW = 0, // "%ld.%03ld%s" = 
        ISO_8601 = 1 // 
    }

    public enum ALogPrefixFormat : uint
    {
        Default = 0, // " %d-%d %c %.128s: "
        Legacy = 1   // " [%d:%d%s][%c][%.128s][, , ]"
    }
}
#endif