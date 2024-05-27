#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class WarningPanel : PavPanel
        {
            public List<CommonDialogWindow.Message> messages;
            private List<WarningPanelCellData> _warningPanelCellData = new List<WarningPanelCellData>();
            private static WarningPanel _instance;

            public static WarningPanel instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<WarningPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/WarningPanel.asset");
                    }
                    return _instance;
                }
            }
            public override string displayName { get => "Warning Panel"; }
            public override string panelName { get => "WarningPanel"; }
            public override string uxmlPathName { get => "Uxml/WarningPanel.uxml"; }

            private GroupBox _scrollContent;
            private PavScrollView _pavScrollView;

            private VisualElement _warningBtnGroup;
            private Button _cancelButton;
            private Button _nextButton;

            public Action OnNextStepCallback;
            protected override bool BuildUIDOM(VisualElement parent)
            {
                if (!base.BuildUIDOM(parent))
                {
                    return false;
                }

                var scrollView = mainElement.Q<ScrollView>("WarningPanel-ScrollView");
                scrollView.style.alignSelf = new StyleEnum<Align>(Align.Center);
                _pavScrollView = new PavScrollView(this, scrollView);
                // 函数赋值
                _pavScrollView.CellCount = CellCount;
                _pavScrollView.CellAtIndex = CellAtIndex;
                _pavScrollView.DataAtIndex = DataAtIndex;
    
                // 默认横向 设置竖直方向 需要改一下WrapMode 
                _pavScrollView.Direction = ScrollViewDirection.Vertical;
                _pavScrollView.WrapMode = Wrap.NoWrap;
                return true;
            }

            protected override bool BindUIActions(params object[] paramGroup) 
            {
                _scrollContent = mainElement.Q<GroupBox>("ScrollContent");
                bool hasError = false;
                if (paramGroup.Length > 0)
                {
                    messages = (List<CommonDialogWindow.Message>)paramGroup[0];
                    OnRecvData(out hasError);
                }
                
                _warningBtnGroup = mainElement.Q<VisualElement>("Node-Button-Group");
                
                _cancelButton = mainElement.Q<Button>("Nod-Button-Cancel");
                _nextButton = mainElement.Q<Button>("Nod-Button-NextStep");
                if (!hasError)
                {
                    _warningBtnGroup.style.display = DisplayStyle.Flex;
                    _cancelButton?.RegisterCallback<ClickEvent>(OnCancelBtnClick);
                    _nextButton?.RegisterCallback<ClickEvent>(OnNextBtnClick);
                }
                else
                {
                    _cancelButton?.RegisterCallback<ClickEvent>(OnCancelBtnClick);
                    _nextButton?.RegisterCallback<ClickEvent>(OnNextBtnClick);
       
                }
            

              
                return base.BindUIActions();
            }

            public void OnCancelBtnClick(ClickEvent evt)
            {
                ((CommonDialogWindow)panelContainer)?.Close();
            }

            public void OnNextBtnClick(ClickEvent evt)
            {
                //OnNextStepCallback?.Invoke();
                var currPanel = MainMenuUIManager.instance.CurrentPanel;
                currPanel?.OnNextStep();
            }
            private void AddMessage(CommonDialogWindow.CheckStatus status, string message)
            {
                string templatePath = null;
                if (status == CommonDialogWindow.CheckStatus.Error)
                {
                    templatePath = "Uxml/WarningPanel.ErrorTemplate.uxml";
                }
                else if (status == CommonDialogWindow.CheckStatus.Warning)
                {
                    templatePath = "Uxml/WarningPanel.WarningTemplate.uxml";
                }
                else
                {
                    return;
                }

                var templateAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetBuilderConfig.instance.uiDataAssetsPath + templatePath);
                var element = templateAsset.Instantiate();
                var messageLabel = element.Q<Label>("Common-Content-Message");
                messageLabel.text = message;
                _scrollContent?.Add(element);
            }
            
            void OnRecvData(out bool hasError)
            {
                _warningPanelCellData.Clear();
                bool error = false;
                messages.ForEach((info) =>
                {
                    error |= info.status == CommonDialogWindow.CheckStatus.Error;
                    _warningPanelCellData.Add(new WarningPanelCellData(info.status, info.message));
                });
                hasError = error;
                // 刷新列表，会触发cell的RefreshCell调用
                _pavScrollView.Refresh();
            }
            
            public int CellCount()
            {
                // 返回列表元素数量
                return _warningPanelCellData.Count;
            }
            
            
            public Type CellAtIndex(int index)
            {
                // 返回某个位置cell的类型，可以支持多种不同的类型
                if (messages[index].status == CommonDialogWindow.CheckStatus.Warning)
                {
                    return typeof(MessageWarningCell);
                }
                else if(messages[index].status == CommonDialogWindow.CheckStatus.Error)
                {
                    return typeof(MessageErrorCell);
                }
                return typeof(MessageWarningCell);
            }
            
            public PavScrollViewCellDataBase DataAtIndex(int index)
            {
                // 返回某个位置的数据
                return _warningPanelCellData[index];
            }
            
            
            public class WarningPanelCellData : PavScrollViewCellDataBase
            {
                public CommonDialogWindow.CheckStatus Status;
                public string Content;

                public WarningPanelCellData(CommonDialogWindow.CheckStatus status, string content)
                {
                    Status = status;
                    Content = content;
                }
                
            }
            
            public class MessageWarningCell : PavScrollViewCell
            {
                // uxml路径
                public override string AssetPath => "Assets/PicoAvatarAssetBuilder/Editors/Views/UxmlWidget/WarningPanel.WarningTemplate.uxml";
                private Label name;

                public override void OnInit()
                {
                    base.OnInit();
                    _cellVisualElement.style.alignSelf = new StyleEnum<Align>(Align.Center);
                    name = _cellVisualElement.Q<Label>("Common-Content-Message");
                }

                public override void RefreshCell()
                {
                    // GetData获取DataAtIndex中的数值
                    var data = GetData<WarningPanelCellData>();
                    name.text = data.Content;
                }
            }
            
            public class MessageErrorCell : PavScrollViewCell
            {
                // uxml路径
                public override string AssetPath => "Assets/PicoAvatarAssetBuilder/Editors/Views/UxmlWidget/WarningPanel.ErrorTemplate.uxml";
                private Label name;

                public override void OnInit()
                {
                    base.OnInit();
                    _cellVisualElement.style.alignSelf = new StyleEnum<Align>(Align.Center);
                    name = _cellVisualElement.Q<Label>("Common-Content-Message");
                    
                }

                public override void RefreshCell()
                {
                    // GetData获取DataAtIndex中的数值
                    var data = GetData<WarningPanelCellData>();
                    name.text = data.Content;
                }
            }
            
            public override void OnDestroy()
            {
                base.OnDestroy();
                if (_instance == this)
                {
                    _instance.OnNextStepCallback = null;
                    _instance = null;
                }
                _cancelButton?.UnregisterCallback<ClickEvent>(OnCancelBtnClick);
                _nextButton?.UnregisterCallback<ClickEvent>(OnNextBtnClick);
            }
        }
    }
}
#endif