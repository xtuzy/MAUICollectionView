namespace MauiUICollectionView.Layouts
{
    public class CollectionViewListLayout : CollectionViewLayout
    {
        public CollectionViewListLayout(MAUICollectionView collectionView) : base(collectionView)
        {
        }

        public override void ArrangeContents()
        {
            Size boundsSize = CollectionView.Bounds.Size;
            var contentOffset = CollectionView.ScrollY; //ContentOffset.Y;
            Rect visibleBounds = new Rect(0, contentOffset, boundsSize.Width, boundsSize.Height);

            if (CollectionView.HeaderView != null)
            {
                CollectionView.LayoutChild(CollectionView.HeaderView.ContentView, new Rect(0, CollectionView.HeaderView.PositionInLayout.Y, visibleBounds.Width, CollectionView.HeaderView.ContentView.DesiredSize.Height));
            }

            var removeDelt = 0;
            // layout sections and rows
            foreach (var cell in CollectionView.PreparedItems)
            {
                CollectionView.LayoutChild(cell.Value.ContentView, new Rect(cell.Value.PositionInLayout.X, cell.Value.PositionInLayout.Y, cell.Value.ContentView.DesiredSize.Width, cell.Value.ContentView.DesiredSize.Height));
            }
            if (CollectionView.FooterView != null)
            {
                CollectionView.LayoutChild(CollectionView.FooterView.ContentView, new Rect(0, CollectionView.FooterView.PositionInLayout.Y, visibleBounds.Width, CollectionView.FooterView.ContentView.DesiredSize.Height));
            }

            foreach (MAUICollectionViewViewHolder cell in CollectionView.ReusableViewHolders)
            {
                CollectionView.LayoutChild(cell.ContentView, new Rect(0, -3000, cell.ContentView.DesiredSize.Width, cell.ContentView.DesiredSize.Height));
            }
        }

        /// <summary>
        /// 存储同类型的已经显示的Row的行高, 用于估计未显示的行.
        /// </summary>
        public Dictionary<string, double> MeasuredSelfHeightCacheForReuse = new Dictionary<string, double>();

        /// <summary>
        /// 存储所有测量的行高
        /// </summary>
        public Dictionary<NSIndexPath, double> MeasuredSelfHeightCache = new Dictionary<NSIndexPath, double>();

        /// <summary>
        /// Image测量可能首先获得的高度为0, 造成要显示Item数目过多. 这个值尽量接近最终高度.
        /// </summary>
        public double EstimatedRowHeight = 100;
        /// <summary>
        /// 第一次显示我们尽量少创建Cell
        /// </summary>
        int measureTimes = 0;

        public override Size MeasureContents(double tableViewWidth, double tableViewHeight)
        {
            if (measureTimes <= 3)
                measureTimes++;

            //tableView自身的大小
            Size tableViewBoundsSize = new Size(tableViewWidth, tableViewHeight);
            //当前可见区域在ContentView中的位置
            Rect visibleBounds = new Rect(0, CollectionView.ScrollY, tableViewBoundsSize.Width, tableViewBoundsSize.Height);
            double tableHeight = 0;

            //表头的View是确定的, 我们可以直接测量
            if (CollectionView.HeaderView != null)
            {
                var _tableHeaderViewH = CollectionView.MeasureChild(CollectionView.HeaderView.ContentView, tableViewWidth, double.PositiveInfinity).Request.Height;
                CollectionView.HeaderView.PositionInLayout = new Point(0, 0);
                tableHeight += _tableHeaderViewH;
            }

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
                    if (cell.Value.ContentView.DesiredSize.Height < scrollOffset)
                    {
                        needRecycleCell.Add(cell.Key);
                        scrollOffset -= cell.Value.ContentView.DesiredSize.Height;
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
                    if (cell.Value.ContentView.DesiredSize.Height < scrollOffset)
                    {
                        needRecycleCell.Add(cell.Key);
                        scrollOffset -= cell.Value.ContentView.DesiredSize.Height;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            /*            else//0
                        {
                            if (removed != null)
                            {
                                if (tempCells[0].Key > removed)
                                {
                                    foreach (var cell in tempCells)
                                        needRemoveCell.Add(cell.Key);
                                }
                                else if (tempCells[tempCells.Count - 1].Key < removed)
                                {

                                }
                                else
                                {
                                    bool startRemove = false;
                                    for (int i = 0; i < tempCells.Count; i++)
                                    {
                                        var cell = tempCells[i];
                                        if (startRemove)
                                        {
                                            needRemoveCell.Add(cell.Key);
                                        }
                                        else
                                        {
                                            if (cell.Key.Compare(removed) == 0)
                                            {
                                                startRemove = true;
                                                needRemoveCell.Add(cell.Key);
                                            }
                                        }
                                    }
                                }

                                removed = null;
                            }
                        }*/
            foreach (var indexPath in needRecycleCell)//需要回收的
            {
                var cell = availableCells[indexPath];
                CollectionView.RecycleViewHolder(cell);
                availableCells.Remove(indexPath);
            }

            //顶部和底部扩展的高度, 头2次布局不扩展, 防止初次显示计算太多item
            var topExtandHeight = measureTimes < 3 ? 0 : CollectionView.ExtendHeight;
            var bottomExtandHeight = measureTimes < 3 ? 0 : measureTimes == 3 ? CollectionView.ExtendHeight * 2 : CollectionView.ExtendHeight;//第一次测量时, 可能顶部缺少空间, 不会创建那么多Extend, 我们在底部先创建好

            tempOrderedCells.Clear();
            needRecycleCell.Clear();
            scrollOffset = 0;//重置为0, 避免只更新数据时也移除cell

            //如果更新了数据源, 这里迭代时获取的是最新的IndexPath, 那么为了复用就需要知道新IndexPath和旧IndexPath的对应关系, 我们这里把缓存的item的旧Index替换为新的

            for (int index = Updates.Count - 1; index >= 0; index--)
            {
                var update = Updates[index];
                if (update.operateType == OperateItem.OperateType.remove)//需要移除的先移除, move后的IndexPath与之相同
                {
                    if (availableCells.ContainsKey(update.source))
                    {
                        CollectionView.RecycleViewHolder(availableCells[update.source]);
                        availableCells.Remove(update.source);
                        Updates.RemoveAt(index);
                    }
                }
                else if (update.operateType == OperateItem.OperateType.update)
                {
                    if (availableCells.ContainsKey(update.source))
                    {
                        CollectionView.RecycleViewHolder(availableCells[update.source]);
                        availableCells.Remove(update.source);
                        Updates.RemoveAt(index);
                    }
                }
            }

            Dictionary<NSIndexPath, MAUICollectionViewViewHolder> tempAvailableCells = new();//move修改旧的IndexPath,可能IndexPath已经存在, 因此使用临时字典存储
            for (int index = Updates.Count - 1; index >= 0; index--)
            {
                var update = Updates[index];

                if (update.operateType == OperateItem.OperateType.move)//移动的且显示的直接替换
                {
                    if (availableCells.ContainsKey(update.source))
                    {
                        var oldView = availableCells[update.source];
                        availableCells.Remove(update.source);
                        if (availableCells.ContainsKey(update.target))
                            tempAvailableCells.Add(update.target, oldView);
                        else
                            availableCells.Add(update.target, oldView);
                        Updates.RemoveAt(index);
                    }
                }
                else if (update.operateType == OperateItem.OperateType.insert)//插入的数据是原来没有的, 但其会与move的相同, 因为插入的位置原来的item需要move, 所以move会对旧的item处理
                {

                }
            }
            foreach (var item in tempAvailableCells)
                availableCells.Add(item.Key, item.Value);

            Updates.Clear();

            int numberOfSections = CollectionView.NumberOfSections();
            for (int section = 0; section < numberOfSections; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);

                for (int row = 0; row < numberOfRows; row++)
                {
                    NSIndexPath indexPath = NSIndexPath.FromRowSection(row, section);
                    var reuseIdentifier = CollectionView.Source.reuseIdentifierForRowAtIndexPath(CollectionView, indexPath);
                    //尝试用之前测量的值或者预设值估计底部在哪
                    var rowMaybeTop = tableHeight;
                    var rowHeightWant = CollectionView.Source.heightForRowAtIndexPath(CollectionView, indexPath);

                    var rowMaybeHeight = (rowHeightWant == MAUICollectionViewViewHolder.MeasureSelf ? (MeasuredSelfHeightCache.ContainsKey(indexPath) ? MeasuredSelfHeightCache[indexPath] : MeasuredSelfHeightCacheForReuse.ContainsKey(reuseIdentifier) ? MeasuredSelfHeightCacheForReuse[reuseIdentifier] : EstimatedRowHeight) : rowHeightWant);
                    var rowMaybeBottom = tableHeight + rowMaybeHeight;
                    //如果在可见区域, 就详细测量
                    if ((rowMaybeTop >= visibleBounds.Top - topExtandHeight && rowMaybeTop <= visibleBounds.Bottom + bottomExtandHeight)
                       || (rowMaybeBottom >= visibleBounds.Top - topExtandHeight && rowMaybeBottom <= visibleBounds.Bottom + bottomExtandHeight)
                       || (rowMaybeTop <= visibleBounds.Top - topExtandHeight && rowMaybeBottom >= visibleBounds.Bottom + bottomExtandHeight))
                    {
                        //获取Cell, 优先获取之前已经被显示的, 这里假定已显示的数据没有变化
                        MAUICollectionViewViewHolder cell = availableCells.ContainsKey(indexPath) ? availableCells[indexPath] : CollectionView.Source.cellForRowAtIndexPath(CollectionView, indexPath, tableViewWidth, false);

                        if (cell != null)
                        {
                            //将Cell添加到正在显示的Cell字典
                            CollectionView.PreparedItems[indexPath] = cell;
                            if (availableCells.ContainsKey(indexPath)) availableCells.Remove(indexPath);
                            //Cell是否是正在被选择的
                            cell.Highlighted = CollectionView._highlightedRow == null ? false : CollectionView._highlightedRow.IsEqual(indexPath);
                            cell.Selected = CollectionView._selectedRow == null ? false : CollectionView._selectedRow.IsEqual(indexPath);

                            //添加到ScrollView, 必须先添加才有测量值
                            if (!CollectionView.ContentView.Children.Contains(cell.ContentView))
                                CollectionView.AddSubview(cell.ContentView);
                            //测量高度
                            if (rowHeightWant != MAUICollectionViewViewHolder.MeasureSelf)//固定高度
                            {
                                cell.ContentView.HeightRequest = rowHeightWant;
                                var measureSize = CollectionView.MeasureChild(cell.ContentView, tableViewBoundsSize.Width, rowHeightWant).Request;
                            }
                            else
                            {
                                var measureSize = CollectionView.MeasureChild(cell.ContentView, tableViewBoundsSize.Width, double.PositiveInfinity).Request;
                                if (measureSize.Height != 0)
                                {
                                    if (!MeasuredSelfHeightCache.ContainsKey(indexPath))
                                        MeasuredSelfHeightCache.Add(indexPath, measureSize.Height);
                                    else MeasuredSelfHeightCache[indexPath] = measureSize.Height;

                                    //存储同类型的高度
                                    if (!MeasuredSelfHeightCacheForReuse.ContainsKey(cell.ReuseIdentifier))
                                    {
                                        MeasuredSelfHeightCacheForReuse.Add(cell.ReuseIdentifier, measureSize.Height);
                                    }
                                    else
                                    {
                                        if (MeasuredSelfHeightCacheForReuse[cell.ReuseIdentifier] < measureSize.Height)
                                        {
                                            MeasuredSelfHeightCacheForReuse[cell.ReuseIdentifier] = measureSize.Height;
                                        }
                                    }
                                }
                            }

                            cell.PositionInLayout = new Point(0, tableHeight);
                            var finalHeight = (rowHeightWant == MAUICollectionViewViewHolder.MeasureSelf ? (MeasuredSelfHeightCache.ContainsKey(indexPath) ? MeasuredSelfHeightCache[indexPath] : MeasuredSelfHeightCacheForReuse.ContainsKey(cell.ReuseIdentifier) ? MeasuredSelfHeightCacheForReuse[cell.ReuseIdentifier] : EstimatedRowHeight) : rowHeightWant);
                            tableHeight += finalHeight;
                        }
                    }
                    else//如果不可见
                    {
                        if (availableCells.ContainsKey(indexPath))
                        {
                            var cell = availableCells[indexPath];
                            if (cell.ReuseIdentifier != default)
                            {
                                CollectionView.RecycleViewHolder(cell);
                                availableCells.Remove(indexPath);
                            }
                            cell.PrepareForReuse();
                        }
                        tableHeight = rowMaybeBottom;
                    }
                }
            }

            // 重新测量后, 需要显示的已经存入缓存的字典, 剩余的放入可重用列表
            foreach (MAUICollectionViewViewHolder cell in availableCells.Values)
            {
                if (cell.ReuseIdentifier != default)
                {
                    if (CollectionView.ReusableViewHolders.Count > 3)
                    {
                        cell.ContentView.RemoveFromSuperview();
                    }
                    else
                        CollectionView.RecycleViewHolder(cell);
                }
                else
                {
                    cell.ContentView.RemoveFromSuperview();
                }
            }

            // non-reusable cells should end up dealloced after at this point, but reusable ones live on in _reusableCells.

            // now make sure that all available (but unused) reusable cells aren't on screen in the visible area.
            // this is done becaue when resizing a table view by shrinking it's height in an animation, it looks better. The reason is that
            // when an animation happens, it sets the frame to the new (shorter) size and thus recalcuates which cells should be visible.
            // If it removed all non-visible cells, then the cells on the bottom of the table view would disappear immediately but before
            // the frame of the table view has actually animated down to the new, shorter size. So the animation is jumpy/ugly because
            // the cells suddenly disappear instead of seemingly animating down and out of view like they should. This tries to leave them
            // on screen as long as possible, but only if they don't get in the way.
            var allCachedCells = CollectionView.PreparedItems.Values;
            foreach (MAUICollectionViewViewHolder cell in CollectionView.ReusableViewHolders)
            {
                if (cell.ContentView.Frame.IntersectsWith(visibleBounds) && !allCachedCells.Contains(cell))
                {
                    //cell.RemoveFromSuperview();
                }
            }

            //表尾的View是确定的, 我们可以直接测量
            if (CollectionView.FooterView != null)
            {
                var footMeasureSize = CollectionView.MeasureChild(CollectionView.FooterView.ContentView, tableViewBoundsSize.Width, double.PositiveInfinity).Request;
                CollectionView.FooterView.PositionInLayout = new Point(0, tableHeight);
                tableHeight += footMeasureSize.Height;
            }
            //Debug.WriteLine("TableView Content Height:" + tableHeight);
            lastMeasure = new Size(tableViewBoundsSize.Width, tableHeight);
            return lastMeasure;
        }

        Size lastMeasure;

        /// <summary>
        /// 可见的区域中的点在哪一行
        /// </summary>
        /// <param name="point">相对于TableView的位置, 可以是在TableView上设置手势获取的位置</param>
        /// <returns></returns>
        public override NSIndexPath IndexPathForVisibaleRowAtPointOfCollectionView(Point point)
        {
            var contentOffset = CollectionView.ScrollY;
            point.Y = point.Y + contentOffset;//相对于content
            return IndexPathForRowAtPointOfContentView(point);
        }

        /// <summary>
        /// 迭代全部内容计算点在哪
        /// </summary>
        /// <param name="point">相对与Content的位置</param>
        /// <returns></returns>
        public override NSIndexPath IndexPathForRowAtPointOfContentView(Point point)
        {
            double totalHeight = 0;
            double tempBottom = 0;
            if (CollectionView.HeaderView != null)
            {
                tempBottom = totalHeight + CollectionView.HeaderView.ContentView.DesiredSize.Height;
                if (totalHeight <= point.Y && tempBottom >= point.Y)
                {
                    return null;
                }
                totalHeight = tempBottom;
            }

            var number = CollectionView.NumberOfSections();
            for (int section = 0; section < number; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                for (int row = 0; row < numberOfRows; row++)
                {
                    NSIndexPath indexPath = NSIndexPath.FromRowSection(row, section);
                    var reuseIdentifier = CollectionView.Source.reuseIdentifierForRowAtIndexPath(CollectionView, indexPath);

                    var rowHeightWant = CollectionView.Source.heightForRowAtIndexPath(CollectionView, indexPath);

                    var rowMaybeHeight = (rowHeightWant == MAUICollectionViewViewHolder.MeasureSelf ? (MeasuredSelfHeightCache.ContainsKey(indexPath) ? MeasuredSelfHeightCache[indexPath] : MeasuredSelfHeightCacheForReuse.ContainsKey(reuseIdentifier) ? MeasuredSelfHeightCacheForReuse[reuseIdentifier] : EstimatedRowHeight) : rowHeightWant);
                    tempBottom = totalHeight + rowMaybeHeight;
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

        public override Rect RectForRowOfIndexPathInContentView(NSIndexPath indexPathTarget)
        {
            double totalHeight = 0;
            double tempBottom = 0;
            if (CollectionView.HeaderView != null)
            {
                tempBottom = totalHeight + CollectionView.HeaderView.ContentView.DesiredSize.Height;
                totalHeight = tempBottom;
            }

            var number = CollectionView.NumberOfSections();
            for (int section = 0; section < number; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                for (int row = 0; row < numberOfRows; row++)
                {
                    NSIndexPath indexPath = NSIndexPath.FromRowSection(row, section);
                    var reuseIdentifier = CollectionView.Source.reuseIdentifierForRowAtIndexPath(CollectionView, indexPath);

                    var rowHeightWant = CollectionView.Source.heightForRowAtIndexPath(CollectionView, indexPath);

                    var rowMaybeHeight = (rowHeightWant == MAUICollectionViewViewHolder.MeasureSelf ? (MeasuredSelfHeightCache.ContainsKey(indexPath) ? MeasuredSelfHeightCache[indexPath] : MeasuredSelfHeightCacheForReuse.ContainsKey(reuseIdentifier) ? MeasuredSelfHeightCacheForReuse[reuseIdentifier] : EstimatedRowHeight) : rowHeightWant);
                    tempBottom = totalHeight + rowMaybeHeight;

                    if (indexPath.Section == indexPathTarget.Section && indexPath.Row == indexPathTarget.Row)
                    {
                        return Rect.FromLTRB(0, totalHeight, CollectionView.ContentSize.Width, tempBottom);
                    }
                    else
                    {
                        totalHeight = tempBottom;
                    }
                }
            }
            return Rect.Zero;
        }

        #region 操作
        /*
         * 需要汇总所有操作, 因为多个操作一起时, 我们需要同时更新动画.
         * 汇总所有操作需要把数据不变的, 只是IndexPath变了的Item找出来, 因为它显示时如果更新数据, 会有加载过程, 导致不像连续的动画.
         */

        public List<OperateItem> Updates = new();

        public class OperateItem
        {
            public enum OperateType
            {
                /// <summary>
                /// 移除de
                /// </summary>
                remove,
                /// <summary>
                /// 新增的
                /// </summary>
                insert,
                /// <summary>
                /// 移动的, 代表IndexPath改变的
                /// </summary>
                move,
                /// <summary>
                /// 内容更新的
                /// </summary>
                update
            }
            //旧Index
            public NSIndexPath source;
            //新的Index
            public NSIndexPath target;
            public OperateType operateType;
        }

        /// <summary>
        /// 通知CollectionView移除某Item, 需要做出改变.
        /// 移除会让移除项后面的Item需要调节高度, 这个高度需要动画改变
        /// </summary>
        /// <param name="indexPaths"></param>
        public void RemoveItems(NSIndexPath indexPaths)
        {
            Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.remove, source = indexPaths });

            //找到已经可见的Item和它们的IndexPath,和目标IndexPath
            foreach (var visiableItem in CollectionView.PreparedItems)
            {
                if (visiableItem.Key.Section == indexPaths.Section)//同一section的item才变化
                {
                    if (visiableItem.Key.Row > indexPaths.Row)//大于移除item的row的需要更新IndexPath
                    {
                        Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.move, source = visiableItem.Key, target = NSIndexPath.FromRowSection(visiableItem.Key.Row - 1, visiableItem.Key.Section) });
                    }
                }
            }
            CollectionView._reloadDataCounts();
        }

        /// <summary>
        /// 通知CollectionView插入了Item, 需要做出改变.
        /// </summary>
        /// <param name="indexPaths">插入应该是在某个位置插入, 比如0, 即插入在0位置</param>
        public void InsertItems(NSIndexPath indexPaths)
        {
            //找到已经可见的Item和它们的IndexPath,和目标IndexPath
            foreach (var visiableItem in CollectionView.PreparedItems)
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

            CollectionView._reloadDataCounts();
        }

        public void MoveItem(NSIndexPath indexPath, NSIndexPath toIndexPath)
        {
            Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.move, source = indexPath, target = toIndexPath });

            //如果同Section, Move影响的只是之间的
            if (indexPath.Section == toIndexPath.Section)
            {
                var isUpMove = indexPath.Row > toIndexPath.Row;
                //先移除
                foreach (var visiableItem in CollectionView.PreparedItems)
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
                foreach (var visiableItem in CollectionView.PreparedItems)
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
                foreach (var visiableItem in CollectionView.PreparedItems)
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
            CollectionView._reloadDataCounts();
        }

        public void ChangeItem(NSIndexPath indexPath)
        {
            //找到已经可见的Item和它们的IndexPath,和目标IndexPath
            foreach (var visiableItem in CollectionView.PreparedItems)
            {
                if (visiableItem.Key.Section == indexPath.Section)//同一section的item才变化
                {
                    if (visiableItem.Key.Row == indexPath.Row)//大于等于item的row的需要更新IndexPath
                    {
                        Updates.Add(new OperateItem() { operateType = OperateItem.OperateType.update, source = visiableItem.Key });
                    }
                }
            }
        }
        #endregion
    }
}
