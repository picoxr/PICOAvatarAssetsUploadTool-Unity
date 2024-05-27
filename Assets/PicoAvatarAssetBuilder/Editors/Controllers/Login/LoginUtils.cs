#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using System;
namespace Pico.AvatarAssetBuilder
{
    /// <summary>
    /// 登陆信息本地保存,非常驻态
    /// </summary>
    public class LoginUtils
    {
        public static bool IsLogin()
        {
            var result = LoadLoginSetting();
            if (MainMenuUIManager.instance.PAAB_OPEN == false)
            {
                var resultfromPAAB = AvatarAssetBuilder.LoginUtils.LoadLoginSetting();
                if (resultfromPAAB != null)
                {
                    result = ScriptableObject.CreateInstance<PaabDeveloperInfoImportSetting>();
                    var json = JsonUtility.ToJson(resultfromPAAB);
                    JsonUtility.FromJsonOverwrite(json, result);
                    AssetImportManager.instance.assetImportDeveloperData = result;
                }
            }
            if (result == null)
                return false;
            return !result.HasToken;
        }

        private static bool _isShow = true;

        public static bool IsShow
        {
            get => _isShow;
            set => _isShow = value;
        }

        public static bool IsPlatformShow()
        {
            return _isShow;
        }

        public static bool HasBindApp()
        {
            var result = LoadLoginSetting();
            if (result == null)
                return false;
            return result.HasBindAppID;
        }
        
        public static bool HasBindInfo()
        {
            var result = LoadLoginSetting();
            if (result == null)
                return false;
            return result.HasBindInfo;
        }
        public static PaabDeveloperInfoImportSetting LoadLoginSetting()
        {
            return AssetImportManager.instance.assetImportDeveloperData;
        }

        public static void Logout()
        {
            var item = LoadLoginSetting();
            if (item == null)
                return;
            item.Logout();
            AssetImportManager.instance.SaveAssetImportSetting<PaabDeveloperInfoImportSetting>(item);
            MainWindow.Clear();
        }
        public static void SaveServerType(AssetServerType serverType)
        {
            var item = LoadLoginSetting();
            if (item == null)
                item = ScriptableObject.CreateInstance<PaabDeveloperInfoImportSetting>();
            item.InitServerType((int)serverType);
            AssetImportManager.instance.SaveAssetImportSetting<PaabDeveloperInfoImportSetting>(item);
        }

        public static void SaveLoginToken(string token, string appid)
        {
            var item = LoadLoginSetting();
            if (item == null)
                item = ScriptableObject.CreateInstance<PaabDeveloperInfoImportSetting>();
            string time = GetClientAbsTime();
            item.InitLoginToken(token, appid, time);
            AssetImportManager.instance.SaveAssetImportSetting<PaabDeveloperInfoImportSetting>(item);
        }

        public static void SaveAppInfoData(string result)
        {
            var item = LoadLoginSetting();
            if (item == null)
                item = ScriptableObject.CreateInstance<PaabDeveloperInfoImportSetting>();
            item.InitAppInfo(result);
           
            AssetImportManager.instance.SaveAssetImportSetting<PaabDeveloperInfoImportSetting>(item);
        }

        static DateTime utcStart = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        static string GetClientAbsTime()
        {
            return Convert.ToInt64((DateTime.UtcNow - utcStart).TotalSeconds).ToString();
        }
    }
}

#endif