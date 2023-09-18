namespace Yang.MAUICollectionView.Layouts
{
    public partial class CollectionViewFlatListLayout : CollectionViewLayout
    {
        public CollectionViewFlatListLayout(MAUICollectionView collectionView) : base(collectionView)
        {
        }

        #region Property or Field

        /// <summary>
        /// When jump to specify position, we use this tag tell <see cref="MeasureItems"/> if remeasure all item.
        /// </summary>
        protected bool isScrollingTo = false;

        /// <summary>
        /// store bounds of items at start in list.
        /// </summary>
        protected List<Rect> StartBoundsCache = new List<Rect> { };

        #endregion

        #region Method

        protected override double MeasureItems(double top, Rect inRect, Rect visibleRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availablePreparedItems)
        {
            if (CollectionView.IsScrolling &&
                isScrollingTo == false &&
                !HasOperation)
            {
                Fill(inRect, AnalysisBaseline(top), availablePreparedItems, false);
            }
            else
            {
                Fill(inRect, AnalysisBaseline(top), availablePreparedItems, true);

                if (isScrollingTo)
                    isScrollingTo = false;
                if (ItemLayoutBaseline != null)
                    ItemLayoutBaseline = null;
            }

            // store some bounds
            if (StartBoundsCache.Count == 0)
            {
                var rowCountInFirstSection = 2;//CollectionView.NumberOfItemsInSection(0);
                for (var index = 0; index < rowCountInFirstSection; index++)
                {
                    var indexPath = NSIndexPath.FromRowSection(index, 0);
                    if (CollectionView.PreparedItems.ContainsKey(indexPath))
                    {
                        StartBoundsCache.Add(CollectionView.PreparedItems[indexPath].ItemBounds);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            LayoutInfor visibleItems = new LayoutInfor();
            foreach (var item in CollectionView.PreparedItems)
            {
                if (item.Value.ItemBounds.IntersectsWith(visibleRect))
                {
                    if (visibleItems.StartItem == null)
                        visibleItems.StartItem = item.Key;
                    visibleItems.EndItem = item.Key;
                }
            }
            FitBoundsWhenCloseHeader(visibleItems);

            //estimate all items' height
            double itemsHeight = 0;
            var numberOfSections = CollectionView.NumberOfSections();
            var (lastPreparedItem, lastPreparedItemViewHolder) = CollectionView.PreparedItems.LastOrDefault();
            if (lastPreparedItemViewHolder != null)
                itemsHeight += (lastPreparedItemViewHolder.ItemBounds.Bottom - top);
            var lastItem = NSIndexPath.FromRowSection(CollectionView.NumberOfItemsInSection(numberOfSections - 1) - 1, numberOfSections - 1);
            if (lastPreparedItem == null)
                lastPreparedItem = NSIndexPath.FromRowSection(0, 0);
            if (lastItem > lastPreparedItem)
            {
                itemsHeight += CollectionView.ItemCountInRange(lastPreparedItem, lastItem) * StartBoundsCache[StartBoundsCache.Count - 1].Height;
            }

            return itemsHeight;
        }

        /// <summary>
        /// When not touch scrolling, we use it to remeasure all prepared items.
        /// </summary>
        /// <param name="top"></param>
        protected virtual LayoutInfor AnalysisBaseline(double top)
        {
            if (ItemLayoutBaseline != null)// use specify baseline to layout
            {
                return ItemLayoutBaseline.Copy();
            }
            else if (OldPreparedItems.StartItem != null)// use last layout item as baseline to layout
            {
                return new LayoutInfor()
                {
                    StartItem = OldPreparedItems.StartItem,
                    StartBounds = OldPreparedItems.StartBounds
                };
            }
            else //use header's bottom as baseline to layout, this will be called when start a new collectionview or error(because fast scroll, scrollto, drag scrollbar)
            {
                if (CollectionView.ScrollY > top)//when header is invisible
                {
                    if (CollectionView.ScrollY < StartBoundsCache[StartBoundsCache.Count - 1].Bottom)//use cache fix
                    {
                        for (int i = 0; i < StartBoundsCache.Count; i++)
                        {
                            var rect = StartBoundsCache[i];
                            if (rect.Contains(0, CollectionView.ScrollY))
                            {
                                return new LayoutInfor()
                                {
                                    StartItem = NSIndexPath.FromRowSection(i, 0),
                                    StartBounds = new Rect(0, rect.Top, 0, 0),
                                };
                            }
                        }
                    }
                    else//estimate scrolly to fix 
                    {
                        return new LayoutInfor()
                        {
                            StartItem = ItemAtPoint(new Point(0, CollectionView.ScrollY)),
                            StartBounds = new Rect(0, CollectionView.ScrollY, 0, 0),
                        };
                    }
                }

                // if we can't easy solve error, try use first item
                return new LayoutInfor()
                {
                    StartItem = NSIndexPath.FromRowSection(0, 0),
                    StartBounds = new Rect(0, top, 0, 0),
                };
            }
        }

        /// <summary>
        /// Use data in <see cref="StartBoundsCache"/> to estimate height of item.
        /// </summary>
        /// <returns></returns>
        protected virtual double EstimateAverageHeight()
        {
            return (StartBoundsCache.Last().Bottom - StartBoundsCache.First().Top) / StartBoundsCache.Count;
        }

        /// <summary>
        /// Arrange items from above or below to fill a given rectangle. 
        /// </summary>
        /// <param name="inRect"></param>
        /// <param name="baselineInfor">according to it to layout other items</param>
        /// <param name="availableCells"></param>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual void Fill(Rect inRect, LayoutInfor baselineInfor, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells, bool isRemeasureAll = true)
        {
            /*
             * from top to bottom, means we have a top value of item, we base on it to calculate bounds of items below.
             */
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
                    int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                    int row = 0;
                    if (section == topBaselineInfor.StartItem.Section)
                        row = topBaselineInfor.StartItem.Row;
                    for (; row < numberOfRows; row++)
                    {
                        indexPath = NSIndexPath.FromRowSection(row, section);
                        (MAUICollectionViewViewHolder viewHolder, Rect bounds) result;
                        if (availableCells.ContainsKey(indexPath) &&
                            !isRemeasureAll)
                        {
                            result = (availableCells[indexPath], availableCells[indexPath].ItemBounds);
                            availableCells.Remove(indexPath);
                        }
                        else
                            result = MeasureItem(inRect, inRect.Width, new Point(0, top), Edge.Top | Edge.Left, indexPath, availableCells);
                        if (result.viewHolder != null)
                        {
                            //here we can change item's size
                            CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, indexPath, result.viewHolder, Edge.Top);
                            result.bounds = result.viewHolder.ItemBounds;
                            CollectionView.PreparedItems.Add(indexPath, result.viewHolder);
                        }
                        if (result.bounds.Bottom >= inRect.Bottom)
                            return;
                        top += result.bounds.Height;
                    }
                }
            }

            /*
             * from bottom to top, means we have a bottom value of item, we base on it to calculate bounds of items above.
             */
            void LayoutFromBottomToTop(LayoutInfor bottomBaselineInfor)
            {
                if (bottomBaselineInfor.EndItem == null)
                    return;
                List<KeyValuePair<NSIndexPath, MAUICollectionViewViewHolder>> tempOrderedPreparedItems = new();
                var bottom = bottomBaselineInfor.EndBounds.Bottom;
                // fill item in rect
                var numberOfSections = CollectionView.NumberOfSections();
                NSIndexPath indexPath = bottomBaselineInfor.EndItem;
                for (int section = bottomBaselineInfor.EndItem.Section; section >= 0; section--)
                {
                    int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                    int row = numberOfRows - 1;
                    if (section == bottomBaselineInfor.EndItem.Section)
                        row = bottomBaselineInfor.EndItem.Row;
                    for (; row >= 0; row--)
                    {
                        indexPath = NSIndexPath.FromRowSection(row, section);
                        (MAUICollectionViewViewHolder viewHolder, Rect bounds) result;
                        if (availableCells.ContainsKey(indexPath) &&
                            !isRemeasureAll)
                        {
                            result = (availableCells[indexPath], availableCells[indexPath].ItemBounds);
                            availableCells.Remove(indexPath);
                        }
                        else
                            result = MeasureItem(inRect, inRect.Width, new Point(0, bottom), Edge.Bottom | Edge.Left, indexPath, availableCells);
                        if (result.viewHolder != null)
                        {
                            //here we can change item's size
                            CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, indexPath, result.viewHolder, Edge.Bottom);
                            result.bounds = result.viewHolder.ItemBounds;
                            tempOrderedPreparedItems.Add(new KeyValuePair<NSIndexPath, MAUICollectionViewViewHolder>(indexPath, result.viewHolder));
                        }
                        if (result.bounds.Top <= inRect.Top)
                            goto FinishLoop;
                        bottom -= result.bounds.Height;
                    }
                }

            FinishLoop:
                // sort it, from small to large
                for (var index = tempOrderedPreparedItems.Count - 1; index >= 0; index--)
                {
                    var item = tempOrderedPreparedItems[index];
                    CollectionView.PreparedItems.Add(item.Key, item.Value);
                }
            }

            if (baselineInfor.StartItem != null)//start item know top
            {
                if (baselineInfor.StartBounds.Top > inRect.Top)
                    LayoutFromBottomToTop(new LayoutInfor() { EndItem = CollectionView.NextItem(baselineInfor.StartItem, -1), EndBounds = new Rect(0, 0, 0, baselineInfor.StartBounds.Top) });
                LayoutFromTopToBottom(baselineInfor);
            }
            else//end item know bottom
            {
                LayoutFromBottomToTop(baselineInfor);
                if (baselineInfor.EndBounds.Bottom < inRect.Bottom)
                    LayoutFromTopToBottom(new LayoutInfor() { StartItem = CollectionView.NextItem(baselineInfor.EndItem, 1), StartBounds = new Rect(0, baselineInfor.EndBounds.Bottom, 0, 0) });
            }
        }

        protected (MAUICollectionViewViewHolder viewHolder, Rect bounds) MeasureItem(Rect inRect, double constrainedWidth, Point baseline, Edge edge, NSIndexPath indexPath, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableViewHolders)
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
                //测量高度
                Size measureSize;

                var rowHeightWant = CollectionView.Source.HeightForItem(CollectionView, indexPath);
                viewHolder.WidthRequest = -1;
                if (rowHeightWant != MAUICollectionViewViewHolder.AutoSize)//fixed value
                {
                    viewHolder.HeightRequest = rowHeightWant;
                    measureSize = viewHolder.MeasureSelf(constrainedWidth, rowHeightWant).Request;
                }
                else//need measure
                {
                    viewHolder.HeightRequest = -1;
                    measureSize = viewHolder.MeasureSelf(constrainedWidth, double.PositiveInfinity).Request;
                }
                viewHolder.IndexPath = indexPath;

                var bounds = new Rect(edge.HasFlag(Edge.Left) ? baseline.X : edge.HasFlag(Edge.Right) ? baseline.X - measureSize.Width : throw new ArgumentException(),
                    edge.HasFlag(Edge.Top) ? baseline.Y : edge.HasFlag(Edge.Bottom) ? baseline.Y - measureSize.Height : throw new ArgumentException(),
                    constrainedWidth,
                    measureSize.Height);

                //store bounds,  we will use it when arrange
                if (viewHolder.Operation == (int)OperateItem.OperateType.Move &&
                    HasOperation)//move + anim + diff bounds
                {
                    if (bounds != viewHolder.ItemBounds)
                        viewHolder.OldItemBounds = viewHolder.ItemBounds;//move operate need old position to make animation
                    else
                        viewHolder.OldItemBounds = Rect.Zero;
                    viewHolder.ItemBounds = bounds;
                }
                else
                {
                    viewHolder.ItemBounds = bounds;
                }

                if (!viewHolder.ItemBounds.IntersectsWith(inRect))
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
        /// Jump to specify item immediately, no animation.
        /// </summary>
        /// <param name="targetIndexPath"></param>
        /// <param name="animated"></param>
        protected virtual void ScrollToItem(NSIndexPath targetIndexPath)
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
                itemsOffset += CollectionView.ItemCountInRange(lastPreparedItem.Key, targetIndexPath) * lastPreparedItem.Value.ItemBounds.Height;
                ItemLayoutBaseline = new LayoutInfor()
                {
                    EndBounds = new Rect(0, 0, 0, CollectionView.ScrollY + itemsOffset + CollectionView.Bounds.Height), //set this item at bottom of visible area.
                    EndItem = targetIndexPath
                };
                CollectionView.ScrollToAsync(0, CollectionView.ScrollY + itemsOffset, false);
            }
            else if (targetIndexPath < firstPreparedItem.Key)//The target item is at the top of the visible area, laid out from the top down.
            {
                //Using proportional calculations is more reasonable than calculating based on individual item heights, avoid negative numbers.
                var itemsCountFromTargetToFirstPrepared = CollectionView.ItemCountInRange(targetIndexPath, firstPreparedItem.Key) + 1;
                var itemsCountFromFirstToFirstPrepared = CollectionView.ItemCountInRange(NSIndexPath.FromRowSection(0, 0), firstPreparedItem.Key) + 1;
                var distanceFromTargetToFirstPrepared = (firstPreparedItem.Value.ItemBounds.Top - StartBoundsCache[0].Top) * itemsCountFromTargetToFirstPrepared / itemsCountFromFirstToFirstPrepared + (CollectionView.ScrollY - firstPreparedItem.Value.ItemBounds.Top);
                ItemLayoutBaseline = new LayoutInfor()
                {
                    StartBounds = new Rect(0, CollectionView.ScrollY - distanceFromTargetToFirstPrepared, 0, 0),
                    StartItem = targetIndexPath
                };
                CollectionView.ScrollToAsync(0, CollectionView.ScrollY - distanceFromTargetToFirstPrepared, false);
            }
        }

        /// <summary>
        /// this layout support go to, so item's position is not accurate sometimes, we need adjust position to fit header's position.
        /// </summary>
        void FitBoundsWhenCloseHeader(LayoutInfor visibleItems)
        {
            //正常布局时
            if (CollectionView.PreparedItems.Count > 0)
            {
                var visibleFirst = visibleItems.StartItem;
                /*if (visibleFirst.Key.Section == 0 && (visibleFirst.Key.Row >= 0 && visibleFirst.Key.Row < StartBoundsCache.Count - 1))
                {
                    *//*
                     * case 1: item's position not fit header, we try find one item let it fit.
                     *//*
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
                    *//*
                     * case 2: top don't have space to scroll to header
                     *//*
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
                    {*/
                var firstItem = NSIndexPath.FromRowSection(0, 0);

                if (CollectionView.ScrollY <= StartBoundsCache[0].Top)
                {
                    if (visibleFirst.Compare(firstItem) == 0)
                    {

                    }
                    else
                    {
                        var lastCache = NSIndexPath.FromRowSection(StartBoundsCache.Count - 1, 0);
                        if (lastCache.Compare(visibleFirst) >= 0)
                        {
                            var targetBounds = StartBoundsCache[visibleFirst.Row];
                            if (CollectionView.PreparedItems[visibleFirst].ItemBounds != targetBounds)
                            {
                                ItemLayoutBaseline = new LayoutInfor()
                                {
                                    StartBounds = new Rect(0, targetBounds.Top, 0, 0),
                                    StartItem = visibleFirst
                                };
                                isScrollingTo = true;
                                CollectionView.ScrollToAsync(0, targetBounds.Top, false);
                            }
                        }
                        else
                        {
                            var targetTop = CollectionView.PreparedItems[visibleFirst].ItemBounds.Bottom + CollectionView.ItemCountInRange(lastCache, visibleFirst) * EstimateAverageHeight();

                            ItemLayoutBaseline = new LayoutInfor()
                            {
                                StartBounds = new Rect(0, targetTop, 0, 0),
                                StartItem = visibleFirst
                            };
                            isScrollingTo = true;
                            CollectionView.ScrollToAsync(0, targetTop, false);
                        }
                    }
                }
                /*
                 * case 3: when top have too big space to scroll header, when first item show, will show space, so we directly scroll to top of first item. 
                 */
                if (visibleFirst.Compare(firstItem) == 0)
                {
                    var firstItemRect = CollectionView.PreparedItems[visibleFirst].ItemBounds;
                    if (firstItemRect.Top > StartBoundsCache[0].Top && //There is space  
                        CollectionView.ScrollY < (firstItemRect.Bottom - firstItemRect.Height * 4 / 5))//when close to first item top
                    {
                        ItemLayoutBaseline = new LayoutInfor()
                        {
                            StartBounds = new Rect(0, StartBoundsCache[0].Top, 0, 0),
                            StartItem = firstItem
                        };
                        isScrollingTo = true;
                        CollectionView.ScrollToAsync(0, StartBoundsCache[0].Top, false);
                    }
                }
                //}
                //}
            }
        }

        public override void ScrollTo(NSIndexPath indexPath, ScrollPosition scrollPosition, bool animated)
        {
            if (animated)
            {
                var first = CollectionView.PreparedItems.First().Key;
                var last = CollectionView.PreparedItems.Last().Key;
                var end = 0;
                if (first > indexPath) end = -(CollectionView.ItemCountInRange(indexPath, first) + 1);
                else if (last < indexPath) end = CollectionView.ItemCountInRange(first, indexPath) + 1;
                var anim = new Animation((v) =>
                {
                    var target = CollectionView.NextItem(first, (int)v);
                    if (target.Row < 0)
                    {

                    }
                    ScrollToItem(target);
                    isScrollingTo = true;
                }, 0, end);
                anim.Commit(CollectionView, "ScrollTo", 16, 250, null, (v, b) =>
                {
                    ScrollToItem(indexPath);
                    isScrollingTo = true;
                });
            }
            else
            {
                ScrollToItem(indexPath);
                isScrollingTo = true;
            }
        }

        /// <summary>
        /// Get <see cref="MAUICollectionViewViewHolder.ItemBounds"/> of item, If it in <see cref="MAUICollectionView.PreparedItems"/>, return bounds. 
        /// If not, estimate a bounds.
        /// </summary>
        /// <param name="indexPath"></param>
        /// <returns></returns>
        public override Rect RectForItem(NSIndexPath indexPath)
        {
            var rect = base.RectForItem(indexPath);
            if (rect == Rect.Zero)
            {
                //base on any visible item
                if (CollectionView.PreparedItems.Count > 0)
                {
                    var item = CollectionView.PreparedItems.First();
                    var itemIndexPath = item.Key;
                    var itemViewHolder = item.Value;
                    if (indexPath < itemIndexPath)
                    {
                        var count = CollectionView.ItemCountInRange(indexPath, itemIndexPath) + 1;//we need get top, so total height include height of target item, so +1.
                        double averageHeight = EstimateAverageHeight();
                        var allItemHeight = count * averageHeight;
                        return new Rect(0, itemViewHolder.ItemBounds.Top - allItemHeight, itemViewHolder.ItemBounds.Width, averageHeight);
                    }

                    item = CollectionView.PreparedItems.Last();
                    itemIndexPath = item.Key;
                    itemViewHolder = item.Value;
                    if (indexPath > itemIndexPath)
                    {
                        var count = CollectionView.ItemCountInRange(itemIndexPath, indexPath);
                        double averageHeight = EstimateAverageHeight();
                        var allItemHeight = count * averageHeight;
                        return new Rect(0, itemViewHolder.ItemBounds.Bottom + allItemHeight, itemViewHolder.ItemBounds.Width, averageHeight);
                    }
                }
            }
            return rect;
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

        /// <summary>
        /// Estimate the item displayed when scrolling to a certain position.
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        NSIndexPath EstimateItem(double x, double y)
        {
            var firstItemBounds = StartBoundsCache.First();
            double averageHeight = EstimateAverageHeight();

            var numberOfSections = CollectionView.NumberOfSections();
            double allHeight = 0;
            for (var section = 0; section < numberOfSections; section++)
            {
                var rowsInSection = CollectionView.NumberOfItemsInSection(section);
                if (y - firstItemBounds.Top < allHeight + rowsInSection * averageHeight)
                {
                    return NSIndexPath.FromRowSection((int)((y - firstItemBounds.Top - allHeight) / averageHeight), section);
                }
                else
                {
                    allHeight += rowsInSection * averageHeight;
                }
            }
            //maybe scrolly is very big
            return NSIndexPath.FromRowSection(CollectionView.NumberOfItemsInSection(numberOfSections - 1), numberOfSections - 1);
        }

        #endregion

        #endregion
    }
}
