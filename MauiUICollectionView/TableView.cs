using MauiUICollectionView.Layouts;
using System.Diagnostics;
using UIView = Microsoft.Maui.Controls.Layout;
namespace MauiUICollectionView
{
    public enum TableViewScrollPosition
    {
        None, Top, Middle, Bottom
    }

    public enum TableViewRowAnimation
    {
        Fade, Right, Left, Top, Bottom, None, Middle, Automatic = 100
    }

    public partial class TableView : ScrollView
    {
        // http://stackoverflow.com/questions/235120/whats-the-uitableview-index-magnifying-glass-character
        const string UITableViewIndexSearch = @"{search}";

        static readonly float _UITableViewDefaultRowHeight = 43;

        #region https://github.com/BigZaphod/Chameleon/blob/master/UIKit/Classes/UITableView.h

        Color separatorColor;

        TableViewViewHolder _tableHeaderView;
        public TableViewViewHolder TableHeaderView
        {
            get => _tableHeaderView;
            set
            {
                if (value != _tableHeaderView)
                {
                    _tableHeaderView = null;// _tableHeaderView?.Dispose();
                    _tableHeaderView = value;
                    this.AddSubview(_tableHeaderView.ContentView);
                }
            }
        }

        TableViewViewHolder _tableFooterView;
        public TableViewViewHolder TableFooterView
        {
            get => _tableFooterView;
            set
            {
                if (value != _tableFooterView)
                {
                    _tableFooterView = null;//_tableFooterView?.Dispose();
                    _tableFooterView = value;
                    this.AddSubview(_tableFooterView.ContentView);
                }
            }
        }

        UIView _backgroundView;
        bool allowsSelection;
        bool allowsSelectionDuringEditing;
        bool editing;

        float _sectionHeaderHeight;
        float _sectionFooterHeight;
        #endregion

        bool _needsReload;
        public NSIndexPath _selectedRow;
        public NSIndexPath _highlightedRow;
        /// <summary>
        /// 当前正在显示区域中的Cell
        /// </summary>
        public Dictionary<NSIndexPath, TableViewViewHolder> _cachedCells;
        /// <summary>
        /// 回收的等待重复利用的Cell
        /// </summary>
        public List<TableViewViewHolder> _reusableCells;
        /// <summary>
        /// 存储Cell的临时数据
        /// </summary>
        public List<TableViewSection> _sections;

        SourceHas _sourceHas;
        struct SourceHas
        {
            public bool numberOfSectionsInTableView = true;
            public bool titleForHeaderInSection = true;
            public bool titleForFooterInSection = true;
            public bool commitEditingStyle = true;
            public bool canEditRowAtIndexPath = true;

            public bool heightForRowAtIndexPath = true;
            public bool heightForHeaderInSection = true;
            public bool heightForFooterInSection = true;
            public bool viewForHeaderInSection = true;
            public bool viewForFooterInSection = true;
            public bool willSelectRowAtIndexPath = true;
            public bool didSelectRowAtIndexPath = true;
            public bool willDeselectRowAtIndexPath = true;
            public bool didDeselectRowAtIndexPath = true;
            public bool willBeginEditingRowAtIndexPath = true;
            public bool didEndEditingRowAtIndexPath = true;
            public bool titleForDeleteConfirmationButtonForRowAtIndexPath = true;

            public SourceHas()
            {
            }
        }

        void Init()
        {
            this._cachedCells = new();
            this._sections = new();
            this._reusableCells = new();
            this.separatorColor = new Color(red: 0.88f, green: 0.88f, blue: 0.88f, alpha: 1);
            this.HorizontalScrollBarVisibility = ScrollBarVisibility.Never;
            this.allowsSelection = true;
            this.allowsSelectionDuringEditing = false;
            this._sectionHeaderHeight = this._sectionFooterHeight = 22;

            this._setNeedsReload();
        }

