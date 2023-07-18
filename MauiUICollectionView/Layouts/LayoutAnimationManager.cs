﻿namespace MauiUICollectionView.Layouts
{
    /// <summary>
    /// manage operate and scroll animation.
    /// </summary>
    public class LayoutAnimationManager : IDisposable
    {
        /// <summary>
        /// define action when item appear. value is 0-1.
        /// </summary>
        public Action<MAUICollectionViewViewHolder, double> AppearAnimAction { get; set; }
        /// <summary>
        /// define action when item disappear. value is 0-1.
        /// </summary>
        public Action<MAUICollectionViewViewHolder, double> DisAppearAnimAction { get; set; }

        MAUICollectionView CollectionView;

        /// <summary>
        /// 存储需要操作的item, 会对它们实施动画.
        /// </summary>
        List<MAUICollectionViewViewHolder> operateItems = new List<MAUICollectionViewViewHolder>();
        private Animation moveOperateAnimation;
        private Animation removeOperateAnimation;
        private Animation insertOperateAnimation;
        /// <summary>
        /// 需要Animation立即停止时, 我们用这个tag立即停止动画循环, 让其不设置值.
        /// </summary>
        bool stopOperateAnim = true;

        public LayoutAnimationManager(MAUICollectionView collectionView)
        {
            CollectionView = collectionView;
            AppearAnimAction = (view, value) =>
            {
                view.Opacity = value;
                view.TranslationX = view.BoundsInLayout.Width * (1 - value);
            };
            DisAppearAnimAction = (view, value) =>
            {
                view.TranslationX = view.BoundsInLayout.Width * value;
                view.Opacity = 1 - value;
            };
        }

        public void Add(MAUICollectionViewViewHolder viewHolder)
        {
            operateItems.Add(viewHolder);
        }

        public bool HasOperateAnim => operateItems.Count > 0;

        /// <summary>
        /// if you don't need animation for scroll, set false.
        /// </summary>
        public bool HasScrollAnim { get; set; } = true;

        /// <summary>
        /// internal load this method when new operation be done, it will stop all animation of last operation.
        /// </summary>
        public void StopOperateAnim()
        {
            stopOperateAnim = true;

            moveOperateAnimation?.Pause();
            removeOperateAnimation?.Pause();
            insertOperateAnimation?.Pause();
            moveOperateAnimation?.Dispose();
            removeOperateAnimation?.Dispose();
            insertOperateAnimation?.Dispose();

            //重置ViewHolder中的操作类型, 主要目的是因为Insert操作的动画时常执行不到, 导致其不透明设置不正确, 我在Arrange时集中对可见的默认操作Item设置不透明度, 防止遗漏
            foreach (var item in CollectionView.PreparedItems)
            {
                item.Value.Operation = -1;
            }

            if (operateItems.Count == 0)
                return;
            //这里调用它是为了确保回收
            SetItemsStateAfterAnimateFinished();
            operateItems.Clear();
        }

        void OperateAnimFinished()
        {
            SetItemsStateAfterAnimateFinished();
            operateItems.Clear();
        }

        /// <summary>
        /// 重新布局之前运行, 运行完需恢复正常布局流程. 像move和remove都是移动当前的item, 其在重新布局之前
        /// </summary>
        public void Run(bool runOperateAnim)
        {
            RunScrollAnim();
            if (runOperateAnim)
                RunOperateAnim();
        }
        /// <summary>
        /// record last which item do scroll animation, animation need some time, so avoid next time set this item.
        /// </summary>
        List<NSIndexPath> InScrollAnimItem = new List<NSIndexPath>();
        /// <summary>
        /// when scroll, item maybe need anim. we also set finial state of item that no operate
        /// </summary>
        public virtual void RunScrollAnim()
        {
            if (CollectionView.scrollOffset > 0)//swipe up
            {
                foreach (var indexPath in CollectionView.PreparedItems.Keys)
                {
                    var item = CollectionView.PreparedItems[indexPath];

                    if (HasScrollAnim && 
                        AppearAnimAction != null && 
                        !indexPath.IsInRange(CollectionView.ItemsLayout.OldPreparedItems[0], CollectionView.ItemsLayout.OldPreparedItems[1]) &&
                        item != CollectionView.DragedItem)//show new item
                    {
                        if (item.Operation == -1)//Operate item have seft anim
                        {
                            item.TranslationX = item.BoundsInLayout.Width * 1;
                            InScrollAnimItem.Add(item.IndexPath);
                            var animation = new Animation(v =>
                            {
                                AppearAnimAction?.Invoke(item, v);
                            }, 0, 1);
                            animation.Commit(CollectionView, indexPath.ToString(), 16, 500, Easing.CubicIn, (v, b) =>
                            {
                                InScrollAnimItem.Remove(indexPath);
                            });
                        }
                    }
                    else //set final state
                    {
                        if (item.Operation == -1 && !InScrollAnimItem.Contains(indexPath))
                        {
                            item.Opacity = 1;
                            item.TranslationX = 0;
                            item.TranslationY = 0;
                        }
                    }
                }
            }
            else //set final state
            {
                foreach (var indexPath in CollectionView.PreparedItems.Keys)
                {
                    var item = CollectionView.PreparedItems[indexPath];

                    if (item.Operation == -1 && !InScrollAnimItem.Contains(indexPath))
                    {
                        item.Opacity = 1;
                        item.TranslationX = 0;
                        item.TranslationY = 0;
                    }
                }
            }
        }

        /// <summary>
        /// when operate item, need anim.
        /// </summary>
        void RunOperateAnim()
        {
            stopOperateAnim = false;

            if (operateItems.Count == 0)
                return;

            moveOperateAnimation?.Dispose();
            removeOperateAnimation?.Dispose();
            insertOperateAnimation?.Dispose();
            moveOperateAnimation = null;
            removeOperateAnimation = null;
            insertOperateAnimation = null;

            var listRemoveViewHolder = new List<MAUICollectionViewViewHolder>();
            var listMoveViewHolder = new List<MAUICollectionViewViewHolder>();
            var listInsertViewHolder = new List<MAUICollectionViewViewHolder>();
            var listUpdateViewHolder = new List<MAUICollectionViewViewHolder>();
            //结束时回收
            for (var i = operateItems.Count - 1; i >= 0; i--)
            {
                var item = operateItems[i];
                if (item.Operation == (int)OperateItem.OperateType.Insert)
                {
                    item.Opacity = 0;
                    listInsertViewHolder.Add(item);
                }
                else if (item.Operation == (int)OperateItem.OperateType.Remove)
                {
                    listRemoveViewHolder.Add(item);
                }
                else if (item.Operation == (int)OperateItem.OperateType.Move)
                {
                    listMoveViewHolder.Add(item);
                }
                else if (item.Operation == (int)OperateItem.OperateType.Update)
                {
                    //需要更新的Item直接回收, 我尝试添加FadeTo动画但产生重叠
                    operateItems.RemoveAt(i);
                    CollectionView.RecycleViewHolder(item);
                }
                else if (item.Operation == (int)OperateItem.OperateType.MoveNow)
                {
                    item.Opacity = 1;
                    //item.TranslationX = 0;
                    item.TranslationY = 0;
                }
                else if (item.Operation == (int)OperateItem.OperateType.RemoveNow)
                {
                    item.Opacity = 0;
                    CollectionView.Dispatcher.Dispatch(() =>
                    {
                        CollectionView.RecycleViewHolder(item);
                    });
                }
            }

            if (listInsertViewHolder.Count > 0)
            {
                if (AppearAnimAction == null)
                {
                    foreach (var item in listInsertViewHolder)
                    {
                        item.Opacity = 1;
                    };
                }
                else
                {
                    insertOperateAnimation = new Animation(v =>
                    {
                        if (stopOperateAnim)
                            return;
                        foreach (var item in listInsertViewHolder)
                        {
                            AppearAnimAction?.Invoke(item, v);
                        };
                    }, 0, 1);
                }
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
                        item.Opacity = 1;
                        item.TranslationX = (item.OldBoundsInLayout.Left - item.BoundsInLayout.Left) * 1;
                        item.TranslationY = (item.OldBoundsInLayout.Top - item.BoundsInLayout.Top) * 1;
                    }
                };
            }
            if (listMoveViewHolder.Count > 0)
            {
                moveOperateAnimation = new Animation(v =>
                {
                    if (stopOperateAnim)
                        return;
                    for (var i = listMoveViewHolder.Count - 1; i >= 0; i--)
                    {
                        var item = listMoveViewHolder[i];
                        if (item.OldBoundsInLayout != Rect.Zero &&
                        item.OldBoundsInLayout != item.BoundsInLayout)
                        {
                            //Debug.WriteLine(v);
                            item.TranslationX = (item.OldBoundsInLayout.Left - item.BoundsInLayout.Left) * v;
                            item.TranslationY = (item.OldBoundsInLayout.Top - item.BoundsInLayout.Top) * v;
                        }
                    };
                }, 1, 0);
            }

            if (listRemoveViewHolder.Count > 0)
            {
                if (DisAppearAnimAction == null)
                {
                    foreach (var item in listRemoveViewHolder)
                    {
                        item.Opacity = 0;
                    };
                }
                else
                {
                    removeOperateAnimation = new Animation(v =>
                    {
                        if (stopOperateAnim)
                            return;
                        foreach (var item in listRemoveViewHolder)
                        {
                            DisAppearAnimAction?.Invoke(item, v);
                        };
                    }, 0, 1);
                }
            }

            //TODO: 当前设计Remove, Insert操作是互斥的, 只能一个操作, 后续需要能一起用
            if (removeOperateAnimation != null)
            {
                //有remove
                removeOperateAnimation.Commit(CollectionView, "removeAnmiation", 16, 250, null, (v, b) =>
                {
                    //结束时回收
                    foreach (var item in listRemoveViewHolder)
                    {
                        operateItems.Remove(item);
                        CollectionView.Dispatcher.Dispatch(() =>
                        {
                            CollectionView.RecycleViewHolder(item);
                        });
                    }
                    //remove了中间的
                    if (moveOperateAnimation != null)
                    {
                        moveOperateAnimation.Commit(CollectionView, "moveAnmiation", 16, 250, null, (v, b) =>
                        {
                            CollectionView.Dispatcher.Dispatch(() =>
                            {
                                CollectionView.ReMeasure();
                            });
                            OperateAnimFinished();
                        });
                    }
                    else
                    {
                        OperateAnimFinished();
                    }
                });
            }
            else//没有remove
            {
                if (moveOperateAnimation != null)
                {
                    moveOperateAnimation.Commit(CollectionView, "moveAnmiation", 16, 250, null, (v, b) =>
                    {
                        CollectionView.Dispatcher.Dispatch(() =>
                        {
                            CollectionView.ReMeasure();
                        });
                        //插入到中间时, 既有move又有insert
                        if (insertOperateAnimation != null)
                        {
                            insertOperateAnimation.Commit(CollectionView, "insertAnimation", 16, 250, null, (v, b) =>
                            {
                                OperateAnimFinished();
                            });
                        }
                        else
                        {
                            OperateAnimFinished();
                        }
                    });
                }
                else//插入到最后的情况
                {
                    if (insertOperateAnimation != null)
                    {
                        insertOperateAnimation.Commit(CollectionView, "insertAnimation", 16, 250, null, (v, b) =>
                        {
                            OperateAnimFinished();
                        });
                    }
                    else
                    {
                        OperateAnimFinished();
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
            if (operateItems.Count == 0)
            {
                return;
            }
            else
            {
                foreach (var item in operateItems)
                {
                    if (item.Operation == (int)OperateItem.OperateType.Remove)
                    {
                        CollectionView.RecycleViewHolder(item);//如果动画步骤有问题, 此处确保回收
                    }

                    item.Operation = -1;
                }
            }

            foreach (var item in CollectionView.PreparedItems)
            {
                item.Value.Operation = -1;
            }
        }

        public void Dispose()
        {
            StopOperateAnim();
            CollectionView = null;
        }
    }
}