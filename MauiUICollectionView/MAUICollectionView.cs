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

            get => _backgroundView;
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

            get => _emptyView;
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
                    //_sourceHas.willSelectRowAtIndexPath = _source.WillSelectItem != null;
                    _sourceHas.didSelectRowAtIndexPath = _source.DidSelectItem != null;
                    //_sourceHas.willDeselectRowAtIndexPath = _source.WillDeselectItem != null;
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
                    size = ItemsLayout.MeasureContents(widthConstraint <= 0 || double.IsInfinity(widthConstraint) ? CollectionViewConstraintSize.Width : widthConstraint, heightConstraint <= 0 || double.IsInfinity(heightConstraint) ? CollectionViewConstraintSize.Height : heightConstraint);
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
                        viewHolder.Opacity = 1;
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

        /// <summary>
        /// get indexPath
        /// </summary>
        /// <param name="indexPath"></param>
        /// <param name="count">after indexPath, don't contain indexPath</param>
        /// <returns></returns>
        public NSIndexPath NextItem(NSIndexPath indexPath, int count)
        {
            var sectionCount = NumberOfSections();
            if (count >= 0)
            {
                for (var section = indexPath.Section; section < sectionCount; section++)
                {
                    var itemCount = NumberOfItemsInSection(section);
                    var itemStartIndex = 0;
                    if (section == indexPath.Section)
                    {
                        itemCount = itemCount - (indexPath.Row + 1);
                        itemStartIndex = indexPath.Row;
                    }
                    var remainCount = count - itemCount;
                    if (remainCount <= 0)
                    {
                        if (section == indexPath.Section)
                            return NSIndexPath.FromRowSection(itemStartIndex + count, section);
                        else
                            return NSIndexPath.FromRowSection(itemStartIndex + count - 1, section);
                    }
                    else
                        count = remainCount;
                }
            }
            else
            {
                count = -count;
                for (var section = indexPath.Section; section >= 0; section--)
                {
                    var itemCount = NumberOfItemsInSection(section);
                    var itemStartIndex = itemCount;
                    if (section == indexPath.Section)//need pass indexPath, so use row
                    {
                        itemCount = indexPath.Row;
                        itemStartIndex = indexPath.Row;
                    }
                    var remainCount = count - itemCount;
                    if (remainCount <= 0)
                    {
                        return NSIndexPath.FromRowSection(itemStartIndex - count, section);
                    }
                    else
                        count = remainCount;
                }
            }
            return null;
        }

        /// <summary>
        /// when start is 1, end is 4, return 2
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public int ItemCountInRange(NSIndexPath start, NSIndexPath end)
        {
            if(start.Compare(end) > 0) throw new ArgumentException("Index of start is bigger than end.");
            if (start.Section == end.Section)
            {
                return end.Row - start.Row - 1;
            }
            else
            {
                int count = 0;
                for (var section = start.Section; section <= end.Section; section++)
                {
                    int numberOfRows = NumberOfItemsInSection(section);
                    if (section == start.Section)
                    {
                        count += (numberOfRows - start.Row - 1); //start item is not counted
                    }
                    else if (section == end.Section)
                    {
                        count += end.Row;//end item is not counted
                    }
                    else
                    {
                        count += numberOfRows;
                    }
                }
                return count;
            }
        }

        #endregion

        #region 操作

        /// <summary>
        /// Notifies the CollectionView that some items have been removed from data set and that changes need to be made.
        /// Remove have some fixed action like native CollectionView:don't change position of visible items.
        /// </summary>
        /// <param name="indexPath"></param>
        public void NotifyItemRangeRemoved(NSIndexPath indexPath, int count = 1)
        {
            if (count < 1) return;
            if (indexPath.Row + count > NumberOfItemsInSection(indexPath.Section))
            {
                throw new IndexOutOfRangeException("Removed item beyond data");
            }
            if (ItemsLayout.Updates != null)
                ItemsLayout.AnimationManager.StopOperateAnim();
            var operation = new DiffAnimation.Operate()
            {
                OperateType = OperateItem.OperateType.Remove,
                Source = indexPath,
                OperateCount = count
            };
            var diff = new DiffAnimation(operation, this);
            ItemsLayout.Updates = (operation, diff);

            //when remove, maybe need change baseline for better animation.
            var layout = ItemsLayout;
            diff.RecordLastViewHolder(PreparedItems, layout.VisibleIndexPath);
            if (layout != null)
            {
                var firstVisibleItem = layout.VisibleIndexPath.StartItem;
                var firstVisibleItemBounds = PreparedItems[layout.VisibleIndexPath.StartItem].BoundsInLayout;
                var lastVisibleItem = layout.VisibleIndexPath.EndItem;
                var lastVisibleItemBounds = PreparedItems[layout.VisibleIndexPath.EndItem].BoundsInLayout;
                var firstRemoved = indexPath;
                var lastRemoved = NSIndexPath.FromRowSection(indexPath.Row + count - 1, indexPath.Section);

                NSIndexPath baselineLastIndexPath = null;
                NSIndexPath baselineCurrentIndexPath = null;
                bool moved = false;
                if (firstVisibleItem.Compare(lastRemoved) > 0)//remove items before first visible item, we need update baseline indexpath
                {
                    /****]*First*****Last*****/
                    baselineLastIndexPath = firstVisibleItem;
                    baselineCurrentIndexPath = firstVisibleItem.Section == indexPath.Section ?
                        NSIndexPath.FromRowSection(firstVisibleItem.Row - count, firstVisibleItem.Section) ://row will change when same section 
                        firstVisibleItem;
                    layout.BaseLineItemUsually = new CollectionViewLayout.LayoutInfor()
                    {
                        //indexpath maybe change
                        StartItem = baselineCurrentIndexPath,
                        StartBounds = firstVisibleItemBounds
                    };
                }
                else
                {
                    if (firstRemoved > firstVisibleItem)// removed item after first visible, use first visible as baseline. 
                    {
                        /*****First*[****Last*****/
                        baselineLastIndexPath = firstVisibleItem;
                        baselineCurrentIndexPath = firstVisibleItem;

                        layout.BaseLineItemUsually = new CollectionViewLayout.LayoutInfor()
                        {
                            StartItem = baselineCurrentIndexPath,
                            StartBounds = firstVisibleItemBounds
                        };
                    }
                    else if (lastRemoved < lastVisibleItem)//removed item before last visible item, and contain first visible
                    {
                        /****[*First****]*Last*****/
                        baselineLastIndexPath = lastVisibleItem;
                        baselineCurrentIndexPath = lastVisibleItem.Section == indexPath.Section ?
                            NSIndexPath.FromRowSection(lastVisibleItem.Row - count, lastVisibleItem.Section) ://row will change when same section 
                            lastVisibleItem;

                        layout.BaseLineItemUsually = new CollectionViewLayout.LayoutInfor()
                        {
                            StartItem = baselineCurrentIndexPath,
                            StartBounds = lastVisibleItemBounds
                        };

                        //fix move first item will measure multiple times when remove top item
                        if (ItemCountInRange(NSIndexPath.FromRowSection(0, 0), baselineCurrentIndexPath) <= count)
                        {
                            layout.BaseLineItemUsually = new CollectionViewLayout.LayoutInfor()
                            {
                                StartItem = baselineCurrentIndexPath,
                                StartBounds = new Rect(0, ScrollY, 0, 0)
                            };
                            moved = true;
                        }
                    }
                    else//removed items contain all visible items
                    {
                        /*****[First*****Last*]****/
                        var next = NextItem(lastRemoved, 1);//last
                        if (next != null)
                        {
                            baselineLastIndexPath = next;
                            baselineCurrentIndexPath = next.Section == indexPath.Section ?
                                NSIndexPath.FromRowSection(next.Row - count, next.Section) ://row will change when same section 
                                next;
                            layout.BaseLineItemUsually = new CollectionViewLayout.LayoutInfor()
                            {
                                StartItem = baselineCurrentIndexPath,
                                StartBounds = new Rect(0, ScrollY, 0, 0)
                            };
                            moved = true;
                        }
                    }
                }

                operation.BaselineItem = (baselineLastIndexPath, baselineCurrentIndexPath, moved);

                diff.Analysis(true);
            }

            ReloadDataCount();

            updatSelectedIndexPathWhenOperate(diff);

            ReMeasure();
        }

        /// <summary>
        /// Notifies the CollectionView that some items have been inserted to data set and that changes need to be made.
        /// </summary>
        /// <param name="indexPath">if insert data in 0-1(section is 0, item index is 1) and count is 2, old data in 0-1 will be move to 0-3, the inserted data is displayed in 0-1 and 0-2</param>
        /// <param name="count"></param>
        public void NotifyItemRangeInserted(NSIndexPath indexPath, int count = 1)
        {
            if (count < 1) return;
            if (ItemsLayout.Updates != null)
                ItemsLayout.AnimationManager.StopOperateAnim();
            var operation = new DiffAnimation.Operate()
            {
                OperateType = OperateItem.OperateType.Insert,
                Source = indexPath,
                OperateCount = count
            };
            var diff = new DiffAnimation(operation, this);
            ItemsLayout.Updates = (operation, diff);

            //when insert, maybe need change baseline for better animation.
            var layout = ItemsLayout;
            diff.RecordLastViewHolder(PreparedItems, layout.VisibleIndexPath);

            if (layout != null)
            {
                var firstVisibleItem = layout.VisibleIndexPath.StartItem;
                var firstVisibleItemBounds = PreparedItems[layout.VisibleIndexPath.StartItem].BoundsInLayout;
                var lastVisibleItem = layout.VisibleIndexPath.EndItem;
                var lastVisibleItemBounds = PreparedItems[layout.VisibleIndexPath.EndItem].BoundsInLayout;

                var firstInsert = indexPath;
                var lastInsert = NSIndexPath.FromRowSection(indexPath.Row + count - 1, indexPath.Section);

                NSIndexPath baselineLastIndexPath = null;
                NSIndexPath baselineCurrentIndexPath = null;
                bool moved = false;

                /*
                 * insert at any position, we don't change old first visible item as baseline, if we insert at position of old first visible item, we also don't change it
                 */
                baselineLastIndexPath = firstVisibleItem;
                if (indexPath.Compare(firstVisibleItem) <= 0)
                {
                    baselineCurrentIndexPath = firstVisibleItem.Section == indexPath.Section ?
                        NSIndexPath.FromRowSection(firstVisibleItem.Row + count, firstVisibleItem.Section) ://row will change when same section 
                        firstVisibleItem;
                }
                else
                {
                    baselineCurrentIndexPath = firstVisibleItem;
                }
                layout.BaseLineItemUsually = new CollectionViewLayout.LayoutInfor()
                {
                    StartItem = baselineCurrentIndexPath,
                    StartBounds = firstVisibleItemBounds
                };

                operation.BaselineItem = (baselineLastIndexPath, baselineCurrentIndexPath, moved);

                diff.Analysis(true);
            }

            ReloadDataCount();

            updatSelectedIndexPathWhenOperate(diff);

            this.ReMeasure();
        }

        public void MoveItem(NSIndexPath indexPath, NSIndexPath toIndexPath)
        {
            var Updates = ItemsLayout.Updates;
            if (Updates != null)
                ItemsLayout.AnimationManager.StopOperateAnim();
            var operation = new DiffAnimation.Operate()
            {
                OperateType = OperateItem.OperateType.Move,
                Source = indexPath,
                Target = toIndexPath,
            };
            var diff = new DiffAnimation(operation, this);
            ItemsLayout.Updates = (operation, diff);

            //when insert, maybe need change baseline for better animation.
            var layout = ItemsLayout;
            diff.RecordLastViewHolder(PreparedItems, layout.VisibleIndexPath);
            if (layout != null)
            {
                var firstVisibleItem = layout.VisibleIndexPath.StartItem;
                var firstVisibleItemBounds = PreparedItems[layout.VisibleIndexPath.StartItem].BoundsInLayout;

                NSIndexPath baselineLastIndexPath = firstVisibleItem;
                NSIndexPath baselineCurrentIndexPath = firstVisibleItem;
                bool moved = false;
                layout.BaseLineItemUsually = new CollectionViewLayout.LayoutInfor()
                {
                    StartItem = baselineCurrentIndexPath,
                    StartBounds = firstVisibleItemBounds
                };

                operation.BaselineItem = (baselineLastIndexPath, baselineCurrentIndexPath, moved);

                diff.Analysis(true);
            }
            ReloadDataCount();
            updatSelectedIndexPathWhenOperate(diff);
            this.ReMeasure();
        }

        /// <summary>
        /// Notifies the CollectionView that data have been replaced in these items.
        /// </summary>
        /// <param name="indexPaths"></param>
        public void NotifyItemRangeChanged(NSIndexPath indexPath)
        {
            var Updates = ItemsLayout.Updates;
            if (Updates != null)
                ItemsLayout.AnimationManager.StopOperateAnim();
            var layout = ItemsLayout;
            if (layout != null)
            {
                var firstVisibleItem = PreparedItems.FirstOrDefault();
                layout.BaseLineItemUsually = new CollectionViewLayout.LayoutInfor()
                {
                    StartItem = firstVisibleItem.Key,
                    StartBounds = firstVisibleItem.Value.BoundsInLayout
                };
            }
            for (var index = PreparedItems.Count - 1; index >= 0; index--)
            {
                var item = PreparedItems.ElementAt(index);
                if (item.Key.Compare(indexPath) == 0)
                {
                    PreparedItems.Remove(item.Key);
                    RecycleViewHolder(item.Value);
                }
            }
            ReloadDataCount();
            this.ReMeasure();
        }

        void updatSelectedIndexPathWhenOperate(DiffAnimation diff)
        {
            var selectedItems = new List<NSIndexPath>();
            for (int i = SelectedItems.Count - 1; i >= 0; i--)
            {
                var indexPath = diff.TryGetCurrentIndexPath(SelectedItems[i]);
                if (indexPath != null)
                {
                    selectedItems.Add(indexPath);
                }
            }
            SelectedItems.Clear();
            SelectedItems = selectedItems;
        }
        #endregion
    }
}