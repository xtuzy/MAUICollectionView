namespace MauiUICollectionView
{
    public class MAUICollectionViewViewHolder
    {
        public const float MeasureSelf = -1;
        /// <summary>
        /// 存储Item的位置和大小, 在Measure时设置, Arrange时使用它作为最终的参数
        /// </summary>
        public Rect BoundsInLayout;

        #region https://github.com/BigZaphod/Chameleon/blob/master/UIKit/Classes/UITableViewCell.h

        SelectionStyle _selectionStyle;
        bool _selected;
        bool _highlighted;

        string _reuseIdentifier;
        public string ReuseIdentifier => _reuseIdentifier;
        #endregion

        public MAUICollectionViewViewHolder(View itemView)
        {
            ContentView = itemView;
            _selectionStyle = SelectionStyle.Blue;
        }

        public MAUICollectionViewViewHolder(View itemView, string reuseIdentifier) : this(itemView)
        {
            _reuseIdentifier = reuseIdentifier;
        }

        public View ContentView { get; private set; }

        void _updateSelectionState()
        {
            bool shouldHighlight = _highlighted || _selected;
            UpdateSelectionState(shouldHighlight);
        }

        public virtual void UpdateSelectionState(bool shouldHighlight)
        {

        }

        public void SetSelected(bool selected, bool animated)
        {
            if (selected != _selected && _selectionStyle != SelectionStyle.None)
            {
                _selected = selected;
                this._updateSelectionState();
            }
        }

        public bool Selected
        {
            set => this.SetSelected(value, false);
        }

        public void SetHighlighted(bool highlighted, bool animated)
        {
            if (_highlighted != highlighted && _selectionStyle != SelectionStyle.None)
            {
                _highlighted = highlighted;
                this._updateSelectionState();
            }
        }

        public bool Highlighted { set => this.SetHighlighted(value, false); get => _highlighted; }

        public static Rect EmptyRect = new Rect(-1, -1, 0, 0);

        public bool IsEmpty = true;
        internal ItemAttribute Attributes;

        public virtual void PrepareForReuse()
        {
            IsEmpty = true;
            BoundsInLayout = EmptyRect;
            ContentView.HeightRequest = -1; //避免之前的Cell被设置了固定值
        }

        public void Apply(ItemAttribute attribute, bool animate)
        {
            if (animate)
            {
                ContentView.ZIndex = attribute.ZIndex;
                ContentView.FadeTo(attribute.Alpha);
            }
            else
            {
                ContentView.ZIndex = attribute.ZIndex;
                ContentView.Opacity = attribute.Alpha;
                ContentView.IsVisible = !attribute.Hiden;
            }
        }

        public class ItemAttribute
        {
            public NSIndexPath IndexPath { get; set; }
            public Rect Bounds { get; set; }
            public int ZIndex { get; set; }
            public bool Hiden { get; set; }
            public int Alpha { get; set; }
        }
    }

    public interface IHighlightable
    {
        void setHighlighted(bool highlighted);
    }
}
