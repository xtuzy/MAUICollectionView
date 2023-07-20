using System.Diagnostics;

namespace MauiUICollectionView.Layouts
{
    /// <summary>
    /// 布局的逻辑放在此处
    /// </summary>
    public abstract class CollectionViewLayout : IDisposable
    {
        public CollectionViewLayout(MAUICollectionView collectionView)
        {
            this.CollectionView = collectionView;
            AnimationManager = new LayoutAnimationManager(collectionView);
        }

        public LayoutAnimationManager AnimationManager { get; set; }


        /*
         * 需要汇总所有操作, 因为多个操作一起时, 我们需要同时更新动画.
         * 汇总所有操作需要把数据不变的, 只是IndexPath变了的Item找出来, 因为它显示时如果更新数据, 会有加载过程, 导致不像连续的动画.
         */
        public List<OperateItem> Updates = new();

        private MAUICollectionView _collectionView;
        public MAUICollectionView CollectionView
        {
            get { return _collectionView; }
            private set => _collectionView = value;
        }

        /// <summary>
        /// 滚动方向. 必须设置值, 默认为垂直方向.
        /// </summary>
        public virtual ItemsLayoutOrientation ScrollDirection
        {
            get; set;
        } = ItemsLayoutOrientation.Vertical;

        /// <summary>
        /// 标志需要remove, move的item的动画开始.
        /// </summary>
        protected bool IsOperating = false;

        /// <summary>
        /// Arrange Header, Items and Footer. They will be arranged according to <see cref="MAUICollectionViewViewHolder.BoundsInLayout"/>
        /// </summary>
        public virtual void ArrangeContents()
        {
            CollectionView.Source?.WillArrange?.Invoke(CollectionView);
            AnimationManager.Run(CollectionView.IsScrolling, IsOperating);
            
            IsOperating = false;//disappear动画结束
            CollectionView.IsScrolling = false;

            if (CollectionView.HeaderView != null)
            {
                CollectionView.LayoutChild(CollectionView.HeaderView, CollectionView.HeaderView.BoundsInLayout);
            }

            // layout sections and rows
            foreach (var cell in CollectionView.PreparedItems)
            {
                if (cell.Value == CollectionView.DragedItem)
                    CollectionView.LayoutChild(cell.Value, cell.Value.DragBoundsInLayout);
                else
                    CollectionView.LayoutChild(cell.Value, cell.Value.BoundsInLayout);

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
                CollectionView.LayoutChild(item, new Rect(0, 0 - item.Bounds.Height, item.Width, item.Height));
            }

            if (CollectionView.FooterView != null)
            {
                CollectionView.LayoutChild(CollectionView.FooterView, CollectionView.FooterView.BoundsInLayout);
            }
        }

        /// <summary>
        /// 第一次显示我们尽量少创建Cell
        /// </summary>
        int measureTimes = 0;

        public NSIndexPath[] OldPreparedItems = new NSIndexPath[2];

        /// <summary>
        /// 存储同类型的已经显示的Row的行高, 用于估计未显示的行.
        /// </summary>
        public Dictionary<string, double> MeasuredSelfHeightCacheForReuse = new Dictionary<string, double>();

