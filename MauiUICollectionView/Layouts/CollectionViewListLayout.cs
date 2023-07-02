namespace MauiUICollectionView.Layouts
{
    public partial class CollectionViewListLayout : CollectionViewLayout
    {
        public CollectionViewListLayout(MAUICollectionView collectionView) : base(collectionView)
        {
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

        protected override double MeasureItems(double top, Rect inRect, Rect visiableRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells)
        {
            double itemsHeight = 0;

            /* 
             * Items
             */
            int numberOfSections = CollectionView.NumberOfSections();
            for (int section = 0; section < numberOfSections; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);

                for (int row = 0; row < numberOfRows; row++)
                {
                    NSIndexPath indexPath = NSIndexPath.FromRowSection(row, section);
                    var reuseIdentifier = CollectionView.Source.reuseIdentifierForRowAtIndexPath(CollectionView, indexPath);
                    //尝试用之前测量的值或者预设值估计底部在哪
                    var rowMaybeTop = itemsHeight + top;
                    var rowHeightWant = CollectionView.Source.heightForRowAtIndexPath(CollectionView, indexPath);

                    var rowMaybeHeight = (rowHeightWant == MAUICollectionViewViewHolder.MeasureSelf ? (MeasuredSelfHeightCache.ContainsKey(indexPath) ? MeasuredSelfHeightCache[indexPath] : MeasuredSelfHeightCacheForReuse.ContainsKey(reuseIdentifier) ? MeasuredSelfHeightCacheForReuse[reuseIdentifier] : EstimatedRowHeight) : rowHeightWant);
                    var rowMaybeBottom = rowMaybeTop + rowMaybeHeight;
                    //如果在布局区域, 就详细测量
                    if ((rowMaybeTop >= inRect.Top && rowMaybeTop <= inRect.Bottom)//Item的顶部在布局区域内
                       || (rowMaybeBottom >= inRect.Top && rowMaybeBottom <= inRect.Bottom)//Item的底部在布局区域内
                       || (rowMaybeTop <= inRect.Top && rowMaybeBottom >= inRect.Bottom))//Item包含布局区域
                    {
                        //获取Cell, 优先获取之前已经被显示的, 这里假定已显示的数据没有变化
                        MAUICollectionViewViewHolder cell = null;
                        if (availableCells.ContainsKey(indexPath))
                            cell = availableCells[indexPath];
                        cell = CollectionView.Source.cellForRowAtIndexPath(CollectionView, indexPath, cell, inRect.Width);

                        if (cell != null)
                        {
                            //将Cell添加到正在显示的Cell字典
                            CollectionView.PreparedItems.Add(indexPath, cell);
                            //CollectionView.PreparedItems[indexPath] = cell;
                            if (availableCells.ContainsKey(indexPath))
                            {
                                availableCells.Remove(indexPath);
                            }
                            //Cell是否是正在被选择的
                            cell.Selected = CollectionView.SelectedRow.Contains(indexPath);

                            //添加到ScrollView, 必须先添加才有测量值
                            if (!CollectionView.ContentView.Children.Contains(cell))
                                CollectionView.AddSubview(cell);
                            //测量高度
                            Size measureSize;
                            cell.WidthRequest = inRect.Width;
                            cell.HeightRequest = -1;
                            if (rowHeightWant != MAUICollectionViewViewHolder.MeasureSelf)//固定高度
                            {
                                cell.HeightRequest = rowHeightWant;
                                measureSize = CollectionView.MeasureChild(cell, inRect.Width, rowHeightWant).Request;
                            }
                            else
                            {
                                measureSize = CollectionView.MeasureChild(cell, inRect.Width, double.PositiveInfinity).Request;
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

                            var finalHeight = (rowHeightWant == MAUICollectionViewViewHolder.MeasureSelf ? (MeasuredSelfHeightCache.ContainsKey(indexPath) ? MeasuredSelfHeightCache[indexPath] : MeasuredSelfHeightCacheForReuse.ContainsKey(cell.ReuseIdentifier) ? MeasuredSelfHeightCacheForReuse[cell.ReuseIdentifier] : EstimatedRowHeight) : rowHeightWant);
                            var bounds = new Rect(0, itemsHeight + top, measureSize.Width != 0 ? measureSize.Width : inRect.Width, finalHeight);
                            if (cell.Operation == (int)OperateItem.OperateType.move && isStartAnimate && bounds!= cell.BoundsInLayout)//move + anim + diff bounds
                            {
                                cell.OldBoundsInLayout = cell.BoundsInLayout;//move动画需要旧的位置
                                cell.BoundsInLayout = bounds;
                            }
                            else
                            {
                                cell.BoundsInLayout = bounds;
                            }

                            //存储可见的
                            if (bounds.IntersectsWith(visiableRect))
                            {
                                VisiableIndexPath.Add(indexPath);
                            }

                            itemsHeight += finalHeight;
                        }
                    }
                    else//如果不在布局区域内
                    {
                        if (availableCells.ContainsKey(indexPath))
                        {
                            var cell = availableCells[indexPath];
                            if (cell.ReuseIdentifier != default)
                            {
                                availableCells.Remove(indexPath);
                                CollectionView.RecycleViewHolder(cell);
                            }
                        }
                        itemsHeight += rowMaybeHeight;
                    }
                }
            }
            return itemsHeight;
        }

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
                tempBottom = totalHeight + CollectionView.HeaderView.DesiredSize.Height;
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
                        return indexPath;
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
                tempBottom = totalHeight + CollectionView.HeaderView.DesiredSize.Height;
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
    }
}
