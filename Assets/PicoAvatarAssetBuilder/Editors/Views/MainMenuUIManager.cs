#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;


namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public partial class MainMenuUIManager
        {
            Dictionary<string, PavPanel> _allDinamicPanels = new();

            public bool PAAB_OPEN = true;
            
            private static MainMenuUIManager _instance;
            public static MainMenuUIManager instance 
            { 
                get
                {
                    if(_instance == null)
                    {
                        _instance = new MainMenuUIManager();
                        _instance.Initialize();
                    }
                    return _instance;
                }
            }
            // Query whether _instance is created. 
            public static bool isValid => _instance != null;

            public Action<PavPanel> OnPanelShow;
            public PavPanel CurrentPanel => _currPanel;

            private List<PavPanel> _pavPanels = new(); //fixed panel
            private Dictionary<PlaceHolderType, VisualElement> _placeholder = new();
            private PavPanel _currPanel = null;

            public MainMenuUIManager()
            {
                UnityEngine.Debug.Log("MainMenuUIManager.MainMenuUIManager");
            }
            ~MainMenuUIManager()
            {
                UnityEngine.Debug.Log("MainMenuUIManager.~MainMenuUIManager");
            }
            void Initialize()
            {
                /*SetupModalPanels();
                ShowCharacterPanel();*/
                _allDinamicPanels.Clear();
                _pavPanels.Clear();
            }

            /// <summary>
            /// register mainwindow area
            /// </summary>
            /// <param name="type">placeholder type</param>
            /// <param name="placeholder">visualelement</param>
            /// <returns></returns>
            public bool RegisterMainPanelPlaceholder(PlaceHolderType type, VisualElement placeholder)
            {
                if (placeholder == null)
                    return false;
                _placeholder[type] = placeholder;
                return true;
            }

            /// <summary>
            ///  unregister mainwindow area
            /// </summary>
            /// <param name="type">placeholder type</param>
            public void UnRegisterMainPanelPlaceholder(PlaceHolderType type)
            {
                _placeholder[type] = null;
            }
            /// <summary>
            /// used to find target panel by panelname
            /// </summary>
            /// <param name="panelName">panelname</param>
            /// <returns></returns>
            public PavPanel GetPavPanelByPanelName(string panelName)
            {
                foreach (var panel in _allDinamicPanels)
                {
                    if (panel.Key == panelName)
                    {
                        return panel.Value;
                    }
                }

                return null;
            }

            /// <summary>
            /// used to find target panel by panel type
            /// </summary>
            /// <param name="ptype">panel type</param>
            /// <returns></returns>
            public PavPanel GetPavPanelByPanelType(PanelType ptype)
            {
                foreach (var panel in _allDinamicPanels)
                {
                    if (Enum.TryParse(panel.Key, out PanelType type))
                    {
                        if (type == ptype)
                        {
                            return panel.Value;
                        }
                    }
                }

                return null;
            }

            /// <summary>
            /// used to find target fixed panel by panelname
            /// </summary>
            /// <param name="panelName">panelname</param>
            /// <returns></returns>
            public PavPanel GetFixedPavPanelByPanelName(string panelName)
            {
                foreach (var panel in _pavPanels)
                {
                    if (panel.panelName == panelName)
                    {
                        return panel;
                    }
                }

                return null;
            }

            /// <summary>
            /// used to find target fixed panel by paneltype
            /// </summary>
            /// <param name="ptype">panel type</param>
            /// <returns></returns>
            public PavPanel GetFixedPavPanelByPanelType(PanelType ptype)
            {
                foreach (var panel in _pavPanels)
                {
                    if (Enum.TryParse(panel.panelName, out PanelType type))
                    {
                        if (type == ptype)
                        {
                            return panel;
                        }
                    }
                }

                return null;
            }
            
            /// <summary>
            /// used to show panel(menubar channel) 
            /// </summary>
            /// <param name="type">panel type</param>
            public void ShowPanelByPanelType(PanelType type)
            {
                ShowPanelByPanelName(type.ToString());
            }

            /// <summary>
            /// used to show panel(menubar channel) 
            /// </summary>
            /// <param name="name">panelname</param>
            public void ShowPanelByPanelName(string name)
            {
                if (_currPanel != null && !_currPanel.panelName.Equals(name))
                {
                    _currPanel.HidePanel();
                }

                if (_allDinamicPanels.ContainsKey(name))
                {
                    _currPanel = _allDinamicPanels[name];
                    _currPanel.ShowPanel();
                    OnPanelShow?.Invoke(_currPanel);
                }
            }
            
            /// <summary>
            /// used to build visualtree
            /// </summary>
            /// <param name="target">target panel</param>
            /// <param name="type">placeholder type</param>
            /// <param name="show">visable</param>
            /// <param name="add">need to be added to the panel container?</param>
            /// <returns></returns>
            public bool AttachToDOMByType(PavPanel target, PlaceHolderType type, bool show = false, bool add = true)
            {
                if (target == null)
                    return false;
                target.AttachToDOM(_placeholder[type], null);
                if (!show)
                {
                    target.mainElement.style.display = DisplayStyle.None;
                }
                
                if (add)
                {
                    if (_allDinamicPanels.ContainsKey(target.panelName))
                    {
                        //DetachFromDOMByType(target);  may be need unified processing
                        _allDinamicPanels.Remove(target.panelName);
                    }
                    _allDinamicPanels.Add(target.panelName ,target);
                }
                else
                {
                    if (!_pavPanels.Contains(target))
                    {
                        _pavPanels.Add(target);
                    }
                }
                return true;
            }
  
            /// <summary>
            /// used to remove from visualtree
            /// </summary>
            /// <param name="target">target panel</param>
            /// <param name="removbe">removbe target panel from panel container?</param>
            /// <returns></returns>
            public bool DetachFromDOMByType(PavPanel target, bool removbe = true)
            {
                if (target == null || target.mainElement == null)
                    return false;
                target.mainElement.RemoveFromHierarchy();
                if (removbe)
                {
                    _allDinamicPanels.Remove(target.panelName);
                }
             
                return true;
            }

            /// <summary>
            /// Used to jump between panels
            /// </summary>
            /// <param name="from"> origin panel</param>
            /// <param name="to">destination panel</param>
            /// <returns></returns>
            public bool SwitchPanel(PavPanel from, PavPanel to)
            {
                if (from == null || to == null)
                    return false;
                from.HidePanel();
                to.ShowPanel();
                return true;
            }

            /*
            public void ShowCharacterPanel()
            {
                ShowModalPanel(m_CharacterPanel);
            }

            public void ShowConfigureComponentPanel()
            {
                ShowModalPanel(m_ConfigureComponentPanel);
            }

            public void ShowTestPanel()
            {
                ShowModalPanel(m_TestPanel);
            }
            */

            public void ClearPanelContainer()
            {
                foreach (var panel in _pavPanels)
                {
                    panel?.OnDestroy();
                }
                _pavPanels.Clear();
            }

            public void Clear()
            {
                ClearPanelContainer();
                foreach (var panel in _allDinamicPanels)
                {
                    panel.Value.OnDestroy();
                }
                _allDinamicPanels.Clear();
                _instance = null;
                
            }

            public void Update()
            {
                if (_currPanel)
                {
                    _currPanel.OnUpdate();
                }
            }

            public void DestroyPanels(List<PavPanel> panels)
            {
                foreach (var panel in panels)
                {
                    if (panel == null)
                        continue;

                    var pName = panel.panelName;
                    panel.OnDestroy();
                    if (NavMenuBarRoute.isValid)
                    {
                        EditorCoroutineUtility.StartCoroutine(NavMenuBarRoute.instance.RegisterPanelByPanelName(pName), this);
                    }
                }
            }
        }
    }
}
#endif