#if UNITY_EDITOR
using System.Collections.Generic;
using Pico.Avatar;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class UploadFailurePanel : PavPanel
        {
            public override string displayName
            {
                get => "UploadFailure";
            }
            public override string panelName
            {
                get => "UploadFailure";
            }
            public override string uxmlPathName
            {
                get => "Uxml/UploadFailurePanel.uxml";
            }

            private static UploadFailurePanel _instance;

            public static UploadFailurePanel instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<UploadFailurePanel>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/UploadFailurePanel.asset");
                    }

                    return _instance;
                }
            }

            private const string BackButtonName = "Upload-Failure-Back";
            private const string CancelButtonName = "Upload-Failure-Cancel";
            private const string ReasonLabelName = "Upload-Failure-Reason";
            private Button _back;
            private Button _cancel;

            private Label _reason;
            protected override bool BuildUIDOM(VisualElement parent)
            {
                base.BuildUIDOM(parent);
                if (mainElement != null)
                {
                    _back = mainElement.Q<Button>(BackButtonName);
                    _cancel = mainElement.Q<Button>(CancelButtonName);
                    _reason = mainElement.Q<Label>(ReasonLabelName);
                }
                return true;
            }

            protected override bool BindUIActions()
            {
                _back?.RegisterCallback<ClickEvent>(GoBackToFix);
                _cancel?.RegisterCallback<ClickEvent>(CancelUpload);
                return base.BindUIActions();
            }


            private void GoBackToFix(ClickEvent evt)
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
                        NavMenuBarRoute.instance.BackToTargetWithoutConfirm("CharacterPanel", "ConfigureNewCharacter", new HashSet<string>(){"ConfigureComponent", "ConfigureCustomAnimationSet", "SkeletonPanel"});
                        
                        break;
                    case UploadAssetsPanel.SourceType.CreateComponent:
                    case UploadAssetsPanel.SourceType.UpdateComponent:
                        NavMenuBarRoute.instance.BackToTargetWithoutConfirm("CharacterPanel", "ConfigureComponent");
                        break;
                    case UploadAssetsPanel.SourceType.CreateCustomAnimationSet:
                    case UploadAssetsPanel.SourceType.UpdateCustomAnimationSet:
                        NavMenuBarRoute.instance.BackToTargetWithoutConfirm("CharacterPanel", "ConfigureCustomAnimationSet");
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
                    case UploadAssetsPanel.SourceType.UpdateCharacter:
                        NavMenuBarRoute.instance.BackToTargetWithoutConfirm("CharacterPanel", "UpdateCharacter", new HashSet<string>(){"ConfigureComponent", "ConfigureCustomAnimationSet", "SkeletonPanel"});
                        break;
                }
            }
            
            private void CancelUpload(ClickEvent evt)
            {
                NavMenuBarRoute.instance.BackToTargetWithoutConfirm("CharacterPanel", "AssetTestPanel");
            }


            public void FillFailureContent(string failureReason)
            {
                _reason.text = failureReason;
            }
            
            public override void OnShow()
            {
                base.OnShow();
                if (NavMenuBarRoute.isValid)
                {
                    NavMenuBarRoute.instance.LockNavBtnClick = true;
                }
            }
            public override void  OnDestroy()
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