        ITableViewSource _source;
        public ITableViewSource Source
        {
            get { return this._source; }
            set
            {
                _source = value;

                _sourceHas.numberOfSectionsInTableView = _source.numberOfSectionsInTableView != null;
                _sourceHas.titleForHeaderInSection = _source.titleForHeaderInSection != null;
                _sourceHas.titleForFooterInSection = _source.titleForFooterInSection != null;
                _sourceHas.commitEditingStyle = _source.commitEditingStyle != null;
                _sourceHas.canEditRowAtIndexPath = _source.canEditRowAtIndexPath != null;

                _sourceHas.heightForRowAtIndexPath = _source.heightForRowAtIndexPath != null;
                _sourceHas.heightForHeaderInSection = _source.heightForHeaderInSection != null;
                _sourceHas.heightForFooterInSection = _source.heightForFooterInSection != null;
                _sourceHas.viewForHeaderInSection = _source.viewForHeaderInSection != null;
                _sourceHas.viewForFooterInSection = _source.viewForFooterInSection != null;
                _sourceHas.willSelectRowAtIndexPath = _source.willSelectRowAtIndexPath != null;
                _sourceHas.didSelectRowAtIndexPath = _source.didSelectRowAtIndexPath != null;
                _sourceHas.willDeselectRowAtIndexPath = _source.willDeselectRowAtIndexPath != null;
                _sourceHas.didDeselectRowAtIndexPath = _source.didDeselectRowAtIndexPath != null;
                _sourceHas.willBeginEditingRowAtIndexPath = _source.willBeginEditingRowAtIndexPath != null;
                _sourceHas.didEndEditingRowAtIndexPath = _source.didEndEditingRowAtIndexPath != null;
                _sourceHas.titleForDeleteConfirmationButtonForRowAtIndexPath = _source.titleForDeleteConfirmationButtonForRowAtIndexPath != null;

                this._setNeedsReload();
            }
        }

        public CollectionViewLayout ItemsLayout;

        /// <summary>
        /// 屏幕顶部和底部多加载Cell的高度, 对于平台ScrollView实现是平移画布的(Android, Windows), 大的扩展高度可以减少滑动时显示空白, 默认设置上下各扩展一屏幕高度.
        /// </summary>
#if IOS || MACCATALYST
        public int ExtendHeight = 0;
#else
        public int ExtendHeight => (int)TableViewConstraintSize.Height;
#endif
        Rect _CGRectFromVerticalOffset(float offset, float height)
        {
            return new Rect(0, offset, this.Bounds.Width > 0 ? this.Bounds.Width : TableViewConstraintSize.Width, height);
        }

        void beginUpdates() { }

        void endUpdates() { }

        public TableViewViewHolder CellForRowAtIndexPath(NSIndexPath indexPath)
        {
            // this is allowed to return nil if the cell isn't visible and is not restricted to only returning visible cells
            // so this simple call should be good enough.
            if (indexPath == null) return null;
            return _cachedCells.ContainsKey(indexPath) ? _cachedCells[indexPath] : null;
        }

        public void setBackgroundView(UIView backgroundView)
        {
            if (_backgroundView != backgroundView)
            {
                _backgroundView = null;//_backgroundView?.Dispose();
                _backgroundView = backgroundView;
                this.InsertSubview(_backgroundView, 0);
            }
        }

        public int NumberOfSections()
        {
            if (_sourceHas.numberOfSectionsInTableView)
            {
                return _source.numberOfSectionsInTableView(this);
            }
            else
            {
                return 1;
            }
        }

        public int NumberOfRowsInSection(int section)
        {
            return _source.numberOfRowsInSection(this, section);
        }

        /// <summary>
        /// 清空字典里存储的View, 并且从ScrollView里移除, 重新统计高度
        /// </summary>
        public void ReloadData()
        {
            // clear the caches and remove the cells since everything is going to change
            foreach (var cell in _cachedCells.Values)
                cell.ContentView.RemoveFromSuperview();
            _reusableCells.ForEach((v) => v.ContentView.RemoveFromSuperview());
            _reusableCells.Clear();
            _cachedCells.Clear();

            // clear prior selection
            this._selectedRow = null;
            this._highlightedRow = null;

            // trigger the section cache to be repopulated
            for (var i = 0; i < NumberOfSections(); i++)
            {
                var section = new TableViewSection();
                section.numberOfRows = NumberOfRowsInSection(i);
                section._rowHeights = new double[section.numberOfRows];
                _sections.Add(section);
            }

            this._needsReload = false;
        }

        void _reloadDataIfNeeded()
        {
            if (_needsReload)
            {
                this.ReloadData();
            }
        }

        void _setNeedsReload()
        {
            _needsReload = true;
            this.InvalidateMeasure();
        }

