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
        /// Split width.
        /// </summary>
        public int ColumnCount { get; set; } = 2;

        /// <summary>
        /// The default height to apply to all items
        /// </summary>
        public Size AspectRatio { get; set; } = new Size(1, 1);

        protected override double MeasureItems(double top, Rect inRect, Rect visiableRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells)
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
                        var itemRect = new Rect(0, rowMaybeTop, itemWidth, rowMaybeHeight);
                        //如果在可见区域, 就详细测量
                        if (itemRect.IntersectsWith(inRect))
                        {
                            //获取Cell, 优先获取之前已经被显示的, 这里假定已显示的数据没有变化
                            MAUICollectionViewViewHolder cell = null;
                            if (availableCells.ContainsKey(indexPath))
                            {
                                cell = availableCells[indexPath];
                                availableCells.Remove(indexPath);
                            }
                            cell = CollectionView.Source.ViewHolderForItem(CollectionView, indexPath, cell, inRect.Width);

                            if (cell != null)
                            {
                                //将Cell添加到正在显示的Cell字典
                                CollectionView.PreparedItems.Add(indexPath, cell);

                                //添加到ScrollView, 必须先添加才有测量值
                                if (!CollectionView.ContentView.Children.Contains(cell))
                                    CollectionView.AddSubview(cell);
                                //测量高度
                                cell.WidthRequest = itemWidth;
                                cell.HeightRequest = itemHeight;
                                var measureSize = CollectionView.MeasureChild(cell, itemWidth, itemHeight).Request;
                                var bounds = new Rect(itemWidth * (currentRow - row), itemsHeight + top, measureSize.Width, measureSize.Height);

                                //存储可见的
                                if (bounds.IntersectsWith(visiableRect))
                                {
                                    VisibleIndexPath.Add(indexPath);
                                }

                                if (cell.Operation == (int)OperateItem.OperateType.Move && IsOperating && bounds != cell.BoundsInLayout)//move + anim + diff bounds
                                {
                                    cell.OldBoundsInLayout = cell.BoundsInLayout;
                                    cell.BoundsInLayout = bounds;
                                }
                                else
                                {
                                    cell.BoundsInLayout = bounds;
                                }

                                //here have a chance to change appearance of this item
                                CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, indexPath, cell);
                                itemsHeight += cell.BoundsInLayout.Height;
                            }
                            else
                            {
                                throw new NotImplementedException($"Get ViewHolder is null of {indexPath} from {nameof(MAUICollectionViewSource.ViewHolderForItem)}.");
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
                            itemsHeight += rowMaybeHeight;
                        }
                    }
                }
            }
            return itemsHeight;
        }

        public override NSIndexPath ItemAtPoint(Point point, bool baseOnContent = true)
        {
            if (!baseOnContent)
            {
                var contentOffset = CollectionView.ScrollY;
                point.Y = point.Y + contentOffset;//convert to base on content
            }
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

        public override Rect RectForItem(NSIndexPath indexPathTarget)
        {
            double totalHeight = 0;
            double tempBottom = 0;
            if (CollectionView.HeaderView != null)
            {
                tempBottom = totalHeight + CollectionView.HeaderView.DesiredSize.Height;
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

        public override double HeightForItems(NSIndexPath indexPath, int count)
        {
            throw new NotImplementedException();
        }
    }
}
