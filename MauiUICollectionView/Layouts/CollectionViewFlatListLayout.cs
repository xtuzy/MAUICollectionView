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
                !HasOperation)
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
                if (item.Value.ItemBounds.IntersectsWith(visiableRect))
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
        /// Use it when touch scrolling or scrolling a small offset. It only measures items in the offset, not remeasuring all prepared items like <see cref="MeasureItemsUsually"/> does, which makes the scrolling performance better
        /// </summary>
        /// <param name="inRect"></param>
        /// <param name="availablePreparedItems"></param>
        void MeasureItemsWhenScroll(Rect inRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availablePreparedItems)
        {
             ScrollByOffset(CollectionView.scrollOffset, inRect, availablePreparedItems);
        }

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
                OnLayoutChildren(inRect, BaseLineItemUsually.Copy(), availablePreparedItems);
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
                if (CollectionView.ScrollY > top)
                {
                    //快速滑动出错时, 出现计算错误, 数据被清空, 尝试修复
                    if (CollectionView.ScrollY < StartBoundsCache[StartBoundsCache.Count - 1].Bottom)
                    {
                        for (int i = 0; i < StartBoundsCache.Count; i++)
                        {
                            var rect = StartBoundsCache[i];
                            if (rect.Contains(0, CollectionView.ScrollY))
                            {
                                OnLayoutChildren(inRect, new LayoutInfor()
                                {
                                    StartItem = NSIndexPath.FromRowSection(i, 0),
                                    StartBounds = new Rect(0, rect.Top, 0, 0),
                                }, availablePreparedItems);
                            }
                        }
                    }
                    else
                    {
                        OnLayoutChildren(inRect, new LayoutInfor()
                        {
                            StartItem = EstimateItem(CollectionView.ScrollY),
                            StartBounds = new Rect(0, CollectionView.ScrollY, 0, 0),
                        }, availablePreparedItems);
                    }
                }
                else
                {
                    OnLayoutChildren(inRect, new LayoutInfor()
                    {
                        StartItem = NSIndexPath.FromRowSection(0, 0),
                        StartBounds = new Rect(0, top, 0, 0),
                    }, availablePreparedItems);
                }
            }
        }

        double EstimateAverageHeight()
        {
            return (StartBoundsCache.Last().Bottom - StartBoundsCache.First().Top) / StartBoundsCache.Count;
        }

        NSIndexPath EstimateItem(double scrollY)
        {
            var firstItemBounds = StartBoundsCache.First();
            double averageHeight = EstimateAverageHeight();

            var numberOfSections = CollectionView.NumberOfSections();
            double allHeight = 0;
            for (var section = 0; section < numberOfSections; section++)
            {
                var rowsInSection = CollectionView.NumberOfItemsInSection(section);
                if (scrollY - firstItemBounds.Top < allHeight + rowsInSection * averageHeight)
                {
                    return NSIndexPath.FromRowSection((int)((scrollY - firstItemBounds.Top - allHeight) / averageHeight), section);
                }
                else
                {
                    allHeight += rowsInSection * averageHeight;
                }
            }
            //maybe scrolly is very big
            return NSIndexPath.FromRowSection(CollectionView.NumberOfItemsInSection(numberOfSections - 1), numberOfSections - 1);
        }

        /// <summary>
        /// 从上方或者下方布置Item以填满给定矩形. 
        /// </summary>
        /// <param name="inRect"></param>
        /// <param name="baselineInfor">according to it to layout other items</param>
        /// <param name="availableCells"></param>
        /// <exception cref="NotImplementedException"></exception>
        void OnLayoutChildren(Rect inRect, LayoutInfor baselineInfor, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells)
        {
            void LayoutFromTopToBottom(LayoutInfor topBaselineInfor)
            {
                if (topBaselineInfor.StartItem == null)
                    return;
                var top = topBaselineInfor.StartBounds.Top;
                // 填充item到矩形高度
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
                        var (viewHolder, bounds) = MeasureItem(inRect, inRect.Width, top, Edge.Top, indexPath, availableCells);
                        if (viewHolder != null)
                        {
                            //here we can change item's size
                            CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, indexPath, viewHolder, Edge.Top);
                            bounds = viewHolder.ItemBounds;
                            CollectionView.PreparedItems.Add(indexPath, viewHolder);
                        }
                        if (bounds.Bottom >= inRect.Bottom)
                            return;
                        top += bounds.Height;
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
                    int row = numberOfRows - 1;
                    if (section == bottomBaselineInfor.EndItem.Section)
                        row = bottomBaselineInfor.EndItem.Row;
                    for (; row >= 0; row--)
                    {
                        indexPath = NSIndexPath.FromRowSection(row, section);
                        var (viewHolder, bounds) = MeasureItem(inRect, inRect.Width, bottom, Edge.Bottom, indexPath, availableCells);
                        if (viewHolder != null)
                        {
                            //here we can change item's size
                            CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, indexPath, viewHolder, Edge.Bottom);
                            bounds = viewHolder.ItemBounds;
                            tempOrderedPreparedItems.Add(new KeyValuePair<NSIndexPath, MAUICollectionViewViewHolder>(indexPath, viewHolder));
                        }
                        if (bounds.Top <= inRect.Top)
                            goto FinishLoop;
                        bottom -= bounds.Height;
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

            if (baselineInfor.StartItem != null)//从上到下
            {
                if (baselineInfor.StartBounds.Top > inRect.Top)
                    LayoutFromBottomToTop(new LayoutInfor() { EndItem = CollectionView.NextItem(baselineInfor.StartItem, -1), EndBounds = new Rect(0, 0, 0, baselineInfor.StartBounds.Top) });
                LayoutFromTopToBottom(baselineInfor);
            }
            else//从下往上
            {
                LayoutFromBottomToTop(baselineInfor);
                if (baselineInfor.EndBounds.Bottom < inRect.Bottom)
                    LayoutFromTopToBottom(new LayoutInfor() { StartItem = CollectionView.NextItem(baselineInfor.EndItem, 1), StartBounds = new Rect(0, baselineInfor.EndBounds.Bottom, 0, 0) });
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
        double ScrollByOffset(double delta, Rect inRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availablePreparedItems)
        {
            var absDelta = Math.Abs(delta);

            var numberOfSections = CollectionView.NumberOfSections();
            var availableHeight = absDelta;
            if (delta >= 0)//加载下面的
            {
                /*
                 * ^
                 * | 
                 */
                double nextItemTop = 0;
                foreach (var item in availablePreparedItems)
                {
                    //here we can change item's size
                    CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, item.Key, item.Value, Edge.Top);
                    if(nextItemTop == 0)
                    {
                        nextItemTop = item.Value.ItemBounds.Bottom;
                    }
                    else
                    {
                        item.Value.ItemBounds.Top = nextItemTop;
                        nextItemTop = item.Value.ItemBounds.Bottom;
                    }
                    CollectionView.PreparedItems.Add(item.Key, item.Value);
                }
                availablePreparedItems.Clear();

                if (nextItemTop >= inRect.Bottom)// 上一次布局的是否依旧填满
                {
                    return absDelta;
                }

                LayoutInfor baselineInfor = new LayoutInfor()
                {
                    StartBounds = new Rect(0, nextItemTop, 0, 0),
                    StartItem = NSIndexPath.FromRowSection(OldPreparedItems.EndItem.Row + 1, OldPreparedItems.EndItem.Section)
                };
                var top = nextItemTop;
                for (var section = baselineInfor.StartItem.Section; section < numberOfSections && availableHeight >= 0; section++)
                {
                    int numberOfRows = CollectionView.NumberOfItemsInSection(section);
                    int row = 0;
                    if (section == baselineInfor.StartItem.Section)
                        row = baselineInfor.StartItem.Row;
                    for (; row < numberOfRows && availableHeight >= 0; row++)
                    {
                        var indexPath = NSIndexPath.FromRowSection(row, section);
                        var (viewHolder, bounds) = MeasureItem(inRect, inRect.Width, top, Edge.Top, indexPath, availablePreparedItems);
                        if (viewHolder != null)
                        {
                            //here we can change item's size
                            CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, indexPath, viewHolder, Edge.Top);
                            bounds = viewHolder.ItemBounds;
                            CollectionView.PreparedItems.Add(indexPath, viewHolder);
                        }
                        availableHeight -= bounds.Height;
                        top += bounds.Height;
                    }
                }
            }
            else//加载上面的
            {
                /*
                 * |
                 * v 
                 */
                if (OldPreparedItems.StartBounds.Top <= inRect.Top)
                {
                    double nextItemTop = 0;
                    foreach (var item in availablePreparedItems)
                    {
                        //here we can change item's size
                        CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, item.Key, item.Value, Edge.Top);
                        if (nextItemTop == 0)
                        {
                            nextItemTop = item.Value.ItemBounds.Bottom;
                        }
                        else
                        {
                            item.Value.ItemBounds.Top = nextItemTop;
                            nextItemTop = item.Value.ItemBounds.Bottom;
                        }
                        CollectionView.PreparedItems.Add(item.Key, item.Value);
                    }
                    availablePreparedItems.Clear();
                    return absDelta;
                }

                LayoutInfor baselineInfor = new LayoutInfor()
                {
                    EndBounds = new Rect(0, 0, 0, OldPreparedItems.StartBounds.Top),
                    EndItem = NSIndexPath.FromRowSection(OldPreparedItems.StartItem.Row - 1, OldPreparedItems.StartItem.Section)
                };

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
                        var (viewHolder, bounds) = MeasureItem(inRect, inRect.Width, bottom, Edge.Bottom, indexPath, availablePreparedItems);
                        if (viewHolder != null)
                        {
                            //here we can change item's size
                            CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, indexPath, viewHolder, Edge.Bottom);
                            bounds = viewHolder.ItemBounds;
                            tempOrderedPreparedItems.Add(new KeyValuePair<NSIndexPath, MAUICollectionViewViewHolder>(indexPath, viewHolder));
                        }
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
                {
                    double nextItemTop = 0;
                    //here we can change item's size
                    CollectionView.Source?.DidPrepareItem?.Invoke(CollectionView, item.Key, item.Value, Edge.Top);
                    if (nextItemTop == 0)
                    {
                        nextItemTop = item.Value.ItemBounds.Bottom;
                    }
                    else
                    {
                        item.Value.ItemBounds.Top = nextItemTop;
                        nextItemTop = item.Value.ItemBounds.Bottom;
                    }
                    CollectionView.PreparedItems.Add(item.Key, item.Value);
                }
                availablePreparedItems.Clear();
            }
            return absDelta - availableHeight;
        }

        (MAUICollectionViewViewHolder viewHolder, Rect bounds) MeasureItem(Rect inRect, double constrainedWidth, double baseline, Edge edge, NSIndexPath indexPath, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableViewHolders)
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

                var bounds = new Rect(0, edge == Edge.Top ? baseline : baseline - measureSize.Height, constrainedWidth, measureSize.Height);

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
                itemsOffset += CollectionView.ItemCountInRange(lastPreparedItem.Key, targetIndexPath) * lastPreparedItem.Value.ItemBounds.Height;
                BaseLineItemUsually = new LayoutInfor()
                {
                    EndBounds = new Rect(0, 0, 0, CollectionView.ScrollY + itemsOffset + CollectionView.Bounds.Height),
                    EndItem = targetIndexPath
                };
                CollectionView.ScrollToAsync(0, CollectionView.ScrollY + itemsOffset, false);
            }
            else if (targetIndexPath < firstPreparedItem.Key)//往上加载, 目标Item在可见区域顶部, 由顶部向下布局
            {
                //Using proportional calculations is more reasonable than calculating based on individual item heights, avoid negative numbers.
                var itemsCountFromTargetToFirstPrepared = CollectionView.ItemCountInRange(targetIndexPath, firstPreparedItem.Key) + 1;
                var itemsCountFromFirstToFirstPrepared = CollectionView.ItemCountInRange(NSIndexPath.FromRowSection(0,0), firstPreparedItem.Key) + 1;
                var distanceFromTargetToFirstPrepared = (firstPreparedItem.Value.ItemBounds.Top - StartBoundsCache[0].Top) * itemsCountFromTargetToFirstPrepared / itemsCountFromFirstToFirstPrepared + (CollectionView.ScrollY - firstPreparedItem.Value.ItemBounds.Top);
                BaseLineItemUsually = new LayoutInfor()
                {
                    StartBounds = new Rect(0, CollectionView.ScrollY - distanceFromTargetToFirstPrepared, 0, 0),
                    StartItem = targetIndexPath
                };
                CollectionView.ScrollToAsync(0, CollectionView.ScrollY - distanceFromTargetToFirstPrepared, false);
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
                                BaseLineItemUsually = new LayoutInfor()
                                {
                                    StartBounds = new Rect(0, targetBounds.Top, 0, 0),
                                    StartItem = visibleFirst
                                };
                                isScrollToDirectly = true;
                                CollectionView.ScrollToAsync(0, targetBounds.Top, false);
                            }
                        }
                        else
                        {
                            var targetTop = CollectionView.PreparedItems[visibleFirst].ItemBounds.Bottom + CollectionView.ItemCountInRange(lastCache, visibleFirst) * EstimateAverageHeight();

                            BaseLineItemUsually = new LayoutInfor()
                            {
                                StartBounds = new Rect(0, targetTop, 0, 0),
                                StartItem = visibleFirst
                            };
                            isScrollToDirectly = true;
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
                        BaseLineItemUsually = new LayoutInfor()
                        {
                            StartBounds = new Rect(0, StartBoundsCache[0].Top, 0, 0),
                            StartItem = firstItem
                        };
                        isScrollToDirectly = true;
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
                if (first > indexPath) end = -(CollectionView.ItemCountInRange(indexPath, first)+1);
                else if (last < indexPath) end = CollectionView.ItemCountInRange(first, indexPath)+1;
                var anim = new Animation((v) =>
                {
                    var target = CollectionView.NextItem(first, (int)v);
                    if(target.Row < 0)
                    {

                    }
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
                    return CollectionView.PreparedItems[indexPath].ItemBounds.Height;
                }
                else
                {
                    var reuseIdentifier = CollectionView.Source.ReuseIdForItem(CollectionView, indexPath);

                    var itemHeight = CollectionView.Source.HeightForItem(CollectionView, indexPath);
                    if (itemHeight == MAUICollectionViewViewHolder.AutoSize)
                    {
                        var (v, bounds) = MeasureItem(CollectionView.Bounds, CollectionView.Bounds.Width, 5000, Edge.Top, indexPath, new());
                        itemHeight = bounds.Height;
                    }
                    return itemHeight;
                }
            }
        }

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
                        var count = CollectionView.ItemCountInRange(indexPath, itemIndexPath) + 1;
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
    }
}
