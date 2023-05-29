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

    public class TableViewCell : Grid
    {
        /// <summary>
        /// 存储Cell的位置, 
        /// </summary>
        internal Point PositionInLayout;

        #region https://github.com/BigZaphod/Chameleon/blob/master/UIKit/Classes/UITableViewCell.h

        Label detailTextLabel;
        View _backgroundView;
        View _selectedBackgroundView;
        TableViewCellSelectionStyle _selectionStyle;
        int indentationLevel;
        TableViewCellAccessoryType accessoryType;
        View _accessoryView;
        TableViewCellAccessoryType editingAccessoryType;
        bool _selected;
        bool _highlighted;
        bool editing; // not yet implemented
        bool showingDeleteConfirmation;  // not yet implemented
        string _reuseIdentifier;  // not yet implemented
        float _indentationWidth; // 10 per default

        public string ReuseIdentifier => _reuseIdentifier;
        #endregion

        Layout _contentView;
        Image _imageView;
        Label _textLabel;

        public TableViewCell()
        {
            _indentationWidth = 10;
            _selectionStyle = TableViewCellSelectionStyle.Blue;

            this.accessoryType = TableViewCellAccessoryType.None;
            this.editingAccessoryType = TableViewCellAccessoryType.None;
        }

        public TableViewCell(string reuseIdentifier) : this()
        {
            _reuseIdentifier = reuseIdentifier;
        }

        public Layout ContentView
        {
            get
            {
                if (_contentView == null)
                {
                    _contentView = new Grid();
                    this.Add(_contentView);
                    this.InvalidateMeasure();
                }

                return _contentView;
            }
        }

        public Label TextLabel
        {
            get
            {
                if (_textLabel == null)
                {
                    _textLabel = new Label() { VerticalTextAlignment = TextAlignment.Center, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
                    _textLabel.BackgroundColor = Colors.Transparent;
                    _textLabel.TextColor = Colors.Black;
                    //_textLabel.HighlightedTextColor = UIColor.White;
                    _textLabel.FontSize = 18;
                    _textLabel.FontAttributes = FontAttributes.Bold;
                    this.ContentView.Add(_textLabel);
                    this.InvalidateMeasure();
                }

                return _textLabel;
            }
        }

        void _setHighlighted(bool highlighted, IList<IView> subviews)
        {
            foreach (var view in subviews)
            {
                if (view is IHighlightable)
                {
                    (view as IHighlightable).setHighlighted(highlighted);
                }

                if (view is Layout)
                    this._setHighlighted(highlighted, (view as Layout).Children);
            }
        }

        void _updateSelectionState()
        {
            bool shouldHighlight = _highlighted || _selected;
            if (_selectedBackgroundView != null)
                _selectedBackgroundView.IsVisible = shouldHighlight;
            this._setHighlighted(shouldHighlight, this.Children);
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

        public View BackgroundView
        {
            get => _backgroundView;
            set
            {
                if (value != _backgroundView)
                {
                    _backgroundView.RemoveFromSuperview();
                    _backgroundView = value;
                    this.Add(_backgroundView);
                    this.BackgroundColor = Colors.Transparent;
                }
            }
        }

        public View SelectedBackgroundView
        {
            get => _selectedBackgroundView;
            set
            {
                if (value != _selectedBackgroundView)
                {
                    if (_selectedBackgroundView != null)
                        _selectedBackgroundView.RemoveFromSuperview();
                    _selectedBackgroundView = value;
                    _selectedBackgroundView.IsVisible = _selected;
                    this.Add(_selectedBackgroundView);
                }
            }
        }

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
