#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pico.Avatar;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.EditorCoroutines.Editor;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public partial class MaterialConfigPanel : PavPanel
        {
            public List<Renderer> messages;
            public List<MaterialConfigPanelCellData> _MaterialConfigPanelCellData = new List<MaterialConfigPanelCellData>();
            public Dictionary<Renderer, MaterialConfigPanelCellData> _renderMaterialDic = new Dictionary<Renderer, MaterialConfigPanelCellData>();
            private static MaterialConfigPanel _instance;

            public static MaterialConfigPanel instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<MaterialConfigPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/MaterialConfigPanel.asset");
                    }
                    return _instance;
                }
            }

            public void InitMaterialConfig(int lodIndex, GroupBox lodGroup, int itemIndex, GameObject obj)
            {
                _lodIndex = lodIndex;
                _lodGroup = lodGroup;
                _itemIndex = itemIndex;
                _currentObj = obj;
            }

            public int _lodIndex;
            public GroupBox _lodGroup;
            public int _itemIndex;
            public GameObject _currentObj;
            
            public override string displayName { get => "Material Config Panel"; }
            public override string panelName { get => "MaterialConfigPanel"; }
            public override string uxmlPathName { get => "Uxml/MaterialConfigPanel.uxml"; }
            
            const string k_TipVisualElement = "mainwin__tip-ve";
            const string k_TipContainerVisualElement = "mainwin__tipcontainer-ve";

            private GroupBox _scrollContent;
            private PavScrollView _pavScrollView;

            private VisualElement _warningBtnGroup;
            private Button _cancelButton;
            private Button _nextButton;
            
            private Label _tip;
            private VisualElement _tipContainer;
            private EditorCoroutine _tipCoroutine;

            public Action OnNextStepCallback;
            protected override bool BuildUIDOM(VisualElement parent)
            {
                if (!base.BuildUIDOM(parent))
                {
                    return false;
                }
                
                _tip = mainElement.Q<Label>(k_TipVisualElement);
                _tipContainer = mainElement.Q(k_TipContainerVisualElement);

                var scrollView = mainElement.Q<ScrollView>("MaterialConfigPanel-ScrollView");
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


            protected override bool BindUIActions(params object[] paramGroup) 
            {
                _scrollContent = mainElement.Q<GroupBox>("ScrollContent");
                if (paramGroup.Length > 0)
                {
                    messages = (List<Renderer>)paramGroup[0];
                    OnRecvData();
                }
                
                _warningBtnGroup = mainElement.Q<VisualElement>("Node-Button-Group");
                
                _cancelButton = mainElement.Q<Button>("Nod-Button-Cancel");
                _nextButton = mainElement.Q<Button>("Nod-Button-NextStep");
                {
                    _warningBtnGroup.style.display = DisplayStyle.Flex;
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
                ConfigureComponentPanel currPanel = MainMenuUIManager.instance.CurrentPanel as ConfigureComponentPanel;
                currPanel?.OnSetGameObject(_lodIndex, _lodGroup, _itemIndex, _currentObj);
                CommonDialogWindow.CloseSelf();
            }
            
            void OnRecvData()
            {
                _MaterialConfigPanelCellData.Clear();
                _renderMaterialDic.Clear();
                messages.ForEach((render) =>
                {
                    MaterialConfigPanelCellData data = new MaterialConfigPanelCellData(render.name);
                    _MaterialConfigPanelCellData.Add(data);
                    _renderMaterialDic.Add(render,data);
                });
                // 刷新列表，会触发cell的RefreshCell调用
                _pavScrollView.Refresh();
            }
            
            public int CellCount()
            {
                // 返回列表元素数量
                return _MaterialConfigPanelCellData.Count;
            }
            
            
            public Type CellAtIndex(int index)
            {
                return typeof(MessageWarningCell);
            }
            
            public PavScrollViewCellDataBase DataAtIndex(int index)
            {
                // 返回某个位置的数据
                return _MaterialConfigPanelCellData[index];
            }
            
            
            public class MaterialConfigPanelCellData : PavScrollViewCellDataBase
            {
                public string Content;
                
                public bool isOfficialMaterial;
                public OfficialShaderTheme ShaderTheme;
                public bool saveCustomMaterial;

                public MaterialConfigPanelCellData(string content)
                {
                    Content = content;
                }
                
            }
            
            public class MessageWarningCell : PavScrollViewCell
            {
                // uxml路径
                public override string AssetPath => "Assets/PicoAvatarAssetPreview/Editors/Views/UxmlWidget/MaterialConfigPanel.WarningTemplate.uxml";
                private Label name;
                private DropdownField materialTypeDropdown;
                private Toggle saveCustomMaterialToggle;
                private GroupBox saveCustomMaterialTip;

                private string _materialType;
                private bool _saveCustomMaterial;

                public override void OnInit()
                {
                    base.OnInit();
                    _cellVisualElement.style.alignSelf = new StyleEnum<Align>(Align.Center);
                    name = _cellVisualElement.Q<Label>("MeshRenderLabel");
                    materialTypeDropdown = _cellVisualElement.Q<DropdownField>("MaterialTypeDropdown");
                    materialTypeDropdown.RegisterValueChangedCallback((eve) =>
                    {
                        _materialType = eve.newValue;
                        var data = GetData<MaterialConfigPanelCellData>();
                        if (_materialType == "NPR")
                        {
                            data.ShaderTheme = OfficialShaderTheme.PicoNPR;
                        }
                        else
                        {
                            data.ShaderTheme = OfficialShaderTheme.PicoPBR;
                        }
                    });
                    
                    saveCustomMaterialToggle = _cellVisualElement.Q<Toggle>("SaveCustomMaterialToggle");
                    saveCustomMaterialToggle.RegisterValueChangedCallback((eve) =>
                    {
                        _saveCustomMaterial = eve.newValue;
                        var data = GetData<MaterialConfigPanelCellData>();
                        data.saveCustomMaterial = _saveCustomMaterial;
                    });
                    
                    saveCustomMaterialTip = _cellVisualElement.Q<GroupBox>("SaveCustomMaterialTip");
                    UIUtils.SetTipText(saveCustomMaterialTip, StringTable.GetString("SaveCustomMaterialTip"), tipMaxWidth:250, panel:MaterialConfigPanel.instance);
                }

                public override void RefreshCell()
                {
                    // GetData获取DataAtIndex中的数值
                    var data = GetData<MaterialConfigPanelCellData>();
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