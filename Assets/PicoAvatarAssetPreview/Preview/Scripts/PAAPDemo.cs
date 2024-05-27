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

		void PicoAvatarAppStart()
		{
			var avatarApp = PicoAvatarApp.instance;
			if (LoginUtils.IsLogin())
			{
				if (AssetImportManager.instance.assetImportDeveloperData != null)
				{
					avatarApp.loginSettings.accessToken =
						AssetImportManager.instance.assetImportDeveloperData.loginToken;
					avatarApp.accessType = AccessType.OwnAssetsPlatform;
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