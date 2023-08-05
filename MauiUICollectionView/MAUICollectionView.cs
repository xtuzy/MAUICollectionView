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

        View _emptyView;
        public View EmptyView
        {
            set
            {
                //如果已经存在, 先移除
                if (_emptyView != null && _emptyView != value)
                {
                    if (ContentView.Contains(_emptyView))
                    {
                        ContentView.Remove(_emptyView);
                    }
                }

                if (value != null && _emptyView != value)
                {
                    _emptyView = value;
                    ContentView.Insert(1, _emptyView);//插入到底部
                }
            }
        }
        #endregion

        bool _needsReload;
        /// <summary>
        /// The selected Items
        /// </summary>
        public List<NSIndexPath> SelectedItems = new();

        /// <summary>
        /// Item that are dragged and dropped to sort. Its IndexPath updates when you drag.
        /// </summary>
        public MAUICollectionViewViewHolder DragedItem;

        /// <summary>
        /// Items in the layout area. Layout area unlike the visible area, the layout area may be larger than the visible area, because there may be white space up and down when sliding quickly, and it is necessary to draw larger than the visible area to avoid blanks.
        /// </summary>
        public Dictionary<NSIndexPath, MAUICollectionViewViewHolder> PreparedItems;

        /// <summary>
        /// Recycled ViewHolders waiting to be reused
        /// </summary>
        public List<MAUICollectionViewViewHolder> ReusableViewHolders;
        int MaxReusableViewHolderCount = 5;

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

                if (_source != null)
                {
                    _sourceHas.numberOfSectionsInCollectionView = _source.NumberOfSections != null;
                    _sourceHas.numberOfItemsInSection = _source.NumberOfItems != null;

                    _sourceHas.heightForRowAtIndexPath = _source.HeightForItem != null;
                    _sourceHas.willSelectRowAtIndexPath = _source.WillSelectItem != null;
                    _sourceHas.didSelectRowAtIndexPath = _source.DidSelectItem != null;
                    _sourceHas.willDeselectRowAtIndexPath = _source.WillDeselectItem != null;
                    _sourceHas.didDeselectRowAtIndexPath = _source.DidDeselectItem != null;
                }
                else
                {
                    _sourceHas.numberOfSectionsInCollectionView = false;
                    _sourceHas.numberOfItemsInSection = false;

                    _sourceHas.heightForRowAtIndexPath = false;
                    _sourceHas.willSelectRowAtIndexPath = false;
                    _sourceHas.didSelectRowAtIndexPath = false;
                    _sourceHas.willDeselectRowAtIndexPath = false;
                    _sourceHas.didDeselectRowAtIndexPath = false;
                }

                this._setNeedsReload();
            }
        }

        /// <summary>
        /// Specify layout for CollectionView
        /// </summary>
        public CollectionViewLayout ItemsLayout;

        /// <summary>
        /// See <see cref="HeightExpansion"/>, advice set 0 on iOS, set 1 on Android, set 0.5 on Windows. You need test different value to find which one is best for your project.
        /// </summary>
        public double HeightExpansionFactor = 0.5;

        /// <summary>
        /// The height of the expansion. 
        /// It will let CollectionView show more view than visible rect, it is related to prepared rect. Height of prepared rect is HeightExpansion + Visible Height + HeightExpansion. Why do this?, Scroll ScrollView will translate canvas on Android and Windows before relayout, if no view in lasted visible rect, will show blank, so we need layout more view to avoid blank for next scroll.
        /// </summary>
        internal int HeightExpansion => (int)(CollectionViewConstraintSize.Height * HeightExpansionFactor);

        public MAUICollectionViewViewHolder ViewHolderForItem(NSIndexPath indexPath)
        {
            // this is allowed to return nil if the cell isn't visible and is not restricted to only returning visible cells
            // so this simple call should be good enough.
            if (indexPath == null) return null;
            return PreparedItems.ContainsKey(indexPath) ? PreparedItems[indexPath] : null;
        }

        /// <summary>
        /// Reloads all the data and views in the collection view. The goal is to be used when the data changes a lot.
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
            this.SelectedItems.Clear();
            DragedItem = null;

            ReloadDataCount();//Section或者Item数目可能变化了, 重新加载

            this._needsReload = false;
            this.ReMeasure();
        }

        /// <summary>
        /// When switching pages back, the data may no longer be displayed, so a forced reload is required.
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

        Size MeasuredContentSize = Size.Zero;
        /// <summary>
        /// It will be call when framework measure view tree. It has a delay after <see cref="ReMeasure"/>.
        /// </summary>
        /// <param name="widthConstraint"></param>
        /// <param name="heightConstraint"></param>
        /// <returns></returns>
        public partial Size OnContentViewMeasure(double widthConstraint, double heightConstraint)
        {
            this._reloadDataIfNeeded();
            Size size = Size.Zero;

            if (ItemsLayout != null && Source != null)
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
                if (MeasuredContentSize != Size.Zero && IsScrolling)
                    size = MeasuredContentSize;
                else
                        size = ItemsLayout.MeasureContents(CollectionViewConstraintSize.Width != 0 ? CollectionViewConstraintSize.Width : widthConstraint, CollectionViewConstraintSize.Height != 0 ? CollectionViewConstraintSize.Height : heightConstraint);
            }

            // set empty view
            if (ItemsLayout != null && Source != null)
            {
                if (_emptyView != null && _emptyView.IsVisible == true)
                    _emptyView.IsVisible = false;
            }
            else
            {
                if (_emptyView != null)
                {
                    if (_emptyView.IsVisible == false)
                        _emptyView.IsVisible = true;
                    _emptyView.MeasureSelf(this.Width, this.Height);
                }
            }

            // measure background view
            if (_backgroundView != null)
            {
                if (size != Size.Zero)
                    _backgroundView.MeasureSelf(size.Width, size.Height);
                else
                    _backgroundView.MeasureSelf(widthConstraint, heightConstraint);
            }

            return size;
        }

        /// <summary>
        /// Measure immediately. It will be call after scroll. 
        /// When scroll, will call measure many times, <see cref="ReMeasure"/> have a delay, so we need this method fast update view's position, avoid view's status conflict.
        /// </summary>
        void MeasureNowAfterScroll()
        {
                if (ItemsLayout != null && Source != null)
                    MeasuredContentSize = ItemsLayout.MeasureContents(CollectionViewConstraintSize.Width, CollectionViewConstraintSize.Height);
        }

        /// <summary>
        /// It will be call when framework layout view tree.
        /// </summary>
        public partial void OnContentViewLayout()
        {
            if ((ItemsLayout == null || Source == null) && _emptyView != null)
            {
                _emptyView.ArrangeSelf(new Rect(0, 0, this.Width, this.Height));
            }

            if (_backgroundView != null)
                _backgroundView.ArrangeSelf(ContentView.Bounds);

            ItemsLayout?.ArrangeContents();

            MeasuredContentSize = Size.Zero;
        }

        public NSIndexPath IndexPathForViewHolder(MAUICollectionViewViewHolder cell)
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
        /// Deselect item.
        /// </summary>
        /// <param name="indexPath"></param>
        /// <param name="animated"></param>
        public void DeselectItem(NSIndexPath indexPath, bool animated = false)
        {
            if (indexPath != null && SelectedItems.Contains(indexPath))
            {
                var cell = this.ViewHolderForItem(indexPath);
                if (cell != null) cell.Selected = false;
                SelectedItems.Remove(indexPath);
            }
        }

        /// <summary>
        /// Select item.
        /// </summary>
        /// <param name="indexPath"></param>
        /// <param name="animated"></param>
        /// <param name="scrollPosition"></param>
        public void SelectItem(NSIndexPath indexPath, bool animated, ScrollPosition scrollPosition)
        {
            this._reloadDataIfNeeded();

            if (!SelectedItems.Contains(indexPath))
            {
                SelectedItems.Add(indexPath);
                var cell = this.ViewHolderForItem(indexPath);
                if (cell != null)//TODO:不知道为什么有时候为空
                    cell.Selected = true;
            }
        }

        /// <summary>
        /// Scroll to item.
        /// </summary>
        /// <param name="indexPath"></param>
        /// <param name="scrollPosition"></param>
        /// <param name="animated"></param>
        public void ScrollToItem(NSIndexPath indexPath, ScrollPosition scrollPosition, bool animated)
        {
            ItemsLayout.ScrollTo(indexPath, scrollPosition, animated);
        }

        #region 复用
        object _obj = new object();
        /// <summary>
        /// Get ViewHolder from <see cref="ReusableViewHolders"/>.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
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
        /// Recycle ViewHolder.
        /// </summary>
        /// <param name="viewHolder"></param>
        public void RecycleViewHolder(MAUICollectionViewViewHolder viewHolder)
        {
            //reset ViewHolder
            if (!ReusableViewHolders.Contains(viewHolder))
            {
                viewHolder.PrepareForReuse();
                lock (_obj)
                {
                    ReusableViewHolders.Add(viewHolder);
                }
            }
        }

        #endregion

        #region 数据
        /// <summary>
        /// The data of the Section and Item is cached, it is obtained from <see cref="Source"/>, and the old one is stored here when the data is updated, need use <see cref="ReloadDataCount"/> to update it.
        /// </summary>
        private List<int> sections = new();

        /// <summary>
        /// When data is inserted or deleted from the end, the updated data can be fast loaded using this method.
        /// </summary>
        public void ReloadDataCount()
        {
            this.sections = fetchDataCounts();
        }

        /// <summary>
        /// Form <see cref="Source"/> get lastest data.
        /// </summary>
        /// <returns></returns>
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
            if (indexPath.Row < NumberOfItemsInSection(indexPath.Section) - 1) // have next item in same section
            {
                return NSIndexPath.FromRowSection(indexPath.Row + 1, indexPath.Section);
            }
            else // find next item from next section
            {
                if (indexPath.Section < NumberOfSections() - 1)
                {
                    return NSIndexPath.FromRowSection(0, indexPath.Section + 1);
                }
                else //don't have next section
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Notifies the CollectionView that some items have been removed from data set and that changes need to be made.
        /// </summary>
        /// <param name="indexPath"></param>
        public void NotifyItemRangeRemoved(NSIndexPath indexPath, int count = 1)
        {
            if (count < 1) return;
            if (indexPath.Row + count > NumberOfItemsInSection(indexPath.Section))
            {
                throw new IndexOutOfRangeException("Removed item beyond data");
            }
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

            // try Move 2*count items:
            // first part, 1*counts items be moved to fill items that be removed.
            // second part, 1*counts items be moved to fill items that be moved in first part. 
            NSIndexPath lastMovedIndexPath = lastNeedRemoveIndexPath;
            for (var moveCount = 1; moveCount <= 2 * count; moveCount++)
            {
                var needMovedIndexPath = GetNextItem(lastMovedIndexPath);
                if (needMovedIndexPath == null) continue;
                Updates.Add(new OperateItem()
                {
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
                    Updates.Add(new OperateItem()
                    {
                        operateType = OperateItem.OperateType.Move,
                        source = visibleItem.Key,
                        target = visibleItem.Key.Section == indexPath.Section ? NSIndexPath.FromRowSection(visibleItem.Key.Row - count, visibleItem.Key.Section) : visibleItem.Key,
                        operateAnimate = !isRemovedBeforeVisible
                    });
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
                    var removeAllHight = ItemsLayout.EstimateHeightForItems(indexPath, count);
                    ScrollToAsync(ScrollX, ScrollY - removeAllHight, false);
                }
            }
            else
            {
                this.ReMeasure();
            }
        }

        /// <summary>
        /// Notifies the CollectionView that some items have been inserted to data set and that changes need to be made.
        /// </summary>
        /// <param name="indexPath">if insert data in 0-1(section is 0, item index is 1) and count is 2, old data in 0-1 will be move to 0-3, the inserted data is displayed in 0-1 and 0-2</param>
        /// <param name="count"></param>
        public void NotifyItemRangeInserted(NSIndexPath indexPath, int count = 1)
        {
            if (count < 1) return;
            var Updates = ItemsLayout.Updates;
            if (Updates.Count > 0)
                ItemsLayout.AnimationManager.StopOperateAnim();

            var isInsertBeforeVisiable = false;//insert before visible item, don't show visible position animation
            if (indexPath.Compare(ItemsLayout.VisibleIndexPath.FirstOrDefault()) < 0)
                isInsertBeforeVisiable = true;

            //visible item maybe need move position after insert items
            foreach (var visibleItem in PreparedItems)
            {
                if (!(visibleItem.Key < indexPath))
                {
                    Updates.Add(new OperateItem()
                    {
                        moveCount = count,
                        operateType = OperateItem.OperateType.Move,
                        source = visibleItem.Key,
                        target = visibleItem.Key.Section == indexPath.Section ? NSIndexPath.FromRowSection(visibleItem.Key.Row + count, visibleItem.Key.Section) : visibleItem.Key,
                        operateAnimate = !isInsertBeforeVisiable
                    });
                }
            }

            //insert
            for (var index = 0; index < count; index++)
            {
                var needInsertedIndexPath = NSIndexPath.FromRowSection(indexPath.Row + index, indexPath.Section);
                Updates.Add(new OperateItem()
                {
                    operateType = OperateItem.OperateType.Insert,
                    source = needInsertedIndexPath
                });
            }

            ReloadDataCount();

            updatSelectedIndexPathWhenInsertOperate(indexPath, count);

            if (isInsertBeforeVisiable)//if insert before VisibleItems, don't change visible item position, so need change ScrollY to fit, Maui official CollectionView use this action.
            {
                var insertAllHight = ItemsLayout.EstimateHeightForItems(indexPath, count);
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

        /// <summary>
        /// Notifies the CollectionView that data have been replaced in these items.
        /// </summary>
        /// <param name="indexPaths"></param>
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
            for (int i = SelectedItems.Count - 1; i >= 0; i--)
            {
                var selectedItem = SelectedItems[i];
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
                for (int i = SelectedItems.Count - 1; i >= 0; i--)
                {
                    var selectedItem = SelectedItems[i];
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
                for (int i = SelectedItems.Count - 1; i >= 0; i--)
                {
                    var selectedItem = SelectedItems[i];
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
                for (int i = SelectedItems.Count - 1; i >= 0; i--)
                {
                    var selectedItem = SelectedItems[i];
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
            for (int i = SelectedItems.Count - 1; i >= 0; i--)
            {
                var selectedItem = SelectedItems[i];
                if (selectedItem.Section == indexPath.Section)
                {
                    if (indexPath.Row + count - 1 < selectedItem.Row)//remove all in front of selected
                    {
                        selectedItem.UpdateRow(selectedItem.Row - count);
                    }
                    else if (indexPath.Row <= selectedItem.Row && selectedItem.Row <= indexPath.Row + count - 1)//remove this selected
                    {
                        SelectedItems.Remove(selectedItem);
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