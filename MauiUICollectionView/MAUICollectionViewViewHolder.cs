using Maui.BindableProperty.Generator.Core;

namespace MauiUICollectionView
{
    public partial class MAUICollectionViewViewHolder : ContentView
    {
        [AutoBindable]
        NSIndexPath _indexPath;

        /// <summary>
        /// Used at <see cref="IMAUICollectionViewSource.HeightForItem"/>
        /// </summary>
        public const double AutoSize = -1;

        /// <summary>
        /// Store position and size of item. set it when Measure, and use it as the final parameter when Arrange
        /// </summary>
        public Rect BoundsInLayout;

        #region https://github.com/BigZaphod/Chameleon/blob/master/UIKit/Classes/UITableViewCell.h

        SelectStatus _selected = SelectStatus.CancelWillSelect;

        string _reuseIdentifier;
        public string ReuseIdentifier => _reuseIdentifier;
        #endregion

        public MAUICollectionViewViewHolder(View itemView)
        {
            Content = itemView;
        }

        public MAUICollectionViewViewHolder(View itemView, string reuseIdentifier) : this(itemView)
        {
            _reuseIdentifier = reuseIdentifier;
        }

        /// <summary>
        /// The subclass implements it to set how it is displayed when selected.
        /// </summary>
        /// <param name="shouldHighlight"></param>
        public virtual void UpdateSelectionState(SelectStatus status)
        {

        }

        public void SetSelected(SelectStatus status)
        {
            if (status != _selected)
            {
                _selected = status;
                this.UpdateSelectionState(status);
            }
        }

        public bool Selected
        {
            set => this.SetSelected(value == true? SelectStatus.Selected: SelectStatus.CancelWillSelect);
        }

        /// <summary>
        /// Resets the state of the ViewHolder, where subclasses can empty the contents of the View. For example, when the ViewHolder is recycled, the object is not destroyed, it will always occupy memory, and the displayed Image will always be cached, so you can set the Image to empty here.
        /// </summary>
        public virtual void PrepareForReuse()
        {
            IndexPath = null;
            this.HeightRequest = -1; //Avoid having a fixed value be set
            this.WidthRequest = -1; 
            OldBoundsInLayout = Rect.Zero;
            BoundsInLayout = Rect.Zero;
            Selected = false;
            this.TranslationX = 0;
            this.TranslationY = 0;
            this.Scale = 1;
            this.Opacity = 0;
            this.ZIndex = 0;//https://github.com/dotnet/maui/pull/3635 it say default 0
            Operation = -1;
        }

        /// <summary>
        /// provide position for animation of item that be moved.
        /// </summary>
        public Rect OldBoundsInLayout = Rect.Zero;
        
        /// <summary>
        /// <see cref="OperateItem.OperateType"/>, if no operate, set to -1
        /// </summary>
        public int Operation = -1;

        public IContextMenu ContextMenu { get; set; }

        /// <summary>
        /// store position and size for dragged item, it be arranged according to this.
        /// </summary>
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