        Stopwatch stopwatch = new Stopwatch();
        public partial Size OnMeasure(double widthConstraint, double heightConstraint)
        {
            stopwatch.Restart();
            this._reloadDataIfNeeded();
            Size size;
            if (ItemsLayout != null)
                if (ItemsLayout.ScrollDirection == ItemsLayoutOrientation.Vertical)
                    size= ItemsLayout.MeasureContents(widthConstraint, TableViewConstraintSize.Height);
                else
                    size= ItemsLayout.MeasureContents(TableViewConstraintSize.Width, heightConstraint);
            else
                size= new Size(0, 0);
            stopwatch.Stop();
            Console.WriteLine($"Measure:{stopwatch.ElapsedMilliseconds}");
            return size;
        }

        public SizeRequest MeasureChild(Element element, double widthConstraint, double heightConstraint)
        {
            return (element as IView).Measure(widthConstraint, heightConstraint);
        }

        public partial void OnLayout()
        {
            if (_backgroundView != null)
                LayoutChild(_backgroundView, Bounds);
            stopwatch.Restart();
            ItemsLayout?.ArrangeContents();
            stopwatch.Stop();
            Console.WriteLine($"Layout:{stopwatch.ElapsedMilliseconds}");
        }

        public void LayoutChild(Element element, Rect rect)
        {
            (element as IView).Arrange(rect);
        }

        public NSIndexPath IndexPathForSelectedRow()
        {
            return _selectedRow;
        }

        public NSIndexPath IndexPathForCell(TableViewViewHolder cell)
        {
            foreach (NSIndexPath index in _cachedCells.Keys)
            {
                if (_cachedCells[index] == cell)
                {
                    return index;
                }
            }

            return null;
        }

        public void DeselectRowAtIndexPath(NSIndexPath indexPath, bool animated)
        {
            if (indexPath != null && indexPath == _selectedRow)
            {
                var cell = this.CellForRowAtIndexPath(_selectedRow);
                if (cell != null) cell.Selected = false;
                _selectedRow = null;
            }
        }

        public void SelectRowAtIndexPath(NSIndexPath indexPath, bool animated, TableViewScrollPosition scrollPosition)
        {
            // unlike the other methods that I've tested, the real UIKit appears to call reload during selection if the table hasn't been reloaded
            // yet. other methods all appear to rebuild the section cache "on-demand" but don't do a "proper" reload. for the sake of attempting
            // to maintain a similar delegate and dataSource access pattern to the real thing, I'll do it this way here. :)
            this._reloadDataIfNeeded();

            if (_selectedRow != indexPath)
            {
                this.DeselectRowAtIndexPath(_selectedRow, animated);
                _selectedRow = indexPath;
                var cell = this.CellForRowAtIndexPath(_selectedRow);
                if (cell != null)//TODO:不知道为什么有时候为空
                    cell.Selected = true;
            }

            // I did not verify if the real UIKit will still scroll the selection into view even if the selection itself doesn't change.
            // this behavior was useful for Ostrich and seems harmless enough, so leaving it like this for now.
            //this.ScrollToRowAtIndexPath(_selectedRow, scrollPosition, animated);
        }

        void _setUserSelectedRowAtIndexPath(NSIndexPath rowToSelect)
        {
            var source = (this.Source as ITableViewSource);
            if (_sourceHas.willSelectRowAtIndexPath)
            {
                rowToSelect = source.willSelectRowAtIndexPath(this, rowToSelect);
            }

            NSIndexPath selectedRow = this.IndexPathForSelectedRow();

            if (selectedRow != null && !(selectedRow == rowToSelect))
            {
                NSIndexPath rowToDeselect = selectedRow;

                if (_sourceHas.willDeselectRowAtIndexPath)
                {
                    rowToDeselect = source.willDeselectRowAtIndexPath(this, rowToDeselect);
                }

                this.DeselectRowAtIndexPath(rowToDeselect, false);

                if (_sourceHas.didDeselectRowAtIndexPath)
                {
                    source.didDeselectRowAtIndexPath(this, rowToDeselect);
                }
            }

            this.SelectRowAtIndexPath(rowToSelect, false, TableViewScrollPosition.None);

            if (_sourceHas.didSelectRowAtIndexPath)
            {
                source.didSelectRowAtIndexPath(this, rowToSelect);
            }
        }

