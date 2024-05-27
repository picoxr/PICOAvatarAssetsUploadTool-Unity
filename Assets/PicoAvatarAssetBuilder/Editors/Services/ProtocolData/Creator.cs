﻿#if UNITY_EDITOR
using System;

namespace Pico.AvatarAssetBuilder.Protocol
{
    [Serializable]
    public class Creator
    {
        public string id;
        public string name;
        public string avatar_url;
    }
}
#endif