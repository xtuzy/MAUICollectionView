namespace MauiUICollectionView.Layouts
{
    public partial class CollectionViewFlatListLayout : CollectionViewLayout
    {
        public CollectionViewFlatListLayout(MAUICollectionView collectionView) : base(collectionView)
        {
        }

        protected override double MeasureItems(double top, Rect inRect, Rect visiableRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availablePreparedItems)
        {
            if (CollectionView.IsScrolling && 
                isScrollToDirectly == false && 
                !IsOperating)
            {
                MeasureItemsWhenScroll(inRect, availablePreparedItems);
            }
            else
            {
                MeasureItemsUsually(inRect, top, availablePreparedItems);
                if (isScrollToDirectly)
                    isScrollToDirectly = false;
                if (BaseLineItemUsually != null)
                    BaseLineItemUsually = null;
            }

            // store some bounds
            if (StartBoundsCache.Count == 0)
            {
                var rowCountInFirstSection = CollectionView.NumberOfItemsInSection(0);
                for (var index = 0; index < rowCountInFirstSection; index++)
                {
                    var indexPath = NSIndexPath.FromRowSection(index, 0);
                    if (CollectionView.PreparedItems.ContainsKey(indexPath))
                    {
                        StartBoundsCache.Add(CollectionView.PreparedItems[indexPath].BoundsInLayout);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            FitBoundsWhenCloseHeader();

            //estimate all items' height
            double itemsHeight = 0;
            var lastPreparedItem = CollectionView.PreparedItems.LastOrDefault();
            itemsHeight += (lastPreparedItem.Value.BoundsInLayout.Bottom - top);
            var numberOfSections = CollectionView.NumberOfSections();
            var lastItem = NSIndexPath.FromRowSection(CollectionView.NumberOfItemsInSection(numberOfSections - 1) - 1, numberOfSections - 1);
            if (lastItem > lastPreparedItem.Key)
            {
                itemsHeight += ItemCountInRange(lastPreparedItem.Key, lastItem) * lastPreparedItem.Value.BoundsInLayout.Height;
            }

            return itemsHeight;
        }

        /// <summary>
        /// Use it when touch scrolling or scrolling a small offset. It only measures items in the offset, not remeasuring all prepared items like <see cref="MeasureItemsUsually"/> does, which makes the scrolling performance better
        /// </summary>
        /// <param name="inRect"></param>
        /// <param name="availablePreparedItems"></param>
        void MeasureItemsWhenScroll(Rect inRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availablePreparedItems)
        {
            double baseLine = 0;

            if (CollectionView.scrollOffset < 0)//加载上面的
            {
                ScrollByOffset(CollectionView.scrollOffset, inRect, new LayoutInfor()
                {
                    EndBounds = new Rect(0, 0, 0, OldPreparedItems.StartBounds.Top),
                    EndItem = NSIndexPath.FromRowSection(OldPreparedItems.StartItem.Row - 1, OldPreparedItems.StartItem.Section)
                }, availablePreparedItems);
            }
            else//加载下面的
            {
                ScrollByOffset(CollectionView.scrollOffset, inRect, new LayoutInfor()
                {
                    StartBounds = new Rect(0, OldPreparedItems.EndBounds.Bottom, 0, 0),
                    StartItem = NSIndexPath.FromRowSection(OldPreparedItems.EndItem.Row + 1, OldPreparedItems.EndItem.Section)
                }, availablePreparedItems);
            }
        }

        LayoutInfor BaseLineItemUsually;

        /// <summary>
        /// When not touch scrolling, we use it to remeasure all prepared items.
        /// </summary>
        /// <param name="inRect"></param>
        /// <param name="top"></param>
        /// <param name="availablePreparedItems"></param>
        void MeasureItemsUsually(Rect inRect, double top, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availablePreparedItems)
        {
            if (BaseLineItemUsually != null)// use specify baseline to layout
            {
                OnLayoutChildren(inRect, BaseLineItemUsually, availablePreparedItems);
            }
            else if (OldPreparedItems.StartItem != null)// use last layout item as baseline to layout
            {
                OnLayoutChildren(inRect, new LayoutInfor()
                {
                    StartItem = OldPreparedItems.StartItem,
                    StartBounds = OldPreparedItems.StartBounds
                }, availablePreparedItems);
            }
            else //use header's bottom as baseline to layout
            {
                OnLayoutChildren(inRect, new LayoutInfor()
                {
                    StartItem = NSIndexPath.FromRowSection(0, 0),
                    StartBounds = new Rect(0, top, 0, 0),
                }, availablePreparedItems);
            }
            if(CollectionView.PreparedItems.Count == 0)
            {

            }
        }

        /// <summary>
        /// when start is 1, end is 4, return 2
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        int ItemCountInRange(NSIndexPath start, NSIndexPath end)
        {
            if (start.Section == end.Section)
            {
                return end.Row - start.Row;
            }
            else
            {
                int count = 0;
                for (var section = start.Section; section <= end.Section; section++)
                {
                    int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                    int row = 0;
                    if (section == start.Section) { row = start.Row; }
                    if (section == end.Section) { numberOfRows = end.Row; }
                    for (; row < numberOfRows; row++)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// 从上方或者下方布置Item以填满给定矩形. 
        /// </summary>
        /// <param name="inRect"></param>
        /// <param name="top"></param>
        /// <param name="isTop"></param>
        /// <param name="availableCells"></param>
        /// <exception cref="NotImplementedException"></exception>
        void OnLayoutChildren(Rect inRect, LayoutInfor baselineInfor, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells)
        {
            void LayoutFromTopToBottom()
            {
                var top = baselineInfor.StartBounds.Top;
                // 填充item到矩形高度
                var numberOfSections = CollectionView.NumberOfSections();
                NSIndexPath indexPath = baselineInfor.StartItem;
                for (int section = baselineInfor.StartItem.Section; section < numberOfSections; section++)
                {
                    int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                    int row = 0;
                    if (section == baselineInfor.StartItem.Section)
                        row = baselineInfor.StartItem.Row;
                    for (; row < numberOfRows; row++)
                    {
                        indexPath = NSIndexPath.FromRowSection(row, section);
                        var (viewHolder, bounds) = layoutChunk(inRect, inRect.Width, top, Edge.Top, indexPath, availableCells);
                        if (viewHolder != null) CollectionView.PreparedItems.Add(indexPath, viewHolder);
                        else
                            return;
                        top += bounds.Height;
                    }
                }
            }

            List<KeyValuePair<NSIndexPath, MAUICollectionViewViewHolder>> LayoutFromBottomToTop()
            {
                List<KeyValuePair<NSIndexPath, MAUICollectionViewViewHolder>> tempOrderedPreparedItems = new();
                var bottom = baselineInfor.EndBounds.Bottom;
                // 填充item到矩形高度
                var numberOfSections = CollectionView.NumberOfSections();
                NSIndexPath indexPath = baselineInfor.EndItem;
                for (int section = baselineInfor.EndItem.Section; section >= 0; section--)
                {
                    int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                    int row = numberOfRows - 1;
                    if (section == baselineInfor.EndItem.Section)
                        row = baselineInfor.EndItem.Row;
                    for (; row >= 0; row--)
                    {
                        indexPath = NSIndexPath.FromRowSection(row, section);
                        var (viewHolder, bounds) = layoutChunk(inRect, inRect.Width, bottom, Edge.Bottom, indexPath, availableCells);
                        if (viewHolder != null) tempOrderedPreparedItems.Add(new KeyValuePair<NSIndexPath, MAUICollectionViewViewHolder>(indexPath, viewHolder));
                        else
                            return tempOrderedPreparedItems;
                        bottom -= bounds.Height;
                    }
                }
                return tempOrderedPreparedItems;
            }

            if (baselineInfor.StartItem != null)//从上到下
            {
                LayoutFromTopToBottom();
            }
            else//从下往上
            {
                List<KeyValuePair<NSIndexPath, MAUICollectionViewViewHolder>> tempOrderedPreparedItems = LayoutFromBottomToTop();

                // 从小到大加入
                for (var index = tempOrderedPreparedItems.Count - 1; index >= 0; index--)
                {
                    var item = tempOrderedPreparedItems[index];
                    CollectionView.PreparedItems.Add(item.Key, item.Value);
                }
            }
        }

        /// <summary>
        /// <see href="https://github.com/xtuzy/androidx/blob/351aa3fa59ddb5c5da4a34b022d9349d07d8d932/recyclerview/recyclerview/src/main/java/androidx/recyclerview/widget/LinearLayoutManager.java#L1475">LinearLayoutManager.scrollBy()</see>
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="inRect"></param>
        /// <param name="baselineInfor">布局items的第一项或者最后一项信息</param>
        /// <param name="availablePreparedItems"></param>
        /// <returns></returns>
        double ScrollByOffset(double delta, Rect inRect, LayoutInfor baselineInfor, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availablePreparedItems)
        {
            if (delta == 0)
                return 0;
            var absDelta = Math.Abs(delta);

            var numberOfSections = CollectionView.NumberOfSections();
            var availableHeight = absDelta;
            if (delta > 0)//加载下面的
            {
                foreach (var item in availablePreparedItems)
                    CollectionView.PreparedItems.Add(item.Key, item.Value);
                availablePreparedItems.Clear();

                if (OldPreparedItems.EndBounds.Bottom >= inRect.Bottom)// 上一次布局的是否依旧填满
                {
                    return absDelta;
                }

                var top = baselineInfor.StartBounds.Top;
                for (var section = baselineInfor.StartItem.Section; section < numberOfSections && availableHeight >= 0; section++)
                {
                    int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                    int row = 0;
                    if (section == baselineInfor.StartItem.Section)
                        row = baselineInfor.StartItem.Row;
                    for (; row < numberOfRows && availableHeight >= 0; row++)
                    {
                        var indexPath = NSIndexPath.FromRowSection(row, section);
                        var (viewHolder, bounds) = layoutChunk(inRect, inRect.Width, top, Edge.Top, indexPath, availablePreparedItems);
                        if (viewHolder != null) CollectionView.PreparedItems.Add(indexPath, viewHolder);
                        availableHeight -= bounds.Height;
                        top += bounds.Height;
                    }
                }
            }
            else//加载上面的

            {
                if (OldPreparedItems.StartBounds.Top <= inRect.Top)
                {
                    foreach (var item in availablePreparedItems)
                        CollectionView.PreparedItems.Add(item.Key, item.Value);
                    availablePreparedItems.Clear();
                    return absDelta;
                }

                List<KeyValuePair<NSIndexPath, MAUICollectionViewViewHolder>> tempOrderedPreparedItems = new();
                var bottom = baselineInfor.EndBounds.Bottom;
                for (var section = baselineInfor.EndItem.Section; section >= 0 && availableHeight >= 0; section--)
                {
                    int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                    int row = numberOfRows - 1;
                    if (section == baselineInfor.EndItem.Section)
                        row = baselineInfor.EndItem.Row;
                    for (; row >= 0 && availableHeight >= 0; row--)
                    {
                        var indexPath = NSIndexPath.FromRowSection(row, section);
                        var (viewHolder, bounds) = layoutChunk(inRect, inRect.Width, bottom, Edge.Bottom, indexPath, availablePreparedItems);
                        if (viewHolder != null) tempOrderedPreparedItems.Add(new KeyValuePair<NSIndexPath, MAUICollectionViewViewHolder>(indexPath, viewHolder));
                        availableHeight -= bounds.Height;
                        bottom -= bounds.Height;
                    }
                }
                // 从小到大加入
                for (var index = tempOrderedPreparedItems.Count - 1; index >= 0; index--)
                {
                    var item = tempOrderedPreparedItems[index];
                    CollectionView.PreparedItems.Add(item.Key, item.Value);
                }
                foreach (var item in availablePreparedItems)
                    CollectionView.PreparedItems.Add(item.Key, item.Value);
                availablePreparedItems.Clear();
            }
            return absDelta - availableHeight;
        }

        public enum Edge
        {
            Top, Bottom, Left, Right
        }

        (MAUICollectionViewViewHolder viewHolder, Rect height) layoutChunk(Rect inRect, double constrainedWidth, double baseline, Edge edge, NSIndexPath indexPath, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableViewHolders)
        {
            //获取Cell, 优先获取之前已经被显示的, 这里假定已显示的数据没有变化
            MAUICollectionViewViewHolder viewHolder = null;
            if (availableViewHolders.ContainsKey(indexPath))
            {
                viewHolder = availableViewHolders[indexPath];
                availableViewHolders.Remove(indexPath);
            }

            viewHolder = CollectionView.Source.ViewHolderForItem(CollectionView, indexPath, viewHolder, constrainedWidth);

            if (viewHolder != null)
            {
                //添加到ScrollView, 必须先添加才有测量值
                try
                {
                    if (!CollectionView.ContentView.Children.Contains(viewHolder))
                        CollectionView.AddSubview(viewHolder);
                }
                catch (Exception ex)
                {

                }
                viewHolder.WidthRequest = constrainedWidth;
                //测量高度
                Size measureSize;
                var rowHeightWant = CollectionView.Source.HeightForItem(CollectionView, indexPath);
                if (rowHeightWant != MAUICollectionViewViewHolder.AutoSize)//fixed value
                {
                    measureSize = viewHolder.MeasureSelf(constrainedWidth, rowHeightWant).Request;
                }
                else//need measure
                {
                    measureSize = viewHolder.MeasureSelf(constrainedWidth, double.PositiveInfinity).Request;
                }
                viewHolder.IndexPath = indexPath;

                var bounds = new Rect(0, edge == Edge.Top ? baseline : baseline - measureSize.Height, constrainedWidth, measureSize.Height);

                //store bounds,  we will use it when arrange
                if (viewHolder.Operation == (int)OperateItem.OperateType.Move &&
                    IsOperating &&
                    bounds != viewHolder.BoundsInLayout)//move + anim + diff bounds
                {
                    viewHolder.OldBoundsInLayout = viewHolder.BoundsInLayout;//move operate need old position to make animation
                    viewHolder.BoundsInLayout = bounds;
                }
                else
                {
                    viewHolder.BoundsInLayout = bounds;
                }

                if (!viewHolder.BoundsInLayout.IntersectsWith(inRect))
                {
                    CollectionView.RecycleViewHolder(viewHolder);
                    viewHolder = null;
                }

                return (viewHolder, bounds);
            }
            else
            {
                throw new NotImplementedException($"Get ViewHolder is null of {indexPath} from {nameof(MAUICollectionViewSource.ViewHolderForItem)}.");
            }
        }

        /// <summary>
        /// When jump to specify position, we need this avoid use <see cref="ScrollByOffset"/>.
        /// </summary>
        bool isScrollToDirectly = false;

        /// <summary>
        /// Jump to specify item now, no animation.
        /// </summary>
        /// <param name="targetIndexPath"></param>
        /// <param name="animated"></param>
        void ScrollToItem(NSIndexPath targetIndexPath)
        {
            //假装Scroll到指定Item
            var firstPreparedItem = CollectionView.PreparedItems.FirstOrDefault();
            var lastPreparedItem = CollectionView.PreparedItems.LastOrDefault();
            double itemsOffset = 0;
            var numberOfSections = CollectionView.NumberOfSections();
            if (targetIndexPath > lastPreparedItem.Key)//往下加载, 目标Item在可见区域底部, 由底部向上布局
            {
                itemsOffset += ItemCountInRange(lastPreparedItem.Key, targetIndexPath) * lastPreparedItem.Value.BoundsInLayout.Height;
                BaseLineItemUsually = new LayoutInfor()
                {
                    EndBounds = new Rect(0, 0, 0, CollectionView.ScrollY + itemsOffset + CollectionView.Bounds.Height),
                    EndItem = targetIndexPath
                };
                CollectionView.ScrollToAsync(0, CollectionView.ScrollY + itemsOffset, false);
            }
            else if (targetIndexPath < firstPreparedItem.Key)//往上加载, 目标Item在可见区域顶部, 由顶部向下布局
            {
                itemsOffset += (ItemCountInRange(targetIndexPath, firstPreparedItem.Key) + 1) * firstPreparedItem.Value.BoundsInLayout.Height;
                BaseLineItemUsually = new LayoutInfor()
                {
                    StartBounds = new Rect(0, CollectionView.ScrollY - itemsOffset, 0, 0),
                    StartItem = targetIndexPath
                };
                CollectionView.ScrollToAsync(0, CollectionView.ScrollY - itemsOffset, false);
            }
            isScrollToDirectly = true;
        }

        /// <summary>
        /// store bounds of items at start in list.
        /// </summary>
        List<Rect> StartBoundsCache = new List<Rect> { };

        /// <summary>
        /// this layout support go to, so item's position is not accurate sometimes, we need adjust position to fit header's position.
        /// </summary>
        void FitBoundsWhenCloseHeader()
        {
            var visibleFirst = CollectionView.PreparedItems.First();
            if (visibleFirst.Key.Section == 0 && (visibleFirst.Key.Row >= 0 && visibleFirst.Key.Row < StartBoundsCache.Count - 1))
            {
                /*
                 * case 1: item's position not fit header, we try find one item let it fit.
                 */
                var targetBounds = StartBoundsCache[visibleFirst.Key.Row];
                var currentBounds = visibleFirst.Value.BoundsInLayout;
                if (targetBounds.Top != currentBounds.Top)
                {
                    BaseLineItemUsually = new LayoutInfor()
                    {
                        StartBounds = new Rect(0, targetBounds.Top, 0, 0),
                        StartItem = visibleFirst.Key
                    };
                    CollectionView.ScrollToAsync(0, CollectionView.ScrollY + (targetBounds.Top - currentBounds.Top), false);
                    isScrollToDirectly = true;
                }
            }
            else
            {
                /*
                 * case 2: top don't have space to scroll to header
                 */
                var minTop = StartBoundsCache.Last().Top;
                if (visibleFirst.Value.BoundsInLayout.Top < minTop && (visibleFirst.Key.Row >= StartBoundsCache.Count - 1))
                {
                    BaseLineItemUsually = new LayoutInfor()
                    {
                        StartBounds = new Rect(0, minTop, 0, 0),
                        StartItem = visibleFirst.Key
                    };
                    CollectionView.ScrollToAsync(0, CollectionView.ScrollY + (minTop - visibleFirst.Value.BoundsInLayout.Top), false);
                    isScrollToDirectly = true;
                }
                else
                {
                    /*
                     * case 3: when top have too big space to scroll header, when first item show, will show space. 
                     */
                    var firstItem = NSIndexPath.FromRowSection(0, 0);
                    if (visibleFirst.Key == firstItem)
                    {
                        var firstItemRect = visibleFirst.Value.BoundsInLayout;
                        if(firstItemRect.Top > 0)
                        {
                            BaseLineItemUsually = new LayoutInfor()
                            {
                                StartBounds = new Rect(0, StartBoundsCache[0].Top, 0, 0),
                                StartItem = firstItem
                            };
                            CollectionView.ScrollToAsync(0, StartBoundsCache[0].Top, false);
                            isScrollToDirectly = true;
                        }
                    }
                }
            }
        }

        NSIndexPath nextItem(NSIndexPath indexPath, int count)
        {
            var sectionCount = CollectionView.NumberOfSections();
            if (count >= 0)
            {
                for (var section = indexPath.Section; section < sectionCount; section++)
                {
                    var itemCount = CollectionView.NumberOfItemsInSection(section);
                    var itemStartIndex = 0;
                    if (section == indexPath.Section)
                    {
                        itemCount = itemCount - (indexPath.Row + 1);
                        itemStartIndex = indexPath.Row;
                    }
                    var remainCount = count - itemCount;
                    if (remainCount <= 0)
                    {
                        return NSIndexPath.FromRowSection(itemStartIndex + count, section);
                    }
                    else
                        count = remainCount;
                }
            }
            else
            {
                count = -count;
                for (var section = indexPath.Section; section >= 0; section--)
                {
                    var itemCount = CollectionView.NumberOfItemsInSection(section);
                    var itemStartIndex = itemCount;
                    if (section == indexPath.Section)
                    {
                        itemCount = indexPath.Row + 1;
                        itemStartIndex = indexPath.Row;
                    }
                    var remainCount = count - itemCount;
                    if (remainCount <= 0)
                    {
                        return NSIndexPath.FromRowSection(itemStartIndex - count, section);
                    }
                    else
                        count = remainCount;
                }
            }
            return null;
        }

        public override void ScrollTo(NSIndexPath indexPath, ScrollPosition scrollPosition, bool animated)
        {
            if (animated)
            {
                var first = CollectionView.PreparedItems.First().Key;
                var last = CollectionView.PreparedItems.Last().Key;
                var end = 0;
                if (first > indexPath) end = -ItemCountInRange(indexPath, first);
                else if (last < indexPath) end = ItemCountInRange(indexPath, first);
                var anim = new Animation((v) =>
                {
                    var target = nextItem(last, (int)v);
                    ScrollToItem(target);
                }, 0, end);
                anim.Commit(CollectionView, "ScrollTo", 16, 250, null, (v, b) =>
                {
                    ScrollToItem(indexPath);
                });
            }
            else
            {
                ScrollToItem(indexPath);
            }
        }

        public override double EstimateHeightForItems(NSIndexPath indexPath, int count)
        {
            double itemsHeight = 0;
            for (var index = 0; index < count; index++)
            {
                var needMeasureIndexPath = NSIndexPath.FromRowSection(indexPath.Row + index, indexPath.Section);
                itemsHeight += GetItemCurrentHeight(needMeasureIndexPath);
            }

            return itemsHeight;

            double GetItemCurrentHeight(NSIndexPath indexPath)
            {
                if (CollectionView.PreparedItems.ContainsKey(indexPath))
                {
                    return CollectionView.PreparedItems[indexPath].BoundsInLayout.Height;
                }
                else
                {
                    var reuseIdentifier = CollectionView.Source.ReuseIdForItem(CollectionView, indexPath);

                    var itemHeight = CollectionView.Source.HeightForItem(CollectionView, indexPath);
                    if (itemHeight == MAUICollectionViewViewHolder.AutoSize)
                    {
                        var (v, bounds) = layoutChunk(CollectionView.Bounds, CollectionView.Bounds.Width, 5000, Edge.Top, indexPath, new());
                        itemHeight = bounds.Height;
                    }
                    return itemHeight;
                }
            }
        }
    }
}
