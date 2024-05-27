#if UNITY_EDITOR
using System;
using Pico.Avatar;
using UnityEditor;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class ConfirmInfoPanel : PavPanel
        {
            public override string displayName
            {
                get => "ConfirmInfo";
            }
            public override string panelName
            {
                get => "ConfirmInfo";
            }
            public override string uxmlPathName
            {
                get => "Uxml/ConfirmInfoPanel.uxml";
            }

            private static ConfirmInfoPanel _instance;

            const string ConfirmButton = "Confirm-Info-Confirm";
            const string CancelButton = "Confirm-Info-Cancel";


            const string WarningContent = "Confirm-Info-Content";
            const string WarningIcon = "Confirm-Info-Icon";

            const string ButtonInactiveClass = "black__button";
            const string ButtonActiveClass = "black__button--active";


            // UI Buttons
            private Button _confirmButton;
            private Button _cancelButton;

            // UI Labels
            private Label _warningContent;
            private VisualElement _warningIcon;

            private WebImage _iconImage;


            //receive param
            private string _iconUrl;
            private string _content;
            private Action _successCallback;
            private Action _failureCallback;

            public static ConfirmInfoPanel instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<ConfirmInfoPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/ConfirmInfoPanel.asset");
                    }

                    return _instance;
                }
            }


            protected override bool BuildUIDOM(VisualElement parent) //SetVisualElements and BuildWithUxml
            {
                base.BuildUIDOM(parent);
                if (mainElement != null)
                {
                    _confirmButton = mainElement.Q<Button>(ConfirmButton);
                    _cancelButton = mainElement.Q<Button>(CancelButton);
                    _warningContent = mainElement.Q<Label>(WarningContent);
                    _warningIcon = mainElement.Q<VisualElement>(WarningIcon);
                }

                return true;
            }

            protected override bool BindUIActions(params object[] paramGroup) //RegisterButtonCallbacks
            {
                _confirmButton?.RegisterCallback<ClickEvent>(OnConfirmBtn);
                _cancelButton?.RegisterCallback<ClickEvent>(OnCancelBtn);
                if (paramGroup is { Length: > 5 })
                {
                    _iconUrl = (string)paramGroup[0];
                    if (_iconUrl != default)
                    {
                        _iconImage = new WebImage(_warningIcon);
                        _iconImage.ClearTexture();
                        _iconImage.SetActive(false);
                        _iconImage.SetTexture(_iconUrl, ImageFileExtension.PNG);
                        _iconImage.onTextureLoad += b => { _iconImage.SetActive(b); };
                    }

                    _content = (string)paramGroup[1];
                    if (_content != default)
                    {
                        _warningContent.text = _content;
                    }

                    _successCallback = (Action)paramGroup[2];
                    _failureCallback = (Action)paramGroup[3];

                    if (_confirmButton != null) _confirmButton.text = (string)paramGroup[4];
                    if (_cancelButton != null) _cancelButton.text = (string)paramGroup[5];
                }

                return base.BindUIActions(paramGroup);
            }

            private void OnConfirmBtn(ClickEvent clickEvent)
            {
                var successCallback = _successCallback;
                ((EditorWindow)panelContainer)?.Close(); //must close first
                successCallback?.Invoke();
            }

            private void OnCancelBtn(ClickEvent clickEvent)
            {
                _failureCallback?.Invoke();
                ((EditorWindow)panelContainer)?.Close();
            }

            public override void OnDestroy()
            {
                base.OnDestroy();
                _iconImage?.ClearTexture();
                _iconImage = null;
                _successCallback = null;
                _failureCallback = null;
                _instance = null;
            }
        }
    }
}
#endif