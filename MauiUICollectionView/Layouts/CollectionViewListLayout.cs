namespace MauiUICollectionView.Layouts
{
    public partial class CollectionViewListLayout : CollectionViewLayout
    {
        public CollectionViewListLayout(MAUICollectionView collectionView) : base(collectionView)
        {
        }

        /// <summary>
        /// Image测量可能首先获得的高度为0, 造成要显示Item数目过多. 这个值尽量接近最终高度.
        /// </summary>
        public double EstimatedRowHeight = 100;

        /// <summary>
        /// 为避免当数据量很大时需要循环很多次去计算总高度, 这里按每<see cref="ItemsCountInRegion">个Item为一组缓存一个高度供下次使用.
        /// 缓存的策略是当需要MeasureSelf测量的Item出现在该区域时, 该区域内的缓存高度需要重新测量
        /// </summary>
        public double[] ItemsHeightCache;
        public double AllItemCount = 0;
        protected override double MeasureItems(double top, Rect inRect, Rect visiableRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells)
        {
            var numberOfSections = CollectionView.NumberOfSections();
            var allItemCount = 0;
            for (int section = 0; section < numberOfSections; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                allItemCount += numberOfRows;
            }

            if (AllItemCount != allItemCount)//代表数据更新了
            {
                ItemsHeightCache = new double[allItemCount % ItemsCountInRegion > 0 ? allItemCount / ItemsCountInRegion + 1 : allItemCount / ItemsCountInRegion];
                AllItemCount = allItemCount;
            }

            double itemsHeight = 0;

            /* 
             * Items
             */
            var targetRegionIndex = 0;
            double recordRegionHeight = 0;
            for (var regionIndex = 0; regionIndex < ItemsHeightCache.Length; regionIndex++)
            {
                if (ItemsHeightCache[0] == 0)//没有缓存时, 统计是无效的
                    break;
                if (recordRegionHeight + ItemsHeightCache[regionIndex] >= CollectionView.ScrollY)
                {
                    targetRegionIndex = regionIndex;
                    break;
                }
                else
                {
                    recordRegionHeight += ItemsHeightCache[regionIndex];
                }
            }

            for (var regionIndex = 0; regionIndex < ItemsHeightCache.Length; regionIndex++)
            {
                if (regionIndex == targetRegionIndex)
                {
                    //目标区域前后都仔细测量
                    if (regionIndex - 1 >= 0)
                    {
                        var beforeTargetRegionIndex = regionIndex - 1;
                        itemsHeight -= ItemsHeightCache[beforeTargetRegionIndex];//减去旧的
                        var heightOfBeforeTargetRegion = CalculateHeightForVisiableRegion(beforeTargetRegionIndex, itemsHeight + top, inRect, visiableRect, availableCells);
                        ItemsHeightCache[beforeTargetRegionIndex] = heightOfBeforeTargetRegion;
                        itemsHeight += ItemsHeightCache[beforeTargetRegionIndex];
                    }
                    var targetRegionHeight = CalculateHeightForVisiableRegion(regionIndex, itemsHeight + top, inRect, visiableRect, availableCells);
                    ItemsHeightCache[regionIndex] = targetRegionHeight;
                    itemsHeight += ItemsHeightCache[regionIndex];
                    if (regionIndex + 1 < ItemsHeightCache.Length)
                    {
                        var afterTargetRegionIndex = regionIndex + 1;
                        var heightOfAfterTargetRegion = CalculateHeightForVisiableRegion(afterTargetRegionIndex, itemsHeight + top, inRect, visiableRect, availableCells);
                        ItemsHeightCache[afterTargetRegionIndex] = heightOfAfterTargetRegion;
                        itemsHeight += ItemsHeightCache[afterTargetRegionIndex];
                        regionIndex++;
                    }
                }
                else
                {
                    if (ItemsHeightCache[regionIndex] == 0)
                    {
                        ItemsHeightCache[regionIndex] = CalculateHeightForInvisiableRegion(regionIndex);
                    }
                    itemsHeight += ItemsHeightCache[regionIndex];
                }
            }

            return itemsHeight;
        }

        NSIndexPath GetStartIndexPathOfRegion(int index)
        {
            int startNeedMeasureItemIndex = index * ItemsCountInRegion;
            int haveRecordItemsCountWhenFindStart = 0;
            var sectionCount = CollectionView.NumberOfSections();
            NSIndexPath startIndexPathOfRegion = null;

            for (var section = 0; section < sectionCount; section++)
            {
                int rows = CollectionView.NumberOfItemsInSection(section);
                if (haveRecordItemsCountWhenFindStart + rows >= startNeedMeasureItemIndex)
                {
                    var targetSectionUsedRowsCount = startNeedMeasureItemIndex - haveRecordItemsCountWhenFindStart;
                    haveRecordItemsCountWhenFindStart = haveRecordItemsCountWhenFindStart + targetSectionUsedRowsCount;
                    startIndexPathOfRegion = NSIndexPath.FromRowSection(targetSectionUsedRowsCount, section);
                    break;
                }
                else
                {
                    haveRecordItemsCountWhenFindStart = haveRecordItemsCountWhenFindStart + rows;
                }
            }
            return startIndexPathOfRegion;
        }

        double CalculateHeightForInvisiableRegion(int index)
        {
            var startIndexPath = GetStartIndexPathOfRegion(index);
            double itemsHeight = 0;
            int numberOfSections = CollectionView.NumberOfSections();
            var haveMeasuredItemCount = 1;
            for (int section = startIndexPath.Section; section < numberOfSections && haveMeasuredItemCount <= ItemsCountInRegion; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                int row = 0;
                if (section == startIndexPath.Section)
                    row = startIndexPath.Row;
                for (; row < numberOfRows && haveMeasuredItemCount <= ItemsCountInRegion; row++)
                {
                    haveMeasuredItemCount++;

                    NSIndexPath indexPath = NSIndexPath.FromRowSection(row, section);
                    var reuseIdentifier = CollectionView.Source.ReuseIdForItem(CollectionView, indexPath);
                    //尝试用之前测量的值或者预设值估计底部在哪
                    var rowMaybeHeight = GetRowMaybeHeight(indexPath, reuseIdentifier);
                    itemsHeight += rowMaybeHeight;
                }
            }

            return itemsHeight;
        }

        double GetRowMaybeHeight(NSIndexPath indexPath, string reuseIdentifier)
        {
            var rowHeightWant = CollectionView.Source.HeightForItem(CollectionView, indexPath);

            return rowHeightWant == MAUICollectionViewViewHolder.MeasureSelf ? MeasuredSelfHeightCacheForReuse.ContainsKey(reuseIdentifier) ? MeasuredSelfHeightCacheForReuse[reuseIdentifier] : EstimatedRowHeight : rowHeightWant;
        }

        const int ItemsCountInRegion = 100;
        double CalculateHeightForVisiableRegion(int index, double top, Rect inRect, Rect visiableRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells)
        {
            var startIndexPath = GetStartIndexPathOfRegion(index);
            double itemsHeight = 0;
            int numberOfSections = CollectionView.NumberOfSections();
            var haveMeasuredItemCount = 1;
            for (int section = startIndexPath.Section; section < numberOfSections && haveMeasuredItemCount <= ItemsCountInRegion; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                int row = 0;
                if (section == startIndexPath.Section)
                    row = startIndexPath.Row;
                for (; row < numberOfRows && haveMeasuredItemCount <= ItemsCountInRegion; row++)
                {
                    haveMeasuredItemCount++;

                    NSIndexPath indexPath = NSIndexPath.FromRowSection(row, section);
                    var reuseIdentifier = CollectionView.Source.ReuseIdForItem(CollectionView, indexPath);
                    //尝试用之前测量的值或者预设值估计底部在哪
                    var rowMaybeTop = itemsHeight + top;
                    var rowMaybeHeight = GetRowMaybeHeight(indexPath, reuseIdentifier);
                    var rowMaybeBottom = rowMaybeTop + rowMaybeHeight;
                    var itemRect = new Rect(0, rowMaybeTop, inRect.Width, rowMaybeHeight);
                    //如果在布局区域, 就详细测量
                    if (itemRect.IntersectsWith(inRect))//Item与布局区域相交
                    {
                        //获取Cell, 优先获取之前已经被显示的, 这里假定已显示的数据没有变化
                        MAUICollectionViewViewHolder cell = null;
                        if (availableCells.ContainsKey(indexPath))
                        {
                            cell = availableCells[indexPath];
                            availableCells.Remove(indexPath);
                        }
                        if (cell != null)
                        {
                            //can update partial control in ViewHolder
                            cell = CollectionView.Source.ViewHolderForItem(CollectionView, indexPath, cell, inRect.Width);
                        }
                        else
                        {
                            //get ViewHolder that maybe be new or reuse from recycle cache
                            cell = CollectionView.Source.ViewHolderForItem(CollectionView, indexPath, cell, inRect.Width);
                            //cell.Scale = 1;
                            ///cell.TranslationX = 0;
                            //cell.TranslationY = 0;
                        }
                        if (cell != null)
                        {
                            //将Cell添加到正在显示的Cell字典
                            CollectionView.PreparedItems.Add(indexPath, cell);

                            //添加到ScrollView, 必须先添加才有测量值
                            if (!CollectionView.ContentView.Children.Contains(cell))
                                CollectionView.AddSubview(cell);
                            //测量高度
                            Size measureSize;
                            cell.WidthRequest = inRect.Width;
                            cell.HeightRequest = -1;
                            var rowHeightWant = CollectionView.Source.HeightForItem(CollectionView, indexPath);
                            if (rowHeightWant != MAUICollectionViewViewHolder.MeasureSelf)//fixed value
                            {
                                cell.HeightRequest = rowHeightWant;
                                measureSize = cell.MeasureSelf(inRect.Width, rowHeightWant).Request;
                            }
                            else//need measure
                            {
                                measureSize = cell.MeasureSelf(inRect.Width, double.PositiveInfinity).Request;
                                if (measureSize.Height != 0)
                                {
                                    //store height for same identify
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

                            var finalHeight = measureSize.Height;//GetRowMaybeHeight(indexPath, reuseIdentifier);
                            var bounds = new Rect(0, itemsHeight + top, measureSize.Width != 0 ? measureSize.Width : inRect.Width, finalHeight);

                            //store bounds,  we will use it when arrange
                            if (cell.Operation == (int)OperateItem.OperateType.Move && 
                                IsOperating && 
                                bounds != cell.BoundsInLayout)//move + anim + diff bounds
                            {
                                cell.OldBoundsInLayout = cell.BoundsInLayout;//move operate need old position to make animation
                                cell.BoundsInLayout = bounds;
                            }
                            else
                            {
                                cell.BoundsInLayout = bounds;
                            }

                            //here have a chance to change appearance of this item
                            CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, indexPath, cell);

                            cell.IndexPath = indexPath;

                            //record visible item
                            if (cell.BoundsInLayout.IntersectsWith(visiableRect))
                            {
                                VisibleIndexPath.Add(indexPath);
                            }

                            itemsHeight += cell.BoundsInLayout.Height;
                        }
                        else
                        {
                            throw new NotImplementedException($"Get ViewHolder is null of {indexPath} from {nameof(MAUICollectionViewSource.ViewHolderForItem)}.");
                        }
                    }
                    else//如果不在布局区域内
                    {
                        if (availableCells.ContainsKey(indexPath))//we want store bounds info for invisible item, because maybe we need it to make animation
                        {
                            var cell = availableCells[indexPath];

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
                        itemsHeight += rowMaybeHeight;
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

            var numberOfSections = CollectionView.NumberOfSections();
            for (int section = 0; section < numberOfSections; section++)
            {
                int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                for (int row = 0; row < numberOfRows; row++)
                {
                    NSIndexPath indexPath = NSIndexPath.FromRowSection(row, section);
                    double rowMaybeHeight = 0;
                    if (CollectionView.PreparedItems.ContainsKey(indexPath))
                    {
                        rowMaybeHeight = CollectionView.PreparedItems[indexPath].BoundsInLayout.Height;
                    }
                    else
                    {
                        var reuseIdentifier = CollectionView.Source.ReuseIdForItem(CollectionView, indexPath);
                        var rowHeightWant = CollectionView.Source.HeightForItem(CollectionView, indexPath);
                        rowMaybeHeight = rowHeightWant == MAUICollectionViewViewHolder.MeasureSelf ? MeasuredSelfHeightCacheForReuse.ContainsKey(reuseIdentifier) ? MeasuredSelfHeightCacheForReuse[reuseIdentifier] : EstimatedRowHeight : rowHeightWant;
                    }
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

        public override Rect RectForItem(NSIndexPath indexPathTarget)
        {
            double totalHeight = 0;
            double tempBottom = 0;
            if (CollectionView.HeaderView != null)
            {
                tempBottom = totalHeight + CollectionView.HeaderView.DesiredSize.Height;
                totalHeight = tempBottom;
            }

            var numberOfSections = CollectionView.NumberOfSections();
            for (int section = 0; section < numberOfSections; section++)
            {
                int numberOfRows = section == indexPathTarget.Section? indexPathTarget.Row + 1 : CollectionView.NumberOfItemsInSection(section);
                for (int row = 0; row < numberOfRows; row++)
                {
                    NSIndexPath indexPath = NSIndexPath.FromRowSection(row, section);
                    double rowMaybeHeight = 0;
                    if (CollectionView.PreparedItems.ContainsKey(indexPath))
                    {
                        rowMaybeHeight = CollectionView.PreparedItems[indexPath].BoundsInLayout.Height;
                    }
                    else
                    {
                        var reuseIdentifier = CollectionView.Source.ReuseIdForItem(CollectionView, indexPath);
                        var rowHeightWant = CollectionView.Source.HeightForItem(CollectionView, indexPath);
                        rowMaybeHeight = rowHeightWant == MAUICollectionViewViewHolder.MeasureSelf ? MeasuredSelfHeightCacheForReuse.ContainsKey(reuseIdentifier) ? MeasuredSelfHeightCacheForReuse[reuseIdentifier] : EstimatedRowHeight : rowHeightWant;
                    }
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

        public override double HeightForItems(NSIndexPath indexPath, int count)
        {
            var isBeforePreparedItems = false;
            if (indexPath.Compare(CollectionView.PreparedItems.ToList().FirstOrDefault().Key) < 0)
                isBeforePreparedItems = true;
            double itemsHeight = 0;
            for (var index = 0; index < count; index++)
            {
                var needMeasureIndexPath = NSIndexPath.FromRowSection(indexPath.Row + index, indexPath.Section);
                itemsHeight += GetItemCurrentHeight(needMeasureIndexPath, !isBeforePreparedItems);
            }

            return itemsHeight;

            double GetItemCurrentHeight(NSIndexPath indexPath, bool measureSelf)
            {
                if (CollectionView.PreparedItems.ContainsKey(indexPath))
                {
                    return CollectionView.PreparedItems[indexPath].BoundsInLayout.Height;
                }
                else
                {
                    var reuseIdentifier = CollectionView.Source.ReuseIdForItem(CollectionView, indexPath);

                    var rowHeightWant = CollectionView.Source.HeightForItem(CollectionView, indexPath);

                    var rowMaybeHeight = GetRowMaybeHeight(indexPath, reuseIdentifier);

                    if (measureSelf)
                    {
                        var w = CollectionView.PreparedItems[VisibleIndexPath[0]].BoundsInLayout.Width;
                        MAUICollectionViewViewHolder cell = CollectionView.Source.ViewHolderForItem(CollectionView, indexPath, null, w);

                        if (cell != null)
                        {
                            //添加到ScrollView, 必须先添加才有测量值
                            if (!CollectionView.ContentView.Children.Contains(cell))
                                CollectionView.AddSubview(cell);
                            //测量高度
                            Size measureSize;
                            cell.WidthRequest = w;
                            cell.HeightRequest = -1;
                            if (rowHeightWant != MAUICollectionViewViewHolder.MeasureSelf)//固定高度
                            {
                                cell.HeightRequest = rowHeightWant;
                                measureSize = cell.MeasureSelf(w, rowHeightWant).Request;
                            }
                            else
                            {
                                measureSize = cell.MeasureSelf(w, double.PositiveInfinity).Request;
                            }

                            var finalHeight = (rowHeightWant == MAUICollectionViewViewHolder.MeasureSelf ? (measureSize.Height != 0 ? measureSize.Height : MeasuredSelfHeightCacheForReuse.ContainsKey(cell.ReuseIdentifier) ? MeasuredSelfHeightCacheForReuse[cell.ReuseIdentifier] : EstimatedRowHeight) : rowHeightWant);
                            rowMaybeHeight = finalHeight;
                            CollectionView.RecycleViewHolder(cell);
                        }
                    }
                    return rowMaybeHeight;
                }
            }
        }
    }
}
