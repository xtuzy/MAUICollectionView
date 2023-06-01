namespace MauiUICollectionView.Layouts
{
    /// <summary>
    /// 布局的逻辑放在此处
    /// </summary>
    public abstract class CollectionViewLayout
    {
        public CollectionViewLayout(TableView collectionView)
        {
            this.CollectionView = collectionView;
        }

        private TableView _collectionView;
        public TableView CollectionView
        {
            get { return _collectionView; }
            private set => _collectionView = value;
        }

        /// <summary>
        /// 滚动方向. 必须设置值, 默认为垂直方向.
        /// </summary>
        public virtual ItemsLayoutOrientation ScrollDirection
        {
            get; set;
        } = ItemsLayoutOrientation.Vertical;

        /// <summary>
        /// 设置Cell等内容的位置
        /// </summary>
        public abstract void ArrangeContents();

        /// <summary>
        /// 测量内容总共占据的大小.
        /// </summary>
        /// <param name="tableViewWidth"></param>
        /// <param name="tableViewHeight"></param>
        /// <returns></returns>
        public abstract Size MeasureContents(double tableViewWidth, double tableViewHeight);

        public abstract NSIndexPath IndexPathForVisibaleRowAtPointOfTableView(Point point);
        public abstract NSIndexPath IndexPathForRowAtPointOfContentView(Point point);
        /// <summary>
        /// 返回IndexPath对应的行在ContentView中的位置. 在某些Item大小不固定的Layout中, 其可能是不精确的, 会变化的. 可能只是即时状态, 比如滑动后数据会变化.
        /// </summary>
        /// <returns></returns>
        public abstract Rect RectForRowOfIndexPathInContentView(NSIndexPath indexPath);
    }
}