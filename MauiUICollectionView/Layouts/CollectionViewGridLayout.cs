namespace Yang.MAUICollectionView.Layouts
{
    /// <summary>
    /// GridLayout 意味着分割列表为几列, 高度直接指定比例, 在布局时是确定的值. Source中的设置高度的方法对其无效. 注意Cell不要直接设置Margin, Layout中未加入计算.
    /// </summary>
    public class CollectionViewGridLayout : CollectionViewFlatListLayout
    {
        public CollectionViewGridLayout(MAUICollectionView collectionView) : base(collectionView)
        {
        }

        /// <summary>
        /// Split width.
        /// </summary>
        public int ColumnCount { get; set; } = 2;

        protected override double MeasureItems(double top, Rect inRect, Rect visibleRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availablePreparedItems)
        {
            base.MeasureItems(top, inRect, visibleRect, availablePreparedItems);

            double itemsHeight = 0;
            int numberOfSections = CollectionView.NumberOfSections();

            //根据已知的最后一个Item来估计总高度
            var (lastPreparedItem, lastPreparedItemViewHolder) = CollectionView.PreparedItems.LastOrDefault();
            if (lastPreparedItemViewHolder != null)
                itemsHeight += (lastPreparedItemViewHolder.ItemBounds.Bottom - top);
            var lastItemOfAll = NSIndexPath.FromRowSection(CollectionView.NumberOfItemsInSection(numberOfSections - 1) - 1, numberOfSections - 1);
            if (lastPreparedItem == null)
                lastPreparedItem = NSIndexPath.FromRowSection(0, 0);
            if (lastItemOfAll > lastPreparedItem)
            {
                var itemCount = CollectionView.ItemCountInRange(lastPreparedItem, lastItemOfAll);
                var rowCount = (itemCount / ColumnCount) + ((itemCount % ColumnCount) > 0 ? 1 : 0);
                itemsHeight += rowCount * EstimateAverageHeight();
            }

            return itemsHeight;
        }

        /// <summary>
        /// It is base on 0. Ignore header and footer, if it is header, row return  -1, column return -1. if it is footer, row return -2, column return -1.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        (int row, int column) GetRowAndColumnOfItem(NSIndexPath item)
        {
            int numberOfItemOfSection = CollectionView.NumberOfItemsInSection(item.Section);
            NSIndexPath firstItemOfSection = NSIndexPath.FromRowSection(0, item.Section);
            var firstIsSectionItem = CollectionView.Source.IsSectionItem?.Invoke(CollectionView, firstItemOfSection);

            if (item.Row == 0)
            {
                if (firstIsSectionItem == true)
                    return (-1, -1);
            }

            if (item.Row == numberOfItemOfSection - 1)
            {
                NSIndexPath lastItemOfSection = NSIndexPath.FromRowSection(numberOfItemOfSection - 1, item.Section);
                var lastIsSectionItem = CollectionView.Source.IsSectionItem?.Invoke(CollectionView, lastItemOfSection);
                if (lastIsSectionItem == true)
                    return (-2, -1);
            }

            int itemRowIndex = 0;
            int itemColumnIndex = 0;
            if (firstIsSectionItem == true)// items 从1开始计数
            {
                itemRowIndex = item.Row / ColumnCount;
                var remain = item.Row % ColumnCount;
                itemColumnIndex = remain == 0 ? ColumnCount - 1 : remain - 1;
            }
            else
            {
                itemRowIndex = (item.Row + 1) / ColumnCount; // items从0开始计数
                var remain = (item.Row + 1) % ColumnCount;
                itemColumnIndex = remain == 0 ? ColumnCount - 1 : remain - 1;
            }
            return (itemRowIndex, itemColumnIndex);
        }


        protected override double EstimateAverageHeight()
        {
            double totalH = 0;
            foreach (var bounds in StartBoundsCache)
                totalH += bounds.Height;
            return totalH / StartBoundsCache.Count;
        }

        protected override void Fill(Rect inRect, LayoutInfor baselineInfor, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells, bool isRemeasureAll = true)
        {
            void LayoutFromTopToBottom(LayoutInfor topBaselineInfor)
            {
                if (topBaselineInfor.StartItem == null)
                    return;
                var top = topBaselineInfor.StartBounds.Top;
                // fill item in rect
                var numberOfSections = CollectionView.NumberOfSections();
                NSIndexPath indexPath = topBaselineInfor.StartItem;
                for (int section = topBaselineInfor.StartItem.Section; section < numberOfSections; section++)
                {
                    int numberOfItems = CollectionView.NumberOfItemsInSection(section);
                    int itemIndex = 0;
                    if (section == topBaselineInfor.StartItem.Section)
                        itemIndex = topBaselineInfor.StartItem.Row;
                    for (; itemIndex < numberOfItems; itemIndex++)
                    {
                        indexPath = NSIndexPath.FromRowSection(itemIndex, section);
                        (MAUICollectionViewViewHolder viewHolder, Rect bounds) result;
                        var (row, column) = GetRowAndColumnOfItem(indexPath);
                        if (availableCells.ContainsKey(indexPath) && //If items that were previously measured are still visible.
                            !isRemeasureAll)// If don't need remeasure all, we will use old viewholder.
                        {
                            result = (availableCells[indexPath], availableCells[indexPath].ItemBounds);
                            availableCells.Remove(indexPath);
                        }
                        else
                        {
                            var w = column == -1 ? inRect.Width : inRect.Width / ColumnCount;
                            var left = column == -1 ? 0 : column * w;
                            result = MeasureItem(inRect, w, new Point(left, top), Edge.Top | Edge.Left, indexPath, availableCells);
                        }

                        if (result.viewHolder != null)
                        {
                            //here we can change item's size
                            CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, indexPath, result.viewHolder, Edge.Top);
                            result.bounds = result.viewHolder.ItemBounds;
                            CollectionView.PreparedItems.Add(indexPath, result.viewHolder);
                        }
                        if (result.bounds.Top >= inRect.Bottom)// In order to ensure that the item on the right is also loaded, an out-of-bounds item will be measured.
                            return;
                        if (column == ColumnCount - 1 || //if item is right item,
                            column == -1 || // or header, footer,  
                            ((column != -1 && indexPath.Row == numberOfItems - 1) || ((indexPath.Row == numberOfItems - 1-1) && GetRowAndColumnOfItem(NSIndexPath.FromRowSection(numberOfSections - 1,section)).column == -1)))// or last data item in this section, top of next item will be added.
                            top += result.bounds.Height;
                    }
                }
            }

            void LayoutFromBottomToTop(LayoutInfor bottomBaselineInfor)
            {
                if (bottomBaselineInfor.EndItem == null)
                    return;
                List<KeyValuePair<NSIndexPath, MAUICollectionViewViewHolder>> tempOrderedPreparedItems = new();
                var bottom = bottomBaselineInfor.EndBounds.Bottom;
                // 填充item到矩形高度
                var numberOfSections = CollectionView.NumberOfSections();
                NSIndexPath indexPath = bottomBaselineInfor.EndItem;
                for (int section = bottomBaselineInfor.EndItem.Section; section >= 0; section--)
                {
                    int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                    int itemIndex = numberOfRows - 1;
                    if (section == bottomBaselineInfor.EndItem.Section)
                        itemIndex = bottomBaselineInfor.EndItem.Row;
                    for (; itemIndex >= 0; itemIndex--)
                    {
                        indexPath = NSIndexPath.FromRowSection(itemIndex, section);
                        (MAUICollectionViewViewHolder viewHolder, Rect bounds) result;
                        var (row, column) = GetRowAndColumnOfItem(indexPath);
                        if (availableCells.ContainsKey(indexPath) &&
                            !isRemeasureAll)
                        {
                            result = (availableCells[indexPath], availableCells[indexPath].ItemBounds);
                            availableCells.Remove(indexPath);
                        }
                        else
                        {
                            var w = column == -1 ? inRect.Width : inRect.Width / ColumnCount;
                            var left = column == -1 ? 0 : column * w;
                            result = MeasureItem(inRect, w, new Point(left, bottom), Edge.Bottom | Edge.Left, indexPath, availableCells);
                        }

                        if (result.viewHolder != null)
                        {
                            //here we can change item's size
                            CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, indexPath, result.viewHolder, Edge.Bottom);
                            result.bounds = result.viewHolder.ItemBounds;
                            tempOrderedPreparedItems.Add(new KeyValuePair<NSIndexPath, MAUICollectionViewViewHolder>(indexPath, result.viewHolder));
                        }
                        if (result.bounds.Bottom <= inRect.Top)
                            goto FinishLoop;
                        if (column == 0 || column == -1)
                            bottom -= result.bounds.Height;
                    }
                }

            FinishLoop:
                // 从小到大加入
                for (var index = tempOrderedPreparedItems.Count - 1; index >= 0; index--)
                {
                    var item = tempOrderedPreparedItems[index];
                    CollectionView.PreparedItems.Add(item.Key, item.Value);
                }
            }

            if (baselineInfor.StartItem != null)//from top to bottom
            {
                // if baseline is right item, we try set left item of same row as baseline.
                var startItemRowAndColum = GetRowAndColumnOfItem(baselineInfor.StartItem);
                if (startItemRowAndColum.column > 0)
                {
                    baselineInfor.StartItem = NSIndexPath.FromRowSection(baselineInfor.StartItem.Row - startItemRowAndColum.column, baselineInfor.StartItem.Section);
                }

                //由于有多列且有Header,Footer, baseline的位置判断更复杂, 它不像Vertical List的X总是0
                if (baselineInfor.StartBounds.Top > inRect.Top - 1)// why "-1":when scroll to top, baseline may be right item, we also need measure left item, but sometimes top of baseline not equal to top of inRect and the difference is less than 1.
                {
                    var frontItem = CollectionView.NextItem(baselineInfor.StartItem, -1);
                    if (frontItem != null)
                    {
                        var frontItemColumnIndex = GetRowAndColumnOfItem(frontItem).column;
                        var frontItemX = frontItemColumnIndex == -1 ? 0 : frontItemColumnIndex * (inRect.Width / ColumnCount);
                        var baselineInforForFrontItems = new LayoutInfor()
                        {
                            EndItem = frontItem,
                            EndBounds = new Rect(frontItemX, 0, 0, baselineInfor.StartBounds.Top)
                        };
                        LayoutFromBottomToTop(baselineInforForFrontItems);
                    }
                }
                LayoutFromTopToBottom(baselineInfor);
            }
            else//from bottom to top
            {
                // if baseline is left item, we try set right item of same row as baseline.
                var endItemRowAndColum = GetRowAndColumnOfItem(baselineInfor.EndItem);
                if (endItemRowAndColum.column < ColumnCount -1)
                {
                    baselineInfor.EndItem = NSIndexPath.FromRowSection(baselineInfor.EndItem.Row + (ColumnCount - 1 - endItemRowAndColum.column), baselineInfor.EndItem.Section);
                }

                LayoutFromBottomToTop(baselineInfor);
                if (baselineInfor.EndBounds.Bottom - 1 < inRect.Bottom)
                {
                    var behindItem = CollectionView.NextItem(baselineInfor.EndItem, 1);
                    if (behindItem != null)
                    {
                        var behindItemColumnIndex = GetRowAndColumnOfItem(behindItem).column;
                        var behindItemX = behindItemColumnIndex == -1 ? 0 : behindItemColumnIndex * (inRect.Width / ColumnCount);

                        LayoutFromTopToBottom(new LayoutInfor() { StartItem = behindItem, StartBounds = new Rect(behindItemX, baselineInfor.EndBounds.Bottom, 0, 0) });
                    }
                }
            }
        }

        #region ItemAtPoint

        public override NSIndexPath ItemAtPoint(Point point, bool baseOnContent = true)
        {
            if (!baseOnContent)
            {
                var contentOffset = CollectionView.ScrollY;
                point.Y = point.Y + contentOffset;//convert to base on content
            }

            foreach (var item in CollectionView.PreparedItems)
            {
                if (item.Value.ItemBounds.Contains(point))
                {
                    return item.Key;
                }
            }

            return EstimateItem(point.X, point.Y);
        }

        NSIndexPath EstimateItem(double x, double y)
        {
            var firstItemBounds = StartBoundsCache.First();
            double averageHeight = EstimateAverageHeight();

            var numberOfSections = CollectionView.NumberOfSections();
            double allHeight = 0;
            for (var section = 0; section < numberOfSections; section++)
            {
                double headerH = 0;
                var firstItem = NSIndexPath.FromRowSection(0, section);
                var firstItemRowColumn = GetRowAndColumnOfItem(firstItem);
                if (firstItemRowColumn.row == -1)// is header
                {
                    var wantH = CollectionView.Source.HeightForItem(CollectionView, firstItem);
                    if (wantH == MAUICollectionViewViewHolder.AutoSize)
                        headerH = averageHeight;
                    else
                        headerH = wantH;
                    // in header item
                    if (y - firstItemBounds.Top < allHeight + headerH)
                    {
                        return NSIndexPath.FromRowSection(0, section);
                    }
                }
                var indexCountInSection = CollectionView.NumberOfItemsInSection(section);
                int rowOfLastDataItem = 0;
                var lastItem = NSIndexPath.FromRowSection(indexCountInSection - 1, section);
                var lastItemRowColumn = GetRowAndColumnOfItem(lastItem);
                if (lastItemRowColumn.row == -2)
                {
                    rowOfLastDataItem = GetRowAndColumnOfItem(NSIndexPath.FromRowSection(indexCountInSection - 1, section)).row;
                }
                else
                {
                    rowOfLastDataItem = lastItemRowColumn.row;
                }

                // in data items
                if (y - firstItemBounds.Top < allHeight + headerH + rowOfLastDataItem * averageHeight)
                {
                    var firstItemIndexAtTargetRow = (int)((y - firstItemBounds.Top - allHeight - headerH) / averageHeight) * ColumnCount;
                    var column = (int)(x / (CollectionView.ContentSize.Width / ColumnCount));
                    return NSIndexPath.FromRowSection(firstItemIndexAtTargetRow + column, section);
                }

                double footerH = 0;
                if (lastItemRowColumn.row == -2)
                {
                    var wantH = CollectionView.Source.HeightForItem(CollectionView, lastItem);
                    if (wantH == MAUICollectionViewViewHolder.AutoSize)
                        footerH = averageHeight;
                    else
                        footerH = wantH;
                    // in footer item
                    if (y - firstItemBounds.Top < allHeight + headerH + rowOfLastDataItem * averageHeight + footerH)
                    {
                        return NSIndexPath.FromRowSection(indexCountInSection - 1, section);
                    }
                }

                allHeight += headerH + rowOfLastDataItem * averageHeight + footerH;
            }
            //maybe scrolly is very big
            return NSIndexPath.FromRowSection(CollectionView.NumberOfItemsInSection(numberOfSections - 1), numberOfSections - 1);
        }

        #endregion

        public override Rect RectForItem(NSIndexPath indexPath)
        {
            if (CollectionView.PreparedItems.ContainsKey(indexPath))
            {
                return CollectionView.PreparedItems[indexPath].ItemBounds;
            }

            //base on any visible item
            if (CollectionView.PreparedItems.Count > 0)
            {
                var item = CollectionView.PreparedItems.First();
                var itemIndexPath = item.Key;
                var itemViewHolder = item.Value;

                var row = GetRowAndColumnOfItem(indexPath);
                var left = row.column == -1 ? 0 : row.column * (CollectionView.ContentSize.Width / ColumnCount);
                if (indexPath < itemIndexPath)
                {
                    var count = CollectionView.ItemCountInRange(indexPath, itemIndexPath) + 1;
                    double averageHeight = EstimateAverageHeight();
                    var allItemHeight = count * averageHeight / ColumnCount;
                    return new Rect(left, itemViewHolder.ItemBounds.Top - allItemHeight, itemViewHolder.ItemBounds.Width, averageHeight);
                }

                item = CollectionView.PreparedItems.Last();
                itemIndexPath = item.Key;
                itemViewHolder = item.Value;
                if (indexPath > itemIndexPath)
                {
                    var count = CollectionView.ItemCountInRange(itemIndexPath, indexPath);
                    double averageHeight = EstimateAverageHeight();
                    var allItemHeight = count * averageHeight / ColumnCount;
                    return new Rect(left, itemViewHolder.ItemBounds.Bottom + allItemHeight, itemViewHolder.ItemBounds.Width, averageHeight);
                }
            }

            return Rect.Zero;
        }

        protected override void ScrollToItem(NSIndexPath targetIndexPath)
        {
            /*
            * Estimate position of item, and set ScrollY
            */
            var firstPreparedItem = CollectionView.PreparedItems.FirstOrDefault();
            var lastPreparedItem = CollectionView.PreparedItems.LastOrDefault();
            double itemsOffset = 0;
            var numberOfSections = CollectionView.NumberOfSections();
            if (targetIndexPath > lastPreparedItem.Key)//The target item is at the bottom of the visible area, laid out from the bottom up.
            {
                itemsOffset += CollectionView.ItemCountInRange(lastPreparedItem.Key, targetIndexPath) / ColumnCount * EstimateAverageHeight();
                
                ItemLayoutBaseline = new LayoutInfor()
                {
                    EndBounds = new Rect(0, 0, 0, lastPreparedItem.Value.ItemBounds.Bottom + itemsOffset + CollectionView.Bounds.Height),
                    EndItem = targetIndexPath
                };
                CollectionView.ScrollToAsync(0, lastPreparedItem.Value.ItemBounds.Bottom + itemsOffset, false);
            }
            else if (targetIndexPath < firstPreparedItem.Key)//The target item is at the top of the visible area, laid out from the top down.
            {
                //Using proportional calculations is more reasonable than calculating based on individual item heights, avoid negative numbers.
                var rowCountFromTargetToFirstPrepared = (CollectionView.ItemCountInRange(targetIndexPath, firstPreparedItem.Key) + 1) / ColumnCount;
                var rowCountFromFirstToFirstPrepared = (CollectionView.ItemCountInRange(NSIndexPath.FromRowSection(0, 0), firstPreparedItem.Key) + 1) / ColumnCount;
                var distanceFromTargetToFirstPrepared = (firstPreparedItem.Value.ItemBounds.Top - StartBoundsCache[0].Top) * rowCountFromTargetToFirstPrepared / rowCountFromFirstToFirstPrepared + (CollectionView.ScrollY - firstPreparedItem.Value.ItemBounds.Top);
                ItemLayoutBaseline = new LayoutInfor()
                {
                    StartBounds = new Rect(0, CollectionView.ScrollY - distanceFromTargetToFirstPrepared, 0, 0),
                    StartItem = targetIndexPath
                };
                CollectionView.ScrollToAsync(0, CollectionView.ScrollY - distanceFromTargetToFirstPrepared, false);
            }
            isScrollingTo = true;
        }
    }
}
