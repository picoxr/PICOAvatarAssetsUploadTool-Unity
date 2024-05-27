#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public partial class MainPanel : PavPanel
        {
             protected override bool BuildUIDOM(VisualElement parent) //SetVisualElements and BuildWithUxml
            {
                base.BuildUIDOM(parent);

                if (mainElement != null)
                {
                    mainElement.style.flexGrow = 1;
                    mainElement.style.flexShrink = 1;
                    _navagationBarVE = mainElement.Q<VisualElement>(k_MainWinNavagationBarVisualElement);
                    MainMenuUIManager.instance.RegisterMainPanelPlaceholder(PlaceHolderType.NavagationBar ,_navagationBarVE);
                    //seem unuse?
                    /*_middleVE = mainElement.Q<VisualElement>(k_MainWinMiddleVisualElement);
                    if (_middleVE != null)
                    {
                        _placeholder[k_MainWinMiddleVisualElement] = _middleVE;
                    }*/
                    _menuBarVE = mainElement.Q<VisualElement>(k_MainWinMenuBarVisualElement);
                    MainMenuUIManager.instance.RegisterMainPanelPlaceholder(PlaceHolderType.PICOMenuBar ,_menuBarVE);
                    if (MainMenuUIManager.instance.PAAB_OPEN == false)
                    {
                        _menuBarVE.SetActive(false);
                    }
                    _displayVE = mainElement.Q<VisualElement>(k_MainWinDisplayVisualElement);
                    MainMenuUIManager.instance.RegisterMainPanelPlaceholder(PlaceHolderType.DisPlayArea ,_displayVE);
                    _tip = mainElement.Q<Label>(k_TipVisualElement);
                    _tipContainer = mainElement.Q(k_TipContainerVisualElement);
                    _refreshBtn = mainElement.Q<Button>(k_MainWinRefreshElement);
                    UIUtils.AddVisualElementHoverMask(_refreshBtn, _refreshBtn);
                }

                MainMenuUIManager.instance.AttachToDOMByType(TopNavMenuBar.instance, PlaceHolderType.NavagationBar, false, false);
                MainMenuUIManager.instance.AttachToDOMByType(PICOMenuBar.instance, PlaceHolderType.PICOMenuBar, true, false);
                MainMenuUIManager.instance.AttachToDOMByType(CharacterPanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(ConfigureComponentPanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(ConfigureNewCharacterPanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(SkeletonPanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(UpdateCharacterPanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(ComponentListPanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(AnimationListPanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(ConfigureBaseAnimationSetPanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(ConfigureCustomAnimationSetPanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(AssetUploadPanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(UploadSuccessPanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(UploadFailurePanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(AssetTestPanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(TestCustomAnimationListPanel.instance, PlaceHolderType.DisPlayArea);
                MainMenuUIManager.instance.AttachToDOMByType(CharacterAndPresetPanel.instance, PlaceHolderType.DisPlayArea);
                return true;
            }
        }
    }
}
#endif