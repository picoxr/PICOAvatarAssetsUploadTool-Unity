#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Pico.AvatarAssetBuilder.Protocol
{
    [Serializable]
    public class CharacterBase
    {
        public string name;
        public string show_name;
        public string cover;
        public string config;
    }
    
    [Serializable]
    public class CharacterUpdateBase
    {
        public string show_name;
        public string cover;
        public string config;
    }
    
    
    [Serializable]
    public class CharacterList
    {
        public int count;
        public int total_count;
        public List<CharacterInfo> characters;
    }
    
    
    [Serializable]
    public class CharacterInfo
    {
        public CharacterBaseInfo character;
        public AssetInfo skeleton;
        public AssetInfo base_body;
        public AssetInfo base_animation_set;
        public CharacterApp app;
    }
    
    
    [Serializable]
    public class CharacterBaseInfo
    {
        public string character_id;
        public string name;
        public string show_name;
        public string cover;
        public string skeleton_id;
        public string base_animation_set_id;
        public string base_body_id;
        public string create_pico_app_id;
        public int status;
        public long order_num;
        public long create_time;
        public long update_time;
        public string config;
        public int item_online_version;
        public string avatar_style;
    }

    [Serializable]
    public class CharacterApp
    {
        public string pico_app_id;
        public string name;
        public bool is_official;
        public bool upload_official_component;
    }
    
    
    // preset Data
    [Serializable]
    public class PresetList
    {
        public int count;
        public int total_count;
        public List<PresetInfo> presets;
    }
    
        
    [Serializable]
    public class PresetInfo
    {
        public PresetBaseInfo preset;
    }
    
    [Serializable]
    public class PresetBaseInfo
    {
        public String preset_id;
        public String show_name;
        public String config;
        public String cover;
    }
    
}
#endif