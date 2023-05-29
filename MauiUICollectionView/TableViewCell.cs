namespace MauiUICollectionView
{
    public enum TableViewCellAccessoryType
    {
        None, DisclosureIndicator, DetailDisclosureButton, Checkmark
    }

    public enum TableViewCellSelectionStyle
    {
        None, Blue, Gray
    }

    public enum TableViewCellEditingStyle
    {
        None, Delete, Insert
    }

    public class TableViewViewHolder
    {
        /// <summary>
        /// 存储Cell的位置, 
        /// </summary>
        internal Point PositionInLayout;

        #region https://github.com/BigZaphod/Chameleon/blob/master/UIKit/Classes/UITableViewCell.h

        TableViewCellSelectionStyle _selectionStyle;
        bool _selected;
        bool _highlighted;

        string _reuseIdentifier;
        public string ReuseIdentifier => _reuseIdentifier;
        #endregion

        public TableViewViewHolder(View itemView)
        {
            ContentView = itemView;
            _selectionStyle = TableViewCellSelectionStyle.Blue;
        }

        public TableViewViewHolder(View itemView, string reuseIdentifier) : this(itemView)
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
            if (selected != _selected && _selectionStyle != TableViewCellSelectionStyle.None)
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
            if (_highlighted != highlighted && _selectionStyle != TableViewCellSelectionStyle.None)
            {
                _highlighted = highlighted;
                this._updateSelectionState();
            }
        }

        public bool Highlighted { set => this.SetHighlighted(value, false); get => _highlighted; }

        public static Point EmptyPoint = new Point(-1, -1);

        public bool IsEmpty = true;
        public virtual void PrepareForReuse()
        {
            IsEmpty = true;
            PositionInLayout = EmptyPoint;
        }
    }

    public interface IHighlightable
    {
        void setHighlighted(bool highlighted);
    }
}
