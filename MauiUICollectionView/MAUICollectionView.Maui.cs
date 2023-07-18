using Microsoft.Maui.Layouts;
using System.Diagnostics;
#if ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#else
using PlatformView = UIKit.UIView;
#endif
namespace MauiUICollectionView
{
    public partial class MAUICollectionView : ScrollView
    {
        /// <summary>
        /// 同<see cref="ScrollView.Content"/>, 直接使用<see cref="ContentViewForScrollView"/>, 避免转换.
        /// </summary>
        public ContentViewForScrollView ContentView { get; protected set; }

        public IGestureManager GestureManager;

        public MAUICollectionView()
        {
            ContentView = new ContentViewForScrollView(this) { };

            Content = ContentView;

            Init();
            //ScrollView自带的滑动
            this.Scrolled += TableView_Scrolled;
            //大小改变时需要记录大小, 因为MeasureOverride可能不被调用
            this.SizeChanged += MAUICollectionView_SizeChanged;

            if (GestureManager == null)
                GestureManager = new MauiUICollectionView.Gestures.GestureManager();//set default gesturemanager
            //选择Item
            GestureManager.SelectPointCommand = new Command(SelectItemCommand);
            //长按弹出Popmenu
            GestureManager.LongPressPointCommand = new Command(ShowContextMenuCommand);
            //拖拽排序
            GestureManager.DragPointCommand = new Command(DragCommand);

        }

        private void MAUICollectionView_SizeChanged(object sender, EventArgs e)
        {
            if (CollectionViewConstraintSize != this.Bounds.Size)
            {
                CollectionViewConstraintSize = this.Bounds.Size;
            }
        }

        void StopRefresh(bool stop = true)
        {
            //这些平台不显示下拉刷新, 设置它可能出错
            if (DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.MacCatalyst || DeviceInfo.Platform == DevicePlatform.macOS)
            {
                return;
            }

            if (this.Parent is not RefreshView refreshView)
            {
                refreshView = this.Parent?.Parent as RefreshView;
            }
            if (refreshView != null)
            {
#if ANDROID
                if (stop)
                {
                    //when set IsEnable = false, refreshview still run on Android, so i try not let ScrollView scroll to Top.
                    if(ScrollY == 0)
                        ScrollToAsync(0, 0.1, false);
                }
#endif
                refreshView.IsRefreshing = false;
                refreshView.IsEnabled = !stop;
            }
        }

        public void AddSubview(View subview)
        {
            ContentView.Add(subview);
        }

        #region 布局相关
        public partial Size OnContentViewMeasure(double widthConstraint, double heightConstraint);

        public partial void OnContentViewLayout();

        /// <summary>
        /// TableView的大小应该是有限制的, 而它的内容可以是无限大小, 因此提前在这里获取这个有限值.
        /// 其用于判断可见区域大小. 内部在MeasureOverride中设置它, 可能不会执行. 内部也在SizeChanged中设置它, 其在第一次布局之后执行, 因此可能不显示Item. 建议调试时看Item是否正常显示, 没有的话建议设置其为Page大小作为初始值.
        /// </summary>
        public Size CollectionViewConstraintSize;

        /// <summary>
        /// 将CollectionView添加到RefreshView里时, 这个方法不被调用, 我另外也在SizeChanged中设置了.
        /// </summary>
        /// <param name="widthConstraint"></param>
        /// <param name="heightConstraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
        {
            CollectionViewConstraintSize = new Size(widthConstraint, heightConstraint);
            return base.MeasureOverride(widthConstraint, heightConstraint);
        }

        /// <summary>
        /// 需要更新界面时, 调用它重新测量和布局
        /// </summary>
        public void ReMeasure()
        {
            //Debug.WriteLine("ReMeasure");
            (this as IView).InvalidateMeasure();
        }
        #endregion

        #region 操作相关

