#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Pico.AvatarAssetBuilder
{
    public class AssetServerErrorCode
    {
        public const int Successful = 0;
        public static string GetErrorMsg()
        {
            return "";
        }
        public static void ShowErrorMessage(int code)
        {
            Debug.LogError("error code : " + code);
        }
    }
}

#endif