        /// <summary>
        /// Measure size of Header, Items and Footer. It will load <see cref="MeasureHeader"/>, <see cref="MeasureItems"/>, <see cref="MeasureFooter"/>.
        /// </summary>
        /// <param name="tableViewWidth">当作可见宽度, 可能是根据屏幕大小的估计值</param>
        /// <param name="tableViewHeight">当作可见宽度, 可能是根据屏幕大小的估计值</param>
        /// <returns></returns>
        public virtual Size MeasureContents(double tableViewWidth, double tableViewHeight)
        {
            Debug.WriteLine("Measure");
            if (Updates.Count > 0)
            {
                IsOperating = true;
            }

            if (measureTimes <= 3)
                measureTimes++;

            //tableView自身的大小
            Size tableViewBoundsSize = new Size(tableViewWidth, tableViewHeight);
            Debug.WriteLine(tableViewBoundsSize);
            //当前可见区域在ContentView中的位置
            Rect visibleBounds = new Rect(0, CollectionView.ScrollY, tableViewBoundsSize.Width, tableViewBoundsSize.Height);
            double tableHeight = 0;
            //顶部和底部扩展的高度, 头2次布局不扩展, 防止初次显示计算太多item
            var topExtandHeight = measureTimes < 3 ? 0 : CollectionView.ExtendHeight;
            var bottomExtandHeight = measureTimes < 3 ? 0 : measureTimes == 3 ? CollectionView.ExtendHeight * 2 : CollectionView.ExtendHeight;//第一次测量时, 可能顶部缺少空间, 不会创建那么多Extend, 我们在底部先创建好
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

            /*
             * Store old indexpath of prepareditem, maybe we need use it
             */
            if (tempOrderedCells.Count > 0)
            {
                OldPreparedItems[0] = tempOrderedCells[0].Key;
                OldPreparedItems[1] = tempOrderedCells[tempOrderedCells.Count - 1].Key;
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
                    if (cell.Value.BoundsInLayout.Bottom < layoutItemsInRect.Top)
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
                    if (cell.Value.BoundsInLayout.Top > layoutItemsInRect.Bottom)
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
             * Remove
             */
            if (IsOperating)
            {
                for (int index = Updates.Count - 1; index >= 0; index--)
                {
                    var update = Updates[index];
                    if (update.operateType == OperateItem.OperateType.Remove)//需要移除的先移除, move后的IndexPath与之相同
                    {
                        if (availableViewHolders.ContainsKey(update.source))
                        {
                            var viewHolder = availableViewHolders[update.source];
                            availableViewHolders.Remove(update.source);

                            if (update.operateAnimate)
                            {
                                viewHolder.Operation = (int)OperateItem.OperateType.Remove;
                            }
                            else
                            {
                                viewHolder.Operation = (int)OperateItem.OperateType.RemoveNow;
                            }
                            AnimationManager.Add(viewHolder);

                            Updates.RemoveAt(index);
                        }
                    }
                    else if (update.operateType == OperateItem.OperateType.Update)
                    {
                        //更新的我觉得不需要动画
                        if (availableViewHolders.ContainsKey(update.source))
                        {
                            var viewHolder = availableViewHolders[update.source];
                            viewHolder.Operation = (int)OperateItem.OperateType.Update;
                            AnimationManager.Add(viewHolder);
                            availableViewHolders.Remove(update.source);
                            Updates.RemoveAt(index);
                        }
                    }
                }
            }

            /*
             * update OldVisibleIndexPath and OldPreparedItm index
             */
            if (IsOperating)
            {
                var oldVisibleIndexPath = new List<NSIndexPath>();
                var oldpreparedIndexPath = new NSIndexPath[2] { OldPreparedItems[0], OldPreparedItems[1] };
                for (int index = Updates.Count - 1; index >= 0; index--)
                {
                    var update = Updates[index];

                    if (update.operateType == OperateItem.OperateType.Move)
                    {
                        if (VisibleIndexPath.Contains(update.source))
                        {
                            VisibleIndexPath.Remove(update.source);
                            oldVisibleIndexPath.Add(update.target);
                        }
                        if (update.source.Equals(oldpreparedIndexPath[0]))
                        {
                            oldpreparedIndexPath[0] = null;
                            OldPreparedItems[0] = update.target;
                        }
                        if (update.source.Equals(oldpreparedIndexPath[1]))
                        {
                            oldpreparedIndexPath[1] = null;
                            OldPreparedItems[1] = update.target;
                        }
                    }
                }
                VisibleIndexPath.AddRange(oldVisibleIndexPath);
            }
            LastVisibleIndexPath = new List<NSIndexPath>(VisibleIndexPath);
            VisibleIndexPath.Clear();

            /*
             * Move: if item in last PreparedItems, we update it's IndexPath, and reuse it directly
             */
            if (IsOperating)
            {
                //move的需要获取在之前可见区域的viewHolder, 更新indexPath为最新的, 然后进行动画.
                Dictionary<NSIndexPath, MAUICollectionViewViewHolder> tempAvailableCells = new();//move修改旧的IndexPath,可能IndexPath已经存在, 因此使用临时字典存储
                for (int index = Updates.Count - 1; index >= 0; index--)
                {
                    var update = Updates[index];

                    if (update.operateType == OperateItem.OperateType.Move)//移动的且显示的直接替换
                    {
                        if (availableViewHolders.ContainsKey(update.source))
                        {
                            var oldViewHolder = availableViewHolders[update.source];
                            if (!oldViewHolder.Equals(CollectionView.DragedItem) //Drag的不需要动画, 因为自身会在Arrange中移动
                            && update.operateAnimate)//move的可以是没有动画但位置移动的
                            {
                                oldViewHolder.Operation = (int)OperateItem.OperateType.Move;
                                AnimationManager.Add(oldViewHolder);
                            }
                            if (!update.operateAnimate)
                            {
                                oldViewHolder.Operation = (int)OperateItem.OperateType.MoveNow;
                                AnimationManager.Add(oldViewHolder);
                            }
                            availableViewHolders.Remove(update.source);
                            if (availableViewHolders.ContainsKey(update.target))
                                tempAvailableCells.Add(update.target, oldViewHolder);
                            else
                                availableViewHolders.Add(update.target, oldViewHolder);
                            oldViewHolder.IndexPath = update.target;
                            Updates.RemoveAt(index);
                        }
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
             * Select
             */
            foreach (var item in CollectionView.PreparedItems)
            {
                item.Value.Selected = CollectionView.SelectedRow.Contains(item.Key);
            }

            /*
             * Move: if item no ViewHolder at last measure, at here can get ViewHolder
             */
            if (IsOperating)
            {
                for (int index = Updates.Count - 1; index >= 0; index--)
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
                                viewHolder.OldBoundsInLayout = RectForItem(update.source); // try get old bounds
                                viewHolder.Operation = (int)OperateItem.OperateType.Move;
                                AnimationManager.Add(viewHolder);
                            }
                            if (!update.operateAnimate)
                            {
                                viewHolder.Operation = (int)OperateItem.OperateType.MoveNow;
                                AnimationManager.Add(viewHolder);
                            }
                            Updates.RemoveAt(index);
                        }
                    }
                }
            }
            /*
             * 标记insert, 添加到动画
             */
            if (IsOperating)
            {
                var insertList = new Dictionary<NSIndexPath, OperateItem>();
                foreach (var item in Updates)
                {
                    if (item.operateType == OperateItem.OperateType.Insert)
                    {
                        insertList.Add(item.source, item);
                    }
                }
                foreach (var item in CollectionView.PreparedItems)
                {
                    if (insertList.ContainsKey(item.Key))//插入的数据是原来没有的, 但其会与move的相同, 因为插入的位置原来的item需要move, 所以move会对旧的item处理
                    {
                        item.Value.Operation = (int)OperateItem.OperateType.Insert;
                        AnimationManager.Add(item.Value);
                    }
                }
                insertList.Clear();
            }
            Updates.Clear();

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
                }else if(cell.Operation == (int)OperateItem.OperateType.Move)
                {

                }else
                {
                    cell.RemoveFromSuperview();
                }
            }

