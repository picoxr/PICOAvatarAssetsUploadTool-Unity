#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Pico.AvatarAssetBuilder.Protocol
{
    [Serializable]
    public class BaseAnimationSetList
    {
        public int count;
        public int total_count;
        public List<BaseAnimationSetListItem> assets;
    }
    
    
    [Serializable]
    public class BaseAnimationSetListItem
    {
        public AssetInfo asset_info;
        public int item_type;
    }
    
    
    [Serializable]
    public class CustomAnimationSetList
    {
        public int count;
        public int total_count;
        public List<ServerAssetData> assets;
    }

    
    [Serializable]
    public class ServerAssetData
    {
        public AssetInfo asset_info;
        public Event @event;
        public AssetCategoryEntry category;
        public List<Label> labels;
        public Creator creator;
        public int item_type;
    }
}
#endif