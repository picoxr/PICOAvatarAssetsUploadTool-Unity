#if UNITY_EDITOR
namespace Pico.AvatarAssetBuilder
{
    public class ALogItem
    {
        public ALogLevel level;
        public string tag;
        public string msg;
        public string stackTrace;
        
        public int tid;
        public ulong timestamp;

        public ALogItem next;

        public void Reset()
        {
            level = ALogLevel.None;
            tag = null;
            msg = null;
            stackTrace = null;
            tid = -1;
            timestamp = 0;
            next = null;
        }
    }
}
#endif