        void _scrollRectToVisible(Rect aRect, TableViewScrollPosition scrollPosition, bool animated)
        {
            if (!(aRect == Rect.Zero) && aRect.Size.Height > 0)
            {
                // adjust the rect based on the desired scroll position setting
                switch (scrollPosition)
                {
                    case TableViewScrollPosition.None:
                        break;

                    case TableViewScrollPosition.Top:
                        aRect.Height = this.Bounds.Size.Height;
                        break;

                    case TableViewScrollPosition.Middle:
                        aRect.Y -= (this.Bounds.Size.Height / 2.0f) - aRect.Size.Height;
                        aRect.Height = this.Bounds.Size.Height;
                        break;

                    case TableViewScrollPosition.Bottom:
                        aRect.Y -= this.Bounds.Size.Height - aRect.Size.Height;
                        aRect.Height = this.Bounds.Size.Height;
                        break;
                }

                //this.ScrollRectToVisible(aRect, animated: animated);
                this.ScrollToAsync(aRect.X, aRect.Y, true);
            }
        }

        public void ScrollToRowAtIndexPath(NSIndexPath indexPath, TableViewScrollPosition scrollPosition, bool animated)
        {
            throw new NotImplementedException();
        }

        public TableViewViewHolder dequeueReusableCellWithIdentifier(string identifier)
        {
            foreach (TableViewViewHolder cell in _reusableCells)
            {
                if (cell.ReuseIdentifier == identifier)
                {
                    TableViewViewHolder strongCell = cell;

                    // the above strongCell reference seems totally unnecessary, but without it ARC apparently
                    // ends up releasing the cell when it's removed on this line even though we're referencing it
                    // later in this method by way of the cell variable. I do not like this.
                    _reusableCells.Remove(cell);

                    strongCell.PrepareForReuse();
                    return strongCell;
                }
            }

            return null;
        }

        void setEditing(bool editing, bool animated)
        {
            this.editing = editing;
        }

        void setEditing(bool editing)
        {
            this.setEditing(editing, false);
        }

        public void InsertSections(int[] sections, TableViewRowAnimation animation)
        {
            this.ReloadData();
        }

        public void DeleteSections(int[] sections, TableViewRowAnimation animation)
        {
            this.ReloadData();
        }

        /// <summary>
        /// See <see cref="UIKit.UITableView.InsertRows(NSIndexPath[], UIKit.UITableViewRowAnimation)"/>
        /// </summary>
        /// <param name="indexPaths"></param>
        /// <param name="animation"></param>
        public void InsertRowsAtIndexPaths(NSIndexPath[] indexPaths, TableViewRowAnimation animation)
        {
            this.ReloadData();
        }

        public void DeleteRowsAtIndexPaths(NSIndexPath[] indexPaths, TableViewRowAnimation animation)
        {
            this.ReloadData();
        }

        /// <summary>
        /// 可见的区域中的点在哪一行
        /// </summary>
        /// <param name="point">相对于TableView的位置, 可以是在TableView上设置手势获取的位置</param>
        /// <returns></returns>
        public NSIndexPath IndexPathForVisibaleRowAtPointOfTableView(Point point)
        {
            var contentOffset = ScrollY;
            point.Y = point.Y + contentOffset;//相对于content
            return IndexPathForRowAtPointOfContentView(point);
        }

        /// <summary>
        /// 迭代全部内容计算点在哪
        /// </summary>
        /// <param name="point">相对与Content的位置</param>
        /// <returns></returns>
        public NSIndexPath IndexPathForRowAtPointOfContentView(Point point)
        {
            double totalHeight = 0;
            double tempBottom = 0;
            if (_tableHeaderView != null)
            {
                tempBottom = totalHeight + _tableHeaderView.ContentView.DesiredSize.Height;
                if (totalHeight <= point.Y && tempBottom >= point.Y)
                {
                    return null;
                }
                totalHeight = tempBottom;
            }

            var number = NumberOfSections();
            for (int section = 0; section < number; section++)
            {
                TableViewSection sectionRecord = _sections[section];
                int numberOfRows = sectionRecord.numberOfRows;
                for (int row = 0; row < numberOfRows; row++)
                {
                    tempBottom = totalHeight + sectionRecord._rowHeights[row];
                    if (totalHeight <= point.Y && tempBottom >= point.Y)
                    {
                        return NSIndexPath.FromRowSection(row, section);
                    }
                    else
                    {
                        totalHeight = tempBottom;
                    }
                }
            }

            return null;
        }

