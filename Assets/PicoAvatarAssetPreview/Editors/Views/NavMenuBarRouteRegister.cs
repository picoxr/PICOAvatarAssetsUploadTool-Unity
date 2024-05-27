#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public partial class NavMenuBarRoute
        {
            public IEnumerator RegisterPanelByType(PanelType type)
            {
                yield return new EditorWaitForSeconds(Time.deltaTime);
                switch (type)
                {
                    case PanelType.None:
                        break;
                    case PanelType.TopNavMenuBar:
                        break;
                    case PanelType.MenuBar:
                        break;
                    case PanelType.PICOMenuBar:
                        break;
                    case PanelType.CharacterPanel:
                        MainMenuUIManager.instance.AttachToDOMByType(CharacterPanel.instance,
                            PlaceHolderType.DisPlayArea);
                        break;
                    case PanelType.ConfigureComponent:
                        MainMenuUIManager.instance.AttachToDOMByType(ConfigureComponentPanel.instance,
                            PlaceHolderType.DisPlayArea);
                        break;
                    case PanelType.ConfigureNewCharacter:
                        MainMenuUIManager.instance.AttachToDOMByType(ConfigureNewCharacterPanel.instance,
                            PlaceHolderType.DisPlayArea);
                        break;
                    case PanelType.SkeletonPanel:
                        MainMenuUIManager.instance.AttachToDOMByType(SkeletonPanel.instance,
                            PlaceHolderType.DisPlayArea);
                        break;
                    case PanelType.UpdateCharacter:
                        MainMenuUIManager.instance.AttachToDOMByType(UpdateCharacterPanel.instance,
                            PlaceHolderType.DisPlayArea);
                        break;
                    case PanelType.ComponentListPanel:
                        MainMenuUIManager.instance.AttachToDOMByType(ComponentListPanel.instance,
                            PlaceHolderType.DisPlayArea);
                        break;
                    case PanelType.AnimationListPanel:
                        MainMenuUIManager.instance.AttachToDOMByType(AnimationListPanel.instance,
                            PlaceHolderType.DisPlayArea);
                        break;
                    case PanelType.ConfigureBaseAnimationSet:
                        MainMenuUIManager.instance.AttachToDOMByType(ConfigureBaseAnimationSetPanel.instance,
                            PlaceHolderType.DisPlayArea);
                        break;
                    case PanelType.ConfigureCustomAnimationSet:
                        MainMenuUIManager.instance.AttachToDOMByType(ConfigureCustomAnimationSetPanel.instance,
                            PlaceHolderType.DisPlayArea);
                        break;
                    case PanelType.UploadSuccess:
                        MainMenuUIManager.instance.AttachToDOMByType(UploadSuccessPanel.instance,
                            PlaceHolderType.DisPlayArea);
                        break;
                    case PanelType.UploadFailure:
                        MainMenuUIManager.instance.AttachToDOMByType(UploadFailurePanel.instance,
                            PlaceHolderType.DisPlayArea);
                        break;
                    case PanelType.AssetTestPanel:
                        MainMenuUIManager.instance.AttachToDOMByType(AssetTestPanel.instance,
                            PlaceHolderType.DisPlayArea);
                        break;
                    case PanelType.CharacterAndPresetPanel:
                        MainMenuUIManager.instance.AttachToDOMByType(CharacterAndPresetPanel.instance,
                            PlaceHolderType.DisPlayArea);
                        break;
                    case PanelType.UploadDialog:
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }

            public IEnumerator RegisterPanelByPanelName(string panelName)
            {
                if (Enum.TryParse(panelName, out PanelType type))
                {
                    yield return RegisterPanelByType(type);
                }
            }
        }
    }
}
#endif