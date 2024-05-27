#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Pico.AvatarAssetBuilder.Protocol
{
    [Serializable]
    public class AssetInfo
    {
        public string cover;
        public string show_name;
        public int status;
        public string asset_id;
        public string name;
        public int order_num;
        public string offline_config;
        public List<FileInfo> files;
        public string online_config;
        public long update_time;
        public string create_pico_app_id;
    }
    
    [Serializable]
    public class NewAssetData
    {
        public string cover;
        public string show_name;
        public string name;
        public string offline_config;
        public List<FileInfo> files;
    }
    
    [Serializable]
    public class UpdateAssetData
    {
        public string cover;
        public string show_name;
        public string offline_config;
        public List<FileInfo> files;
    }

    //
    [Serializable]
    public class AssetCategory
    {
        public List<AssetCategoryGroup> data;
        public long total_count;
        public int count;
    }
    
    [Serializable]
    public class AssetCategoryEntry
    {
        public string key;
        public string name;
        public int asset_type;
        public AssetExtra extra;
    }

    public class AssetExtra
    {
        public bool can_create_preset;
        public long total_count;
        public int count;
    }

    [Serializable]
    public class AssetCategoryGroup
    {
        public AssetCategoryEntry category;
        public List<AssetCategoryEntry> subs;
    }
    
    //
    [Serializable]
    public class AssetList
    {
        public int count;
        public int total_count;
        public List<ServerAssetData> assets;
    }
    
    //
    [Serializable]
    public class ReqAssetList
    {
        public List<string> category_keys;
        public short need_paging;
        public string character_id;
    }
}
#endif