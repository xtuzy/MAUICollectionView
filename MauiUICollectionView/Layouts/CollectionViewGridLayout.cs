namespace MauiUICollectionView.Layouts
{
    /// <summary>
    /// GridLayout 意味着分割列表为几列, 高度直接指定比例, 在布局时是确定的值. Source中的设置高度的方法对其无效. 注意Cell不要直接设置Margin, Layout中未加入计算.
    /// </summary>
    public class CollectionViewGridLayout : CollectionViewLayout
    {
        public CollectionViewGridLayout(MAUICollectionView collectionView) : base(collectionView)
        {
        }

        /// <summary>
        /// 分成几列
        /// </summary>
        public int ColumnCount { get; set; } = 2;

        /// <summary>
        /// The default height to apply to all items
        /// </summary>
        public Size AspectRatio { get; set; } = new Size(1, 1);

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

        protected override double MeasureItems(double top, Rect inRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells)
        {
            double itemsHeight = 0;
            var itemWidth = inRect.Width / ColumnCount;
            var itemHeight = itemWidth * AspectRatio.Height / AspectRatio.Width;
            //Console.WriteLine($"itemWidth:{itemWidth} itemHeight:{itemHeight}");
            int numberOfSections = CollectionView.NumberOfSections();
            for (int section = 0; section < numberOfSections; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);

                for (int row = 0; row < numberOfRows; row = row + ColumnCount)
                {
                    var rowMaybeTop = itemsHeight + top;
                    var rowMaybeHeight = itemHeight;
                    var rowMaybeBottom = rowMaybeTop + rowMaybeHeight;
                    for (var currentRow = row; currentRow < numberOfRows && currentRow < row + ColumnCount; currentRow++)
                    {
                        NSIndexPath indexPath = NSIndexPath.FromRowSection(currentRow, section);

                        //如果在可见区域, 就详细测量
                        if ((rowMaybeTop >= inRect.Top && rowMaybeTop <= inRect.Bottom)
                           || (rowMaybeBottom >= inRect.Top && rowMaybeBottom <= inRect.Bottom)
                           || (rowMaybeTop <= inRect.Top && rowMaybeBottom >= inRect.Bottom))
                        {
                            //获取Cell, 优先获取之前已经被显示的, 这里假定已显示的数据没有变化
                            MAUICollectionViewViewHolder cell = availableCells.ContainsKey(indexPath) ? availableCells[indexPath] : CollectionView.Source.cellForRowAtIndexPath(CollectionView, indexPath, inRect.Width, false);

                            if (cell != null)
                            {
                                //将Cell添加到正在显示的Cell字典
                                CollectionView.PreparedItems.Add(indexPath, cell);
                                //CollectionView.PreparedItems[indexPath] = cell;
                                if (availableCells.ContainsKey(indexPath)) availableCells.Remove(indexPath);
                                //Cell是否是正在被选择的
                                cell.Highlighted = CollectionView._highlightedRow == null ? false : CollectionView._highlightedRow.IsEqual(indexPath);
                                cell.Selected = CollectionView._selectedRow == null ? false : CollectionView._selectedRow.IsEqual(indexPath);

                                //添加到ScrollView, 必须先添加才有测量值
                                if (!CollectionView.ContentView.Children.Contains(cell.ContentView))
                                    CollectionView.AddSubview(cell.ContentView);
                                //测量高度
                                cell.ContentView.WidthRequest = itemWidth;
                                cell.ContentView.HeightRequest = itemHeight;
                                var measureSize = CollectionView.MeasureChild(cell.ContentView, itemWidth, itemHeight).Request;
                                var bounds = new Rect(itemWidth * (currentRow - row), itemsHeight + top, measureSize.Width, measureSize.Height);

                                if (cell.Operation == (int)OperateItem.OperateType.move && isStartAnimate)
                                {
                                    cell.OldBoundsInLayout = cell.BoundsInLayout;
                                    cell.BoundsInLayout = bounds;
                                }
                                else
                                {
                                    cell.BoundsInLayout = bounds;
                                }
                            }
                        }
                        else//如果不可见
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
                        }
                    }
                    itemsHeight += rowMaybeHeight;
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
                tempBottom = totalHeight + CollectionView.HeaderView.ContentView.DesiredSize.Height;
                if (totalHeight <= point.Y && tempBottom >= point.Y)
                {
                    return null;
                }
                totalHeight = tempBottom;
            }

            var itemWidth = CollectionView.ContentSize.Width / ColumnCount;
            var itemHeight = itemWidth * AspectRatio.Height / AspectRatio.Width;
            //Console.WriteLine($"itemWidth:{itemWidth} itemHeight:{itemHeight}");
            int numberOfSections = CollectionView.NumberOfSections();
            for (int section = 0; section < numberOfSections; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);

                for (int row = 0; row < numberOfRows; row = row + ColumnCount)
                {
                    var rowMaybeTop = totalHeight;
                    var rowMaybeHeight = itemHeight;
                    var rowMaybeBottom = totalHeight + rowMaybeHeight;
                    for (var currentRow = row; currentRow < numberOfRows && currentRow < row + ColumnCount; currentRow++)
                    {
                        if (point.X > itemWidth * (currentRow - row) && point.X < itemWidth * (currentRow - row + 1) &&
                            point.Y > rowMaybeTop && point.Y < rowMaybeBottom)
                        {
                            return NSIndexPath.FromRowSection(currentRow, section);
                        }
                        else
                        {
                        };
                    }
                    totalHeight = rowMaybeBottom;
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

            var itemWidth = CollectionView.ContentSize.Width / ColumnCount;
            var itemHeight = itemWidth * AspectRatio.Height / AspectRatio.Width;
            //Console.WriteLine($"itemWidth:{itemWidth} itemHeight:{itemHeight}");
            int numberOfSections = CollectionView.NumberOfSections();
            for (int section = 0; section < numberOfSections; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);

                for (int row = 0; row < numberOfRows; row = row + ColumnCount)
                {
                    var rowMaybeTop = totalHeight;
                    var rowMaybeHeight = itemHeight;
                    var rowMaybeBottom = totalHeight + rowMaybeHeight;
                    for (var currentRow = row; currentRow < numberOfRows && currentRow < row + ColumnCount; currentRow++)
                    {
                        var indexPath = NSIndexPath.FromRowSection(currentRow, section);
                        if (indexPath.Section == indexPathTarget.Section && indexPath.Row == indexPathTarget.Row)
                        {
                            return Rect.FromLTRB(itemWidth * (currentRow - row), rowMaybeTop, itemWidth * (currentRow - row + 1), rowMaybeBottom);
                        }
                        else
                        {
                        };
                    }
                    totalHeight = rowMaybeBottom;
                }
            }
            return Rect.Zero;
        }
    }
}
