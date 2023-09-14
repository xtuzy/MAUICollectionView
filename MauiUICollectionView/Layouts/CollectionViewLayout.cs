using MauiUICollectionView.Layouts;
using System.Diagnostics;

namespace MauiUICollectionView.Layouts
{
    /// <summary>
    /// layout content.
    /// </summary>
    public abstract partial class CollectionViewLayout : IDisposable
    {
        public CollectionViewLayout(MAUICollectionView collectionView)
        {
            this.CollectionView = collectionView;
            AnimationManager = new LayoutAnimationManager(collectionView);
        }

        /// <summary>
        /// manage scroll and operate animation.
        /// </summary>
        public ILayoutAnimationManager AnimationManager { get; set; }

        /// <summary>
        /// Store all operate
        /// </summary>
        public (DiffAnimation.Operate Operation, DiffAnimation DiffAnimation)? Updates;

        private MAUICollectionView _collectionView;
        public MAUICollectionView CollectionView
        {
            get { return _collectionView; }
            private set => _collectionView = value;
        }

        /// <summary>
        /// scroll direction.
        /// </summary>
        public ItemsLayoutOrientation ScrollDirection
        {
            get; set;
        } = ItemsLayoutOrientation.Vertical;

        /// <summary>
        /// when operating, it is true.
        /// notice only use it in Layout, because Maui maybe measure many times before arrange. i set it to false at the end of measure.
        /// </summary>
        protected bool HasOperation = false;

        /// <summary>
        /// it start equal to true when <see cref="HasOperation"/> equal to, but it be false after start operation animation.
        /// </summary>
        protected bool RunOperateAnimation = false;

        /// <summary>
        /// it contain information about item and bounds, we layout item according to it. it's IndexPath is latest.
        /// </summary>
        public LayoutInfor ItemLayoutBaseline;

        /// <summary>
        /// current Visible items, it is different with <see cref="MAUICollectionView.PreparedItems"/>
        /// </summary>
        public LayoutInfor VisibleIndexPath { get; protected set; }
        public LayoutInfor LastVisibleIndexPath { get; protected set; }

        public LayoutInfor OldPreparedItems;

        /// <summary>
        /// Arrange Header, Items and Footer. They will be arranged according to <see cref="MAUICollectionViewViewHolder.ItemBounds"/>
        /// </summary>
        public virtual void ArrangeContents()
        {
            CollectionView.Source?.WillArrange?.Invoke(CollectionView);
            AnimationManager.Run(CollectionView.IsScrolling, RunOperateAnimation);

            RunOperateAnimation = false;//disappear动画结束
            CollectionView.IsScrolling = false;

            if (CollectionView.HeaderView != null)
            {
                CollectionView.HeaderView.ArrangeSelf(CollectionView.HeaderView.ItemBounds);
            }

            // layout sections and rows
            foreach (var cell in CollectionView.PreparedItems)
            {
                if (cell.Value == CollectionView.DragedItem)
                    cell.Value.ArrangeSelf(cell.Value.DragItemBounds);
                else
                    cell.Value.ArrangeSelf(cell.Value.ItemBounds);

                //避免Measure时处理错误回收了可见的Item
                if (CollectionView.ReusableViewHolders.Contains(cell.Value))
                {
                    CollectionView.ReusableViewHolders.Remove(cell.Value);
                }
            }

            foreach (var item in CollectionView.ReusableViewHolders)
            {
                //if (item.Opacity != 0)
                //    item.Opacity = 0;
                item.ArrangeSelf(new Rect(0, -1000, item.Width, item.Height));
            }

            if (CollectionView.FooterView != null)
            {
                CollectionView.FooterView.ArrangeSelf(CollectionView.FooterView.ItemBounds);
            }
        }

