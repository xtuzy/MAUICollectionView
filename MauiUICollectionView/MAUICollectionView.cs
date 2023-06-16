using MauiUICollectionView.Layouts;
using UIView = Microsoft.Maui.Controls.Layout;
namespace MauiUICollectionView
{
    public partial class MAUICollectionView : ScrollView
    {
        #region https://github.com/BigZaphod/Chameleon/blob/master/UIKit/Classes/UITableView.h

        MAUICollectionViewViewHolder _headerView;
        public MAUICollectionViewViewHolder HeaderView
        {
            get => _headerView;
            set
            {
                if (value != _headerView)
                {
                    _headerView = null;
                    _headerView = value;
                    this.AddSubview(_headerView.ContentView);
                }
            }
        }

        MAUICollectionViewViewHolder _footerView;
        public MAUICollectionViewViewHolder FooterView
        {
            get => _footerView;
            set
            {
                if (value != _footerView)
                {
                    _footerView = null;
                    _footerView = value;
                    this.AddSubview(_footerView.ContentView);
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
        /// 当前正在显示区域中的Items
        /// </summary>
        public Dictionary<NSIndexPath, MAUICollectionViewViewHolder> PreparedItems;
        /// <summary>
        /// 回收的等待重复利用的ViewHolder
        /// </summary>
        public List<MAUICollectionViewViewHolder> ReusableViewHolders;

        SourceHas _sourceHas;
        struct SourceHas
        {
            public bool numberOfSectionsInCollectionView = true;
            public bool numberOfItemsInSection = true;
            public bool heightForRowAtIndexPath = true;
            public bool willSelectRowAtIndexPath = true;
            public bool didSelectRowAtIndexPath = true;
            public bool willDeselectRowAtIndexPath = true;
            public bool didDeselectRowAtIndexPath = true;

            public SourceHas()
            {
            }
        }

        void Init()
        {
            this.PreparedItems = new();
            this.ReusableViewHolders = new();
            this.HorizontalScrollBarVisibility = ScrollBarVisibility.Never;
            this.allowsSelection = true;
            this.allowsSelectionDuringEditing = false;
            this._sectionHeaderHeight = this._sectionFooterHeight = 22;

            this._setNeedsReload();
        }

        IMAUICollectionViewSource _source;
        public IMAUICollectionViewSource Source
        {
            get { return this._source; }
            set
            {
                _source = value;

                _sourceHas.numberOfSectionsInCollectionView = _source.numberOfSectionsInCollectionView != null;
                _sourceHas.numberOfItemsInSection = _source.numberOfItemsInSection != null;

                _sourceHas.heightForRowAtIndexPath = _source.heightForRowAtIndexPath != null;
                _sourceHas.willSelectRowAtIndexPath = _source.willSelectRowAtIndexPath != null;
                _sourceHas.didSelectRowAtIndexPath = _source.didSelectRowAtIndexPath != null;
                _sourceHas.willDeselectRowAtIndexPath = _source.willDeselectRowAtIndexPath != null;
                _sourceHas.didDeselectRowAtIndexPath = _source.didDeselectRowAtIndexPath != null;

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
        public int ExtendHeight => (int)CollectionViewConstraintSize.Height;
#endif
        Rect _CGRectFromVerticalOffset(float offset, float height)
        {
            return new Rect(0, offset, this.Bounds.Width > 0 ? this.Bounds.Width : CollectionViewConstraintSize.Width, height);
        }

        public MAUICollectionViewViewHolder CellForRowAtIndexPath(NSIndexPath indexPath)
        {
            // this is allowed to return nil if the cell isn't visible and is not restricted to only returning visible cells
            // so this simple call should be good enough.
            if (indexPath == null) return null;
            return PreparedItems.ContainsKey(indexPath) ? PreparedItems[indexPath] : null;
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

        /// <summary>
        /// Reloads all the data and views in the collection view
        /// </summary>
        public void ReloadData()
        {
            // clear the caches and remove the cells since everything is going to change
            foreach (var cell in PreparedItems.Values)
            {
                cell.PrepareForReuse();
                ReusableViewHolders.Add(cell);
            }

            PreparedItems.Clear();

            // clear prior selection
            this._selectedRow = null;
            this._highlightedRow = null;

            _reloadDataCounts();//Section或者Item数目可能变化了, 重新加载

            this._needsReload = false;
            (this as IView).InvalidateMeasure();
        }

        /// <summary>
        /// 切换页面返回时, 数据可能不再展示, 因此需要强制重新加载
        /// </summary>
        public void ReAppear()
        {
            foreach (var cell in PreparedItems.Values)
            {
                cell.PrepareForReuse();
                ReusableViewHolders.Add(cell);
            }

            PreparedItems.Clear();

            _reloadDataCounts();
            this._needsReload = false;

            (this as IView).InvalidateMeasure();
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

        public partial Size OnContentViewMeasure(double widthConstraint, double heightConstraint)
        {
            this._reloadDataIfNeeded();
            Size size;
            if (ItemsLayout != null)
                if (ItemsLayout.ScrollDirection == ItemsLayoutOrientation.Vertical)
                    size = ItemsLayout.MeasureContents(widthConstraint, CollectionViewConstraintSize.Height);
                else
                    size = ItemsLayout.MeasureContents(CollectionViewConstraintSize.Width, heightConstraint);
            else
                size = new Size(0, 0);
            return size;
        }

        public SizeRequest MeasureChild(Element element, double widthConstraint, double heightConstraint)
        {
            return (element as IView).Measure(widthConstraint, heightConstraint);
        }

        public partial void OnContentViewLayout()
        {
            if (_backgroundView != null)
                LayoutChild(_backgroundView, Bounds);
            ItemsLayout?.ArrangeContents();
        }

        public void LayoutChild(Element element, Rect rect)
        {
            (element as IView).Arrange(rect);
        }

        public NSIndexPath IndexPathForSelectedRow()
        {
            return _selectedRow;
        }

        public NSIndexPath IndexPathForCell(MAUICollectionViewViewHolder cell)
        {
            foreach (NSIndexPath index in PreparedItems.Keys)
            {
                if (PreparedItems[index] == cell)
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

        public void SelectRowAtIndexPath(NSIndexPath indexPath, bool animated, ScrollPosition scrollPosition)
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
            var source = (this.Source as IMAUICollectionViewSource);
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

            this.SelectRowAtIndexPath(rowToSelect, false, ScrollPosition.None);

            if (_sourceHas.didSelectRowAtIndexPath)
            {
                source.didSelectRowAtIndexPath(this, rowToSelect);
            }
        }

        void _scrollRectToVisible(Rect aRect, ScrollPosition scrollPosition, bool animated)
        {
            if (!(aRect == Rect.Zero) && aRect.Size.Height > 0)
            {
                // adjust the rect based on the desired scroll position setting
                switch (scrollPosition)
                {
                    case ScrollPosition.None:
                        break;

                    case ScrollPosition.Top:
                        aRect.Height = this.Bounds.Size.Height;
                        break;

                    case ScrollPosition.Middle:
                        aRect.Y -= (this.Bounds.Size.Height / 2.0f) - aRect.Size.Height;
                        aRect.Height = this.Bounds.Size.Height;
                        break;

                    case ScrollPosition.Bottom:
                        aRect.Y -= this.Bounds.Size.Height - aRect.Size.Height;
                        aRect.Height = this.Bounds.Size.Height;
                        break;
                }

                //this.ScrollRectToVisible(aRect, animated: animated);
                this.ScrollToAsync(aRect.X, aRect.Y, true);
            }
        }

        public void ScrollToRowAtIndexPath(NSIndexPath indexPath, ScrollPosition scrollPosition, bool animated)
        {
            var rect = ItemsLayout.RectForRowOfIndexPathInContentView(indexPath);
            switch (scrollPosition)
            {
                case ScrollPosition.None:
                case ScrollPosition.Top:
                    ScrollToAsync(0, rect.Top, animated);
                    break;
                case ScrollPosition.Middle:
                    ScrollToAsync(0, rect.Y + rect.Height / 2, animated);
                    break;
                case ScrollPosition.Bottom:
                    ScrollToAsync(0, rect.Bottom, animated);
                    break;
            }
        }

        #region 复用
        public MAUICollectionViewViewHolder DequeueRecycledViewHolderWithIdentifier(string identifier)
        {
            foreach (MAUICollectionViewViewHolder viewHolder in ReusableViewHolders)
            {
                if (viewHolder.ReuseIdentifier == identifier)
                {
                    ReusableViewHolders.Remove(viewHolder);

                    viewHolder.PrepareForReuse();
                    return viewHolder;
                }
            }

            return null;
        }

        public void RecycleViewHolder(MAUICollectionViewViewHolder viewHolder)
        {
            ReusableViewHolders.Add(viewHolder);
            viewHolder.ContentView.RemoveFromSuperview();
        }

        #endregion

        #region 数据
        /// <summary>
        /// 缓存Section和Item的数据, 从Source获取的是最新的, 在数据更新时, 这里存储的是旧的
        /// </summary>
        private List<int> sections = new();

        public void _reloadDataCounts()
        {
            this.sections = fetchDataCounts();
        }

        private List<int> fetchDataCounts()
        {
            var res = new List<int>();
            if (_sourceHas.numberOfSectionsInCollectionView && _sourceHas.numberOfItemsInSection)
            {
                var sCount = Source.numberOfSectionsInCollectionView(this);

                for (var sIndex = 0; sIndex < sCount; sIndex++)
                {
                    res.Add(Source.numberOfItemsInSection(this, sIndex));
                }
            }
            return res;
        }

        /// <summary>
        /// Returns the number of sections displayed by the collection view.
        /// </summary>
        /// <returns>The number of sections</returns>
        public int NumberOfSections()
        {
            return sections.Count;
        }

        /// <summary>
        /// Returns the number of items in the specified section.
        /// </summary>
        /// <param name="section">The index of the section for which you want a count of the items.</param>
        /// <returns> The number of items in the specified section</returns>
        public int NumberOfItemsInSection(int section)
        {
            return sections.Count > section ? sections[section] : 0;
        }

        #endregion

        #region 操作

        private void InsertSections(int[] sections, RowAnimation animation)
        {
            this.ReloadData();
        }

        private void DeleteSections(int[] sections, RowAnimation animation)
        {
            this.ReloadData();
        }

        /// <summary>
        /// Insert items at specific index paths
        /// </summary>
        /// <param name="indexPaths">The index paths at which to insert items.</param>
        /// <param name="animation"></param>
        private void InsertItems(NSIndexPath[] indexPaths, bool animate)
        {
            if (indexPaths == null || indexPaths.Length == 0) { return; }
            beginUpdates();
            _updateContext.items.inserted.AddRange(indexPaths);
            endUpdates(animate);
        }

        private void DeleteItems(NSIndexPath[] indexPaths, bool animation)
        {
            this.ReloadData();
        }

        private UpdateContext _updateContext = new UpdateContext();
        private int _editing = 0;

        void beginUpdates()
        {
            if (_editing == 0)
            {
                _updateContext.Reset();
            }
            _editing += 1;
        }

        void endUpdates(bool animated, Action completion = null)
        {
            if (_editing > 1)
            {
                _editing -= 1;
                return;
            }
            _editing = 0;

            if (_updateContext.IsEmpty)
            {
                completion?.Invoke();
                return;
            }

            var oldData = this.sections;
            _reloadDataCounts();
            var newData = this.sections;

            foreach (var idx in _updateContext.reloadSections)
            {
                // Reuse existing operation to reload, delete, and insert items in the section as needed
                var oldCount = oldData[idx];
                var newCount = newData[idx];
                var shared = Math.Min(oldCount, newCount);
                var update = NSIndexPath.InRange((0, shared - 1), section: idx);
                this._updateContext.reloadedItems.AddRange(update);
                if (oldCount > newCount)
                {
                    var delete = NSIndexPath.InRange((shared, oldCount - 1), section: idx);
                    this._updateContext.items.deleted.AddRange(delete);
                }
                else if (oldCount < newCount)
                {
                    var insert = NSIndexPath.InRange((shared, newCount - 1), section: idx);
                    this._updateContext.items.inserted.AddRange(insert);
                }
            }

            if (!this._updateContext.items.IsEmpty || !this._updateContext.sections.IsEmpty)
            {
                if (_updateContext.reloadedItems.Count != 0)
                {
                    foreach (var ip in _updateContext.reloadedItems)
                    {
                        //if (!this.contentDocumentView.preparedCellIndex.TryGetValue(ip, out var cell)) continue;
                        //this.contentDocumentView.preparedCellIndex[ip] = _prepareReplacementCell(cell, ip);
                    }
                    //this.ReloadLayout(animated, ScrollPosition.None, completion);
                }
                else
                {
                    completion?.Invoke();
                }
                return;
            }

        }

        private struct ItemTracker
        {
            public List<NSIndexPath> inserted;
            public List<NSIndexPath> deleted;
            public Dictionary<NSIndexPath, NSIndexPath> moved;
            public bool IsEmpty
            {
                get
                {
                    return deleted.Count == 0 && inserted.Count == 0 && moved.Count == 0;
                }
            }
        }

        private struct SectionTracker
        {
            public List<int> deleted; // Original Indexes for deleted sections
            public List<int> inserted; // Destination Indexes for inserted sections
            public Dictionary<int, int> moved; // Source and Destination indexes for moved sections
            public bool IsEmpty
            {
                get
                {
                    return deleted.Count == 0 && inserted.Count == 0 && moved.Count == 0;
                }
            }
        }

        private class SectionValidator : IEquatable<SectionValidator>, ICustomFormatter
        {
            public int? source;
            public int? target;
            public int count = 0;

            public int estimatedCount
            {
                get
                {
                    if (this.target == null) return 0;
                    if (this.source == null) return count;
                    return count + (inserted.Count + movedIn.Count) - (removed.Count + movedOut.Count);
                }
            }

            public List<int> inserted = new List<int>();
            public List<int> removed = new List<int>();
            public List<int> movedOut = new List<int>();
            public List<int> movedIn = new List<int>();
            public Dictionary<int, int> moves = new Dictionary<int, int>();

            public SectionValidator(int? source, int? target, int count)
            {
                this.source = source;
                this.target = target;
                this.count = count;
            }

            bool IEquatable<SectionValidator>.Equals(SectionValidator other)
            {
                return this.source == other.source && this.target == other.target;
            }

            string ICustomFormatter.Format(string format, object arg, IFormatProvider formatProvider)
            {
                return $"Source: {source ?? -1} Target: {this.target ?? -1} Count: {count} expected: {estimatedCount}";
            }
        }

        private struct UpdateContext
        {
            public SectionTracker sections;
            public ItemTracker items;
            public List<NSIndexPath> reloadedItems; // Track reloaded items to reload after adjusting IPs
            public List<int> reloadSections;

            public List<ItemUpdate> updates;

            public void Reset()
            {
                this.items = new ItemTracker();
                this.sections = new SectionTracker();
                updates.Clear();
                reloadedItems.Clear();
            }

            public bool IsEmpty
            {
                get
                {
                    return sections.IsEmpty && items.IsEmpty && reloadedItems.Count == 0 && reloadSections.Count == 0;
                }
            }
        }

        private struct ItemUpdate : IEquatable<ItemUpdate>
        {
            public enum UpdateType
            {
                Insert,
                Remove,
                Update
            }

            public MAUICollectionViewViewHolder View { get; }
            public MAUICollectionViewViewHolder.ItemAttribute? _Attrs { get; }
            public NSIndexPath IndexPath { get; }
            public UpdateType Type { get; }

            private MAUICollectionViewViewHolder.ItemAttribute GetAttrs()
            {
                if (_Attrs != null)
                {
                    return _Attrs;
                }

                var cv = View.ContentView.Parent as MAUICollectionView;

                MAUICollectionViewViewHolder.ItemAttribute a = null;
                a = cv.LayoutAttributesForItem(IndexPath);

                a = a ?? View.Attributes;
                if (a == null)
                {
                    throw new InvalidOperationException("Internal error: unable to find layout attributes for view at " + IndexPath);
                }
                return a;
            }

            public bool Equals(ItemUpdate other) =>
                View == other.View &&
                Type == other.Type &&
                IndexPath == other.IndexPath;

            public override int GetHashCode() => View.GetHashCode();
        }

        private MAUICollectionViewViewHolder.ItemAttribute LayoutAttributesForItem(NSIndexPath indexPath)
        {
            //ItemsLayout.LayoutAttributesForItem(indexPath);
            return null;
        }
        #endregion

        #region 操作

        /// <summary>
        /// 通知CollectionView移除某Item, 需要做出改变.
        /// 移除会让移除项后面的Item需要调节高度, 这个高度需要动画改变
        /// </summary>
        /// <param name="indexPaths"></param>
        public void RemoveItems(NSIndexPath indexPaths)
        {
            var Updates = ItemsLayout.Updates;
            Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.remove, source = indexPaths });

            //找到已经可见的Item和它们的IndexPath,和目标IndexPath
            foreach (var visiableItem in PreparedItems)
            {
                if (visiableItem.Key.Section == indexPaths.Section)//同一section的item才变化
                {
                    if (visiableItem.Key.Row > indexPaths.Row)//大于移除item的row的需要更新IndexPath
                    {
                        Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.move, source = visiableItem.Key, target = NSIndexPath.FromRowSection(visiableItem.Key.Row - 1, visiableItem.Key.Section) });
                    }
                }
            }
            _reloadDataCounts();
        }

        /// <summary>
        /// 通知CollectionView插入了Item, 需要做出改变.
        /// </summary>
        /// <param name="indexPaths">插入应该是在某个位置插入, 比如0, 即插入在0位置</param>
        public void InsertItems(NSIndexPath indexPaths)
        {
            var Updates = ItemsLayout.Updates;
            //找到已经可见的Item和它们的IndexPath,和目标IndexPath
            foreach (var visiableItem in PreparedItems)
            {
                if (visiableItem.Key.Section == indexPaths.Section)//同一section的item才变化
                {
                    if (visiableItem.Key.Row >= indexPaths.Row)//大于等于item的row的需要更新IndexPath
                    {
                        Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.move, source = visiableItem.Key, target = NSIndexPath.FromRowSection(visiableItem.Key.Row + 1, visiableItem.Key.Section) });
                    }
                }
            }
            //先move后面的, 再插入
            Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.insert, source = indexPaths });

            _reloadDataCounts();
        }

