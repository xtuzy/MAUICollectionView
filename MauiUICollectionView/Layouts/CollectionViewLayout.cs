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
    }
}