            /*
             * Footer
             */
            tableHeight += MeasureFooter(tableHeight, layoutItemsInRect.Width);

            //Debug.WriteLine("TableView Content Height:" + tableHeight);
            return new Size(tableViewBoundsSize.Width, tableHeight);
        }

        protected virtual double MeasureHeader(double top, double widthConstraint)
        {
            //表头的View是确定的, 我们可以直接测量
            if (CollectionView.HeaderView != null)
            {
                var measuredSize = CollectionView.MeasureChild(CollectionView.HeaderView, widthConstraint, double.PositiveInfinity).Request;
                CollectionView.HeaderView.BoundsInLayout = new Rect(0, top, widthConstraint, measuredSize.Height);
                return measuredSize.Height;
            }
            return 0;
        }

        /// <summary>
        /// current Visible items, it is different with <see cref="MAUICollectionView.PreparedItems"/>
        /// </summary>
        public List<NSIndexPath> VisibleIndexPath { get; protected set; } = new List<NSIndexPath>();
        public List<NSIndexPath> LastVisibleIndexPath { get; protected set; } = new List<NSIndexPath>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="top"></param>
        /// <param name="inRect"></param>
        /// <param name="visiableRect">可见区域, 对应于ScrollView大小</param>
        /// <param name="availableCells"></param>
        /// <returns></returns>
        protected abstract double MeasureItems(double top, Rect inRect, Rect visiableRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells);

        protected virtual double MeasureFooter(double top, double widthConstraint)
        {
            //表尾的View是确定的, 我们可以直接测量
            if (CollectionView.FooterView != null)
            {
                var measuredSize = CollectionView.MeasureChild(CollectionView.FooterView, widthConstraint, double.PositiveInfinity).Request;
                CollectionView.FooterView.BoundsInLayout = new Rect(0, top, widthConstraint, measuredSize.Height);
                return measuredSize.Height;
            }
            return 0;
        }

        /// <summary>
        /// Get item at point.
        /// </summary>
        /// <param name="point">the point in Content or CollectionView</param>
        /// <param name="baseOnContent"> specify point is base on Content or CollectionView</param>
        /// <returns></returns>
        public abstract NSIndexPath ItemAtPoint(Point point, bool baseOnContent = true);

        /// <summary>
        /// Get rect of item. this method maybe be slow.
        /// </summary>
        /// <returns></returns>
        public abstract Rect RectForItem(NSIndexPath indexPath);

        /// <summary>
        /// Get total height of items. we also measure invisible items after visible item, because it be used to adjust ScrollY for stay don't move visible items when Remove or Insert.
        /// </summary>
        /// <param name="indexPath"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public abstract double HeightForItems(NSIndexPath indexPath, int count);

        public void Dispose()
        {
            AnimationManager.Dispose();
            _collectionView = null;
        }
    }
}