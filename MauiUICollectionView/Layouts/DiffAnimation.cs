namespace MauiUICollectionView.Layouts
{
    /// <summary>
    /// Analysis data change, and 
    /// </summary>
    public class DiffAnimation : IDisposable
    {
        public class Operate
        {
            public OperateItem.OperateType OperateType;
            public int OperateCount;
            public NSIndexPath Source;
            public NSIndexPath Target;
            public (NSIndexPath lastIndex, NSIndexPath currentIndex, bool moved) BaselineItem;
        }

        Operate Operation;

        public Dictionary<NSIndexPath, MAUICollectionViewViewHolder> LastViewHolders = new Dictionary<NSIndexPath, MAUICollectionViewViewHolder>();
        public Dictionary<NSIndexPath, MAUICollectionViewViewHolder> CurrentViewHolders = new Dictionary<NSIndexPath, MAUICollectionViewViewHolder>();
        public void RecordLastViewHolder(Dictionary<NSIndexPath, MAUICollectionViewViewHolder> lastViewHolders, CollectionViewLayout.LayoutInfor visibleItems)
        {
            foreach (var item in lastViewHolders)
            {
                if (item.Key.Compare(visibleItems.StartItem) >= 0 &&
                    item.Key.Compare(visibleItems.EndItem) <= 0)
                    LastViewHolders.Add(item.Key, item.Value);
            }
        }

        /// <summary>
        /// no change return old, remove return null, move return current
        /// </summary>
        /// <param name="oldIndexPath"></param>
        /// <returns></returns>
        public NSIndexPath TryGetCurrentIndexPath(NSIndexPath oldIndexPath)
        {
            if (Operation.OperateType == OperateItem.OperateType.Remove)
            {
                var startRemovedItem = Operation.Source;
                var endRemovedItem = NSIndexPath.FromRowSection(startRemovedItem.Row + Operation.OperateCount - 1, startRemovedItem.Section);
                if (oldIndexPath.IsInRange(startRemovedItem, endRemovedItem))
                {
                    return null;
                }

                if (oldIndexPath.Section == startRemovedItem.Section)
                {
                    if (oldIndexPath.Row > endRemovedItem.Row)
                    {
                        return NSIndexPath.FromRowSection(oldIndexPath.Row - Operation.OperateCount, oldIndexPath.Section);
                    }
                }

                return oldIndexPath;
            }
            else if (Operation.OperateType == OperateItem.OperateType.Insert)
            {
                if (oldIndexPath.Section == Operation.Source.Section)//same section
                {
                    if (oldIndexPath.Compare(Operation.Source) >= 0)//after insert position
                    {
                        return NSIndexPath.FromRowSection(oldIndexPath.Row + Operation.OperateCount, oldIndexPath.Section);
                    }
                    else
                    {
                        return oldIndexPath;
                    }
                }
                else
                {
                    return oldIndexPath;
                }
            }
            return null;
        }

        public bool IsRemoved(NSIndexPath oldIndexPath)
        {
            if (Operation.OperateType == OperateItem.OperateType.Remove)
            {
                var startRemovedItem = Operation.Source;
                var endRemovedItem = NSIndexPath.FromRowSection(startRemovedItem.Row + Operation.OperateCount - 1, startRemovedItem.Section);
                if (oldIndexPath.IsInRange(startRemovedItem, endRemovedItem))
                {
                    return true;
                }
            }
            return false;
        }

        public void RecordCurrentViewHolder(Dictionary<NSIndexPath, MAUICollectionViewViewHolder> currentViewHolders, CollectionViewLayout.LayoutInfor visibleItems)
        {
            foreach (var item in currentViewHolders)
            {
                if (item.Key.Compare(visibleItems.StartItem) >= 0 &&
                    item.Key.Compare(visibleItems.EndItem) <= 0)
                    CurrentViewHolders.Add(item.Key, item.Value);
            }
        }

        ILayoutAnimationManager AnimationManager => CollectionView.ItemsLayout.AnimationManager;
        MAUICollectionView CollectionView;

        public DiffAnimation(Operate operation, MAUICollectionView collectionView)
        {
            Operation = operation;
            CollectionView = collectionView;
        }

        public void Analysis(bool isBeforeMeasure)
        {
            if (Operation.OperateType == OperateItem.OperateType.Remove)
            {
                var startRemovedItem = Operation.Source;
                var endRemovedItem = CollectionView.NextItem(Operation.Source, Operation.OperateCount - 1);
                if (endRemovedItem < LastViewHolders.First().Key ||
                    startRemovedItem > LastViewHolders.Last().Key)
                {
                    //移除的都是不可见的
                }
                else
                {
                    if (isBeforeMeasure)
                    {
                        foreach (var item in LastViewHolders)
                        {
                            if (item.Key.IsInRange(startRemovedItem, endRemovedItem))//removed
                            {
                                var viewHolder = item.Value;
                                viewHolder.Operation = (int)OperateItem.OperateType.Remove;
                                AnimationManager.AddOperatedItem(viewHolder);
                            }
                            else if (item.Key < Operation.BaselineItem.lastIndex)
                            {

                            }
                            else if (item.Key > Operation.BaselineItem.lastIndex)
                            {
                                if (startRemovedItem.IsInRange(Operation.BaselineItem.lastIndex, item.Key) &&
                                    endRemovedItem.IsInRange(Operation.BaselineItem.lastIndex, item.Key))//when removed items in baseline and item, item need move.
                                {
                                    var viewHolder = item.Value;
                                    viewHolder.Operation = (int)OperateItem.OperateType.Move;
                                    AnimationManager.AddOperatedItem(viewHolder);
                                }
                            }
                            else if (item.Key.Compare(Operation.BaselineItem.lastIndex) == 0)
                            {
                                if (Operation.BaselineItem.moved)
                                {
                                    var viewHolder = item.Value;
                                    viewHolder.Operation = (int)OperateItem.OperateType.Move;
                                    AnimationManager.AddOperatedItem(viewHolder);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in CurrentViewHolders)
                        {
                            // invisible to visible
                            if (!LastViewHolders.Values.Contains(item.Value))
                            {
                                var viewHolder = item.Value;
                                viewHolder.Operation = (int)OperateItem.OperateType.Move;

                                NSIndexPath emitateLastIndexPath;
                                if (Operation.BaselineItem.currentIndex > item.Key)
                                {
                                    emitateLastIndexPath = CollectionView.NextItem(item.Key, -Operation.OperateCount);
                                }
                                else
                                {
                                    emitateLastIndexPath = CollectionView.NextItem(item.Key, Operation.OperateCount);
                                }
                                var bounds = CollectionView.ItemsLayout.RectForItem(emitateLastIndexPath);//if delete other section's item
                                viewHolder.OldBoundsInLayout = new Rect(bounds.X, bounds.Y, viewHolder.BoundsInLayout.Width, viewHolder.BoundsInLayout.Height);

                                AnimationManager.AddOperatedItem(viewHolder);
                            }
                        }
                    }
                }
            }
            else if (Operation.OperateType == OperateItem.OperateType.Insert)
            {
                var startInsertItem = Operation.Source;
                var endInsertItem = CollectionView.NextItem(Operation.Source, Operation.OperateCount - 1);
                if (startInsertItem.Compare(Operation.BaselineItem.lastIndex) <= 0 || // don't move before baseline 
                    startInsertItem > LastViewHolders.Last().Key)
                {
                }
                else
                {
                    if (isBeforeMeasure)
                    {
                        foreach (var item in LastViewHolders)
                        {
                            // all moved
                            if (item.Key.Compare(startInsertItem) >= 0)//在插入位置后
                            {
                                var viewHolder = item.Value;
                                viewHolder.Operation = (int)OperateItem.OperateType.Move;
                                AnimationManager.AddOperatedItem(viewHolder);
                            }
                        }
                    }
                    else
                    {
                        //insert
                        foreach (var item in CurrentViewHolders)
                        {
                            if (item.Key.IsInRange(startInsertItem, endInsertItem))
                            {
                                var viewHolder = item.Value;
                                viewHolder.Operation = (int)OperateItem.OperateType.Insert;
                                AnimationManager.AddOperatedItem(viewHolder);
                            }
                        }

                        //visible to invisible
                        foreach (var item in LastViewHolders)
                        {
                            if (!CurrentViewHolders.Values.Contains(item.Value))
                            {
                                var viewHolder = item.Value;
                                viewHolder.Operation = (int)OperateItem.OperateType.Move;

                                NSIndexPath emitateLastIndexPath;

                                emitateLastIndexPath = CollectionView.NextItem(item.Key, Operation.OperateCount);

                                var bounds = CollectionView.ItemsLayout.RectForItem(emitateLastIndexPath);
                                if (bounds == viewHolder.BoundsInLayout && 
                                    viewHolder.OldBoundsInLayout != Rect.Zero)//if have have data(in extend items), pass
                                    continue;
                                viewHolder.OldBoundsInLayout = viewHolder.BoundsInLayout;
                                viewHolder.BoundsInLayout = new Rect(bounds.X, bounds.Y, viewHolder.OldBoundsInLayout.Width, viewHolder.OldBoundsInLayout.Height);
                            }
                        }
                    }
                }
            }
            else if (Operation.OperateType == OperateItem.OperateType.Move)
            {
            }
        }

        public void Dispose()
        {
            CollectionView = null;
            LastViewHolders.Clear();
            CurrentViewHolders.Clear();
        }
    }
}
