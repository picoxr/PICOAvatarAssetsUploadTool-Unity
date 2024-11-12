#if UNITY_EDITOR
using System.Collections;
using Pico.Avatar;
using Pico.AvatarAssetPreview;
using UnityEngine;

namespace AssemblyCSharp.Assets.AmzAvatar.TestTools
{
	public class PAAPDemo:MonoBehaviour
	{
		public bool skipQAConfig = false;
		private bool m_loginPlatformSDK = false;

		private IEnumerator Start()
		{
			while (!PicoAvatarApp.isWorking)
				yield return null;
			PicoAvatarAppStart();
		}
		
		private class AppConfigData
		{
			public string channel;
			public string PicoDevelopAppId;
		}

		void PicoAvatarAppStart()
		{
			var avatarApp = PicoAvatarApp.instance;
			if (LoginUtils.IsLogin())
			{
				if (AssetImportManager.instance.assetImportDeveloperData != null)
				{
					avatarApp.loginSettings.accessToken = AssetImportManager.instance.assetImportDeveloperData.loginToken;
					avatarApp.accessType = AccessType.OwnAssetsPlatform;
					avatarApp.appSettings.serverType = AssetServerProConfig.IsBoe ? ServerType.OfflineEnv : ServerType.ProductionEnv;
#if false
					avatarApp.appSettings.serverType = AssetServerProConfig.IsBoe ? ServerType.OfflineEnv : ServerType.ProductionEnv;
					AppConfigData configData = new AppConfigData();
					configData.channel = "test";
					//configData.PicoDevelopAppId = "b9b26fc936e96819441c162da61a9294"; //os
					configData.PicoDevelopAppId = "c8e18d21899ece4ca6fdc44c1aab1370"; //test
					//configData.PicoDevelopAppId = AssetImportManager.instance.assetImportDeveloperData.appID; 
					avatarApp.extraSettings.configString = JsonUtility.ToJson(configData);
#endif
					// start PicoAvatarApp
					avatarApp.StartAvatarManager();
				}
				else
				{
					Debug.LogError("get login token failed!");
				}
			}
			else
			{
				Debug.LogError("get login token failed!");
			}

			UITools.DebugLog(
				$" userToken = {avatarApp.loginSettings.accessToken} \n" +
				$" defaultPlaybackLevel = {avatarApp.netBodyPlaybackSettings.defaultPlaybackLevel} \n" +
				$" serverType = {avatarApp.appSettings.serverType} \n"
			);
		}
	}

	public class AppConfigDataInPAAPDemo
	{
		public string channel;
		public string PicoDevelopAppId;
	}
}
#endif