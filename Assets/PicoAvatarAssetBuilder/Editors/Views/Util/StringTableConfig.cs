
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pico.AvatarAssetBuilder
{
#if UNITY_EDITOR
    public class StringTable
    {
        private static StringTableConfig stringTableConfig;
        private static string language = "en";
        
        
        public static string GetString(string key)
        {
            if (stringTableConfig == null)
                stringTableConfig = AssetDatabase.LoadAssetAtPath<StringTableConfig>("Assets/PicoAvatarAssetBuilder/Assets/Resources/StringTableConfig.asset");
            if (stringTableConfig == null)
                return String.Empty;
            return stringTableConfig.stringConfigs.First((entry => entry.key == key))?.values
                .First((entry => entry.language == language))?.value;
        }
    }
#endif
    
    [Serializable]
    public class StringTableEntry 
    {
        public string key;
        public List<StringLocalizeEntry> values;
    }

    [Serializable]
    public class StringLocalizeEntry
    {
        public string language;
        public string value;
    }

    [CreateAssetMenu(menuName = "Scriptable Object/StringTableConfig",order =1)]
    public class StringTableConfig : ScriptableObject
    {
        public List<StringTableEntry> stringConfigs = new List<StringTableEntry>();
    }
}

