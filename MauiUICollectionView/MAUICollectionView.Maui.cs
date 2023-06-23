using System.Diagnostics;
using Microsoft.Maui.Layouts;

namespace MauiUICollectionView
{
    public partial class MAUICollectionView : ScrollView
    {
        /// <summary>
        /// 同<see cref="ScrollView.Content"/>, 直接使用<see cref="ContentViewForScrollView"/>, 避免转换.
        /// </summary>
        public ContentViewForScrollView ContentView { get; protected set; }
        public MAUICollectionView()
        {
            this.Orientation = ScrollOrientation.Vertical;

            ContentView = new ContentViewForScrollView(this) { };

            Content = ContentView;

            Init();
            this.Scrolled += TableView_Scrolled;

            this.SizeChanged += MAUICollectionView_SizeChanged;
        }

        private void MAUICollectionView_SizeChanged(object sender, EventArgs e)
        {
            if (CollectionViewConstraintSize != this.Bounds.Size)
            {
                CollectionViewConstraintSize = this.Bounds.Size;
            }
        }

        double lastScrollY;
        /// <summary>
        /// 与上次滑动的差值
        /// </summary>
        public double scrollOffset { get; private set; } = 0;
        private void TableView_Scrolled(object sender, ScrolledEventArgs e)
        {
            Debug.WriteLine("Scrolled");
            scrollOffset = e.ScrollY - lastScrollY;
            lastScrollY = e.ScrollY;
            //Console.WriteLine($"Scrolled {e.ScrollY}");
            ItemsLayout.AnimationManager.Stop();
            (this as IView).InvalidateMeasure();
        }

        public void AddSubview(View subview)
        {
            ContentView.Add(subview);
        }

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
            Debug.WriteLine("ReMeasure");
            (this as IView).InvalidateMeasure();
        }
    }

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
