#if UNITY_EDITOR
using System;

namespace Pico.AvatarAssetPreview.Protocol
{
    [Serializable]
    public class Event
    {
        public string event_id;
        public int app_id;
        public string user_id;
        public int event_type;
        public long create_time;
        public long update_time;
        public int status;
        public string name;
        public string item_id;
        public string reason;
        public string content;
        public EventParam param;
    }

    [Serializable]
    public class EventParam
    {
        public string file_uniq_id;
    }
}
#endif