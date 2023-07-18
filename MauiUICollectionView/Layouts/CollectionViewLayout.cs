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
        protected bool isStartOperateAnimate = false;

        /// <summary>
        /// Arrange Header, Items and Footer. They will be arranged according to <see cref="MAUICollectionViewViewHolder.BoundsInLayout"/>
        /// </summary>
        public virtual void ArrangeContents()
        {
            CollectionView.Source?.WillArrange?.Invoke(CollectionView);
            //AnimationManager.Run(isStartOperateAnimate);
            isStartOperateAnimate = false;//disappear动画结束

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
            //Debug.WriteLine("Measure");
            if (Updates.Count > 0)
            {
                isStartOperateAnimate = true;
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

            /* 
             * Header
             */
            tableHeight += MeasureHeader(0, visibleBounds.Width);

            // PreparedItems will be update, so use a local variable to store old prepareditems, IndexPath still is old.
            Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells = new();
            foreach (var cell in CollectionView.PreparedItems)
                availableCells.Add(cell.Key, cell.Value);
            CollectionView.PreparedItems.Clear();

            // ToList wil be sortable, we can get first or end item
            var tempOrderedCells = availableCells.ToList();

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
                    if (cell.Value.DesiredSize.Height < scrollOffset)
                    {
                        needRecycleCell.Add(cell.Key);
                        scrollOffset -= cell.Value.DesiredSize.Height;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else if (scrollOffset < 0)//when swipe down, recycle bottom
            {
                scrollOffset = -scrollOffset;
                for (int i = tempOrderedCells.Count - 1; i >= 0; i--)
                {
                    var cell = tempOrderedCells[i];
                    if (cell.Value.DesiredSize.Height < scrollOffset)
                    {
                        needRecycleCell.Add(cell.Key);
                        scrollOffset -= cell.Value.DesiredSize.Height;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            foreach (var indexPath in needRecycleCell)
            {
                var cell = availableCells[indexPath];
                if (cell == CollectionView.DragedItem)//don't recycle DragedItem
                {
                    continue;
                }
                CollectionView.RecycleViewHolder(cell);
                availableCells.Remove(indexPath);
            }

            tempOrderedCells.Clear();
            needRecycleCell.Clear();
            scrollOffset = 0;//重置为0, 避免只更新数据时也移除cell

            /*
             * Remove
             */
            if (isStartOperateAnimate)
            {
                for (int index = Updates.Count - 1; index >= 0; index--)
                {
                    var update = Updates[index];
                    if (update.operateType == OperateItem.OperateType.Remove)//需要移除的先移除, move后的IndexPath与之相同
                    {
                        if (availableCells.ContainsKey(update.source))
                        {
                            var cell = availableCells[update.source];

                            availableCells.Remove(update.source);

                            if (update.operateAnimate)
                            {
                                cell.Operation = (int)OperateItem.OperateType.Remove;
                            }
                            else
                            {
                                cell.Operation = (int)OperateItem.OperateType.RemoveNow;
                            }
                            AnimationManager.Add(cell);

                            Updates.RemoveAt(index);
                        }
                    }
                    else if (update.operateType == OperateItem.OperateType.Update)
                    {
                        //更新的我觉得不需要动画
                        if (availableCells.ContainsKey(update.source))
                        {
                            var cell = availableCells[update.source];
                            cell.Operation = (int)OperateItem.OperateType.Update;
                            AnimationManager.Add(cell);
                            availableCells.Remove(update.source);
                            Updates.RemoveAt(index);
                        }
                    }
                }
            }

            /*
             * update OldVisibleIndexPath and OldPreparedItm index
             */
            if (isStartOperateAnimate)
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
            if (isStartOperateAnimate)
            {
                //move的需要获取在之前可见区域的viewHolder, 更新indexPath为最新的, 然后进行动画.
                Dictionary<NSIndexPath, MAUICollectionViewViewHolder> tempAvailableCells = new();//move修改旧的IndexPath,可能IndexPath已经存在, 因此使用临时字典存储
                for (int index = Updates.Count - 1; index >= 0; index--)
                {
                    var update = Updates[index];

                    if (update.operateType == OperateItem.OperateType.Move)//移动的且显示的直接替换
                    {
                        if (availableCells.ContainsKey(update.source))
                        {
                            var oldView = availableCells[update.source];
                            if (!oldView.Equals(CollectionView.DragedItem) //Drag的不需要动画, 因为自身会在Arrange中移动
                            && update.operateAnimate)//move的可以是没有动画但位置移动的
                            {
                                oldView.Operation = (int)OperateItem.OperateType.Move;
                                AnimationManager.Add(oldView);
                            }
                            if (!update.operateAnimate)
                            {
                                oldView.Operation = (int)OperateItem.OperateType.MoveNow;
                                AnimationManager.Add(oldView);
                            }
                            availableCells.Remove(update.source);
                            if (availableCells.ContainsKey(update.target))
                                tempAvailableCells.Add(update.target, oldView);
                            else
                                availableCells.Add(update.target, oldView);
                            Updates.RemoveAt(index);
                        }
                    }
                }
                foreach (var item in tempAvailableCells)
                    availableCells.Add(item.Key, item.Value);
            }

            /*
             * Measure Items
             */
            Rect layoutItemsInRect = Rect.FromLTRB(visibleBounds.Left, visibleBounds.Top - topExtandHeight, visibleBounds.Right, visibleBounds.Bottom + bottomExtandHeight);
            tableHeight += MeasureItems(tableHeight, layoutItemsInRect, visibleBounds, availableCells);

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
            if (isStartOperateAnimate)
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
            if (isStartOperateAnimate)
            {
                var insertList = new List<NSIndexPath>();
                foreach (var item in Updates)
                {
                    if (item.operateType == OperateItem.OperateType.Insert)
                    {
                        insertList.Add(item.source);
                    }
                }
                foreach (var item in CollectionView.PreparedItems)
                {
                    if (insertList.Contains(item.Key))//插入的数据是原来没有的, 但其会与move的相同, 因为插入的位置原来的item需要move, 所以move会对旧的item处理
                    {
                        item.Value.Operation = (int)OperateItem.OperateType.Insert;
                        AnimationManager.Add(item.Value);
                    }
                }
            }
            Updates.Clear();

            // 重新测量后, 需要显示的已经存入缓存的字典, 剩余的放入可重用列表
            foreach (MAUICollectionViewViewHolder cell in availableCells.Values)
            {
                if(cell == CollectionView.DragedItem)
                {
                    CollectionView.PreparedItems.Add(cell.IndexPath, cell);
                    continue;
                }
                if (cell.ReuseIdentifier != default)
                {
                    CollectionView.RecycleViewHolder(cell);
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
        /// 每次布局可见的Item, 区别于<see cref="MAUICollectionView.PreparedItems"/>
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
        /// 
        /// </summary>
        /// <param name="point">相对于TableView的位置, 可以是在TableView上设置手势获取的位置</param>
        /// <returns>未找到时可返回null</returns>
        public abstract NSIndexPath IndexPathForVisibaleRowAtPointOfCollectionView(Point point);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="point">相对与Content的位置</param>
        /// <returns>未找到时可返回null</returns>
        public abstract NSIndexPath IndexPathForRowAtPointOfContentView(Point point);
        /// <summary>
        /// 返回IndexPath对应的行在ContentView中的位置. 在某些Item大小不固定的Layout中, 其可能是不精确的, 会变化的. 可能只是即时状态, 比如滑动后数据会变化.
        /// </summary>
        /// <returns></returns>
        public abstract Rect RectForRowOfIndexPathInContentView(NSIndexPath indexPath);

        public abstract double GetItemsCurrentHeight(NSIndexPath indexPath, int count);
        public void Dispose()
        {
            AnimationManager.Dispose();
            _collectionView = null;
        }
    }
}