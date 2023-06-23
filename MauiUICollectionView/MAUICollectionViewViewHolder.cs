namespace MauiUICollectionView
{
    public class MAUICollectionViewViewHolder
    {
        /// <summary>
        /// Debug
        /// </summary>
        public NSIndexPath NSIndexPath;

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

        /// <summary>
        /// Item实际对应的View
        /// </summary>
        /// <value></value>
        public View ContentView { get; private set; }

        void _updateSelectionState()
        {
            bool shouldHighlight = _highlighted || _selected;
            UpdateSelectionState(shouldHighlight);
        }

        /// <summary>
        /// 子类实现它来设置被选择时如何显示.
        /// </summary>
        /// <param name="shouldHighlight"></param>
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

        /// <summary>
        /// 标记ViewHolder是否设置了内容.
        /// </summary>
        public bool IsEmpty = true;

        /// <summary>
        /// 重置ViewHolder的状态, 子类可以在其中清空View的内容. 例如, ViewHolder被回收时, 对象并没有被销毁, 会一直占用内存, 其中展示的Image也会一直被缓存, 因此可在此处设置Image为空.
        /// </summary>
        public virtual void PrepareForReuse()
        {
            IsEmpty = true;
            ContentView.HeightRequest = -1; //避免之前的Cell被设置了固定值
            OldBoundsInLayout = Rect.Zero;
            BoundsInLayout = Rect.Zero;
            ContentView.TranslationX = 0;
            ContentView.TranslationY = 0;
            ContentView.Opacity = 0;
            Operation = -1;
        }

        /// <summary>
        /// 内部用于需要移动Item的操作, 为动画提供位置
        /// </summary>
        public Rect OldBoundsInLayout = Rect.Zero;
        /// <summary>
        /// <see cref="OperateItem.OperateType"/>, if no operate, set to -1
        /// </summary>
        public int Operation;
    }
}
