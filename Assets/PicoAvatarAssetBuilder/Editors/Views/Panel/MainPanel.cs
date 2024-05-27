#if UNITY_EDITOR
using System.Collections;
using Pico.Avatar;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public partial class MainPanel : PavPanel
        {
            public override string displayName { get => "MainPanel"; }
            public override string panelName { get => "MainPanel"; }
            public override string uxmlPathName { get => "Uxml/MainPanel.uxml"; }
            
            private static MainPanel _instance;
            private MessageCenter _messageCenter;

            // menu button (bottom)
            const string k_MainWinNavagationBarVisualElement = "mainwin__navigation-ve";
            const string k_MainWinMiddleVisualElement = "mainwin__middle-ve";
            const string k_MainWinMenuBarVisualElement = "mainwin__menubar-ve";
            const string k_MainWinDisplayVisualElement = "mainwin__display-ve";
            const string k_MainWinRefreshElement = "mainwin__refresh-ve";
            const string k_TipVisualElement = "mainwin__tip-ve";
            const string k_TipContainerVisualElement = "mainwin__tipcontainer-ve";
            
            private VisualElement _navagationBarVE;
            private VisualElement _middleVE;
            private VisualElement _menuBarVE;
            private VisualElement _displayVE;
            private Button _refreshBtn;
            private Label _tip;
            private VisualElement _tipContainer;
            private EditorCoroutine _tipCoroutine;
            
            // 
            public static MainPanel instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<MainPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/MainPanel.asset");
                        _instance._messageCenter = new MessageCenter();
                    }
                    return _instance;
                }
            }
            // whether singleton object has been created.
            public static bool isValid => _instance != null;
            
            public override void OnDestroy()
            {
                base.OnDestroy();

                _messageCenter.Release();
                _messageCenter = null;

                if (_instance == this)
                {
                    _instance = null;
                }

                if (CharacterManager.isValid)
                {
                    CharacterManager.instance.ClearCurrentCharacter();
                }
            }
            
            
            protected override bool BindUIActions()
            {
                NavMenuBarRoute.instance.SwitchChannelByPanelType(PanelType.CharacterPanel);
                _refreshBtn.RegisterCallback<ClickEvent>(OnRefreshBtnClick);
                MainMenuUIManager.instance.OnPanelShow -= OnPanelShow;
                MainMenuUIManager.instance.OnPanelShow += OnPanelShow;
                return base.BindUIActions();
            }

            public override void OnUpdate()
            {
                base.OnUpdate();
                if (MainMenuUIManager.isValid)
                {
                    MainMenuUIManager.instance.Update();
                }
                if (_messageCenter != null)
                {
                    _messageCenter.Update();
                }
            }

            public override void ShowTip(VisualElement target, string msg, float offsetX = 0, float offsetY = -9.67f, float tipMaxWidth = 250)
            {
                HideTip();
                _tipCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(ShowTipImpl(target, msg, offsetX, offsetY, tipMaxWidth));
            }

            public override void HideTip()
            {
                if (_tipCoroutine != null)
                    EditorCoroutineUtility.StopCoroutine(_tipCoroutine);
                
                _tip.SetVisibility(false);
                _tipCoroutine = null;
            }

            private IEnumerator ShowTipImpl(VisualElement target, string msg, float offsetX, float offsetY, float tipMaxWidth)
            {
                _tipContainer.style.width = tipMaxWidth;
                _tip.text = msg;
                _tip.MarkDirtyRepaint();
                yield return null;
                Rect localRect = new Rect(new Vector2((target.resolvedStyle.width - _tip.resolvedStyle.width) / 2f + offsetX, -_tip.resolvedStyle.height + offsetY), new Vector2(_tip.resolvedStyle.width, _tip.resolvedStyle.height));
                var finalRect = mainElement.WorldToLocal(target.LocalToWorld(localRect));
                _tip.style.left = finalRect.x ;
                _tip.style.top = finalRect.y;
                _tip.SetVisibility(true);
                _tipCoroutine = null;
            }

            private void OnRefreshBtnClick(ClickEvent evt)
            {
                if (MainMenuUIManager.instance.CurrentPanel == null || !(MainMenuUIManager.instance.CurrentPanel is IPavPanelExtra))
                    return;
                
                (MainMenuUIManager.instance.CurrentPanel as IPavPanelExtra).OnRefresh();
            }

            private void OnPanelShow(PavPanel panel)
            {
                if (panel is IPavPanelExtra)
                {
                    _refreshBtn.SetActive((panel as IPavPanelExtra).IsRefreshVisible());
                }
                else
                {
                    _refreshBtn.SetActive(false);
                }
            }
            
        }
    }
}
#endif