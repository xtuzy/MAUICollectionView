namespace MauiUICollectionView.Layouts
{
    public class LayoutAnimationManager
    {
        public MAUICollectionView CollectionView;
        public Action Finished;
        List<MAUICollectionViewViewHolder> items = new List<MAUICollectionViewViewHolder>();
        private Animation moveAnimation;
        private Animation removeAnimation;
        private Animation insertAnimation;
        /// <summary>
        /// Animation直接停止时, 我们用这个tag间接停止动画.
        /// </summary>
        bool stopAnim = true;
        public void Add(MAUICollectionViewViewHolder viewHolder)
        {
            items.Add(viewHolder);
        }

        public bool HasAnim => items.Count > 0;

        /// <summary>
        /// 多次操作时为避免动画冲突, 对之前的动画直接设置最终状态
        /// </summary>
        public void StopRunWhenScroll()
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

            //直接设置最终的状态
            foreach (var item in items)
            {
                if (item.Operation == (int)OperateItem.OperateType.remove
                    || item.Operation == (int)OperateItem.OperateType.update)
                {
                    CollectionView.RecycleViewHolder(item);
                }
                else if (item.Operation == (int)OperateItem.OperateType.move)
                {
                    item.ContentView.TranslationX = 0;
                    item.ContentView.TranslationY = 0;
                    item.ContentView.Opacity = 1;
                }
                else if (item.Operation == (int)OperateItem.OperateType.insert
                    || item.Operation == -1)
                {
                    item.ContentView.Opacity = 1;
                }
                item.Operation = -1;
            }
            items.Clear();
        }

        /// <summary>
        /// 重新布局之前运行, 运行完需恢复正常布局流程. 像move和remove都是移动当前的item, 其在重新布局之前
        /// </summary>
        public void RunBeforeReLayout()
        {
            stopAnim = false;

            if (items.Count == 0)
                return;


            foreach (var item in items)
            {
                if (item.Operation == (int)OperateItem.OperateType.insert)
                {
                    item.ContentView.Opacity = 0;
                }
            }

            var listRemoveViewHolder = new List<MAUICollectionViewViewHolder>();
            foreach (var item in items)
            {
                if (item.Operation == (int)OperateItem.OperateType.remove)
                {
                    listRemoveViewHolder.Add(item);
                }
            }

            var listMoveViewHolder = new List<MAUICollectionViewViewHolder>();
            foreach (var item in items)
            {
                if (item.Operation == (int)OperateItem.OperateType.move)
                {
                    listMoveViewHolder.Add(item);
                }
            }

            moveAnimation = new Animation(v =>
            {
                if (stopAnim)
                    return;
                for (var i = listMoveViewHolder.Count - 1; i >= 0; i--)
                {
                    var item = listMoveViewHolder[i];
                    if (item.OldBoundsInLayout != item.BoundsInLayout)
                    {
                        //Debug.WriteLine(v);
                        item.ContentView.TranslationX = (item.BoundsInLayout.Left - item.OldBoundsInLayout.Left) * v;
                        item.ContentView.TranslationY = (item.BoundsInLayout.Top - item.OldBoundsInLayout.Top) * v;
                    }
                };
            }, 0, 1);

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
                removeAnimation.Commit(CollectionView, "removeAnmiation", 16, 250, null, (v, b) =>
                {
                    //结束时回收
                    for (var i = items.Count - 1; i >= 0; i--)
                    {
                        var item = items[i];
                        if (item.Operation == (int)OperateItem.OperateType.remove)
                        {
                            items.RemoveAt(i);
                            CollectionView.Dispatcher.Dispatch(() =>
                            {
                                CollectionView.RecycleViewHolder(item);
                            });
                        }
                    }
                    moveAnimation.Commit(CollectionView, "moveAnmiation", 16, 250, null, (v, b) =>
                    {
                        CollectionView.Dispatcher.Dispatch(() =>
                        {
                            CollectionView.ContentView.ReMeasure();
                        });
                    });
                });
            }
            else
            {
                moveAnimation.Commit(CollectionView, "moveAnmiation", 16, 250, null, (v, b) =>
                {
                    CollectionView.Dispatcher.Dispatch(() =>
                    {
                        CollectionView.ContentView.ReMeasure();
                    });
                });
            }

            //需要更新的Item直接回收, 我尝试添加FadeTo动画但产生重叠
            foreach (var item in items)
            {
                if (item.Operation == (int)OperateItem.OperateType.update)
                {
                    CollectionView.RecycleViewHolder(item);
                }
            }
        }

        /// <summary>
        /// 重新布局之后运行, 如insert后新Item逐渐出现的过程. 此时应该都在正确的位置
        /// </summary>
        public void RunAfterReLayout()
        {
            //注意, items可能因未知情况为空, 必要的步骤需要直接在Arrange中设置
            if (items.Count == 0)
            {
                return;
            }
            else
            {
                List<MAUICollectionViewViewHolder> insertList = new();
                for (var i = items.Count - 1; i >= 0; i--)
                {
                    var item = items[i];
                    if (item.Operation == (int)OperateItem.OperateType.move)
                    {
                        item.ContentView.TranslationX = 0;
                        item.ContentView.TranslationY = 0;
                        items.RemoveAt(i);
                    }
                    else if (item.Operation == (int)OperateItem.OperateType.insert)
                    {
                        insertList.Add(item);
                    }
                }

                if (insertList.Count > 0)
                {
                    insertAnimation = new Animation(v =>
                    {
                        if (stopAnim)
                            return;
                        foreach (var item in insertList)
                        {
                            item.ContentView.Opacity = v;
                        };
                    }, 0, 1);

                    insertAnimation.Commit(CollectionView, "insertAnomation", 16, 250, null, (v, b) =>
                    {
                        //保证insert Item最后都可见(布局流程可能打断插入动画造成不可见)
                        foreach (var item in insertList)
                        {
                            item.ContentView.Opacity = 1;
                        };
                        items.Clear();
                    });
                }
            }
        }
    }
}