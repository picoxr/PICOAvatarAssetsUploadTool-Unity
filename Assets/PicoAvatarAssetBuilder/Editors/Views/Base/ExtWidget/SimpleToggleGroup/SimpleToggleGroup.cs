#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Pico.AvatarAssetBuilder
{
    public interface ISimpleToggleGroupElement
    {
        void Release();
        void OnValueChanged(bool value);
        void Toggle();
    }

    public class SimpleButtonToggle : ISimpleToggleGroupElement
    {
        private SimpleToggleGroup group;
        private Button button;

        public event Action<bool> onValueChange;

        public SimpleButtonToggle(SimpleToggleGroup group, Button button)
        {
            this.group = group;
            this.button = button;
            Init();
        }

        public void Release()
        {
            group.RemoveElement(this);
            button.UnregisterCallback<ClickEvent>(OnButtonClick);
        }

        public void OnValueChanged(bool value)
        {
            onValueChange?.Invoke(value);
            button.pickingMode = value ? PickingMode.Ignore : PickingMode.Position;
        }

        public void Toggle()
        {
            group.ToggleElement(this);
        }

        private void Init()
        {
            group.AddElement(this);
            button.RegisterCallback<ClickEvent>(OnButtonClick);
        }

        private void OnButtonClick(ClickEvent @event)
        {
            group.ToggleElement(this);
        }
    }

    public class SimpleToggleGroup
    {
        private HashSet<ISimpleToggleGroupElement> elements = new HashSet<ISimpleToggleGroupElement>();
        private ISimpleToggleGroupElement curToggled = null;

        public void AddElement(ISimpleToggleGroupElement ve)
        {
            if (!elements.Contains(ve))
                elements.Add(ve);
        }

        public void RemoveElement(ISimpleToggleGroupElement ve)
        {
            elements.Remove(ve);
            if (curToggled != null && ve == curToggled)
                curToggled = null;
        }

        public void ToggleElement(ISimpleToggleGroupElement ve)
        {
            // if (curToggled != null && curToggled == ve)
            //     return;

            curToggled = ve;
            foreach (var element in elements)
            {
                element.OnValueChanged(ve == element);
            }
        }
        
    }
}
#endif