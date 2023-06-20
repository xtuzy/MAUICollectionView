using System.Diagnostics;
using Microsoft.Maui.Layouts;

namespace MauiUICollectionView
{
    public partial class MAUICollectionView : ScrollView
    {
        public ContentViewForScrollView ContentView { get; protected set; }
        public MAUICollectionView()
        {
            this.Orientation = ScrollOrientation.Vertical;

            ContentView = new ContentViewForScrollView(this) { };

            Content = ContentView;

            Init();
            this.Scrolled += TableView_Scrolled;
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
            ItemsLayout.AnimationManager.StopRunWhenScroll();
            (this as IView).InvalidateMeasure();
        }

        public void AddSubview(View subview)
        {
            ContentView.Add(subview);
        }

        public void InsertSubview(View view, int index) { }

        public partial Size OnContentViewMeasure(double widthConstraint, double heightConstraint);

        public partial void OnContentViewLayout();

        /// <summary>
        /// TableView的大小应该是有限制的, 而它的内容可以是无限大小, 因此提前在这里获取这个有限值.
        /// 其用于判断可见区域大小.
        /// </summary>
        Size CollectionViewConstraintSize;

        protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
        {
            CollectionViewConstraintSize = new Size(widthConstraint, heightConstraint);
            return base.MeasureOverride(widthConstraint, heightConstraint);
        }
    }

    public class ContentViewForScrollView : Layout
    {
        public void ReMeasure()
        {
             Debug.WriteLine("ReMeasure");
            (this as IView).InvalidateMeasure();
        }

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
