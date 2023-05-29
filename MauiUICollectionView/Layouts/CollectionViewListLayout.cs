namespace MauiUICollectionView.Layouts
{
    public class CollectionViewListLayout : CollectionViewLayout
    {
        public CollectionViewListLayout(TableView collectionView) : base(collectionView)
        {
        }

        /// <summary>
        /// 行间距
        /// </summary>
        public double InterItemSpacing = 0;

        public override void ArrangeContents()
        {
            Size boundsSize = CollectionView.Bounds.Size;
            var contentOffset = CollectionView.ScrollY; //ContentOffset.Y;
            Rect visibleBounds = new Rect(0, contentOffset, boundsSize.Width, boundsSize.Height);

            if (CollectionView.TableHeaderView != null)
            {
                CollectionView.LayoutChild(CollectionView.TableHeaderView.ContentView, new Rect(0, CollectionView.TableHeaderView.PositionInLayout.Y, visibleBounds.Width, CollectionView.TableHeaderView.ContentView.DesiredSize.Height));
            }

            // layout sections and rows
            foreach (var cell in CollectionView._cachedCells)
                CollectionView.LayoutChild(cell.Value.ContentView, new Rect(0, cell.Value.PositionInLayout.Y, visibleBounds.Width, cell.Value.ContentView.DesiredSize.Height));


            if (CollectionView.TableFooterView != null)
            {
                CollectionView.LayoutChild(CollectionView.TableFooterView.ContentView, new Rect(0, CollectionView.TableFooterView.PositionInLayout.Y, visibleBounds.Width, CollectionView.TableFooterView.ContentView.DesiredSize.Height));
            }

            foreach (TableViewViewHolder cell in CollectionView._reusableCells)
            {
                CollectionView.LayoutChild(cell.ContentView, new Rect(0, -3000, cell.ContentView.DesiredSize.Width, cell.ContentView.DesiredSize.Height));
            }
        }

        List<NSIndexPath> needRemoveCell = new List<NSIndexPath>();

        /// <summary>
        /// 存储已经显示的Row的行高, 用于估计未显示的行. 这种预估默认是存储同
        /// </summary>
        public Dictionary<string, double> EstimatedRowHeightPro = new Dictionary<string, double>();
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
            if (CollectionView.TableHeaderView != null)
            {
                var _tableHeaderViewH = CollectionView.MeasureChild(CollectionView.TableHeaderView.ContentView, tableViewWidth, double.PositiveInfinity).Request.Height;
                CollectionView.TableHeaderView.PositionInLayout = new Point(0, 0);
                tableHeight += _tableHeaderViewH;
            }

            // 需要重新布局后, cell会变动, 先将之前显示的cell放入可供使用的cell字典
            Dictionary<NSIndexPath, TableViewViewHolder> availableCells = new();
            foreach (var cell in CollectionView._cachedCells)
                availableCells.Add(cell.Key, cell.Value);
            int numberOfSections = CollectionView._sections.Count;
            CollectionView._cachedCells.Clear();

            //复用是从_reusableCells获取的, 需要让不可见的先回收
            var tempCells = availableCells.ToList();
            var scrollOffset = CollectionView.scrollOffset;
            if (scrollOffset > 0)//往上滑, 上面的需要回收
            {
                foreach (var cell in tempCells)
                {
                    if (cell.Value.ContentView.DesiredSize.Height < scrollOffset)
                    {
                        needRemoveCell.Add(cell.Key);
                        scrollOffset -= cell.Value.ContentView.DesiredSize.Height;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else//往下滑, 下面的需要回收
            {
                scrollOffset = -scrollOffset;
                for (int i = tempCells.Count - 1; i >= 0; i--)
                {
                    var cell = tempCells[i];
                    if (cell.Value.ContentView.DesiredSize.Height < scrollOffset)
                    {
                        needRemoveCell.Add(cell.Key);
                        scrollOffset -= cell.Value.ContentView.DesiredSize.Height;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            foreach (var indexPath in needRemoveCell)
            {
                var cell = availableCells[indexPath];
                CollectionView._reusableCells.Add(cell);
                availableCells.Remove(indexPath);
            }

            var topExtandHeight = measureTimes < 3 ? 0 : CollectionView.ExtendHeight;
            var bottomExtandHeight = measureTimes < 3 ? 0 : measureTimes == 3 ? CollectionView.ExtendHeight * 2 : CollectionView.ExtendHeight;//第一次测量时, 可能顶部缺少空间, 不会创建那么多Extend, 我们在底部先创建好

            tempCells.Clear();
            needRemoveCell.Clear();
            scrollOffset = 0;//重置为0, 避免只更新数据时也移除cell

            for (int section = 0; section < numberOfSections; section++)
            {
                TableViewSection sectionRecord = CollectionView._sections[section];
                int numberOfRows = sectionRecord.numberOfRows;

                for (int row = 0; row < numberOfRows; row++)
                {
                    NSIndexPath indexPath = NSIndexPath.FromRowSection(row, section);
                    string cellType = CollectionView.Source.cellTypeForRowAtIndexPath(CollectionView, indexPath);
                    var sizeStrategy = CollectionView.Source.sizeStrategyForRowAtIndexPath(CollectionView, indexPath);
                    //尝试用之前测量的值或者预设值估计底部在哪
                    var rowMaybeTop = tableHeight;
                    var rowHeightWant = CollectionView.Source.heightForRowAtIndexPath(CollectionView, indexPath);

                    var rowMaybeHeight = sizeStrategy == SizeStrategy.FixedSize ? rowHeightWant : (sectionRecord._rowHeights[row] != 0 ? sectionRecord._rowHeights[row] : rowHeightWant);
                    var rowMaybeBottom = tableHeight + rowMaybeHeight;
                    //如果在可见区域, 就详细测量
                    if ((rowMaybeTop >= visibleBounds.Top - topExtandHeight && rowMaybeTop <= visibleBounds.Bottom + bottomExtandHeight)
                       || (rowMaybeBottom >= visibleBounds.Top - topExtandHeight && rowMaybeBottom <= visibleBounds.Bottom + bottomExtandHeight)
                       || (rowMaybeTop <= visibleBounds.Top - topExtandHeight && rowMaybeBottom >= visibleBounds.Bottom + bottomExtandHeight))
                    {
                        //获取Cell, 优先获取之前已经被显示的, 这里假定已显示的数据没有变化
                        TableViewViewHolder cell = availableCells.ContainsKey(indexPath) ? availableCells[indexPath] : CollectionView.Source.cellForRowAtIndexPath(CollectionView, indexPath, false);

                        if ((rowMaybeTop >= visibleBounds.Top && rowMaybeTop <= visibleBounds.Bottom)
                       || (rowMaybeBottom >= visibleBounds.Top && rowMaybeBottom <= visibleBounds.Bottom)
                       || (rowMaybeTop <= visibleBounds.Top && rowMaybeBottom >= visibleBounds.Bottom))
                        {
                        }
                        else
                        {
                            //cell.PrepareForReuse();
                        }

                        if (cell != null)
                        {
                            //将Cell添加到正在显示的Cell字典
                            CollectionView._cachedCells[indexPath] = cell;
                            if (availableCells.ContainsKey(indexPath)) availableCells.Remove(indexPath);
                            //Cell是否是正在被选择的
                            cell.Highlighted = CollectionView._highlightedRow == null ? false : CollectionView._highlightedRow.IsEqual(indexPath);
                            cell.Selected = CollectionView._selectedRow == null ? false : CollectionView._selectedRow.IsEqual(indexPath);

                            //添加到ScrollView, 必须先添加才有测量值
                            if (!CollectionView.ContentView.Children.Contains(cell.ContentView))
                                CollectionView.AddSubview(cell.ContentView);
                            //测量高度
                            if (sizeStrategy == SizeStrategy.FixedSize)
                            {
                                cell.ContentView.HeightRequest = rowHeightWant;
                                var measureSize = CollectionView.MeasureChild(cell.ContentView, tableViewBoundsSize.Width, rowHeightWant).Request;
                                //sectionRecord._rowHeights[row] = rowHeightWant;
                            }
                            else if (sizeStrategy == SizeStrategy.MeasureSelf)
                            {
                                cell.ContentView.HeightRequest = -1; //避免之前的Cell被设置了固定值
                                var measureSize = CollectionView.MeasureChild(cell.ContentView, tableViewBoundsSize.Width, double.PositiveInfinity).Request;
                                sectionRecord._rowHeights[row] = measureSize.Height;
                            }
                            else if (sizeStrategy == SizeStrategy.MeasureSelfGreaterThanMinFixedSize)
                            {
                                cell.ContentView.HeightRequest = -1; //避免之前的Cell被设置了固定值
                                cell.ContentView.MinimumHeightRequest = rowHeightWant;
                                var measureSize = CollectionView.MeasureChild(cell.ContentView, tableViewBoundsSize.Width, double.PositiveInfinity).Request;
                                sectionRecord._rowHeights[row] = measureSize.Height;
                            }
                            else if (sizeStrategy == SizeStrategy.MeasureSelfGreaterThanMinFixedSize)
                            {
                                cell.ContentView.HeightRequest = -1; //避免之前的Cell被设置了固定值
                                cell.ContentView.MaximumHeightRequest = rowHeightWant;
                                var measureSize = CollectionView.MeasureChild(cell.ContentView, tableViewBoundsSize.Width, rowHeightWant).Request;
                                sectionRecord._rowHeights[row] = measureSize.Height;
                            }

                            if (!EstimatedRowHeightPro.ContainsKey(cellType))
                            {
                                EstimatedRowHeightPro.Add(cellType, sectionRecord._rowHeights[row]);
                            }
                            else
                            {
                                if (EstimatedRowHeightPro[cellType] < sectionRecord._rowHeights[row])
                                {
                                    EstimatedRowHeightPro[cellType] = sectionRecord._rowHeights[row];
                                }
                            }

                            cell.PositionInLayout = new Point(0, tableHeight);

                            tableHeight += (sizeStrategy == SizeStrategy.FixedSize ? rowHeightWant : sectionRecord._rowHeights[row]);
                        }
                    }
                    else//如果不可见
                    {
                        if (availableCells.ContainsKey(indexPath))
                        {
                            var cell = availableCells[indexPath];
                            if (cell.ReuseIdentifier != default)
                            {
                                CollectionView._reusableCells.Add(cell);
                                availableCells.Remove(indexPath);
                            }
                            cell.PrepareForReuse();
                        }
                        tableHeight = rowMaybeBottom;
                    }
                }
            }

            // 重新测量后, 需要显示的已经存入缓存的字典, 剩余的放入可重用列表
            foreach (TableViewViewHolder cell in availableCells.Values)
            {
                if (cell.ReuseIdentifier != default)
                {
                    if (CollectionView._reusableCells.Count > 3)
                    {
                        cell.ContentView.RemoveFromSuperview();
                    }
                    else
                        CollectionView._reusableCells.Add(cell);
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
            var allCachedCells = CollectionView._cachedCells.Values;
            foreach (TableViewViewHolder cell in CollectionView._reusableCells)
            {
                if (cell.ContentView.Frame.IntersectsWith(visibleBounds) && !allCachedCells.Contains(cell))
                {
                    //cell.RemoveFromSuperview();
                }
            }

            //表尾的View是确定的, 我们可以直接测量
            if (CollectionView.TableFooterView != null)
            {
                var footMeasureSize = CollectionView.MeasureChild(CollectionView.TableFooterView.ContentView, tableViewBoundsSize.Width, double.PositiveInfinity).Request;
                CollectionView.TableFooterView.PositionInLayout = new Point(0, tableHeight);
                tableHeight += footMeasureSize.Height;
            }

            return new Size(tableViewBoundsSize.Width, tableHeight);
        }
    }
}