        /// <summary>
        /// Measure size of Header, Items and Footer. It will load <see cref="MeasureHeader"/>, <see cref="MeasureItems"/>, <see cref="MeasureFooter"/>.
        /// </summary>
        /// <param name="tableViewWidth">visible width, it may be not accurate.</param>
        /// <param name="tableViewHeight">visible height</param>
        /// <returns></returns>
        public virtual Size MeasureContents(double tableViewWidth, double tableViewHeight)
        {
            Debug.WriteLine($"Measure ScrollY={CollectionView.ScrollY}");
            if (Updates != null)
            {
                HasOperation = true;
                RunOperateAnimation = true;
            }

            //tableView自身的大小
            Size tableViewBoundsSize = new Size(tableViewWidth, tableViewHeight);
            //Debug.WriteLine(tableViewBoundsSize);
            //当前可见区域在ContentView中的位置
            Rect visibleBounds = new Rect(0, CollectionView.ScrollY, tableViewBoundsSize.Width, tableViewBoundsSize.Height);
            double tableHeight = 0;
            //顶部和底部扩展的高度, 头2次布局不扩展, 防止初次显示计算太多item
            var topExtandHeight = CollectionView.HeightExpansion;
            var bottomExtandHeight = CollectionView.HeightExpansion;//第一次测量时, 可能顶部缺少空间, 不会创建那么多Extend, 我们在底部先创建好
            Rect layoutItemsInRect = Rect.FromLTRB(visibleBounds.Left, visibleBounds.Top - topExtandHeight, visibleBounds.Right, visibleBounds.Bottom + bottomExtandHeight);

            /* 
             * Header
             */
            tableHeight += MeasureHeader(0, visibleBounds.Width);

            // PreparedItems will be update, so use a local variable to store old prepareditems, IndexPath still is old.
            Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableViewHolders = new();
            foreach (var cell in CollectionView.PreparedItems)
                availableViewHolders.Add(cell.Key, cell.Value);
            CollectionView.PreparedItems.Clear();

            // ToList wil be sortable, we can get first or end item
            var tempOrderedCells = availableViewHolders.ToList();
            tempOrderedCells.Sort((x, y) =>
            {
                return x.Key.Compare(y.Key);
            });

            /*
             * Store old indexpath of prepareditem, maybe we need use it
             */
            OldPreparedItems = new LayoutInfor();
            if (tempOrderedCells.Count > 0)
            {
                var start = tempOrderedCells[0];
                OldPreparedItems.StartItem = start.Key;
                OldPreparedItems.StartBounds = start.Value.ItemBounds;
                var end = tempOrderedCells[tempOrderedCells.Count - 1];
                OldPreparedItems.EndItem = end.Key;
                OldPreparedItems.EndBounds = end.Value.ItemBounds;
                //Debug.WriteLine($"last start={start.Key} end={end.Key}");
            }

            /*
             * Recycle: according offset to recycle invisible items
             */
            var needRecycleCell = new List<NSIndexPath>();
            var scrollOffset = CollectionView.scrollOffset;
            if (scrollOffset > 0)//when swipe up, recycle top
            {
                foreach (var cell in tempOrderedCells)
                {
                    if (cell.Value.ItemBounds.Bottom < layoutItemsInRect.Top)
                    {
                        needRecycleCell.Add(cell.Key);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else if (scrollOffset < 0)//when swipe down, recycle bottom
            {
                for (int i = tempOrderedCells.Count - 1; i >= 0; i--)
                {
                    var cell = tempOrderedCells[i];
                    if (cell.Value.ItemBounds.Top > layoutItemsInRect.Bottom)
                    {
                        needRecycleCell.Add(cell.Key);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            foreach (var indexPath in needRecycleCell)
            {
                var cell = availableViewHolders[indexPath];
                if (cell == CollectionView.DragedItem)//don't recycle DragedItem
                {
                    continue;
                }
                CollectionView.RecycleViewHolder(cell);
                availableViewHolders.Remove(indexPath);
            }

            tempOrderedCells.Clear();
            needRecycleCell.Clear();
            scrollOffset = 0;//重置为0, 避免只更新数据时也移除cell

            /*
             * update OldVisibleIndexPath and OldPreparedItm index
             */
            DiffAnimation diffAnimation = null;
            if (HasOperation)
            {
                diffAnimation = Updates.Value.DiffAnimation;
                if (VisibleIndexPath.StartItem != null)
                    VisibleIndexPath.StartItem = diffAnimation.TryGetCurrentIndexPath(VisibleIndexPath.StartItem);
                if (VisibleIndexPath.EndItem != null)
                    VisibleIndexPath.EndItem = diffAnimation.TryGetCurrentIndexPath(VisibleIndexPath.EndItem);
                OldPreparedItems.StartItem = diffAnimation.TryGetCurrentIndexPath(OldPreparedItems.StartItem);
                OldPreparedItems.EndItem = diffAnimation.TryGetCurrentIndexPath(OldPreparedItems.EndItem);
            }
            LastVisibleIndexPath = VisibleIndexPath == null ? null : VisibleIndexPath.Copy();
            VisibleIndexPath = new LayoutInfor();

            /*
             * Remove: need remove viewholder, we don't directly reuse it.
             * Move: if item in last PreparedItems, we update it's IndexPath, and reuse it directly
             */
            if (HasOperation)
            {
                //move的需要获取在之前可见区域的viewHolder, 更新indexPath为最新的, 然后进行动画.
                Dictionary<NSIndexPath, MAUICollectionViewViewHolder> tempAvailableCells = new();//move修改旧的IndexPath,可能IndexPath已经存在, 因此使用临时字典存储
                for (int index = availableViewHolders.Count - 1; index >= 0; index--)
                {
                    var oldItem = availableViewHolders.ElementAt(index);
                    if (diffAnimation.IsRemoved(oldItem.Key))
                    {
                        availableViewHolders.Remove(oldItem.Key);
                    }
                    else
                    {
                        var oldViewHolder = oldItem.Value;
                        var currentIndexPath = diffAnimation.TryGetCurrentIndexPath(oldItem.Key);
                        availableViewHolders.Remove(oldItem.Key);
                        tempAvailableCells.Add(currentIndexPath, oldViewHolder);
                        oldViewHolder.IndexPath = currentIndexPath;
                    }
                }
                foreach (var item in tempAvailableCells)
                    availableViewHolders.Add(item.Key, item.Value);
            }

            /*
             * Measure Items
             */
            tableHeight += MeasureItems(tableHeight, layoutItemsInRect, visibleBounds, availableViewHolders);

            /*
             * record visible item
             */
            foreach (var item in CollectionView.PreparedItems)
            {
                if (item.Value.ItemBounds.IntersectsWith(visibleBounds))
                {
                    if (VisibleIndexPath.StartItem == null)
                        VisibleIndexPath.StartItem = item.Key;
                    VisibleIndexPath.EndItem = item.Key;
                }
            }

            /*
             * Select
             */
            foreach (var item in CollectionView.PreparedItems)
            {
                if (CollectionView.SelectedItems.Contains(item.Key))
                    item.Value.Selected = true;
            }


            /*
             * Move: if item no ViewHolder at last measure, at here can get ViewHolder
             */
            if (HasOperation)
            {
                diffAnimation.RecordCurrentViewHolder(CollectionView.PreparedItems, VisibleIndexPath);
                diffAnimation.Analysis(false);
                /*for (int index = Updates.Count - 1; index >= 0; index--)
                {
                    var update = Updates[index];

                    if (update.operateType == OperateItem.OperateType.Move)
                    {
                        if (CollectionView.PreparedItems.ContainsKey(update.target))
                        {
                            var viewHolder = CollectionView.PreparedItems[update.target];
                            if (!viewHolder.Equals(CollectionView.DragedItem) //Drag的不需要动画, 因为自身会在Arrange中移动
                            && update.operateAnimate)//move的可以是没有动画但位置移动的
                            {
                                if (update.source == update.target && viewHolder.OldBoundsInLayout == Rect.Zero)
                                {
                                    var bounds = RectForItem(NSIndexPath.FromRowSection(update.source.Row - update.moveCount, update.source.Section));//if delete other section's item
                                    viewHolder.OldBoundsInLayout = new Rect(bounds.X, bounds.Y, viewHolder.BoundsInLayout.Width, viewHolder.BoundsInLayout.Height);
                                }
                                else
                                {
                                    var bounds = RectForItem(update.source); //if delete same section's item
                                    viewHolder.OldBoundsInLayout = new Rect(bounds.X, bounds.Y, viewHolder.BoundsInLayout.Width, viewHolder.BoundsInLayout.Height);
                                }
                                viewHolder.Operation = (int)OperateItem.OperateType.Move;
                                AnimationManager.AddOperatedItem(viewHolder);
                            }
                            *//*if (!update.operateAnimate)
                            {
                                viewHolder.Operation = (int)OperateItem.OperateType.MoveNow;
                                AnimationManager.AddOperatedItem(viewHolder);
                            }*//*
                            Updates.RemoveAt(index);
                        }
                    }
                }*/
                Updates = null;
                diffAnimation.Dispose();
            }


            // 重新测量后, 需要显示的已经存入缓存的字典, 剩余的放入可重用列表
            foreach (MAUICollectionViewViewHolder cell in availableViewHolders.Values)
            {
                if (cell == CollectionView.DragedItem)
                {
                    CollectionView.PreparedItems.Add(cell.IndexPath, cell);
                    continue;
                }

                if (cell.ReuseIdentifier != default &&
                    cell.Operation != (int)OperateItem.OperateType.Move)//avoid recycle will invisible item, we recycle it in animation
                {
                    CollectionView.RecycleViewHolder(cell);
                }
                else if (cell.Operation == (int)OperateItem.OperateType.Move)
                {
                    /*if (HasOperation)
                    {
                        //these item is: last is visible, now will be invisible.
                        //we check animation data correct
                        var bounds = RectForItem(cell.IndexPath); // try get new bounds
                        if (bounds != cell.BoundsInLayout)
                        {
                            cell.OldBoundsInLayout = cell.BoundsInLayout;
                            cell.BoundsInLayout = new Rect(bounds.X, bounds.Y, cell.OldBoundsInLayout.Width, cell.OldBoundsInLayout.Height);
                        }
                    }*/
                }
                else
                {
                    cell.RemoveFromSuperview();
                }
            }

            /*
             * Footer
             */
            tableHeight += MeasureFooter(tableHeight, layoutItemsInRect.Width);

            HasOperation = false;
            Debug.WriteLine($"ChildCount={CollectionView.ContentView.Children.Count} PreparedItem={CollectionView.PreparedItems.Count} RecycleCount={CollectionView.ReusableViewHolders.Count}");
            //Debug.WriteLine("TableView Content Height:" + tableHeight);
            return new Size(tableViewBoundsSize.Width, tableHeight);
        }

        protected virtual double MeasureHeader(double top, double widthConstraint)
        {
            //表头的View是确定的, 我们可以直接测量
            if (CollectionView.HeaderView != null)
            {
                var measuredSize = CollectionView.HeaderView.MeasureSelf(widthConstraint, double.PositiveInfinity).Request;
                CollectionView.HeaderView.ItemBounds = new Rect(0, top, widthConstraint, measuredSize.Height);
                return measuredSize.Height;
            }
            return 0;
        }

        /// <summary>
        /// Measure items to fill target rect.
        /// </summary>
        /// <param name="top"></param>
        /// <param name="inRect">rect for prepared items</param>
        /// <param name="visibleRect">rect for visible items</param>
        /// <param name="availableCells"></param>
        /// <returns></returns>
        protected abstract double MeasureItems(double top, Rect inRect, Rect visibleRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells);

        protected virtual double MeasureFooter(double top, double widthConstraint)
        {
            //表尾的View是确定的, 我们可以直接测量
            if (CollectionView.FooterView != null)
            {
                var measuredSize = CollectionView.FooterView.MeasureSelf(widthConstraint, double.PositiveInfinity).Request;
                CollectionView.FooterView.ItemBounds = new Rect(0, top, widthConstraint, measuredSize.Height);
                return measuredSize.Height;
            }
            return 0;
        }

        /// <summary>
        /// Get item at point. <see cref="CollectionViewLayout.ItemAtPoint(Point, bool)"/> find it in <see cref="MAUICollectionView.PreparedItems"/>.
        /// <para>
        /// The main purpose of this method is to get IndexPath in visible position, if you want get item in invisible position, you need to know if subclass support it.
        /// </para>
        /// </summary>
        /// <param name="point">the point in Content or CollectionView</param>
        /// <param name="baseOnContent"> specify point is base on Content or CollectionView</param>
        /// <returns></returns>
        public abstract NSIndexPath ItemAtPoint(Point point, bool baseOnContent = true);

        /// <summary>
        /// Get <see cref="MAUICollectionViewViewHolder.ItemBounds"/> of item. If it in <see cref="MAUICollectionView.PreparedItems"/>, will return bounds, if not, return <see cref="Rect.Zero"/>.
        /// Subclass need implement it for other item.
        /// <para>
        /// The main purpose of this method is to get the position of visible and invisible items for manipulating animations.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public virtual Rect RectForItem(NSIndexPath indexPath)
        {
            if (CollectionView.PreparedItems.ContainsKey(indexPath))
            {
                return CollectionView.PreparedItems[indexPath].ItemBounds;
            }
            return Rect.Zero;
        }

        public virtual void ScrollTo(NSIndexPath indexPath, ScrollPosition scrollPosition, bool animated)
        {
            var rect = RectForItem(indexPath);
            switch (scrollPosition)
            {
                case ScrollPosition.None:
                case ScrollPosition.Top:
                    CollectionView.ScrollToAsync(0, rect.Top, animated);
                    break;
                case ScrollPosition.Middle:
                    CollectionView.ScrollToAsync(0, rect.Y + rect.Height / 2, animated);
                    break;
                case ScrollPosition.Bottom:
                    CollectionView.ScrollToAsync(0, rect.Bottom, animated);
                    break;
            }
        }

        public void Dispose()
        {
            AnimationManager.Dispose();
            _collectionView = null;
        }
    }
}