        public void MoveItem(NSIndexPath indexPath, NSIndexPath toIndexPath)
        {
            var Updates = ItemsLayout.Updates;
            Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.move, source = indexPath, target = toIndexPath });

            //如果同Section, Move影响的只是之间的
            if (indexPath.Section == toIndexPath.Section)
            {
                var isUpMove = indexPath.Row > toIndexPath.Row;
                //先移除
                foreach (var visiableItem in PreparedItems)
                {
                    if (visiableItem.Key.Section == indexPath.Section)//同一section的item才变化
                    {
                        if (isUpMove)//从底部向上移动, 目标位置下面的都需要向下移动
                        {
                            if (visiableItem.Key.Row >= toIndexPath.Row && visiableItem.Key.Row < indexPath.Row)
                            {
                                Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.move, source = visiableItem.Key, target = NSIndexPath.FromRowSection(visiableItem.Key.Row + 1, visiableItem.Key.Section) });
                            }
                        }
                        else
                        {
                            if (visiableItem.Key.Row > indexPath.Row && visiableItem.Key.Row <= toIndexPath.Row)
                            {
                                Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.move, source = visiableItem.Key, target = NSIndexPath.FromRowSection(visiableItem.Key.Row - 1, visiableItem.Key.Section) });
                            }
                        }

                    }
                }
            }
            //如果不同Section, 则影响不同的section后面的
            else
            {
                //先移除, 移除的Item后面的Item需要向前移动
                foreach (var visiableItem in PreparedItems)
                {
                    if (visiableItem.Key.Section == indexPath.Section)
                    {
                        if (visiableItem.Key.Row > indexPath.Row)
                        {
                            Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.move, source = visiableItem.Key, target = NSIndexPath.FromRowSection(visiableItem.Key.Row - 1, visiableItem.Key.Section) });
                        }
                    }
                }
                //后插入, 后面的需要向后移动
                foreach (var visiableItem in PreparedItems)
                {
                    if (visiableItem.Key.Section == toIndexPath.Section)
                    {
                        if (visiableItem.Key.Row >= toIndexPath.Row)
                        {
                            Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.move, source = visiableItem.Key, target = NSIndexPath.FromRowSection(visiableItem.Key.Row + 1, visiableItem.Key.Section) });
                        }
                    }
                }
            }
            _reloadDataCounts();
        }

        public void ChangeItem(IEnumerable<NSIndexPath> indexPaths)
        {
            var Updates = ItemsLayout.Updates;
            foreach (var visiableItem in PreparedItems)
            {
                if (indexPaths.Contains(visiableItem.Key))//如果可见的Items包含需要更新的Item
                {
                    Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.update, source = visiableItem.Key });
                }
            }
        }
        #endregion
    }
}