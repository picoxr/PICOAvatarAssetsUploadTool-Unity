#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pico.AvatarAssetBuilder
{
    public enum ScrollViewDirection
    {
        Horizontal,
        Vertical
    }
    
    public class PavScrollView
    {
        private PavPanel _panel;
        private ScrollView _scrollView;
        private ScrollViewDirection _direction;
        private List<PavScrollViewCell> _cells = new List<PavScrollViewCell>(); 
        private Dictionary<Type, HashSet<PavScrollViewCell>> cellCache = new Dictionary<Type, HashSet<PavScrollViewCell>>();
        private EditorCoroutine setPositionCoroutine;

        
        public Func<int> CellCount;
        public Func<int, Type> CellAtIndex;
        public Func<int, PavScrollViewCellDataBase> DataAtIndex;

        // 竖向显示时需要把wrap改成nowrap
        public ScrollViewDirection Direction
        {
            get { return _direction; }
            set
            {
                SetDirection(value);
            }
        }

        public Wrap WrapMode
        {
            get { return ScrollView.contentContainer.style.flexWrap.value; }
            set
            {
                ScrollView.contentContainer.style.flexWrap = value;
            }
        }

        public ScrollView ScrollView => _scrollView;
        public PavPanel Panel => _panel;

        public PavScrollView(PavPanel panel, ScrollView sv)
        {
            _scrollView = sv;
            _panel = panel;
            WrapMode = Wrap.Wrap;
            _direction = ScrollViewDirection.Horizontal; // 默认设置为横向
        }

        public virtual void OnDestroy()
        {
            ClearAllCell();
            foreach (var kv in cellCache)
            {
                foreach (var cell in kv.Value)
                {
                    cell.OnDestroy();
                }
            }
            cellCache.Clear();
        }

        public void SetActive(bool value)
        {
            _scrollView.SetActive(value);
        }

        public void SetScrollPositon(float value)
        {
            if (setPositionCoroutine != null)
                EditorCoroutineUtility.StopCoroutine(setPositionCoroutine);
            
            setPositionCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(DelaySetScrollPositon(value));
        }

        public float GetScrollPosition()
        {
            if (Direction == ScrollViewDirection.Horizontal)
            {
                return (_scrollView.verticalScroller.value - _scrollView.verticalScroller.lowValue) /
                       (_scrollView.verticalScroller.highValue - _scrollView.verticalScroller.lowValue);
            }
            else 
            {
                return (_scrollView.horizontalScroller.value - _scrollView.horizontalScroller.lowValue) /
                       (_scrollView.horizontalScroller.highValue - _scrollView.horizontalScroller.lowValue);
            }
        }

        public PavScrollViewCell GetCellAtIndex(int index)
        {
            if (index >= 0 && index < _cells.Count)
                return _cells[index];

            return null;
        }

        public void ClearAllCell()
        {
            ReturnToCache(_cells);
            _cells.Clear();
        }

        public void Refresh()
        {
            if (!Check())
            {
                Debug.LogError("[CellCount, CellAtIndex, DataAtIndex] need value");
                return;
            }
            
            int count = CellCount();
            ReturnToCache(_cells);
            _cells.Clear();
            for (int i = 0; i < count; i++)
            {
                var type = CellAtIndex(i);
                var cell = GetCacheCell(type);
                if (cell != null)
                {
                    cell.Index = i;
                    cell.Data = DataAtIndex(i);
                    _scrollView.Add(cell.CellVisualElement);
                }
                else
                {
                    cell = Activator.CreateInstance(type) as PavScrollViewCell;
                    var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(cell.AssetPath);
                    if (asset == null)
                    {
                        Debug.LogError($"Can not load [{cell.AssetPath}]");
                        return;
                    }

                    var ve = asset.Instantiate();
                    cell.Init(ve, this);
                    cell.OnInit();
                    cell.Index = i;
                    cell.Data = DataAtIndex(i);
                    _scrollView.Add(cell.CellVisualElement);
                }
                
                _cells.Add(cell);
            }

            for (int i = 0; i < _cells.Count; i++)
            {
                _cells[i].RefreshCell();
            }
        }
        

        private void SetDirection(ScrollViewDirection direction)
        {
            switch (direction)
            {
                case ScrollViewDirection.Horizontal:
                    _scrollView.contentContainer.style.flexDirection = FlexDirection.Row;
                    break;
                case ScrollViewDirection.Vertical:
                    _scrollView.contentContainer.style.flexDirection = FlexDirection.Column;
                    break;
            }

            _direction = direction;
        }

        private void ReturnToCache(List<PavScrollViewCell> cells)
        {
            foreach (var cell in cells)
            {
                var type = cell.GetType();
                if (!cellCache.ContainsKey(type))
                {
                    cellCache.Add(type, new HashSet<PavScrollViewCell>());
                }

                if (!cellCache[type].Contains(cell))
                    cellCache[type].Add(cell);
                
                cell.CellVisualElement.RemoveFromHierarchy();
            }
        }

        private PavScrollViewCell GetCacheCell(Type type)
        {
            if (!cellCache.ContainsKey(type))
                return null;

            if (cellCache[type].Count == 0)
            {
                cellCache.Remove(type);
                return null;
            }

            var cell = cellCache[type].Last();
            cellCache[type].Remove(cell);
            return cell;
        }

        private bool Check()
        {
            if (CellCount == null || CellAtIndex == null || DataAtIndex == null)
                return false;

            return true;
        }

        private IEnumerator DelaySetScrollPositon(float value)
        {
            yield return null;
            if (Direction == ScrollViewDirection.Horizontal)
            {
                _scrollView.verticalScroller.value = _scrollView.verticalScroller.lowValue +
                                                     (_scrollView.verticalScroller.highValue -
                                                      _scrollView.verticalScroller.lowValue) * value;
            }
            else if (Direction == ScrollViewDirection.Vertical)
            {
                _scrollView.horizontalScroller.value = _scrollView.horizontalScroller.lowValue +
                                                       (_scrollView.horizontalScroller.highValue -
                                                        _scrollView.horizontalScroller.lowValue) * value;
            }

            setPositionCoroutine = null;
        }
    }
}
#endif