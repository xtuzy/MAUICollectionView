using MauiUICollectionView.Layouts;

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
                    this.AddSubview(_headerView);
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
                    this.AddSubview(_footerView);
                }
            }
        }

        View _backgroundView;
        public View BackgroundView
        {
            set
            {
                //如果已经存在, 先移除
                if (_backgroundView != null && _backgroundView != value)
                {
                    if (ContentView.Contains(_backgroundView))
                    {
                        ContentView.Remove(_backgroundView);
                    }
                }

                if (value != null && _backgroundView != value)
                {
                    _backgroundView = value;
                    ContentView.Insert(0, _backgroundView);//插入到底部
                }
            }
        }
        #endregion

        bool _needsReload;
        /// <summary>
        /// 被选择的Item
        /// </summary>
        public List<NSIndexPath> SelectedRow = new();

        /// <summary>
        /// 被拖拽用来排序的Item, 其IndexPath会在拖动时更新
        /// </summary>
        public MAUICollectionViewViewHolder DragedItem;

        /// <summary>
        /// 当前正在布局区域中的Items, 与可见区域不同, 布局区域可能大于可见区域, 因为快速滑动时上下可能出现空白, 为了避免空白需要绘制大于可见区域的
        /// </summary>
        public Dictionary<NSIndexPath, MAUICollectionViewViewHolder> PreparedItems;
        /// <summary>
        /// 回收的等待重复利用的ViewHolder
        /// </summary>
        public List<MAUICollectionViewViewHolder> ReusableViewHolders;
        public int MaxReusableViewHolderCount = 5;

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
            this.Orientation = ScrollOrientation.Vertical;
            this.HorizontalScrollBarVisibility = ScrollBarVisibility.Never;

            this.PreparedItems = new();
            this.ReusableViewHolders = new();

            this._setNeedsReload();
        }


        IMAUICollectionViewSource _source;
        public IMAUICollectionViewSource Source
        {
            get { return this._source; }
            set
            {
                _source = value;

                _sourceHas.numberOfSectionsInCollectionView = _source.NumberOfSections != null;
                _sourceHas.numberOfItemsInSection = _source.NumberOfItems != null;

                _sourceHas.heightForRowAtIndexPath = _source.HeightForItem != null;
                _sourceHas.willSelectRowAtIndexPath = _source.WillSelectItem != null;
                _sourceHas.didSelectRowAtIndexPath = _source.DidSelectItem != null;
                _sourceHas.willDeselectRowAtIndexPath = _source.WillDeselectItem != null;
                _sourceHas.didDeselectRowAtIndexPath = _source.DidDeselectItem != null;

                this._setNeedsReload();
            }
        }

        public CollectionViewLayout ItemsLayout;

        /// <summary>
        /// 屏幕顶部和底部多加载Cell的高度, 对于平台ScrollView实现是平移画布的(Android, Windows), 大的扩展高度可以减少滑动时显示空白, 默认设置上下各扩展一屏幕高度.
        /// </summary>
#if IOS
        public int ExtendHeight => 0;
#else
        public int ExtendHeight => (int)CollectionViewConstraintSize.Height * 0;