        /*public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);
        }

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            base.TouchesMoved(touches, evt);
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);
            if (_highlightedRow == null)
            {
                UITouch touch = touches.AnyObject as UITouch;
                CGPoint location = touch.LocationInView(this);

                _highlightedRow = this.IndexPathForRowAtPoint(location);
                if (_highlightedRow != null)
                    this.CellForRowAtIndexPath(_highlightedRow).Highlighted = true;
            }

            if (_highlightedRow != null)
            {
                this.CellForRowAtIndexPath(_highlightedRow).Highlighted = false;
                this._setUserSelectedRowAtIndexPath(_highlightedRow);
                _highlightedRow = null;
            }
        }

        public override void TouchesCancelled(NSSet touches, UIEvent evt)
        {
            base.TouchesCancelled(touches, evt);
            if (_highlightedRow != null)
            {
                this.CellForRowAtIndexPath(_highlightedRow).Highlighted = false;
                _highlightedRow = null;
            }
        }*/

        bool _canEditRowAtIndexPath(NSIndexPath indexPath)
        {
            // it's YES by default until the dataSource overrules
            return _sourceHas.commitEditingStyle && (!_sourceHas.canEditRowAtIndexPath || _source.canEditRowAtIndexPath(this, indexPath));
        }

        void _beginEditingRowAtIndexPath(NSIndexPath indexPath)
        {
            if (this._canEditRowAtIndexPath(indexPath))
            {
                this.editing = true;

                if (_sourceHas.willBeginEditingRowAtIndexPath)
                {
                    (this.Source as ITableViewSource).willBeginEditingRowAtIndexPath(this, indexPath);
                }

                // deferring this because it presents a modal menu and that's what we do everywhere else in Chameleon
                _showEditMenuForRowAtIndexPath(indexPath);//this.PerformSelector(new ObjCRuntime.Selector(nameof(_showEditMenuForRowAtIndexPath)), indexPath, afterDelay: 0, null);
            }
        }

        void _endEditingRowAtIndexPath(NSIndexPath indexPath)
        {
            if (this.editing)
            {
                this.editing = false;

                if (_sourceHas.didEndEditingRowAtIndexPath)
                {
                    (this.Source as ITableViewSource).didEndEditingRowAtIndexPath(this, indexPath);
                }
            }
        }

        void _showEditMenuForRowAtIndexPath(NSIndexPath indexPath)
        {
            // re-checking for safety since _showEditMenuForRowAtIndexPath is deferred. this may be overly paranoid.
            if (this._canEditRowAtIndexPath(indexPath))
            {
                /*UITableViewCell cell = this.cellForRowAtIndexPath(indexPath);
                string menuItemTitle = null;

                // fetch the title for the delete menu item
                if (_delegateHas.titleForDeleteConfirmationButtonForRowAtIndexPath)
                {
                    menuItemTitle = (this.Delegate as UITableViewDelegate).titleForDeleteConfirmationButtonForRowAtIndexPath(this, indexPath);
                }
                if(menuItemTitle.Length == 0)
                {
                    menuItemTitle = @"Delete";
                }
                cell.Highlighted = true;
                UIMenuItem theItem = new UIMenuItem(menuItemTitle, null);// [[NSMenuItem alloc] initWithTitle: menuItemTitle action:NULL keyEquivalent:@""];

                UIMenu menu = UIMenu.Create("", null);// [[NSMenu alloc] initWithTitle: @""];
                menu.AutoenablesItems :NO] ;
                menu.setAllowsContextMenuPlugIns:NO] ;
                menu.AddItem:theItem] ;

                // calculate the mouse's current position so we can present the menu from there since that's normal OSX behavior
                NSPoint mouseLocation = [NSEvent mouseLocation];
                CGPoint screenPoint = [self.window.screen convertPoint: NSPointToCGPoint(mouseLocation) fromScreen: nil];

                // modally present a menu with the single delete option on it, if it was selected, then do the delete, otherwise do nothing
                bool didSelectItem = menu.popUpMenuPositioningItem: nil atLocation: NSPointFromCGPoint(screenPoint) inView: self.window.screen.UIKitView];

                UIApplication.InterruptTouchesInView(nil);

                if (didSelectItem)
                {
                    _dataSource.commitEditingStyle(this, UITableViewCellEditingStyle.Delete, indexPath) ;
                }

                cell.Highlighted = false;*/
            }

            // all done
            this._endEditingRowAtIndexPath(indexPath);
        }
    }
}