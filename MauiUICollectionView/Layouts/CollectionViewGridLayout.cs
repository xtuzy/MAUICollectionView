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

        protected override double MeasureItems(double top, Rect inRect, Rect visiableRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availablePreparedItems)
        {
            double measureItem(NSIndexPath indexPath, Rect itemRect, bool unknowItemHeight)
            {
                //如果在可见区域, 就详细测量
                if (itemRect.IntersectsWith(inRect))
                {
                    //获取Cell, 优先获取之前已经被显示的, 这里假定已显示的数据没有变化
                    MAUICollectionViewViewHolder cell = null;
                    if (availablePreparedItems.ContainsKey(indexPath))
                    {
                        cell = availablePreparedItems[indexPath];
                        availablePreparedItems.Remove(indexPath);
                        if(CollectionView.IsScrolling &&!IsOperating)
                        {
                            //return cell.BoundsInLayout.Height;
                        }
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
                        cell.WidthRequest = itemRect.Width;
                        cell.HeightRequest = unknowItemHeight ? -1 : itemRect.Height;
                        var measureSize = cell.MeasureSelf(itemRect.Width, unknowItemHeight ? double.PositiveInfinity : itemRect.Height).Request;
                        var bounds = new Rect(itemRect.X, itemRect.Y, measureSize.Width, measureSize.Height);

                        if (cell.Operation == (int)OperateItem.OperateType.Move && // move
                            IsOperating && // anim
                            bounds != cell.BoundsInLayout) // diff bounds
                        {
                            cell.OldBoundsInLayout = cell.BoundsInLayout;
                            cell.BoundsInLayout = bounds;
                        }
                        else
                        {
                            if (cell.Operation == (int)OperateItem.OperateType.Move &&
                                IsOperating)
                                cell.OldBoundsInLayout = Rect.Zero;
                            cell.BoundsInLayout = bounds;
                        }

                        //here have a chance to change appearance of this item
                        CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, indexPath, cell);

                        cell.IndexPath = indexPath;

                        return cell.BoundsInLayout.Height;
                    }
                    else
                    {
                        throw new NotImplementedException($"Get ViewHolder is null of {indexPath} from {nameof(MAUICollectionViewSource.ViewHolderForItem)}.");
                    }
                }
                else// if don't in prepared rect
                {
                    if (availablePreparedItems.ContainsKey(indexPath))//we want store bounds info for invisible item, because maybe we need it to make animation
                    {
                        var cell = availablePreparedItems[indexPath];

                        if (cell.Operation == (int)OperateItem.OperateType.Move
                            && IsOperating
                            && itemRect != cell.BoundsInLayout)//move + anim + diff bounds
                        {
                            cell.OldBoundsInLayout = cell.BoundsInLayout;//move operate need old position to make animation
                            cell.BoundsInLayout = itemRect;
                        }
                        else
                        {
                            cell.BoundsInLayout = itemRect;
                        }
                    }

                    return itemRect.Height;
                }
            }

            double itemsHeight = 0;
            var itemWidth = inRect.Width / ColumnCount;
            var itemHeight = itemWidth * AspectRatio.Height / AspectRatio.Width;
            int numberOfSections = CollectionView.NumberOfSections();

            for (int section = 0; section < numberOfSections; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                NSIndexPath firstItem = NSIndexPath.FromRowSection(0, section);
                NSIndexPath lastItem = NSIndexPath.FromRowSection(numberOfRows - 1, section);

                var firstIsSectionItem = CollectionView.Source.IsSectionItem?.Invoke(CollectionView, firstItem);
                var lastIsSectionItem = CollectionView.Source.IsSectionItem?.Invoke(CollectionView, lastItem);

                int dataItemCount = numberOfRows;
                if (firstIsSectionItem == true) dataItemCount--;
                if (lastIsSectionItem == true) dataItemCount--;

                double firstItemHeight = 0;
                bool firstItemIsInPrepared = false;
                if (firstIsSectionItem == true)
                {
                    var id = CollectionView.Source.ReuseIdForItem(CollectionView, firstItem);
                    var wantHeight = CollectionView.Source.HeightForItem(CollectionView, firstItem);
                    firstItemHeight = wantHeight == MAUICollectionViewViewHolder.AutoSize ? (MeasuredSelfHeightCacheForReuse.ContainsKey(id) ? MeasuredSelfHeightCacheForReuse[id] : 0) : wantHeight;
                    var firstItemRect = new Rect(0, itemsHeight + top, inRect.Width, firstItemHeight);
                    firstItemIsInPrepared = firstItemRect.IntersectsWith(inRect);
                    if (firstItemIsInPrepared)
                    {
                        firstItemHeight = measureItem(firstItem, firstItemRect, firstItemHeight == MAUICollectionViewViewHolder.AutoSize);
                        if (MeasuredSelfHeightCacheForReuse.ContainsKey(id))
                            MeasuredSelfHeightCacheForReuse[id] = firstItemHeight;
                        else
                            MeasuredSelfHeightCacheForReuse.Add(id, firstItemHeight);
                    }
                }

                var sectionAllDataItemsHeight = itemHeight * (dataItemCount / ColumnCount) + itemHeight * (dataItemCount % ColumnCount > 0 ? 1 : 0);

                if (firstItemIsInPrepared)//first item in, we only need find next
                {
                    var rowCount = dataItemCount % ColumnCount == 0 ? dataItemCount / ColumnCount : dataItemCount / ColumnCount + 1;
                    for (var itemRowIndex = 0; itemRowIndex < rowCount; itemRowIndex++)
                    {
                        var rowTop = itemsHeight + firstItemHeight + itemRowIndex * itemHeight + top;
                        var rowHeight = itemHeight;
                        for (var currentColumIndex = 1; currentColumIndex <= ColumnCount; currentColumIndex++)
                        {
                            var itemIndex = itemRowIndex * ColumnCount + currentColumIndex;
                            if (itemIndex > dataItemCount)
                            {
                                break;
                            }
                            NSIndexPath itemIndexPath = NSIndexPath.FromRowSection(itemIndex, section);
                            var itemRect = new Rect(itemWidth * (currentColumIndex - 1), rowTop, itemWidth, rowHeight);
                            if (itemRect.IntersectsWith(inRect))
                            {
                                measureItem(itemIndexPath, itemRect, false);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                else//first not, we try know allDataItem in rect
                {
                    var allDataItemRect = new Rect(0, itemsHeight + firstItemHeight + top, inRect.Width, sectionAllDataItemsHeight);
                    if (allDataItemRect.IntersectsWith(inRect))
                    {
                        var rowIndex = (int)((inRect.Top - allDataItemRect.Top) / itemHeight);
                        var rowCount = dataItemCount % ColumnCount == 0 ? dataItemCount / ColumnCount : dataItemCount / ColumnCount + 1;
                        for (var itemRowIndex = rowIndex; itemRowIndex < rowCount; itemRowIndex++)
                        {
                            var rowTop = top + itemsHeight + firstItemHeight + itemRowIndex * itemHeight;
                            var rowHeight = itemHeight;
                            for (var currentColumIndex = 1; currentColumIndex <= ColumnCount; currentColumIndex++)
                            {
                                var itemIndex = itemRowIndex * ColumnCount + currentColumIndex;
                                if (itemIndex > dataItemCount)
                                {
                                    break;
                                }
                                NSIndexPath itemIndexPath = NSIndexPath.FromRowSection(itemIndex, section);
                                var itemRect = new Rect(itemWidth * (currentColumIndex - 1), rowTop, itemWidth, rowHeight);
                                if (itemRect.IntersectsWith(inRect))
                                {
                                    measureItem(itemIndexPath, itemRect, false);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {

                    }

                }

                var lastItemHeight = lastIsSectionItem == true ? CollectionView.Source.HeightForItem(CollectionView, lastItem) : 0;

                itemsHeight = itemsHeight + firstItemHeight + sectionAllDataItemsHeight + lastItemHeight;
            }

            //if viewHolder is invisible, we calculate its position for animation
            foreach(var viewHolder in availablePreparedItems)
            {
                var rect = RectForItem(viewHolder.Key);
                measureItem(viewHolder.Key, rect, false);
            }

            return itemsHeight;
        }

        public override NSIndexPath ItemAtPoint(Point point, bool baseOnContent = true)
        {
            var visibleIndexPath = base.ItemAtPoint(point, baseOnContent);
            if (visibleIndexPath != null)
                return visibleIndexPath;

            if (!baseOnContent)
            {
                var contentOffset = CollectionView.ScrollY;
                point.Y = point.Y + contentOffset;//convert to base on content
            }

            double top = 0;
            if (CollectionView.HeaderView != null)
            {
                top = CollectionView.HeaderView.DesiredSize.Height;
            }

            double itemsHeight = 0;
            var contentWidth = CollectionView.ContentSize.Width;
            var itemWidth = contentWidth / ColumnCount;
            var itemHeight = itemWidth * AspectRatio.Height / AspectRatio.Width;
            int numberOfSections = CollectionView.NumberOfSections();

            for (int section = 0; section < numberOfSections; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                NSIndexPath firstItem = NSIndexPath.FromRowSection(0, section);
                NSIndexPath lastItem = NSIndexPath.FromRowSection(numberOfRows - 1, section);

                var firstIsSectionItem = CollectionView.Source.IsSectionItem?.Invoke(CollectionView, firstItem);
                var lastIsSectionItem = CollectionView.Source.IsSectionItem?.Invoke(CollectionView, lastItem);

                int dataItemCount = numberOfRows;
                if (firstIsSectionItem == true) dataItemCount--;
                if (lastIsSectionItem == true) dataItemCount--;

                double firstItemHeight = 0;
                if (firstIsSectionItem == true)
                {
                    var id = CollectionView.Source.ReuseIdForItem(CollectionView, firstItem);
                    var wantHeight = CollectionView.Source.HeightForItem(CollectionView, firstItem);
                    firstItemHeight = CollectionView.PreparedItems.ContainsKey(firstItem) ? CollectionView.PreparedItems[firstItem].BoundsInLayout.Height : wantHeight == MAUICollectionViewViewHolder.AutoSize ? (MeasuredSelfHeightCacheForReuse.ContainsKey(id) ? MeasuredSelfHeightCacheForReuse[id] : 0) : wantHeight;
                    var firstItemRect = new Rect(0, itemsHeight + top, contentWidth, firstItemHeight);
                    var firstItemIsTarget = firstItemRect.Contains(point);
                    if (firstItemIsTarget)
                    {
                        return firstItem;
                    }
                }

                var sectionAllDataItemsHeight = itemHeight * (dataItemCount / ColumnCount) + itemHeight * (dataItemCount % ColumnCount > 0 ? 1 : 0);

                var allDataItemRect = new Rect(0, itemsHeight + firstItemHeight + top, contentWidth, sectionAllDataItemsHeight);
                if (allDataItemRect.Contains(point))
                {
                    var rowIndex = (int)((point.Y - allDataItemRect.Top) / itemHeight);
                    var rowCount = dataItemCount % ColumnCount == 0 ? dataItemCount / ColumnCount : dataItemCount / ColumnCount + 1;
                    for (var itemRowIndex = rowIndex; itemRowIndex < rowCount; itemRowIndex++)
                    {
                        var rowTop = top + itemsHeight + firstItemHeight + itemRowIndex * itemHeight;
                        var rowHeight = itemHeight;
                        for (var currentColumIndex = 1; currentColumIndex <= ColumnCount; currentColumIndex++)
                        {
                            var itemIndex = itemRowIndex * ColumnCount + currentColumIndex;
                            if (itemIndex > dataItemCount)
                            {
                                break;
                            }
                            NSIndexPath itemIndexPath = NSIndexPath.FromRowSection(itemIndex, section);
                            var itemRect = new Rect(itemWidth * (currentColumIndex - 1), rowTop, itemWidth, rowHeight);
                            if (itemRect.Contains(point))
                            {
                                return itemIndexPath;
                            }
                        }
                    }
                }

                double lastItemHeight = 0;
                if (lastIsSectionItem == true)
                {
                    var id = CollectionView.Source.ReuseIdForItem(CollectionView, lastItem);
                    var wantHeight = CollectionView.Source.HeightForItem(CollectionView, lastItem);
                    lastItemHeight = CollectionView.PreparedItems.ContainsKey(lastItem) ? CollectionView.PreparedItems[lastItem].BoundsInLayout.Height : wantHeight == MAUICollectionViewViewHolder.AutoSize ? (MeasuredSelfHeightCacheForReuse.ContainsKey(id) ? MeasuredSelfHeightCacheForReuse[id] : 0) : wantHeight;
                    var lastItemRect = new Rect(0, top + itemsHeight + firstItemHeight + sectionAllDataItemsHeight, contentWidth, lastItemHeight);
                    var lastItemIsTarget = lastItemRect.Contains(point);
                    if (lastItemIsTarget)
                    {
                        return firstItem;
                    }
                }

                itemsHeight = itemsHeight + firstItemHeight + sectionAllDataItemsHeight + lastItemHeight;
            }
            return null;
        }

        public override Rect RectForItem(NSIndexPath indexPathTarget)
        {
            double top = 0;
            if (CollectionView.HeaderView != null)
            {
                top = CollectionView.HeaderView.DesiredSize.Height;
            }

            double itemsHeight = 0;
            var contentWidth = CollectionView.ContentSize.Width;
            var itemWidth = contentWidth / ColumnCount;
            var itemHeight = itemWidth * AspectRatio.Height / AspectRatio.Width;
            int numberOfSections = CollectionView.NumberOfSections();

            for (int section = 0; section < numberOfSections; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                NSIndexPath firstItem = NSIndexPath.FromRowSection(0, section);
                NSIndexPath lastItem = NSIndexPath.FromRowSection(numberOfRows - 1, section);

                var firstIsSectionItem = CollectionView.Source.IsSectionItem?.Invoke(CollectionView, firstItem);
                var lastIsSectionItem = CollectionView.Source.IsSectionItem?.Invoke(CollectionView, lastItem);

                int dataItemCount = numberOfRows;
                if (firstIsSectionItem == true) dataItemCount--;
                if (lastIsSectionItem == true) dataItemCount--;

                double firstItemHeight = 0;
                if (firstIsSectionItem == true)
                {

                    var id = CollectionView.Source.ReuseIdForItem(CollectionView, firstItem);
                    var wantHeight = CollectionView.Source.HeightForItem(CollectionView, firstItem);
                    firstItemHeight = CollectionView.PreparedItems.ContainsKey(firstItem) ? CollectionView.PreparedItems[firstItem].BoundsInLayout.Height : wantHeight == MAUICollectionViewViewHolder.AutoSize ? (MeasuredSelfHeightCacheForReuse.ContainsKey(id) ? MeasuredSelfHeightCacheForReuse[id] : 0) : wantHeight;
                    if (firstItem.Equals(indexPathTarget))
                    {
                        return new Rect(0, itemsHeight + top, contentWidth, firstItemHeight);
                    }
                }

                var sectionAllDataItemsHeight = itemHeight * (dataItemCount / ColumnCount) + itemHeight * (dataItemCount % ColumnCount > 0 ? 1 : 0);

                var allDataItemRect = new Rect(0, itemsHeight + firstItemHeight + top, contentWidth, sectionAllDataItemsHeight);
                if (section == indexPathTarget.Section)
                {
                    var itemIndex = indexPathTarget.Row;
                    if (firstIsSectionItem == true)
                        itemIndex--; //if have section, item index start from 1, i want use 0 to calculate line
                    var itemRowIndex = itemIndex % ColumnCount == 0 ? itemIndex / ColumnCount : itemIndex / ColumnCount + 1;
                    var rowTop = top + itemsHeight + firstItemHeight + itemRowIndex * itemHeight;
                    return new Rect((itemIndex % ColumnCount == 0 ? 2 : itemIndex % ColumnCount - 1) * itemWidth, rowTop, itemWidth, itemHeight);
                }

                double lastItemHeight = 0;
                if (lastIsSectionItem == true)
                {
                    var id = CollectionView.Source.ReuseIdForItem(CollectionView, lastItem);
                    var wantHeight = CollectionView.Source.HeightForItem(CollectionView, lastItem);
                    lastItemHeight = CollectionView.PreparedItems.ContainsKey(lastItem) ? CollectionView.PreparedItems[lastItem].BoundsInLayout.Height : wantHeight == MAUICollectionViewViewHolder.AutoSize ? (MeasuredSelfHeightCacheForReuse.ContainsKey(id) ? MeasuredSelfHeightCacheForReuse[id] : 0) : wantHeight;
                    if (lastItem.Equals(indexPathTarget))
                    {
                        return new Rect(0, top + itemsHeight + firstItemHeight + sectionAllDataItemsHeight, contentWidth, lastItemHeight);
                    }
                }

                itemsHeight = itemsHeight + firstItemHeight + sectionAllDataItemsHeight + lastItemHeight;
            }
            return Rect.Zero;
        }

        public override double EstimateHeightForItems(NSIndexPath indexPath, int count)
        {
            var contentWidth = CollectionView.ContentSize.Width;
            var itemWidth = contentWidth / ColumnCount;
            var itemHeight = itemWidth * AspectRatio.Height / AspectRatio.Width;

            var firstItem = NSIndexPath.FromRowSection(0, indexPath.Section);
            var firstIsSectionItem = CollectionView.Source.IsSectionItem?.Invoke(CollectionView, firstItem);

            double totleHeight = 0;
            for (var index = indexPath.Row; count > 0; count--)
            {
                var item = NSIndexPath.FromRowSection(index, indexPath.Section);
                var isSectionItem = CollectionView.Source.IsSectionItem?.Invoke(CollectionView, item);
                if (isSectionItem == true)
                {
                    var id = CollectionView.Source.ReuseIdForItem(CollectionView, item);
                    var wantHeight = CollectionView.Source.HeightForItem(CollectionView, item);
                    var sectionItemHeight = CollectionView.PreparedItems.ContainsKey(item) ? CollectionView.PreparedItems[item].BoundsInLayout.Height : wantHeight == MAUICollectionViewViewHolder.AutoSize ? (MeasuredSelfHeightCacheForReuse.ContainsKey(id) ? MeasuredSelfHeightCacheForReuse[id] : 0) : wantHeight;
                    totleHeight += sectionItemHeight;
                }
                else
                {
                    if (firstIsSectionItem == true)
                    {
                        if (index % ColumnCount == 1) //index start from 1
                        {
                            totleHeight += itemHeight;
                        }
                    }
                    else
                    {
                        if ((index + 1) % ColumnCount == 1) //index start from 0
                        {
                            totleHeight += itemHeight;
                        }
                    }
                }
                index++;
            }
            return totleHeight;
        }
    }
}
