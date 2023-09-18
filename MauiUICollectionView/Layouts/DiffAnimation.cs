namespace Yang.MAUICollectionView.Layouts
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
            /// <summary>
            /// moved means item is moved, not only change index.
            /// </summary>
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
        /// no change return old, remove return null, move return current.
        /// Notice, it need be used after call <see cref="MAUICollectionView.ReloadDataCount"/>.
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
            else if (Operation.OperateType == OperateItem.OperateType.Move)
            {
                if (oldIndexPath.Compare(Operation.Source) == 0)
                {
                    return Operation.Target;
                }

                if (Operation.Source.Compare(Operation.Target) < 0)
                {
                    /*
                     * 0*****1*S***T*2
                     */
                    if (Operation.Source.Section == Operation.Target.Section)
                    {
                        if (oldIndexPath.IsInRange(Operation.Source, Operation.Target))
                        {
                            return NSIndexPath.FromRowSection(oldIndexPath.Row - 1, oldIndexPath.Section);
                        }
                        else
                        {
                            return oldIndexPath;
                        }
                    }
                    else
                    {
                        /*
                         * 0***S**1*****2**T***3
                         */
                        if (oldIndexPath.IsInRange(Operation.Source, Operation.Target))
                        {
                            if (oldIndexPath.Section == Operation.Source.Section)
                                return NSIndexPath.FromRowSection(oldIndexPath.Row - 1, oldIndexPath.Section);
                            else if (oldIndexPath.Compare(Operation.Target) == 0)
                            {
                                return NSIndexPath.FromRowSection(oldIndexPath.Row + 1, oldIndexPath.Section);
                            }
                            else
                            {
                                return oldIndexPath;
                            }
                        }
                        else if (oldIndexPath.Section == Operation.Target.Section)
                        {
                            return NSIndexPath.FromRowSection(oldIndexPath.Row + 1, oldIndexPath.Section);
                        }
                        else
                        {
                            return oldIndexPath;
                        }
                    }
                }
                else if (Operation.Source.Compare(Operation.Target) > 0)
                {
                    /*
                     * 0*****1*T***S*2
                     */
                    if (Operation.Source.Section == Operation.Target.Section)
                    {
                        if (oldIndexPath.IsInRange(Operation.Target, Operation.Source))
                        {
                            return NSIndexPath.FromRowSection(oldIndexPath.Row + 1, oldIndexPath.Section);
                        }
                        else
                        {
                            return oldIndexPath;
                        }
                    }
                    else
                    {
                        /*
                         * 0***T**1*****2**S***3
                         */
                        if (oldIndexPath.IsInRange(Operation.Target, Operation.Source))
                        {
                            if (oldIndexPath.Section == Operation.Source.Section)
                                return oldIndexPath;
                            else if (oldIndexPath.Compare(Operation.Target) == 0)
                            {
                                return NSIndexPath.FromRowSection(oldIndexPath.Row + 1, oldIndexPath.Section);
                            }
                            else
                            {
                                return oldIndexPath;
                            }
                        }
                        else if (oldIndexPath.Section == Operation.Source.Section)
                        {
                            return NSIndexPath.FromRowSection(oldIndexPath.Row - 1, oldIndexPath.Section);
                        }
                        else
                        {
                            return oldIndexPath;
                        }
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

        /// <summary>
        /// when delete, maybe some invisible items become visible, we need get bounds of they before remeasure, because we maybe can't get it after measure.
        /// </summary>
        public List<CollectionViewLayout.LayoutInfor> LastInvisibleItemsInfor;

        public void Analysis(bool isBeforeMeasure)
        {
            if (Operation.OperateType == OperateItem.OperateType.Remove)
            {
                var startRemovedItem = Operation.Source;
                var endRemovedItem = NSIndexPath.FromRowSection(Operation.Source.Row + Operation.OperateCount - 1, Operation.Source.Section);//CollectionView.NextItem(Operation.Source, Operation.OperateCount - 1);
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

                        var count = Operation.OperateCount;
                        LastInvisibleItemsInfor = new List<CollectionViewLayout.LayoutInfor> { };
                        for (var index = 1; count >= 0; index++)
                        {
                            var item = CollectionView.NextItem(endRemovedItem, index);
                            if (item == null)//if no next
                            {
                                break;
                            }

                            if (!LastViewHolders.ContainsKey(item))
                            {
                                LastInvisibleItemsInfor.Add(new CollectionViewLayout.LayoutInfor()
                                {
                                    StartItem = item,
                                    StartBounds = CollectionView.ItemsLayout.RectForItem(item)
                                });
                            }
                        }
                    }
                    else
                    {
                        foreach(var item in LastInvisibleItemsInfor)
                        {
                            item.EndItem = TryGetCurrentIndexPath(item.StartItem);
                        }

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

                                var bounds = Rect.Zero;
                                //NextItem maybe return null when delete item
                                if (emitateLastIndexPath == null)
                                {
                                    foreach(var oldInvisibleItem in LastInvisibleItemsInfor)
                                    {
                                        if (oldInvisibleItem.EndItem.Compare(item.Key) == 0)
                                        {
                                            bounds = oldInvisibleItem.StartBounds; break;
                                        }
                                    }
                                }else
                                    bounds = CollectionView.ItemsLayout.RectForItem(emitateLastIndexPath);//if delete other section's item
                               
                                if (bounds != Rect.Zero)
                                {
                                    viewHolder.OldItemBounds = new Rect(bounds.X, bounds.Y, viewHolder.ItemBounds.Width, viewHolder.ItemBounds.Height);

                                    AnimationManager.AddOperatedItem(viewHolder);
                                }
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
                                if (bounds == viewHolder.ItemBounds &&
                                    viewHolder.OldItemBounds != Rect.Zero)//if have have data(in extend items), pass
                                    continue;
                                viewHolder.OldItemBounds = viewHolder.ItemBounds;
                                viewHolder.ItemBounds = new Rect(bounds.X, bounds.Y, viewHolder.OldItemBounds.Width, viewHolder.OldItemBounds.Height);
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
