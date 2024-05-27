#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetBuilder
    {


        public partial class NavMenuBarRoute
        {
            //channel=>(panel,navbutton)
            private Dictionary<string, NavChannelEntry> _navChannelMap = new();

            //panelname => channel
            private Dictionary<string, string> _panelChannelMap = new();
            private static NavMenuBarRoute _instance;

            private readonly Color _navSelectStyle = new(1f, 1f, 1f, 1);
            private readonly Color _navUnSelectStyle = new(1f, 1f, 1f, 0.4f);

            public static NavMenuBarRoute instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = new NavMenuBarRoute();
                        //_instance.Initialize();
                    }

                    return _instance;
                }
            }
            // Query whether _instance is created. 
            public static bool isValid => _instance != null;

            public bool LockNavBtnClick
            {
                get => _lockNavBtnClick;
                set => _lockNavBtnClick = value;
            }

            private string _currChannelName = string.Empty;
            private string _currPanelName = string.Empty;
            private bool _lockNavBtnClick =false ;
            /// <summary>
            /// each menubar first panel entry, called channel
            /// </summary>
            /// <param name="PanelType">panel type</param>
            public void SwitchChannelByPanelType(PanelType type)
            {
                SwitchchannelByPanelName(type.ToString());
            }

            /// <summary>
            /// each menubar first panel entry, called channel
            /// </summary>
            /// <param name="channelName">menubar  first panel name</param>
            public void SwitchchannelByPanelName(string channelName)
            {
                if (_currChannelName == channelName)
                {
                    return;
                }
                CommonDialogWindow.CloseSelf();
                Stack<PavPanel> stackPanel = null;
                Stack<Button> stackBtn = null;
                if (_navChannelMap.ContainsKey(_currChannelName))
                {
                    var beforeRoute = _navChannelMap[_currChannelName].panelAndBtnStack;
                    foreach (var route in beforeRoute)
                    {
                        stackPanel = route.Key;
                        stackBtn = route.Value;
                        //show top
                        stackPanel.Peek().HidePanel();
                        int i = 0;
                        //show nav button
                        foreach (var button in stackBtn)
                        {
                            button.style.color = i == 0 ? _navSelectStyle : _navUnSelectStyle;
                            button.style.unityFontStyleAndWeight = 
                                i == 0?new StyleEnum<FontStyle>(FontStyle.Bold):new StyleEnum<FontStyle>(FontStyle.Normal);
                            button.style.display = DisplayStyle.None;
                            int index = button.parent.IndexOf(button) - 1;
                            if (index > 0)
                            {
                                //change nav lable status
                                if (button.parent[index].GetType() == typeof(Label))
                                {
                                    button.parent[index].style.display = DisplayStyle.None;
                                }
                            }
                            i++;
                        }
                    }
                }

                _currChannelName = channelName;
                if (_navChannelMap.ContainsKey(_currChannelName))
                {
                    var nowRoute = _navChannelMap[_currChannelName].panelAndBtnStack;

                    foreach (var route in nowRoute)
                    {
                        stackPanel = route.Key;
                        stackBtn = route.Value;
                        if (stackPanel.Count > 0)
                        {
                            //show top
                            stackPanel.Peek().ShowPanel();
                        }

                        int i = 0;
                        //show nav button
                        foreach (var button in stackBtn)
                        {
                            button.style.color = i == 0 ? _navSelectStyle : _navUnSelectStyle;
                            button.style.unityFontStyleAndWeight = 
                                i == 0?new StyleEnum<FontStyle>(FontStyle.Bold):new StyleEnum<FontStyle>(FontStyle.Normal);
                            button.style.display = DisplayStyle.Flex;
                            int index = button.parent.IndexOf(button) - 1;
                            if (index > 0)
                            {
                                //change nav lable status
                                if (button.parent[index].GetType() == typeof(Label))
                                {
                                    button.parent[index].style.display = DisplayStyle.Flex;
                                }
                            }
                            i++;
                        }
                    }

                    return;
                }

                //new enter
                var newRoute = new Dictionary<Stack<PavPanel>, Stack<Button>>();
                var panelStack = new Stack<PavPanel>();
                var btnStack = new Stack<Button>();

                var targetPanel = MainMenuUIManager.instance.GetPavPanelByPanelName(_currChannelName);
                if (targetPanel == null)
                {
                    Debug.LogError("target panel not found, please check channel name!");
                    return;
                }
                _currPanelName = _currChannelName;
                panelStack.Push(targetPanel);
                MainMenuUIManager.instance.ShowPanelByPanelName(targetPanel.panelName);
                //add pair button
                TopNavMenuBar.instance.mainElement.style.display = DisplayStyle.Flex;
                btnStack.Push(TopNavMenuBar.instance.OnGenerateButtonAndArrow(NavButtonClick, _currChannelName,
                    targetPanel.displayName, targetPanel.panelName));
                newRoute.Add(panelStack, btnStack);
                _navChannelMap[_currChannelName] = new NavChannelEntry(newRoute);
            }


            /// <summary>
            /// used to trigegr next panel in channel
            /// </summary>
            /// <param name="fromPaneltype">origin panel type</param>
            /// <param name="toPaneltype">destination panel type</param>
            public PavPanel RouteNextByType(PanelType fromPaneltype, PanelType toPaneltype)
            {
                return RouteNextByPanelName(fromPaneltype.ToString(), toPaneltype.ToString());
            }

            /// <summary>
            /// used to trigegr next panel in channel
            /// </summary>
            /// <param name="fromPanelName">origin panel name</param>
            /// <param name="toPanelType">destination panel type</param>
            public PavPanel RouteNextByType(string fromPanelName, PanelType toPanelType)
            {
                return RouteNextByPanelName(fromPanelName, toPanelType.ToString());
            }

            
            /// <summary>
            /// used to trigegr next panel in channel
            /// </summary>
            /// <param name="fromPanelName">origin panel panelname</param>
            /// <param name="toPanelName">destination panel panelname</param>
            public PavPanel RouteNextByPanelName(string fromPanelName, string toPanelName)
            {
                //now must have channel
                if (string.IsNullOrEmpty(_currChannelName))
                {
                    Debug.LogError("no panel channel info!");
                    return null;
                }
                CommonDialogWindow.CloseSelf();
                var fromPanel = MainMenuUIManager.instance.GetPavPanelByPanelName(fromPanelName);
                if (fromPanel != null)
                {
                    fromPanel.HidePanel();
                }

                var route = _navChannelMap[_currChannelName].panelAndBtnStack;
                if (route == null)
                {
                    Debug.LogError("something wrong!");
                    return null;
                }

                var targetPanel = MainMenuUIManager.instance.GetPavPanelByPanelName(toPanelName);
                if (targetPanel == null)
                {
                    Debug.LogError("targetPanel not found!");
                    return null;
                }

                _currPanelName = toPanelName;
                MainMenuUIManager.instance.ShowPanelByPanelName(targetPanel.panelName);
                route.First().Key.Push(targetPanel);
                var stackBtn = route.First().Value;
                if (stackBtn.Count > 0)
                {
                    var lastBtn = stackBtn.Peek();
                    lastBtn.style.color = _navUnSelectStyle;
                    lastBtn.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Normal);
                }
              
                stackBtn.Push(TopNavMenuBar.instance.OnGenerateButtonAndArrow(NavButtonClick, _currChannelName,
                        targetPanel.displayName, targetPanel.panelName,true));

                _navChannelMap[_currChannelName].RecordPanel(targetPanel);
                return targetPanel;
            }

            public void RouteAny(string fromPanelName, string toPanelName)
            {
                //todo
            }

            /// <summary>
            /// route to target panel by nav button
            /// </summary>
            /// <param name="channelName">from which channel</param>
            /// <param name="panelName">which panel</param>
            public void NavButtonClick(string channelName, string panelName)
            {
                if (_currChannelName != channelName || _currPanelName == panelName || _lockNavBtnClick)
                    return;

                CommonDialogWindow.CloseSelf();
                if (Enum.TryParse(panelName, out PanelType type))
                {
                    if (type == PanelType.CharacterPanel 
                        && MainMenuUIManager.instance.PAAB_OPEN
                        && _navChannelMap[_currChannelName].panelAndBtnStack.First().Key.Count > 1)
                    {

                        var panelExtra = MainMenuUIManager.instance.CurrentPanel as IPavPanelExtra;
                        if (panelExtra != null && !panelExtra.CheckNavShowWarningWhenSelfIsShow())
                        {
                            BackToTargetWithoutConfirm(channelName, panelName);
                            return;
                        }
                        
                        //spec rules for home btn
                        CommonDialogWindow.ShowPopupConfirmDialog(() =>
                            {
                                var nowRoute = _navChannelMap[_currChannelName].panelAndBtnStack;
                                CharacterManager.instance.ClearCurrentCharacter();
                                foreach (var route in nowRoute)
                                {
                                    var stackPanel = route.Key;
                                    var stackBtn = route.Value;
                                    if (stackPanel == null || stackBtn == null)
                                        continue;
                    
                                    while (stackPanel.Count > 0)
                                    {
                                        var panel = stackPanel.Peek();
                                        var btn = stackBtn.Peek();
                                        var parent = btn.parent;
                                        if (panel.panelName != panelName)
                                        {
                                            stackPanel.Pop();
                                            int index = parent.IndexOf(btn);
                                            btn.style.color = _navUnSelectStyle;
                                            parent.RemoveAt(index); //remove nav button
                                            if (index - 1 > 0)
                                            {
                                                parent.RemoveAt(index - 1); //remove nav lable
                                            }

                                            stackBtn.Pop();
                                            if (stackBtn.Count <= 0)
                                            {
                                                TopNavMenuBar.instance.mainElement.style.display = DisplayStyle.None;
                                            }
                                        }
                                        else
                                        {
                                            btn.style.color = _navSelectStyle;
                                            btn.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                                            int index = parent.IndexOf(btn);
                                            if (index - 1 > 0)
                                            {
                                                
                                            }
                                            _currPanelName = panel.panelName;
                                            MainMenuUIManager.instance.ShowPanelByPanelName(panel.panelName);
                                            break;
                                        }
                                    }
                                }

                                var panelToRemove = _navChannelMap[_currChannelName].GetUnusedPanel(null);
                                foreach (var panel in panelToRemove)
                                {
                                    var pName = panel.panelName;
                                    panel.OnDestroy();
                                    EditorCoroutineUtility.StartCoroutine(
                                        RegisterPanelByPanelName(pName), this);
                                }
                                
                            }, null,
                            "Closing the current window will result in loss of configuration data, are you sure you want to exit?");
                    }
                    else
                    {
                        var nowRoute = _navChannelMap[_currChannelName].panelAndBtnStack;
                        foreach (var route in nowRoute)
                        {
                            var stackPanel = route.Key;
                            var stackBtn = route.Value;
                            if (stackPanel == null || stackBtn == null)
                                continue;

                            while (stackPanel.Count > 0)
                            {
                                var panel = stackPanel.Peek();
                                var btn = stackBtn.Peek();
                                var parent = btn.parent;
                                if (panel.panelName != panelName)
                                {
                                    panel.HidePanel(); //maybe need clear function
                                    stackPanel.Pop();
                                    //btn.RemoveFromHierarchy(); //remove nav button
                                    int index = parent.IndexOf(btn);
                                    btn.style.color = _navUnSelectStyle;
                                    parent.RemoveAt(index); //remove nav button
                                    if (index - 1 > 0)
                                    {
                                        parent.RemoveAt(index - 1); //remove nav lable
                                    }

                                    stackBtn.Pop();
                                    if (stackBtn.Count <= 0)
                                    {
                                        TopNavMenuBar.instance.mainElement.style.display = DisplayStyle.None;
                                    }
                                }
                                else
                                {
                                    btn.style.color = _navSelectStyle;
                                    btn.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                                    _currPanelName = panel.panelName;
                                    MainMenuUIManager.instance.ShowPanelByPanelName(panel.panelName);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            /// <summary>
            /// BackToTargetWithoutConfirm, notice: no confirm dialog and destroy no target panel
            /// </summary>
            /// <param name="channelName"></param>
            /// <param name="panelName"></param>
            public void BackToTargetWithoutConfirm(string channelName, string panelName, HashSet<string> ignoreDestroyPanels = null)
            {
                if (_currChannelName != channelName || _currPanelName == panelName)
                    return;

                CommonDialogWindow.CloseSelf();
                var nowRoute = _navChannelMap[_currChannelName].panelAndBtnStack;
                foreach (var route in nowRoute)
                {
                    var stackPanel = route.Key;
                    var stackBtn = route.Value;
                    if (stackPanel == null || stackBtn == null)
                        continue;

                    while (stackPanel.Count > 0)
                    {
                        var panel = stackPanel.Peek();
                        var btn = stackBtn.Peek();
                        var parent = btn.parent;
                        if (panel.panelName != panelName)
                        {
                            stackPanel.Pop();
                            int index = parent.IndexOf(btn);
                            btn.style.color = _navUnSelectStyle;
                            parent.RemoveAt(index); //remove nav button
                            if (index - 1 > 0)
                            {
                                parent.RemoveAt(index - 1); //remove nav lable
                            }

                            stackBtn.Pop();
                            if (stackBtn.Count <= 0)
                            {
                                TopNavMenuBar.instance.mainElement.style.display = DisplayStyle.None;
                            }
                        }
                        else
                        {
                            btn.style.color = _navSelectStyle;
                            btn.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                            _currPanelName = panel.panelName;
                            MainMenuUIManager.instance.ShowPanelByPanelName(panel.panelName);
                            break;
                        }
                    }
                }
                
                var panelToRemove = _navChannelMap[_currChannelName].GetUnusedPanel(ignoreDestroyPanels);
                foreach (var panel in panelToRemove)
                {
                    var pName = panel.panelName;
                    panel.OnDestroy();
                    EditorCoroutineUtility.StartCoroutine(
                        RegisterPanelByPanelName(pName), this);
                }
            }
            public void Clear()
            {
                _navChannelMap.Clear();
                _currChannelName = string.Empty;
                _currPanelName = string.Empty;
            }
        }
        
        public class NavChannelEntry
        {
            public Dictionary<Stack<PavPanel>, Stack<Button>> panelAndBtnStack = new ();
            private List<PavPanel> panelTrace = new ();

            public NavChannelEntry(Dictionary<Stack<PavPanel>, Stack<Button>> stack)
            {
                panelAndBtnStack = stack;
            }

            public void RecordPanel(PavPanel panel)
            {
                if (!panelTrace.Contains(panel))
                    panelTrace.Add(panel);
            }

            public List<PavPanel> GetUnusedPanel(HashSet<string> ignoreDestroyPanels)
            {
                HashSet<PavPanel> currentPanels = new HashSet<PavPanel>();
                List<PavPanel> result = new List<PavPanel>();
                foreach (var kv in panelAndBtnStack)
                {
                    foreach (var panel in kv.Key)
                    {
                        if (!currentPanels.Contains(panel))
                            currentPanels.Add(panel);
                    }
                }

                int startIndex = panelTrace.Count - 1;
                for (int i = startIndex; i >= 0; i--)
                {
                    if (!currentPanels.Contains(panelTrace[i]) && (ignoreDestroyPanels == null || !ignoreDestroyPanels.Contains(panelTrace[i].panelName)))
                    {
                        result.Add(panelTrace[i]);
                        panelTrace.RemoveAt(i);
                    }
                }

                return result;
            }
        }

    }
}
#endif