#endif
        public MAUICollectionViewViewHolder CellForRowAtIndexPath(NSIndexPath indexPath)
        {
            // this is allowed to return nil if the cell isn't visible and is not restricted to only returning visible cells
            // so this simple call should be good enough.
            if (indexPath == null) return null;
            return PreparedItems.ContainsKey(indexPath) ? PreparedItems[indexPath] : null;
        }

        /// <summary>
        /// Reloads all the data and views in the collection view. 常在非数据末尾的位置插入或者移除大量数据时使用.
        /// </summary>
        public void NotifyDataSetChanged()
        {
            // clear the caches and remove the cells since everything is going to change
            foreach (var cell in PreparedItems.Values)
            {
                RecycleViewHolder(cell);
            }

            PreparedItems.Clear();

            // clear prior selection
            this.SelectedRow.Clear();
            DragedItem = null;

            ReloadDataCount();//Section或者Item数目可能变化了, 重新加载

            this._needsReload = false;
            this.ReMeasure();
        }

        /// <summary>
        /// 切换页面返回时, 数据可能不再展示, 因此需要强制重新加载
        /// </summary>
        public void ReAppear()
        {
            foreach (var cell in PreparedItems.Values)
            {
                RecycleViewHolder(cell);
            }

            PreparedItems.Clear();

            ReloadDataCount();
            this._needsReload = false;

            this.ReMeasure();
        }

        void _reloadDataIfNeeded()
        {
            if (_needsReload)
            {
                this.NotifyDataSetChanged();
            }
        }

        void _setNeedsReload()
        {
            _needsReload = true;
            this.ReMeasure();
        }

        public partial Size OnContentViewMeasure(double widthConstraint, double heightConstraint)
        {
            this._reloadDataIfNeeded();
            Size size = Size.Zero;

            if (ItemsLayout != null)
            {
                if (ItemsLayout.ScrollDirection == ItemsLayoutOrientation.Vertical)
                {
                    if (CollectionViewConstraintSize.Height == 0)
                        CollectionViewConstraintSize.Height = DeviceDisplay.Current.MainDisplayInfo.Height;
                }
                else
                {
                    if (CollectionViewConstraintSize.Width == 0)//首次显示可能没有值, 取屏幕大小
                        CollectionViewConstraintSize.Width = DeviceDisplay.Current.MainDisplayInfo.Width;
                }
                size = ItemsLayout.MeasureContents(CollectionViewConstraintSize.Width != 0 ? CollectionViewConstraintSize.Width : widthConstraint, CollectionViewConstraintSize.Height != 0 ? CollectionViewConstraintSize.Height : heightConstraint);
            }

            if (_backgroundView != null)
            {
                if (size != Size.Zero)
                    MeasureChild(_backgroundView, size.Width, size.Height);
                else
                    MeasureChild(_backgroundView, widthConstraint, heightConstraint);
            }

            return size;
        }

        public SizeRequest MeasureChild(Element element, double widthConstraint, double heightConstraint)
        {
            return (element as IView).Measure(widthConstraint, heightConstraint);
        }

        bool animating = false;
        public partial void OnContentViewLayout()
        {
            if (_backgroundView != null)
                LayoutChild(_backgroundView, ContentView.Bounds);

            ItemsLayout?.ArrangeContents();
        }

        public void LayoutChild(Element element, Rect rect)
        {
            (element as IView).Arrange(rect);
        }

        public List<NSIndexPath> IndexPathForSelectedRow()
        {
            return SelectedRow;
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

        /// <summary>
        /// 取消选择某IndexPath
        /// </summary>
        /// <param name="indexPath"></param>
        /// <param name="animated"></param>
        public void DeselectRowAtIndexPath(NSIndexPath indexPath, bool animated = false)
        {
            if (indexPath != null && SelectedRow.Contains(indexPath))
            {
                var cell = this.CellForRowAtIndexPath(indexPath);
                if (cell != null) cell.Selected = false;
                SelectedRow.Remove(indexPath);
            }
        }

        /// <summary>
        /// 选择某IndexPath
        /// </summary>
        /// <param name="indexPath"></param>
        /// <param name="animated"></param>
        /// <param name="scrollPosition"></param>
        public void SelectRowAtIndexPath(NSIndexPath indexPath, bool animated, ScrollPosition scrollPosition)
        {
            // unlike the other methods that I've tested, the real UIKit appears to call reload during selection if the table hasn't been reloaded
            // yet. other methods all appear to rebuild the section cache "on-demand" but don't do a "proper" reload. for the sake of attempting
            // to maintain a similar delegate and dataSource access pattern to the real thing, I'll do it this way here. :)
            this._reloadDataIfNeeded();

            if (!SelectedRow.Contains(indexPath))
            {
                SelectedRow.Add(indexPath);
                var cell = this.CellForRowAtIndexPath(indexPath);
                if (cell != null)//TODO:不知道为什么有时候为空
                    cell.Selected = true;
            }
        }

        /// <summary>
        /// 滑动到NSIndexPath对应的Item.
        /// </summary>
        /// <param name="indexPath"></param>
        /// <param name="scrollPosition"></param>
        /// <param name="animated"></param>
        public void ScrollToRowAtIndexPath(NSIndexPath indexPath, ScrollPosition scrollPosition, bool animated)
        {
            var rect = ItemsLayout.RectForItem(indexPath);
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
        object _obj = new object();
        public MAUICollectionViewViewHolder DequeueRecycledViewHolderWithIdentifier(string identifier)
        {
            lock (_obj)
            {
                for (var index = ReusableViewHolders.Count - 1; index >= 0; index--)
                {
                    MAUICollectionViewViewHolder viewHolder = ReusableViewHolders[index];
                    if (viewHolder.ReuseIdentifier == identifier)
                    {
                        ReusableViewHolders.RemoveAt(index);
                        return viewHolder;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 没有操作动画的Item直接回收
        /// </summary>
        /// <param name="viewHolder"></param>
        public void RecycleViewHolder(MAUICollectionViewViewHolder viewHolder)
        {
            //回收时重置ViewHolder
            if (!ReusableViewHolders.Contains(viewHolder))
            {
                viewHolder.PrepareForReuse();
                lock (_obj)
                {
                    ReusableViewHolders.Add(viewHolder);
                }
            }
            //viewHolder.ContentView.RemoveFromSuperview(); 移除会触发Measure, 导致动画流程混乱
        }

        #endregion

        #region 数据
        /// <summary>
        /// 缓存Section和Item的数据, 从Source获取的是最新的, 在数据更新时, 这里存储的是旧的
        /// </summary>
        private List<int> sections = new();

        /// <summary>
        /// 当数据被从末尾插入或删除时, 可以使用该方法加载更新后的数据.
        /// </summary>
        public void ReloadDataCount()
        {
            this.sections = fetchDataCounts();
        }

        private List<int> fetchDataCounts()
        {
            var res = new List<int>();
            if (_sourceHas.numberOfSectionsInCollectionView && _sourceHas.numberOfItemsInSection)
            {
                var sCount = Source.NumberOfSections(this);

                for (var sIndex = 0; sIndex < sCount; sIndex++)
                {
                    res.Add(Source.NumberOfItems(this, sIndex));
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

        NSIndexPath GetNextItem(NSIndexPath indexPath)
        {
            if(indexPath.Row < NumberOfItemsInSection(indexPath.Section) -1) // have next item in same section
            {
                return NSIndexPath.FromRowSection(indexPath.Row + 1, indexPath.Section);
            }
            else // find next item from next section
            {
                if(indexPath.Section < NumberOfSections() -1)
                {
                    return NSIndexPath.FromRowSection(0, indexPath.Section+1);
                }
                else //don't have next section
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 通知CollectionView移除某Item, 需要做出改变.
        /// 移除会让移除项后面的Item需要调节高度, 这个高度需要动画改变
        /// </summary>
        /// <param name="indexPath"></param>
        public void NotifyItemRangeRemoved(NSIndexPath indexPath, int count = 1)
        {
            if (count < 1 || indexPath.Row + count > NumberOfItemsInSection(indexPath.Section)) return;
            var Updates = ItemsLayout.Updates;
            if (Updates.Count > 0)
                ItemsLayout.AnimationManager.StopOperateAnim();

            /*
             * Animation Analysis:
             * If remove item in front of first visible item, and don't remove visible item, visible item don't need move.
             * If remove visible item, need move, all items after last removed item need move, but we only move visible items and 'items equal to count'
             */
            var isRemovedBeforeVisible = false;//remove before visible item, don't show visible position animation
            var lastNeedRemoveIndexPath = NSIndexPath.FromRowSection(indexPath.Row + count - 1, indexPath.Section);//use last item, avoid remove visible item
            if (lastNeedRemoveIndexPath.Compare(ItemsLayout.VisibleIndexPath.FirstOrDefault()) < 0)
                isRemovedBeforeVisible = true;

            NSIndexPath lastRemovedIndexPath = NSIndexPath.FromRowSection(indexPath.Row - 1, indexPath.Section);
            for (var index = 0; index < count; index++)
            {
                var needRemovedIndexPath = GetNextItem(lastRemovedIndexPath);
                if (needRemovedIndexPath.Section != indexPath.Section)
                    break;
                Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.Remove, source = needRemovedIndexPath, operateAnimate = !(ItemsLayout.VisibleIndexPath.FirstOrDefault().Row - indexPath.Row >= count) });
                lastRemovedIndexPath = needRemovedIndexPath;
            }

            //移动2*count数目的Item:第一部分 count items 填充移除的, 后面的填充移动的
            NSIndexPath lastMovedIndexPath = lastNeedRemoveIndexPath;
            for (var moveCount = 1; moveCount <= 2 * count; moveCount++)
            {
                var needMovedIndexPath = GetNextItem(lastMovedIndexPath);
                if (needMovedIndexPath == null) continue;
                Updates.Add(new OperateItem() { 
                    operateType = OperateItem.OperateType.Move, 
                    source = needMovedIndexPath, 
                    target = needMovedIndexPath.Section == indexPath.Section ? NSIndexPath.FromRowSection(needMovedIndexPath.Row - count, needMovedIndexPath.Section) : needMovedIndexPath, //if not same section, don't change IndexPath
                    operateAnimate = !isRemovedBeforeVisible,
                    moveCount = -count
                });
                lastMovedIndexPath = needMovedIndexPath;
            }

            lastMovedIndexPath = NSIndexPath.FromRowSection(lastNeedRemoveIndexPath.Row + 2 * count, lastNeedRemoveIndexPath.Section);
            //if visible items is too more(> 2 * count), maybe need more more.
            foreach (var visibleItem in PreparedItems)
            {
                if (visibleItem.Key > lastMovedIndexPath)
                {
                    Updates.Add(new OperateItem() { 
                        operateType = OperateItem.OperateType.Move, 
                        source = visibleItem.Key, 
                        target = visibleItem.Key.Section == indexPath.Section ? NSIndexPath.FromRowSection(visibleItem.Key.Row - count, visibleItem.Key.Section) : visibleItem.Key, 
                        operateAnimate = !isRemovedBeforeVisible });
                }
            }

            ReloadDataCount();

            updatSelectedIndexPathWhenRemoveOperate(indexPath, count);

            if (isRemovedBeforeVisible)//if remove before visible, don't change visible item position, so need change ScrollY to fit, Maui official CollectionView use this action.
            {
                var visibleFirst = ItemsLayout.VisibleIndexPath.FirstOrDefault();
                if (visibleFirst.Section == indexPath.Section && visibleFirst.Row - indexPath.Row < count)//if remove first visible, we remeasure, not scroll
                    this.ReMeasure();
                else
                {
                    var removeAllHight = ItemsLayout.HeightForItems(indexPath, count);
                    ScrollToAsync(ScrollX, ScrollY - removeAllHight, false);
                }
            }
            else
            {
                this.ReMeasure();
            }
        }

        /// <summary>
        /// 通知CollectionView插入了Item, 需要做出改变.
        /// </summary>
        /// <param name="indexPath">插入应该是在某个位置插入, 比如0, 即插入在0位置</param>
        public void NotifyItemRangeInserted(NSIndexPath indexPath, int count = 1)
        {
            if (count < 1) return;
            var Updates = ItemsLayout.Updates;
            if (Updates.Count > 0)
                ItemsLayout.AnimationManager.StopOperateAnim();

            var isInsertBeforeVisiable = false;//insert before visible item, don't show visible position animation
            if (indexPath.Compare(ItemsLayout.VisibleIndexPath.FirstOrDefault()) < 0)
                isInsertBeforeVisiable = true;

            //visible item maybe need move position when insert items
            foreach (var visiableItem in PreparedItems)
            {
                if (visiableItem.Key.Section == indexPath.Section)//同一section的item才变化
                {
                    if (visiableItem.Key.Row >= indexPath.Row)//大于等于item的row的需要更新IndexPath
                    {
                        Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.Move, source = visiableItem.Key, target = NSIndexPath.FromRowSection(visiableItem.Key.Row + count, visiableItem.Key.Section), operateAnimate = !isInsertBeforeVisiable });
                    }
                }
            }

            //insert
            for (var index = 0; index < count; index++)
            {
                var needInsertedIndexPath = NSIndexPath.FromRowSection(indexPath.Row + index, indexPath.Section);
                Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.Insert, source = needInsertedIndexPath });
            }

            ReloadDataCount();

            updatSelectedIndexPathWhenInsertOperate(indexPath, count);

            if (isInsertBeforeVisiable)//if insert before VisibleItems, don't change visible item position, so need change ScrollY to fit, Maui official CollectionView use this action.
            {
                var insertAllHight = ItemsLayout.HeightForItems(indexPath, count);
                ScrollToAsync(ScrollX, ScrollY + insertAllHight, false);
            }
            else
            {
                this.ReMeasure();
            }
        }

        public void MoveItem(NSIndexPath indexPath, NSIndexPath toIndexPath)
        {
            var Updates = ItemsLayout.Updates;
            if (Updates.Count > 0)
                ItemsLayout.AnimationManager.StopOperateAnim();
            Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.Move, source = indexPath, target = toIndexPath });

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
                                Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.Move, source = visiableItem.Key, target = NSIndexPath.FromRowSection(visiableItem.Key.Row + 1, visiableItem.Key.Section) });
                            }
                        }
                        else
                        {
                            if (visiableItem.Key.Row > indexPath.Row && visiableItem.Key.Row <= toIndexPath.Row)
                            {
                                Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.Move, source = visiableItem.Key, target = NSIndexPath.FromRowSection(visiableItem.Key.Row - 1, visiableItem.Key.Section) });
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
                            Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.Move, source = visiableItem.Key, target = NSIndexPath.FromRowSection(visiableItem.Key.Row - 1, visiableItem.Key.Section) });
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
                            Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.Move, source = visiableItem.Key, target = NSIndexPath.FromRowSection(visiableItem.Key.Row + 1, visiableItem.Key.Section) });
                        }
                    }
                }
            }
            ReloadDataCount();
            updatSelectedIndexPathWhenMoveOperate(indexPath, toIndexPath);
            this.ReMeasure();
        }

        public void NotifyItemRangeChanged(IEnumerable<NSIndexPath> indexPaths)
        {
            var Updates = ItemsLayout.Updates;
            if (Updates.Count > 0)
                ItemsLayout.AnimationManager.StopOperateAnim();
            foreach (var visiableItem in PreparedItems)
            {
                if (indexPaths.Contains(visiableItem.Key))//如果可见的Items包含需要更新的Item
                {
                    Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.Update, source = visiableItem.Key });
                }
            }
            this.ReMeasure();
        }

        void updatSelectedIndexPathWhenInsertOperate(NSIndexPath indexPath, int count)
        {
            for (int i = SelectedRow.Count - 1; i >= 0; i--)
            {
                var selectedItem = SelectedRow[i];
                if (selectedItem.Section == indexPath.Section)
                {
                    if (indexPath.Row <= selectedItem.Row)//insert in front of selected
                    {
                        selectedItem.UpdateRow(selectedItem.Row + count);
                    }
                    else//insert behind selected
                    {

                    }
                }
            }
        }

        void updatSelectedIndexPathWhenMoveOperate(NSIndexPath indexPath, NSIndexPath toIndexPath)
        {
            //如果同Section, Move影响的只是之间的
            if (indexPath.Section == toIndexPath.Section)
            {
                var isUpMove = indexPath.Row > toIndexPath.Row;
                //先移除
                for (int i = SelectedRow.Count - 1; i >= 0; i--)
                {
                    var selectedItem = SelectedRow[i];
                    if (selectedItem.Section == indexPath.Section)//同一section的item才变化
                    {
                        if (selectedItem.Row == indexPath.Row)
                        {
                            selectedItem.UpdateRow(toIndexPath.Row);
                            continue;
                        }

                        if (isUpMove)//从底部向上移动, 目标位置下面的都需要向下移动
                        {
                            if (selectedItem.Row >= toIndexPath.Row && selectedItem.Row < indexPath.Row)
                            {
                                selectedItem.UpdateRow(selectedItem.Row + 1);
                            }
                        }
                        else
                        {
                            if (selectedItem.Row > indexPath.Row && selectedItem.Row <= toIndexPath.Row)
                            {
                                selectedItem.UpdateRow(selectedItem.Row - 1);
                            }
                        }

                    }
                }
            }
            //如果不同Section, 则影响不同的section后面的
            else
            {
                //先移除, 移除的Item后面的Item需要向前移动
                for (int i = SelectedRow.Count - 1; i >= 0; i--)
                {
                    var selectedItem = SelectedRow[i];
                    if (selectedItem.Section == indexPath.Section)
                    {
                        if (selectedItem.Row == indexPath.Row)
                        {
                            selectedItem.UpdateRow(toIndexPath.Row);
                            continue;
                        }

                        if (selectedItem.Row > indexPath.Row)
                        {
                            selectedItem.UpdateRow(selectedItem.Row - 1);
                        }
                    }
                }

                //后插入, 后面的需要向后移动
                for (int i = SelectedRow.Count - 1; i >= 0; i--)
                {
                    var selectedItem = SelectedRow[i];
                    if (selectedItem.Section == toIndexPath.Section)
                    {
                        if (selectedItem.Row >= toIndexPath.Row)
                        {
                            selectedItem.UpdateRow(selectedItem.Row + 1);
                        }
                    }
                }
            }
        }

        void updatSelectedIndexPathWhenRemoveOperate(NSIndexPath indexPath, int count)
        {
            for (int i = SelectedRow.Count - 1; i >= 0; i--)
            {
                var selectedItem = SelectedRow[i];
                if (selectedItem.Section == indexPath.Section)
                {
                    if (indexPath.Row + count - 1 < selectedItem.Row)//remove all in front of selected
                    {
                        selectedItem.UpdateRow(selectedItem.Row - count);
                    }
                    else if (indexPath.Row <= selectedItem.Row && selectedItem.Row <= indexPath.Row + count - 1)//remove this selected
                    {
                        SelectedRow.Remove(selectedItem);
                    }
                    else//remove behind selected
                    {

                    }
                }
            }
        }
        #endregion
    }
}