#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace Pico.AvatarAssetBuilder
{
    
    public class PavScrollViewCellDataBase
    {

    }
    
    public abstract class PavScrollViewCell
    {
        protected VisualElement _cellVisualElement;
        protected PavScrollViewCellDataBase _data;
        protected PavScrollView _customScrollView;
        protected int _index;

        public abstract string AssetPath
        {
            get;
        }

        public VisualElement CellVisualElement => _cellVisualElement;

        public PavScrollViewCellDataBase Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }

        public void Init(VisualElement ve, PavScrollView scrollView)
        {
            _cellVisualElement = ve;
            _customScrollView = scrollView;
        }
        
        public virtual void OnDestroy(){}
        
        public virtual void OnInit(){}

        public T GetData<T>() where T : PavScrollViewCellDataBase
        {
            return _data as T;
        }

        public T GetPanel<T>() where T : PavPanel
        {
            return _customScrollView.Panel as T;
        }


        public abstract void RefreshCell();
    }
}
#endif