namespace MauiUICollectionView.Layouts
{
    public class LayoutAnimationManager: IDisposable
    {
        MAUICollectionView CollectionView;
        public Action Finished;
        /// <summary>
        /// 存储需要操作的item, 会对它们实施动画.
        /// </summary>
        List<MAUICollectionViewViewHolder> items = new List<MAUICollectionViewViewHolder>();
        private Animation moveAnimation;
        private Animation removeAnimation;
        private Animation insertAnimation;
        /// <summary>
        /// 需要Animation立即停止时, 我们用这个tag立即停止动画循环, 让其不设置值.
        /// </summary>
        bool stopAnim = true;

        public LayoutAnimationManager(MAUICollectionView collectionView)
        {
            CollectionView = collectionView;
        }

        public void Add(MAUICollectionViewViewHolder viewHolder)
        {
            items.Add(viewHolder);
        }

        public bool HasAnim => items.Count > 0;

        /// <summary>
        /// 多次操作时为避免动画冲突, 对之前的动画直接设置最终状态
        /// </summary>
        public void Stop()
        {
            stopAnim = true;

            moveAnimation?.Pause();
            removeAnimation?.Pause();
            insertAnimation?.Pause();
            moveAnimation?.Dispose();
            removeAnimation?.Dispose();
            insertAnimation?.Dispose();

            //重置ViewHolder中的操作类型, 主要目的是因为Insert操作的动画时常执行不到, 导致其不透明设置不正确, 我在Arrange时集中对可见的默认操作Item设置不透明度, 防止遗漏
            foreach (var item in CollectionView.PreparedItems)
            {
                item.Value.Operation = -1;
            }

            if (items.Count == 0)
                return;
            //这里调用它是为了确保回收
            SetItemsStateAfterAnimateFinished();
            items.Clear();
        }

        void AnimateFinished()
        {
            SetItemsStateAfterAnimateFinished();
            items.Clear();
            Finished?.Invoke();
        }

