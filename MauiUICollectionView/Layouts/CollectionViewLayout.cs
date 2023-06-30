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

        public LayoutAnimationManager AnimationManager;


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
        protected bool isStartAnimate = false;

        /// <summary>
        /// Arrange Header, Items and Footer. They will be arranged according to <see cref="MAUICollectionViewViewHolder.BoundsInLayout"/>
        /// </summary>
        public virtual void ArrangeContents()
        {
            if (isStartAnimate)
            {
                Debug.WriteLine("Anim ArrangeContents");

                AnimationManager.Run();

                isStartAnimate = false;//disappear动画结束
            }

            //Debug.WriteLine("ArrangeContents");

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
                //回收时把不透明度都设置为了0, 显示时需要设置回来
                if (cell.Value.Operation == -1)//默认的状态, 这一步骤也对应于动画结束状态
                {
                    cell.Value.TranslationX = 0;
                    cell.Value.TranslationY = 0;
                    if (cell.Value.Opacity != 1)
                    {
                        cell.Value.Opacity = 1;
                    }
                }

                //避免Measure时处理错误回收了可见的Item
                if (CollectionView.ReusableViewHolders.Contains(cell.Value))
                {
                    CollectionView.ReusableViewHolders.Remove(cell.Value);
                }
            }

            foreach (var item in CollectionView.ReusableViewHolders)
            {
                if (item.Opacity != 0)
                    item.Opacity = 0;
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
                isStartAnimate = true;
            }

            if (measureTimes <= 3)
                measureTimes++;

            //tableView自身的大小
            Size tableViewBoundsSize = new Size(tableViewWidth, tableViewHeight);
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

            /*
             * Recycle
             */
            // 需要重新布局后, cell会变动, 先将之前显示的cell放入可供使用的cell字典, 如果数据源更新, 这里的IndexPath都还是对应旧的
            Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells = new();
            foreach (var cell in CollectionView.PreparedItems)
                availableCells.Add(cell.Key, cell.Value);
            CollectionView.PreparedItems.Clear();

            //复用是从_reusableCells获取的, 需要让不可见的先回收
            var tempOrderedCells = availableCells.ToList();//创建一个临时的有序列表, 有序可以知道上下显示的item
            var needRecycleCell = new List<NSIndexPath>();
            var scrollOffset = CollectionView.scrollOffset;
            if (scrollOffset > 0)//往上滑, 上面的需要回收
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
            else if (scrollOffset < 0)//往下滑, 下面的需要回收
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

            foreach (var indexPath in needRecycleCell)//需要回收的
            {
                var cell = availableCells[indexPath];
                if (cell == CollectionView.DragedItem)//Drag的不回收
                {
                    continue;
                }
                CollectionView.RecycleViewHolder(cell);
                availableCells.Remove(indexPath);
            }

            tempOrderedCells.Clear();
            needRecycleCell.Clear();
            scrollOffset = 0;//重置为0, 避免只更新数据时也移除cell

            //被remove的Item需要加入动画管理器, 动画后再回收
            for (int index = Updates.Count - 1; index >= 0; index--)
            {
                var update = Updates[index];
                if (update.operateType == OperateItem.OperateType.remove)//需要移除的先移除, move后的IndexPath与之相同
                {
                    if (CollectionView.SelectedRow.Contains(update.source))
                    {
                        CollectionView.SelectedRow.Remove(update.source);
                    }

                    if (availableCells.ContainsKey(update.source))
                    {
                        var cell = availableCells[update.source];
                        cell.Operation = (int)OperateItem.OperateType.remove;
                        availableCells.Remove(update.source);
                        AnimationManager.Add(cell);
                        Updates.RemoveAt(index);
                    }
                }
                else if (update.operateType == OperateItem.OperateType.update)
                {
                    //更新的我觉得不需要动画
                    if (availableCells.ContainsKey(update.source))
                    {
                        var cell = availableCells[update.source];
                        cell.Operation = (int)OperateItem.OperateType.update;
                        AnimationManager.Add(cell);
                        availableCells.Remove(update.source);
                        //CollectionView.RecycleViewHolder(cell);
                        Updates.RemoveAt(index);
                    }
                }
            }

            //如果move的里有选择的, 需要更新选择的IndexPath
            var oldSelectedIndexPath = new List<NSIndexPath>();
            for (int index = Updates.Count - 1; index >= 0; index--)
            {
                var update = Updates[index];

                if (update.operateType == OperateItem.OperateType.move)
                {
                    if (CollectionView.SelectedRow.Contains(update.source))
                    {
                        CollectionView.SelectedRow.Remove(update.source);
                        oldSelectedIndexPath.Add(update.target);
                        break;
                    }
                }
            }
            CollectionView.SelectedRow.AddRange(oldSelectedIndexPath);

            //move的需要获取在之前可见区域的viewHolder, 更新indexPath为最新的, 然后进行动画.
            Dictionary<NSIndexPath, MAUICollectionViewViewHolder> tempAvailableCells = new();//move修改旧的IndexPath,可能IndexPath已经存在, 因此使用临时字典存储
            for (int index = Updates.Count - 1; index >= 0; index--)
            {
                var update = Updates[index];

                if (update.operateType == OperateItem.OperateType.move)//移动的且显示的直接替换
                {
                    if (availableCells.ContainsKey(update.source))
                    {
                        var oldView = availableCells[update.source];
                        oldView.Operation = (int)OperateItem.OperateType.move;
                        if (!oldView.Equals(CollectionView.DragedItem))//Drag的不需要动画, 因为自身会在Arrange中移动
                            AnimationManager.Add(oldView);
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

            /*
             * Items
             */
            NSIndexPath preLastVisiableIndexPath = null;
            if (VisiableIndexPath.Count > 0)
                preLastVisiableIndexPath = VisiableIndexPath[VisiableIndexPath.Count - 1];
            VisiableIndexPath.Clear();
            Rect layoutItemsInRect = Rect.FromLTRB(visibleBounds.Left, visibleBounds.Top - topExtandHeight, visibleBounds.Right, visibleBounds.Bottom + bottomExtandHeight);
            tableHeight += MeasureItems(tableHeight, layoutItemsInRect, visibleBounds, availableCells);

            /*
             * 实现加载更多
             */
            NSIndexPath newLastVisiableIndexPath = null;
            if (VisiableIndexPath.Count > 0)
                newLastVisiableIndexPath = VisiableIndexPath[VisiableIndexPath.Count - 1];
            
            if (preLastVisiableIndexPath == null || !preLastVisiableIndexPath.Equals(newLastVisiableIndexPath))//避免多次加载
            {
                //判断是否是最后一个
                if (newLastVisiableIndexPath != null)
                {
                    var sectionIndex = CollectionView.NumberOfSections() - 1;

                    if (newLastVisiableIndexPath.Section == sectionIndex && newLastVisiableIndexPath.Row == CollectionView.NumberOfItemsInSection(sectionIndex) - 1)
                    {
                        CollectionView.Source.lastItemWillShow?.Invoke(CollectionView, newLastVisiableIndexPath);
                    }
                }
            }

            /*
             * 标记insert, 添加到动画
             */
            var insertList = new List<NSIndexPath>();
            foreach (var item in Updates)
            {
                if (item.operateType == OperateItem.OperateType.insert)
                {
                    insertList.Add(item.source);
                }
            }
            foreach (var item in CollectionView.PreparedItems)
            {
                if (insertList.Contains(item.Key))//插入的数据是原来没有的, 但其会与move的相同, 因为插入的位置原来的item需要move, 所以move会对旧的item处理
                {
                    item.Value.Operation = (int)OperateItem.OperateType.insert;
                    AnimationManager.Add(item.Value);
                }
            }
            Updates.Clear();

            // 重新测量后, 需要显示的已经存入缓存的字典, 剩余的放入可重用列表
            foreach (MAUICollectionViewViewHolder cell in availableCells.Values)
            {
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
        public List<NSIndexPath> VisiableIndexPath { get; protected set; } = new List<NSIndexPath>();

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

        public void Dispose()
        {
            AnimationManager.Dispose();
            _collectionView = null;
        }
    }
}