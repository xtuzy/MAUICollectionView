using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace MauiUICollectionView.Layouts
{
    /// <summary>
    /// layout content.
    /// </summary>
    public abstract class CollectionViewLayout : IDisposable
    {
        public CollectionViewLayout(MAUICollectionView collectionView)
        {
            this.CollectionView = collectionView;
            AnimationManager = new LayoutAnimationManager(collectionView);
        }

        /// <summary>
        /// manage scroll and operate animation.
        /// </summary>
        public ILayoutAnimationManager AnimationManager { get; set; }

        /// <summary>
        /// Store all operate
        /// </summary>
        public List<OperateItem> Updates = new();

        private MAUICollectionView _collectionView;
        public MAUICollectionView CollectionView
        {
            get { return _collectionView; }
            private set => _collectionView = value;
        }

        /// <summary>
        /// scroll direction.
        /// </summary>
        public virtual ItemsLayoutOrientation ScrollDirection
        {
            get; set;
        } = ItemsLayoutOrientation.Vertical;

        /// <summary>
        /// when operating, it is true.
        /// </summary>
        protected bool IsOperating = false;

        /// <summary>
        /// Arrange Header, Items and Footer. They will be arranged according to <see cref="MAUICollectionViewViewHolder.BoundsInLayout"/>
        /// </summary>
        public virtual void ArrangeContents()
        {
            CollectionView.Source?.WillArrange?.Invoke(CollectionView);
            AnimationManager.Run(CollectionView.IsScrolling, IsOperating);
            
            IsOperating = false;//disappear动画结束
            CollectionView.IsScrolling = false;

            if (CollectionView.HeaderView != null)
            {
                CollectionView.HeaderView.ArrangeSelf(CollectionView.HeaderView.BoundsInLayout);
            }

            // layout sections and rows
            foreach (var cell in CollectionView.PreparedItems)
            {
                if (cell.Value == CollectionView.DragedItem)
                    cell.Value.ArrangeSelf(cell.Value.DragBoundsInLayout);
                else
                    cell.Value.ArrangeSelf(cell.Value.BoundsInLayout);

                //避免Measure时处理错误回收了可见的Item
                if (CollectionView.ReusableViewHolders.Contains(cell.Value))
                {
                    CollectionView.ReusableViewHolders.Remove(cell.Value);
                }
            }

            foreach (var item in CollectionView.ReusableViewHolders)
            {
                //if (item.Opacity != 0)
                //    item.Opacity = 0;
                item.ArrangeSelf(new Rect(0, -1000, item.Width, item.Height));
            }

            if (CollectionView.FooterView != null)
            {
                CollectionView.FooterView.ArrangeSelf(CollectionView.FooterView.BoundsInLayout);
            }
        }

        public LayoutInfor OldPreparedItems;

        public class LayoutInfor
        {
            public NSIndexPath StartItem;
            public NSIndexPath EndItem;
            public Rect StartBounds; 
            public Rect EndBounds; 
        }

        /// <summary>
        /// Cache height of item that have same Id, it be use for predict height.
        /// </summary>
        public Dictionary<string, double> MeasuredSelfHeightCacheForReuse = new Dictionary<string, double>();

        /// <summary>
        /// Measure size of Header, Items and Footer. It will load <see cref="MeasureHeader"/>, <see cref="MeasureItems"/>, <see cref="MeasureFooter"/>.
        /// </summary>
        /// <param name="tableViewWidth">visible width, it may be not accurate.</param>
        /// <param name="tableViewHeight">visible height</param>
        /// <returns></returns>
        public virtual Size MeasureContents(double tableViewWidth, double tableViewHeight)
        {
            Debug.WriteLine($"Measure ScrollY={CollectionView.ScrollY}");
            if (Updates.Count > 0)
            {
                IsOperating = true;
            }

            //tableView自身的大小
            Size tableViewBoundsSize = new Size(tableViewWidth, tableViewHeight);
            //Debug.WriteLine(tableViewBoundsSize);
            //当前可见区域在ContentView中的位置
            Rect visibleBounds = new Rect(0, CollectionView.ScrollY, tableViewBoundsSize.Width, tableViewBoundsSize.Height);
            double tableHeight = 0;
            //顶部和底部扩展的高度, 头2次布局不扩展, 防止初次显示计算太多item
            var topExtandHeight = CollectionView.HeightExpansion;
            var bottomExtandHeight = CollectionView.HeightExpansion;//第一次测量时, 可能顶部缺少空间, 不会创建那么多Extend, 我们在底部先创建好
            Rect layoutItemsInRect = Rect.FromLTRB(visibleBounds.Left, visibleBounds.Top - topExtandHeight, visibleBounds.Right, visibleBounds.Bottom + bottomExtandHeight);

            /* 
             * Header
             */
            tableHeight += MeasureHeader(0, visibleBounds.Width);

            // PreparedItems will be update, so use a local variable to store old prepareditems, IndexPath still is old.
            Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableViewHolders = new();
            foreach (var cell in CollectionView.PreparedItems)
                availableViewHolders.Add(cell.Key, cell.Value);
            CollectionView.PreparedItems.Clear();

            // ToList wil be sortable, we can get first or end item
            var tempOrderedCells = availableViewHolders.ToList();
            tempOrderedCells.Sort((x, y) =>
            {
                return x.Key.Compare(y.Key);
            });
            /*
             * Store old indexpath of prepareditem, maybe we need use it
             */
            OldPreparedItems = new LayoutInfor();
            if (tempOrderedCells.Count > 0)
            {
                
                var start = tempOrderedCells[0];
                OldPreparedItems.StartItem= start.Key;
                OldPreparedItems.StartBounds= start.Value.BoundsInLayout;
                var end = tempOrderedCells[tempOrderedCells.Count - 1];
                OldPreparedItems.EndItem = end.Key;
                OldPreparedItems.EndBounds = end.Value.BoundsInLayout;
                //Debug.WriteLine($"last start={start.Key} end={end.Key}");
            }

            /*
             * Recycle: according offset to recycle invisible items
             */
            var needRecycleCell = new List<NSIndexPath>();
            var scrollOffset = CollectionView.scrollOffset;
            if (scrollOffset > 0)//when swipe up, recycle top
            {
                foreach (var cell in tempOrderedCells)
                {
                    if (cell.Value.BoundsInLayout.Bottom < layoutItemsInRect.Top)
                    {
                        needRecycleCell.Add(cell.Key);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else if (scrollOffset < 0)//when swipe down, recycle bottom
            {
                for (int i = tempOrderedCells.Count - 1; i >= 0; i--)
                {
                    var cell = tempOrderedCells[i];
                    if (cell.Value.BoundsInLayout.Top > layoutItemsInRect.Bottom)
                    {
                        needRecycleCell.Add(cell.Key);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            foreach (var indexPath in needRecycleCell)
            {
                var cell = availableViewHolders[indexPath];
                if (cell == CollectionView.DragedItem)//don't recycle DragedItem
                {
                    continue;
                }
                CollectionView.RecycleViewHolder(cell);
                availableViewHolders.Remove(indexPath);
            }

            tempOrderedCells.Clear();
            needRecycleCell.Clear();
            scrollOffset = 0;//重置为0, 避免只更新数据时也移除cell

            /*
             * Remove
             */
            if (IsOperating)
            {
                for (int index = Updates.Count - 1; index >= 0; index--)
                {
                    var update = Updates[index];
                    if (update.operateType == OperateItem.OperateType.Remove)//需要移除的先移除, move后的IndexPath与之相同
                    {
                        if (availableViewHolders.ContainsKey(update.source))
                        {
                            var viewHolder = availableViewHolders[update.source];
                            availableViewHolders.Remove(update.source);

                            if (update.operateAnimate)
                            {
                                viewHolder.Operation = (int)OperateItem.OperateType.Remove;
                            }
                            else
                            {
                                viewHolder.Operation = (int)OperateItem.OperateType.RemoveNow;
                            }
                            AnimationManager.AddOperatedItem(viewHolder);

                            Updates.RemoveAt(index);
                        }
                    }
                    else if (update.operateType == OperateItem.OperateType.Update)
                    {
                        //更新的我觉得不需要动画
                        if (availableViewHolders.ContainsKey(update.source))
                        {
                            var viewHolder = availableViewHolders[update.source];
                            viewHolder.Operation = (int)OperateItem.OperateType.Update;
                            AnimationManager.AddOperatedItem(viewHolder);
                            availableViewHolders.Remove(update.source);
                            Updates.RemoveAt(index);
                        }
                    }
                }
            }

            /*
             * update OldVisibleIndexPath and OldPreparedItm index
             */
            if (IsOperating)
            {
                var oldVisibleIndexPath = new List<NSIndexPath>();
                var oldpreparedIndexPath = new NSIndexPath[2] { OldPreparedItems.StartItem, OldPreparedItems.EndItem };
                for (int index = Updates.Count - 1; index >= 0; index--)
                {
                    var update = Updates[index];

                    if (update.operateType == OperateItem.OperateType.Move)
                    {
                        if (VisibleIndexPath.Contains(update.source))
                        {
                            VisibleIndexPath.Remove(update.source);
                            oldVisibleIndexPath.Add(update.target);
                        }
                        if (update.source.Equals(oldpreparedIndexPath[0]))
                        {
                            oldpreparedIndexPath[0] = null;
                            OldPreparedItems.StartItem = update.target;
                        }
                        if (update.source.Equals(oldpreparedIndexPath[1]))
                        {
                            oldpreparedIndexPath[1] = null;
                            OldPreparedItems.EndItem = update.target;
                        }
                    }
                }
                VisibleIndexPath.AddRange(oldVisibleIndexPath);
            }
            LastVisibleIndexPath = new List<NSIndexPath>(VisibleIndexPath);
            VisibleIndexPath.Clear();

            /*
             * Move: if item in last PreparedItems, we update it's IndexPath, and reuse it directly
             */
            if (IsOperating)
            {
                //move的需要获取在之前可见区域的viewHolder, 更新indexPath为最新的, 然后进行动画.
                Dictionary<NSIndexPath, MAUICollectionViewViewHolder> tempAvailableCells = new();//move修改旧的IndexPath,可能IndexPath已经存在, 因此使用临时字典存储
                for (int index = Updates.Count - 1; index >= 0; index--)
                {
                    var update = Updates[index];

                    if (update.operateType == OperateItem.OperateType.Move)//移动的且显示的直接替换
                    {
                        if (availableViewHolders.ContainsKey(update.source))
                        {
                            var oldViewHolder = availableViewHolders[update.source];
                            if (!oldViewHolder.Equals(CollectionView.DragedItem) //Drag的不需要动画, 因为自身会在Arrange中移动
                            && update.operateAnimate)//move的可以是没有动画但位置移动的
                            {
                                oldViewHolder.Operation = (int)OperateItem.OperateType.Move;
                                AnimationManager.AddOperatedItem(oldViewHolder);
                            }
                            if (!update.operateAnimate)
                            {
                                oldViewHolder.Operation = (int)OperateItem.OperateType.MoveNow;
                                AnimationManager.AddOperatedItem(oldViewHolder);
                            }
                            availableViewHolders.Remove(update.source);
                            if (availableViewHolders.ContainsKey(update.target))
                                tempAvailableCells.Add(update.target, oldViewHolder);
                            else
                                availableViewHolders.Add(update.target, oldViewHolder);
                            oldViewHolder.IndexPath = update.target;
                            Updates.RemoveAt(index);
                        }
                    }
                }
                foreach (var item in tempAvailableCells)
                    availableViewHolders.Add(item.Key, item.Value);
            }

            /*
             * Measure Items
             */
            tableHeight += MeasureItems(tableHeight, layoutItemsInRect, visibleBounds, availableViewHolders);

            //record visible item
            foreach (var item in CollectionView.PreparedItems)
            {
                if (item.Value.BoundsInLayout.IntersectsWith(visibleBounds))
                {
                    VisibleIndexPath.Add(item.Key);
                }
            }

            /*
             * Select
             */
            foreach (var item in CollectionView.PreparedItems)
            {
                item.Value.Selected = CollectionView.SelectedItems.Contains(item.Key);
            }

            /*
             * Move: if item no ViewHolder at last measure, at here can get ViewHolder
             */
            if (IsOperating)
            {
                for (int index = Updates.Count - 1; index >= 0; index--)
                {
                    var update = Updates[index];

                    if (update.operateType == OperateItem.OperateType.Move)
                    {
                        if (CollectionView.PreparedItems.ContainsKey(update.target))
                        {
                            var viewHolder = CollectionView.PreparedItems[update.target];
                            if (!viewHolder.Equals(CollectionView.DragedItem) //Drag的不需要动画, 因为自身会在Arrange中移动
                            && update.operateAnimate)//move的可以是没有动画但位置移动的
                            {
                                viewHolder.OldBoundsInLayout = update.source== update.target && viewHolder.OldBoundsInLayout==Rect.Zero ? RectForItem(NSIndexPath.FromRowSection(update.source.Row - update.moveCount, update.source.Section)) : RectForItem(update.source); // try get old bounds
                                viewHolder.Operation = (int)OperateItem.OperateType.Move;
                                AnimationManager.AddOperatedItem(viewHolder);
                            }
                            if (!update.operateAnimate)
                            {
                                viewHolder.Operation = (int)OperateItem.OperateType.MoveNow;
                                AnimationManager.AddOperatedItem(viewHolder);
                            }
                            Updates.RemoveAt(index);
                        }
                    }
                }
            }
            /*
             * 标记insert, 添加到动画
             */
            if (IsOperating)
            {
                var insertList = new Dictionary<NSIndexPath, OperateItem>();
                foreach (var item in Updates)
                {
                    if (item.operateType == OperateItem.OperateType.Insert)
                    {
                        insertList.Add(item.source, item);
                    }
                }
                foreach (var item in CollectionView.PreparedItems)
                {
                    if (insertList.ContainsKey(item.Key))//插入的数据是原来没有的, 但其会与move的相同, 因为插入的位置原来的item需要move, 所以move会对旧的item处理
                    {
                        item.Value.Operation = (int)OperateItem.OperateType.Insert;
                        AnimationManager.AddOperatedItem(item.Value);
                    }
                }
                insertList.Clear();
            }
            Updates.Clear();

            // 重新测量后, 需要显示的已经存入缓存的字典, 剩余的放入可重用列表
            foreach (MAUICollectionViewViewHolder cell in availableViewHolders.Values)
            {
                if (cell == CollectionView.DragedItem)
                {
                    CollectionView.PreparedItems.Add(cell.IndexPath, cell);
                    continue;
                }

                if (cell.ReuseIdentifier != default && 
                    cell.Operation != (int)OperateItem.OperateType.Move)//avoid recycle will invisible item, we recycle it in animation
                {
                    CollectionView.RecycleViewHolder(cell);
                }else if(cell.Operation == (int)OperateItem.OperateType.Move)
                {

                }else
                {
                    cell.RemoveFromSuperview();
                }
            }

            /*
             * Footer
             */
            tableHeight += MeasureFooter(tableHeight, layoutItemsInRect.Width);

            //Debug.WriteLine($"ChildCount={CollectionView.ContentView.Children.Count} PreparedItem={CollectionView.PreparedItems.Count} RecycleCount={CollectionView.ReusableViewHolders.Count}");
            //Debug.WriteLine("TableView Content Height:" + tableHeight);
            return new Size(tableViewBoundsSize.Width, tableHeight);
        }

        protected virtual double MeasureHeader(double top, double widthConstraint)
        {
            //表头的View是确定的, 我们可以直接测量
            if (CollectionView.HeaderView != null)
            {
                var measuredSize = CollectionView.HeaderView.MeasureSelf(widthConstraint, double.PositiveInfinity).Request;
                CollectionView.HeaderView.BoundsInLayout = new Rect(0, top, widthConstraint, measuredSize.Height);
                return measuredSize.Height;
            }
            return 0;
        }

        /// <summary>
        /// current Visible items, it is different with <see cref="MAUICollectionView.PreparedItems"/>
        /// </summary>
        public List<NSIndexPath> VisibleIndexPath { get; protected set; } = new List<NSIndexPath>();
        public List<NSIndexPath> LastVisibleIndexPath { get; protected set; } = new List<NSIndexPath>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="top"></param>
        /// <param name="inRect">rect for prepared items</param>
        /// <param name="visibleRect">rect for visible items</param>
        /// <param name="availableCells"></param>
        /// <returns></returns>
        protected abstract double MeasureItems(double top, Rect inRect, Rect visibleRect, Dictionary<NSIndexPath, MAUICollectionViewViewHolder> availableCells);

        protected virtual double MeasureFooter(double top, double widthConstraint)
        {
            //表尾的View是确定的, 我们可以直接测量
            if (CollectionView.FooterView != null)
            {
                var measuredSize = CollectionView.FooterView.MeasureSelf(widthConstraint, double.PositiveInfinity).Request;
                CollectionView.FooterView.BoundsInLayout = new Rect(0, top, widthConstraint, measuredSize.Height);
                return measuredSize.Height;
            }
            return 0;
        }

        /// <summary>
        /// Get item at point.
        /// </summary>
        /// <param name="point">the point in Content or CollectionView</param>
        /// <param name="baseOnContent"> specify point is base on Content or CollectionView</param>
        /// <returns></returns>
        public virtual NSIndexPath ItemAtPoint(Point point, bool baseOnContent = true)
        {
            if (!baseOnContent)
            {
                var contentOffset = CollectionView.ScrollY;
                point.Y = point.Y + contentOffset;//convert to base on content
            }

            foreach (var item in CollectionView.PreparedItems)
            {
                if(item.Value.BoundsInLayout.Contains(point))
                {
                    return item.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// Get rect of item. this method maybe be slow.
        /// </summary>
        /// <returns></returns>
        public virtual Rect RectForItem(NSIndexPath indexPath) 
        {
            if (CollectionView.PreparedItems.ContainsKey(indexPath))
            {
                return CollectionView.PreparedItems[indexPath].BoundsInLayout;
            }
            return Rect.Zero;
        }

        public virtual void ScrollTo(NSIndexPath indexPath, ScrollPosition scrollPosition, bool animated)
        {
            var rect = RectForItem(indexPath);
            switch (scrollPosition)
            {
                case ScrollPosition.None:
                case ScrollPosition.Top:
                    CollectionView.ScrollToAsync(0, rect.Top, animated);
                    break;
                case ScrollPosition.Middle:
                    CollectionView.ScrollToAsync(0, rect.Y + rect.Height / 2, animated);
                    break;
                case ScrollPosition.Bottom:
                    CollectionView.ScrollToAsync(0, rect.Bottom, animated);
                    break;
            }
        }

        /// <summary>
        /// Get total height of items. we also measure invisible items after visible item, because it be used to adjust ScrollY for stay don't move visible items when Remove or Insert.
        /// Notice, these items' section should be same.
        /// </summary>
        /// <param name="indexPath"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public abstract double EstimateHeightForItems(NSIndexPath indexPath, int count);

        public void Dispose()
        {
            AnimationManager.Dispose();
            _collectionView = null;
        }
    }
}