        /// <summary>
        /// 重新布局之前运行, 运行完需恢复正常布局流程. 像move和remove都是移动当前的item, 其在重新布局之前
        /// </summary>
        public void Run()
        {
            stopAnim = false;

            if (items.Count == 0)
                return;

            moveAnimation?.Dispose();
            removeAnimation?.Dispose();
            insertAnimation?.Dispose();
            moveAnimation = null;
            removeAnimation = null;
            insertAnimation = null;

            var listRemoveViewHolder = new List<MAUICollectionViewViewHolder>();
            var listMoveViewHolder = new List<MAUICollectionViewViewHolder>();
            var listInsertViewHolder = new List<MAUICollectionViewViewHolder>();
            var listUpdateViewHolder = new List<MAUICollectionViewViewHolder>();
            //结束时回收
            for (var i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                if (item.Operation == (int)OperateItem.OperateType.insert)
                {
                    item.ContentView.Opacity = 0;
                    listInsertViewHolder.Add(item);
                }
                else if (item.Operation == (int)OperateItem.OperateType.remove)
                {
                    listRemoveViewHolder.Add(item);
                }
                else if (item.Operation == (int)OperateItem.OperateType.move)
                {
                    listMoveViewHolder.Add(item);
                }
                else if (item.Operation == (int)OperateItem.OperateType.update)
                {
                    //需要更新的Item直接回收, 我尝试添加FadeTo动画但产生重叠
                    items.RemoveAt(i);
                    CollectionView.RecycleViewHolder(item);
                }
            }

            if (listInsertViewHolder.Count > 0)
            {
                insertAnimation = new Animation(v =>
                {
                    if (stopAnim)
                        return;
                    foreach (var item in listInsertViewHolder)
                    {
                        item.ContentView.Opacity = v;
                    };
                }, 0, 1);
            }

            //move Items的初始状态应该是Arrange在目标位置, tanslate后在之前的位置
            if (listMoveViewHolder.Count > 0)
            {
                for (var i = listMoveViewHolder.Count - 1; i >= 0; i--)
                {
                    var item = listMoveViewHolder[i];
                    if (item.OldBoundsInLayout != Rect.Zero &&
                    item.OldBoundsInLayout != item.BoundsInLayout)
                    {
                        item.ContentView.TranslationX = (item.OldBoundsInLayout.Left - item.BoundsInLayout.Left) * 1;
                        item.ContentView.TranslationY = (item.OldBoundsInLayout.Top - item.BoundsInLayout.Top) * 1;
                    }
                };
            }
            if (listMoveViewHolder.Count > 0)
            {
                moveAnimation = new Animation(v =>
                {
                    if (stopAnim)
                        return;
                    for (var i = listMoveViewHolder.Count - 1; i >= 0; i--)
                    {
                        var item = listMoveViewHolder[i];
                        if (item.OldBoundsInLayout != Rect.Zero &&
                        item.OldBoundsInLayout != item.BoundsInLayout)
                        {
                            //Debug.WriteLine(v);
                            item.ContentView.TranslationX = (item.OldBoundsInLayout.Left - item.BoundsInLayout.Left) * v;
                            item.ContentView.TranslationY = (item.OldBoundsInLayout.Top - item.BoundsInLayout.Top) * v;
                        }
                    };
                }, 1, 0);
            }

            if (listRemoveViewHolder.Count > 0)
            {
                removeAnimation = new Animation(v =>
                {
                    if (stopAnim)
                        return;
                    foreach (var item in listRemoveViewHolder)
                    {
                        //Debug.WriteLine(v);
                        item.ContentView.Opacity = v;
                    };
                }, 1, 0);
            }

            //TODO: 当前设计Remove, Insert操作是互斥的, 只能一个操作, 后续需要能一起用
            if (removeAnimation != null)
            {
                //有remove
                removeAnimation.Commit(CollectionView, "removeAnmiation", 16, 250, null, (v, b) =>
                {
                    //结束时回收
                    foreach (var item in listRemoveViewHolder)
                    {
                        items.Remove(item);
                        CollectionView.Dispatcher.Dispatch(() =>
                        {
                            CollectionView.RecycleViewHolder(item);
                        });
                    }
                    //remove了中间的
                    if (moveAnimation != null)
                    {
                        moveAnimation.Commit(CollectionView, "moveAnmiation", 16, 250, null, (v, b) =>
                        {
                            CollectionView.Dispatcher.Dispatch(() =>
                            {
                                CollectionView.ReMeasure();
                            });
                            AnimateFinished();
                        });
                    }
                    else
                    {
                        AnimateFinished();
                    }
                });
            }
            else//没有remove
            {
                if (moveAnimation != null)
                {
                    moveAnimation.Commit(CollectionView, "moveAnmiation", 16, 250, null, (v, b) =>
                    {
                        CollectionView.Dispatcher.Dispatch(() =>
                        {
                            CollectionView.ReMeasure();
                        });
                        //插入到中间时, 既有move又有insert
                        if (insertAnimation != null)
                        {
                            insertAnimation.Commit(CollectionView, "insertAnimation", 16, 250, null, (v, b) =>
                            {
                                AnimateFinished();
                            });
                        }
                        else
                        {
                            AnimateFinished();
                        }
                    });
                }
                else//插入到最后的情况
                {
                    if (insertAnimation != null)
                    {
                        insertAnimation.Commit(CollectionView, "insertAnimation", 16, 250, null, (v, b) =>
                        {
                            AnimateFinished();
                        });
                    }
                    else
                    {
                        AnimateFinished();
                    }
                }
            }
        }

        /// <summary>
        /// 标记Item的操作为默认值
        /// </summary>
        void SetItemsStateAfterAnimateFinished()
        {
            //注意, items可能因未知情况为空, 必要的步骤如move item的最终translate需要直接在Arrange中设置
            if (items.Count == 0)
            {
                return;
            }
            else
            {
                foreach (var item in items)
                {
                    if (item.Operation == (int)OperateItem.OperateType.remove)
                    {
                        CollectionView.RecycleViewHolder(item);//如果动画步骤有问题, 此处确保回收
                    }

                    item.Operation = -1;
                }
            }
        }

        public void Dispose()
        {
            Stop();
            Finished = null;
            CollectionView = null;
        }
    }
}