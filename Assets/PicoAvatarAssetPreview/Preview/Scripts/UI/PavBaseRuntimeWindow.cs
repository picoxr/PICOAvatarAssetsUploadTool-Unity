#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.UIElements;
using Pico.AvatarAssetPreview;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class PavBaseRuntimeWindow : MonoBehaviour, IPavPanelContainer
        {
            // current panel.
            public PavPanel curPanel { get => _curPanel; }

            public VisualElement rootVisualElement
            {
                get => _rootVisualElement;
                set => _rootVisualElement = value;
            }

            public virtual void ShowPreviousPanel()
            {

            }
            protected void OnEnable()
            {
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
            protected void Update()
            {
                if (_curPanel == null)
                    return;
                _curPanel.OnUpdate();
            }
           

#region For Single panel mode.

            /**
             * @brief Show with single Panel.
             * @param asNextPanel whether show as next panel and can go back from previous panel.
             */
            public virtual void ShowPanel(PavPanel panel, bool asNextPanel, UnityEngine.Object dataObjToBind)
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


            private VisualElement _rootVisualElement = null;
            
            // current panel.
            private PavPanel _curPanel = null;
        }
    }
}
#endif