        /// <summary>
        /// 点击选择Item
        /// </summary>
        /// <param name="t"></param>
        void SelectItemCommand(object t)
        {
            if (SelectionMode == SelectionMode.None)
                return;
            var args = t as SelectEventArgs;

            if (args != null)
            {
                if (args.status == SelectStatus.Selected)
                {
                    var indexPath = this.ItemsLayout.IndexPathForVisibaleRowAtPointOfCollectionView(args.point);

                    if (SelectionMode == SelectionMode.Single)
                    {
                        for (int index = SelectedRow.Count - 1; index >= 0; index--)
                        {
                            var old = SelectedRow[index];
                            if (old.Equals(indexPath))//单选时, 如果点击了已选的, 不取消选择
                                continue;
                            DeselectRowAtIndexPath(old);
                            Source?.DidDeselectItem?.Invoke(this, indexPath);
                        }
                        if (indexPath != null)
                        {
                            this.SelectRowAtIndexPath(indexPath, false, ScrollPosition.None);
                            Source?.DidSelectItem?.Invoke(this, indexPath);
                        }
                    }
                    else if (SelectionMode == SelectionMode.Multiple)
                    {
                        if (SelectedRow.Contains(indexPath))//多选模式下, 点击已经选择了的会取消选择
                        {
                            DeselectRowAtIndexPath(indexPath);
                            Source?.DidDeselectItem?.Invoke(this, indexPath);
                        }
                        else
                        {
                            if (indexPath != null)
                            {
                                this.SelectRowAtIndexPath(indexPath, false, ScrollPosition.None);
                                Source?.DidSelectItem?.Invoke(this, indexPath);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 长按手势弹出ContextMenu. 桌面上使用右键, 不需执行该方法; iOS上默认长按, 不能通过自定义的长按手势触发.
        /// </summary>
        /// <param name="t"></param>
        void ShowContextMenuCommand(object t)
        {
            if (CanContextMenu)
            {
                var args = (Point)t;
                var indexPath = this.ItemsLayout.IndexPathForVisibaleRowAtPointOfCollectionView(args);
                if (PreparedItems.ContainsKey(indexPath))
                {
                    var item = PreparedItems[indexPath];
                    item.ContextMenu?.Show();
                }
            }
        }

        Point lastDragPosition = Point.Zero;
        /// <summary>
        /// 拖拽时
        /// </summary>
        /// <param name="t"></param>
        void DragCommand(object t)
        {
            if (CanDrag)
            {
                var args = t as DragEventArgs;

                if (args.point.Y > this.Bounds.Height - 2 || args.point.Y < 2)//到很边缘时指定为结束, 因为可能出界, 导致不好判断
                {
                    args.status = GestureStatus.Canceled;
                }
                //Debug.WriteLine("CanDrag" + args.status);
                if (args.status == GestureStatus.Started)
                {
                    StopRefresh(true);

                    var indexPath = this.ItemsLayout.IndexPathForVisibaleRowAtPointOfCollectionView(args.point);

                    if (PreparedItems.ContainsKey(indexPath))
                    {
                        DragedItem = PreparedItems[indexPath];
                        DragedItem.ZIndex = 3;
                        DragedItem.Scale = 0.9;
                        DragedItem.DragBoundsInLayout = DragedItem.BoundsInLayout;
                        lastDragPosition = args.point;
                        if (args.Device == GestureDevice.Touch)//触摸时滑动不滚动, 不然与拖动冲突
                            stopScroll = true;
                    }
                }
                else if (args.status == GestureStatus.Running)
                {
                    if (DragedItem == null)
                        return;
                    var indexPath = this.ItemsLayout.IndexPathForVisibaleRowAtPointOfCollectionView(args.point);

                    if (args.Device == GestureDevice.Touch)
                    {
                        enableAutoScroll = true;
                        //计算距离底部或顶部多少距离时需要自动滑动
                        var offset = 20; //DragedItem.BoundsInLayout.Height /2 > 20? DragedItem.BoundsInLayout.Height /2:20;
                        if (this.Bounds.Height - args.point.Y < offset)
                        {
                            var dy = offset;

                            if (!autoScrolling)
                            {
                                autoScrolling = true;
                                AutoScrollTask(dy);
                            }

                        }
                        else if (args.point.Y - 0 < offset)
                        {
                            var dy = -offset;
                            if (lastScrollY + dy > 0)
                            {
                                if (!autoScrolling)//如果正在滑动, 就不增加Task
                                {
                                    autoScrolling = true;
                                    AutoScrollTask(dy);
                                }
                            }
                        }
                        else
                        {
                            autoScrolling = false;
                            enableAutoScroll = false;//默认不自动滑动
                        }
                        DragedItem.DragBoundsInLayout = new Rect(0, DragedItem.DragBoundsInLayout.Y + (args.point.Y - lastDragPosition.Y) + (ScrollY - lastScrollY), DragedItem.BoundsInLayout.Width, DragedItem.BoundsInLayout.Height);
                    }
                    else
                    {
                        DragedItem.DragBoundsInLayout = new Rect(0, DragedItem.DragBoundsInLayout.Y + (args.point.Y - lastDragPosition.Y) + (ScrollY - lastScrollY), DragedItem.BoundsInLayout.Width, DragedItem.BoundsInLayout.Height);
                    }

                    lastDragPosition = args.point;
                    if (indexPath != null &&
                        !indexPath.Equals(DragedItem?.IndexPath)//不是同一个
                        )
                    {
                        var targetViewHolder = PreparedItems[indexPath].BoundsInLayout;
                        if ((indexPath < DragedItem?.IndexPath && new Rect(targetViewHolder.X, targetViewHolder.Y - ScrollY, targetViewHolder.Width, targetViewHolder.Height / 2).Contains(args.point)) || //在DragItem的上面, 需要到目标Item的上半部分才交换
                            (indexPath > DragedItem?.IndexPath && new Rect(targetViewHolder.X, targetViewHolder.Y - ScrollY + targetViewHolder.Height / 2, targetViewHolder.Width, targetViewHolder.Height / 2).Contains(args.point)))
                        {
                            Source.WillDragTo?.Invoke(this, DragedItem.IndexPath, indexPath);
                        }
                    }

                    ReMeasure();//位置需要一直刷新
                }
                else
                {
                    StopRefresh(false);
                    if (DragedItem == null)
                        return;

                    DragedItem.DragBoundsInLayout = Rect.Zero;
                    DragedItem.ZIndex = 1;
                    DragedItem.Scale = 1;
                    DragedItem = null;
                    lastDragPosition = args.point;
                    stopScroll = false;
                    autoScrolling = false;
                    ReMeasure();
                }
            }
        }

        /// <summary>
        /// 拖拽时禁止一般的滑动
        /// </summary>
        bool stopScroll = false;
        /// <summary>
        /// 拖拽到顶部或底部时自动滑动
        /// </summary>
        bool enableAutoScroll = false;
        bool autoScrolling = false;
        /// <summary>
        /// 触摸拖拽时, 手势冲突无法滑动, 因此需要自动滑动.
        /// </summary>
        /// <param name="dy"></param>
        void AutoScrollTask(int dy)
        {
            Task.Run(async () =>
            {
                while (autoScrolling && DragedItem != null)
                {
                    if (lastScrollY + dy > ContentView.Bounds.Height || lastScrollY + dy < 0)//不能越界
                    {
                        DragCommand(new DragEventArgs(GestureStatus.Canceled, Point.Zero));
                        break;
                    }
                    this.Dispatcher.Dispatch(() =>
                    {
                        ScrollToAsync(0, lastScrollY + dy, false);
                    });

                    await Task.Delay(100);
                }
            });
        }

        /// <summary>
        /// 记录上一次ScrollY
        /// </summary>
        public double lastScrollY { get; protected set; }
        /// <summary>
        /// 与上次滑动的差值
        /// </summary>
        public double scrollOffset { get; private set; } = 0;
        private void TableView_Scrolled(object sender, ScrolledEventArgs e)
        {
            if (enableAutoScroll)
            {
                scrollOffset = e.ScrollY - lastScrollY;
                if (DragedItem != null)
                {
                    DragCommand(new DragEventArgs(GestureStatus.Running, lastDragPosition) { Device = GestureDevice.Touch });
                }
                lastScrollY = e.ScrollY;
            }
            else if (stopScroll)
            {
                //Debug.WriteLine(e.ScrollY);
                ScrollToAsync(0, lastScrollY, false);//停止Scroll代表滑到旧的地方
            }
            else
            {
                Debug.WriteLine("Scrolled");
                scrollOffset = e.ScrollY - lastScrollY;
                //如果DragItem能执行到这里, 说明非触摸, 使用鼠标可以滑动, 因此更新DragItem的滑动距离
                if (DragedItem != null)
                {
                    DragCommand(new DragEventArgs(GestureStatus.Running, lastDragPosition) { Device = GestureDevice.Mouse });
                }
                lastScrollY = e.ScrollY;
                //Console.WriteLine($"Scrolled {e.ScrollY}");
                ItemsLayout.AnimationManager.StopOperateAnim();
            }
            ReMeasure();
        }

        SelectionMode selectionMode = SelectionMode.None;
        public SelectionMode SelectionMode
        {
            get => selectionMode;
            set
            {
                if (value == SelectionMode.None)
                {
                    //设置成None时清除之前选择的
                    SelectedRow?.Clear();
                }
                selectionMode = value;
            }
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            GestureManager?.SubscribeGesture(this);
        }

        protected override void OnHandlerChanging(HandlerChangingEventArgs args)
        {
            base.OnHandlerChanging(args);
            if (args.OldHandler != null)
            {
                GestureManager?.CancleSubscribeGesture();
            }
        }

        bool canDrag = false;
        public bool CanDrag
        {
            set
            {
                canDrag = value;

                GestureManager.SetCanDrag(value);
            }
            get { return canDrag; }
        }

        bool canContextMenu = false;
        public bool CanContextMenu
        {
            set
            {
                canContextMenu = value;
            }
            get
            {
                return canContextMenu;
            }
        }

        #endregion

        public class ContentViewForScrollView : Layout
        {
            MAUICollectionView container;
            public ContentViewForScrollView(MAUICollectionView container)
            {
                this.container = container;
                this.IsClippedToBounds = true;
            }

            protected override ILayoutManager CreateLayoutManager()
            {
                return new ContentViewLayoutManager(this, container);
            }

            public class ContentViewLayoutManager : LayoutManager
            {
                MAUICollectionView container;
                public ContentViewLayoutManager(Microsoft.Maui.ILayout layout, MAUICollectionView container) : base(layout)
                {
                    this.container = container;
                }

                public override Size ArrangeChildren(Rect bounds)
                {
                    container.OnContentViewLayout();
                    return bounds.Size;
                }

                public override Size Measure(double widthConstraint, double heightConstraint)
                {
                    var size = container.OnContentViewMeasure(widthConstraint, heightConstraint);
                    //Console.WriteLine($"Measure Size={size}");
                    return size;
                }
            }
        }
    }
}
