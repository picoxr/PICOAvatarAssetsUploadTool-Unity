#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Pico.Avatar;
using Unity.Collections;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class UploadSuccessPanel : PavPanel
        {
            public override string displayName
            {
                get => "UploadSuccess";
            }
            public override string panelName
            {
                get => "UploadSuccess";
            }
            public override string uxmlPathName
            {
                get => "Uxml/UploadSuccessPanel.uxml";
            }

            private static UploadSuccessPanel _instance;

            public static UploadSuccessPanel instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<UploadSuccessPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/UploadSuccessPanel.asset");
                    }

                    return _instance;
                }
            }
            private const string ConfirmButtonName = "Upload-Success-Platform";
            private const string GoOnButtonName = "Upload-Success-Another";

            private Button _confirm;
            private Button _goon;
            protected override bool BuildUIDOM(VisualElement parent)
            {
                base.BuildUIDOM(parent);
                if (mainElement != null)
                {
                    _confirm = mainElement.Q<Button>(ConfirmButtonName);
                    _goon = mainElement.Q<Button>(GoOnButtonName);
                }
                return true;
            }

            protected override bool BindUIActions()
            {
                _confirm?.RegisterCallback<ClickEvent>(JumpToWebServer);
                _goon?.RegisterCallback<ClickEvent>(BackToCharacterPanel);
                return base.BindUIActions();
            }
            
            
            private void JumpToWebServer(ClickEvent evt)
            {
                DealBtnClickByType(true);
            }
            
            private void BackToCharacterPanel(ClickEvent evt)
            {
                DealBtnClickByType();
            }

            private void DealBtnClickByType(bool isJump = false)
            {
                var operType = UploadAssetsPanel.instance.sourceType;
                switch (operType)
                {
                    case UploadAssetsPanel.SourceType.CreateSkeleton:
                    case UploadAssetsPanel.SourceType.CreateAnimationSet:
                    case UploadAssetsPanel.SourceType.CreateBaseBody:
                    case UploadAssetsPanel.SourceType.UpdateSkeleton:
                    case UploadAssetsPanel.SourceType.UpdateAnimationSet:
                    case UploadAssetsPanel.SourceType.UpdateBaseBody:
                    case UploadAssetsPanel.SourceType.CreateCharacter:
                    case UploadAssetsPanel.SourceType.UpdateCharacter:
                        if (isJump)
                        {
                            var setting = LoginUtils.LoadLoginSetting();
                            AssetServerManager.instance.OpenDeveloperAppWebsite(
                                string.Format(AssetServerProConfig.CreateOrUpdateCharacterWeb, 
                                    setting.orzID,setting.appID,setting.userID,AssetUploadManager.instance.newUploadAsset.character_id));
                        }
                        
                        NavMenuBarRoute.instance.BackToTargetWithoutConfirm("CharacterPanel", "CharacterPanel");
                        break;
                    case UploadAssetsPanel.SourceType.CreateComponent:
                    case UploadAssetsPanel.SourceType.UpdateComponent:
                        if (isJump)
                        {
                            var setting = LoginUtils.LoadLoginSetting();
                            AssetServerManager.instance.OpenDeveloperAppWebsite(
                                string.Format(AssetServerProConfig.CreateOrUpdateComponentWeb, 
                                    setting.orzID,setting.appID,setting.userID,AssetUploadManager.instance.newUploadAsset.asset_id,
                                    AssetUploadManager.instance.newUploadAsset.character_id));
                        }
                        // else
                        // {
                        //     NavMenuBarRoute.instance.BackToTargetWithoutConfirm("CharacterPanel", "ComponentListPanel");
                        // }
                        NavMenuBarRoute.instance.BackToTargetWithoutConfirm("CharacterPanel", "ComponentListPanel");
                        break;
                    case UploadAssetsPanel.SourceType.CreateCustomAnimationSet:
                    case UploadAssetsPanel.SourceType.UpdateCustomAnimationSet:
                        if (isJump)
                        {
                            var setting = LoginUtils.LoadLoginSetting();
                            AssetServerManager.instance.OpenDeveloperAppWebsite(
                                string.Format(AssetServerProConfig.CreateOrUpdateCustomAniWeb, 
                                    setting.orzID,setting.appID,setting.userID,AssetUploadManager.instance.newUploadAsset.asset_id,
                                    AssetUploadManager.instance.newUploadAsset.character_id));
                        }
                        // else
                        // {
                        //     NavMenuBarRoute.instance.BackToTargetWithoutConfirm("CharacterPanel", "AnimationListPanel");
                        // }
                        NavMenuBarRoute.instance.BackToTargetWithoutConfirm("CharacterPanel", "AnimationListPanel");
                        break;
                    case UploadAssetsPanel.SourceType.CreateHair:
                    case UploadAssetsPanel.SourceType.UpdateHair:
                        break;
                    case UploadAssetsPanel.SourceType.CreateShoe:
                    case UploadAssetsPanel.SourceType.UpdateShoe:
                        break;
                    case UploadAssetsPanel.SourceType.CreateClothes:
                    case UploadAssetsPanel.SourceType.UpdateClothes:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public override void OnShow()
            {
                base.OnShow();
                NavMenuBarRoute.instance.LockNavBtnClick = true;
            }

            public override void OnDestroy()
            {
                base.OnDestroy();
                if (NavMenuBarRoute.isValid)
                { 
                    NavMenuBarRoute.instance.LockNavBtnClick = false;
                }
                _instance = null;
            }
        }
    }
}
#endif