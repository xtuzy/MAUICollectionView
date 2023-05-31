using Microsoft.Maui.Layouts;

namespace MauiUICollectionView
{
    public partial class TableView : ScrollView
    {
        public event EventHandler<ScrolledEventArgs> Scrolling;
        public void ScrollFinished(ScrolledEventArgs e)
        {
            Scrolling?.Invoke(this, e);
        }

        public ScrollViewContentView ContentView { get; protected set; }
        public TableView()
        {
            this.Orientation = ScrollOrientation.Vertical;

            ContentView = new ScrollViewContentView(this) { };

            Content = ContentView;

            Init();
            this.Scrolled += TableView_Scrolled;
            //Scrolling += TableView_Scrolling;
        }

        private void TableView_Scrolling(object sender, ScrolledEventArgs e)
        {
            scrollOffset = e.ScrollY - lastScrollY;
            lastScrollY = e.ScrollY;

            (this as IView).InvalidateMeasure();
        }

        double lastScrollY;
        /// <summary>
        /// 与上次滑动的差值
        /// </summary>
        public double scrollOffset { get; private set; } = 0;
        private void TableView_Scrolled(object sender, ScrolledEventArgs e)
        {
            scrollOffset = e.ScrollY - lastScrollY;
            lastScrollY = e.ScrollY;
            //Console.WriteLine($"Scrolled {e.ScrollY}");
            (this as IView).InvalidateMeasure();
        }

        public void AddSubview(View subview)
        {
            ContentView.Add(subview);
        }

        public void InsertSubview(View view, int index) { }

        public partial Size OnMeasure(double widthConstraint, double heightConstraint);

        public partial void OnLayout();

        /// <summary>
        /// TableView的大小应该是有限制的, 而它的内容可以是无限大小, 因此提前在这里获取这个有限值.
        /// 其用于判断可见区域大小.
        /// </summary>
        Size TableViewConstraintSize;

        protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
        {
            TableViewConstraintSize = new Size(widthConstraint, heightConstraint);
            return base.MeasureOverride(widthConstraint, heightConstraint);
        }
    }

    public class ScrollViewContentView : Layout
    {
        public void ReMeasure()
        {
            InvalidateMeasure();
        }

        internal Size ContentSize;
        TableView container;
        public ScrollViewContentView(TableView container)
        {
            this.container = container;
            this.IsClippedToBounds = true;
        }

        protected override ILayoutManager CreateLayoutManager()
        {
            return new M(this, container);
        }

        internal void SetContentSize(Size size)
        {
            ContentSize = size;
        }
    }

    class M : LayoutManager
    {
        TableView container;
        public M(Microsoft.Maui.ILayout layout, TableView container) : base(layout)
        {
            this.container = container;
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            container.OnLayout();
            return bounds.Size;
        }

        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            var size = container.OnMeasure(widthConstraint, heightConstraint);
            //Console.WriteLine($"Measure Size={size}");
            return size;
        }
    }
}
