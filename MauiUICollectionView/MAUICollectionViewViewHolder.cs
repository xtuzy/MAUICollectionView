using Maui.BindableProperty.Generator.Core;

namespace MauiUICollectionView
{
    public partial class MAUICollectionViewViewHolder : ContentView
    {
        [AutoBindable]
        NSIndexPath _indexPath;

        public const float MeasureSelf = -1;
        /// <summary>
        /// 存储Item的位置和大小, 在Measure时设置, Arrange时使用它作为最终的参数
        /// </summary>
        public Rect BoundsInLayout;

        #region https://github.com/BigZaphod/Chameleon/blob/master/UIKit/Classes/UITableViewCell.h

        SelectionStyle _selectionStyle;
        bool _selected;

        string _reuseIdentifier;
        public string ReuseIdentifier => _reuseIdentifier;
        #endregion

        public MAUICollectionViewViewHolder(View itemView)
        {
            Content = itemView;
            _selectionStyle = SelectionStyle.Blue;
        }

        public MAUICollectionViewViewHolder(View itemView, string reuseIdentifier) : this(itemView)
        {
            _reuseIdentifier = reuseIdentifier;
        }

        void _updateSelectionState()
        {
            bool shouldHighlight = _selected;
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

        /// <summary>
        /// 重置ViewHolder的状态, 子类可以在其中清空View的内容. 例如, ViewHolder被回收时, 对象并没有被销毁, 会一直占用内存, 其中展示的Image也会一直被缓存, 因此可在此处设置Image为空.
        /// </summary>
        public virtual void PrepareForReuse()
        {
            IndexPath = null;
            this.HeightRequest = -1; //避免之前的Cell被设置了固定值
            this.WidthRequest = -1; //避免之前的Cell被设置了固定值
            OldBoundsInLayout = Rect.Zero;
            BoundsInLayout = Rect.Zero;
            Selected = false;
            this.TranslationX = 0;
            this.TranslationY = 0;
            this.Scale = 1;
            this.Opacity = 1;
            this.ZIndex = 1;
            Operation = -1;
        }

        /// <summary>
        /// 内部用于需要移动Item的操作, 为动画提供位置
        /// </summary>
        public Rect OldBoundsInLayout = Rect.Zero;
        /// <summary>
        /// <see cref="OperateItem.OperateType"/>, if no operate, set to -1
        /// </summary>
        public int Operation = -1;

        public IContextMenu ContextMenu { get; set; }

        public Rect DragBoundsInLayout = Rect.Zero;

        public override string ToString()
        {
            if (IndexPath != null)
                return $"IndexPath={IndexPath} Operation={Operation} Guid={this.Id}";
            else
                return base.ToString();
        }
    }
}
