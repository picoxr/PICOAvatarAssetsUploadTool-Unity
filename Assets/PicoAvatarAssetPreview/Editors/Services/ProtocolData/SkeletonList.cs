#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Pico.AvatarAssetPreview.Protocol
{
    [Serializable]
    public class SkeletonList
    {
        public List<SkeletonListItem> skeletons;
    }
    
    [Serializable]
    public class SkeletonListItem
    {
        public AssetInfo asset_info;
        public Event @event;
        public AssetCategoryEntry category;
        public List<Label> labels;
        public Creator creator;
    }
}
#endif