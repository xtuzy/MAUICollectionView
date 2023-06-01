namespace MauiUICollectionView.Layouts
{
    /// <summary>
    /// 布局的逻辑放在此处
    /// </summary>
    public abstract class CollectionViewLayout
    {
        public CollectionViewLayout(MAUICollectionView collectionView)
        {
            this.CollectionView = collectionView;
        }

        private MAUICollectionView _collectionView;
        public MAUICollectionView CollectionView
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
        /// <param name="collectionViewWidth"></param>
        /// <param name="collectionViewHeight"></param>
        /// <returns></returns>
        public abstract Size MeasureContents(double collectionViewWidth, double collectionViewHeight);

        public abstract NSIndexPath IndexPathForVisibaleRowAtPointOfCollectionView(Point point);
        public abstract NSIndexPath IndexPathForRowAtPointOfContentView(Point point);
        /// <summary>
        /// 返回IndexPath对应的行在ContentView中的位置. 在某些Item大小不固定的Layout中, 其可能是不精确的, 会变化的. 可能只是即时状态, 比如滑动后数据会变化.
        /// </summary>
        /// <returns></returns>
        public abstract Rect RectForRowOfIndexPathInContentView(NSIndexPath indexPath);
    }
}