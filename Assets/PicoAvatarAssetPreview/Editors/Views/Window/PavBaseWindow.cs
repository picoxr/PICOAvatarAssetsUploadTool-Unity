#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class PavBaseWindow : EditorWindow, IPavPanelContainer
        {
            // current panel.
            public PavPanel curPanel { get => _curPanel; }

            public PavBaseWindow()
            {
            }

            public virtual void ShowPreviousPanel()
            {

            }
            protected void OnEnable()
            {
                // keep the play mode.
                _isInPlaymode = Application.isPlaying;

                if (_curPanel == null)
                    return;
                _curPanel.OnEnable();
            }

            protected void OnDestroy()
            {
                if (_curPanel == null)
                    return;
                _curPanel.OnDestroy();
            }
            protected virtual void Update()
            {
                if(Application.isPlaying  != _isInPlaymode)
                {
                    _isInPlaymode = !_isInPlaymode;
                    OnPlaymodeChanged(_isInPlaymode);
                    return;
                }

                if (_curPanel == null)
                    return;
                _curPanel.OnUpdate();
            }
           
            protected virtual void OnPlaymodeChanged(bool isPlaymode)
            {
                if(_curPanel != null)
                {
                    _curPanel.DetachFromDOM();
                    _curPanel.AttachToDOM(this.rootVisualElement, this);
                }
                //UnityEngine.Debug.Log(string.Format("{0} Playmode changed", this.GetType().Name));
            }

#region For Single panel mode.

            /**
             * @brief Show with single Panel.
             * @param asNextPanel whether show as next panel and can go back from previous panel.
             */
            public virtual void ShowPanel(PavPanel panel, bool asNextPanel, UnityEngine.Object dataObjToBind)
            {
                // if same object, just return.
                if(_curPanel == panel)
                {
                    return;
                }

                // keep track of previous panel.
                if (_curPanel != null)
                {
                    _curPanel.DetachFromDOM();
                    _curPanel.OnRemove();
                }

                _curPanel = panel; 

                if (_curPanel != null)
                {
                    _curPanel.AttachToDOM(this.rootVisualElement, this);
                }
            }

            /**
             * @brief Show with single Panel.
             * @param asNextPanel whether show as next panel and can go back from previous panel.
             */
            public virtual void ShowPanel(PavPanel panel, params object[] initParams)
            {
                // keep track of previous panel.
                if (_curPanel != null)
                {
                    _curPanel.DetachFromDOM();
                    _curPanel.OnRemove();
                }

                _curPanel = panel; 

                if (_curPanel != null)
                {
                    _curPanel.AttachToDOM(this.rootVisualElement, this, initParams);
                }
            }
            
#endregion


#region For Panel Table.
            /**
             * Adds a panel for Panel Table.
             */
            public void AddPanel(PavPanel panel)
            {
                // Not Implemented.
            }

            /**
             * Remove a panel from Panel Table.
             */
            public virtual void RemovePanel(PavPanel panel)
            {
                // Not Implemented.
                if (_curPanel != null)
                {
                    _curPanel.DetachFromDOM();
                    _curPanel.OnRemove();

                    _curPanel = null;
                }
            }

#endregion


            // current panel.
            private PavPanel _curPanel = null;
            private bool _isInPlaymode = false;
            
            //curr cortinue
            public  EditorCoroutine CurrCortinue = null;
        }
    }
}
#endif