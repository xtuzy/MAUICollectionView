namespace MauiUICollectionView
{
    public delegate NSIndexPath willXRowAtIndexPathDelegate(MAUICollectionView tableView, NSIndexPath indexPath);
    public delegate void didXRowAtIndexPathDelegate(MAUICollectionView tableView, NSIndexPath indexPath);
    public delegate float heightForRowAtIndexPathDelegate(MAUICollectionView tableView, NSIndexPath indexPath);
    public delegate string reuseIdentifierForRowAtIndexPathDelegate(MAUICollectionView tableView, NSIndexPath indexPath);

    public delegate int numberOfRowsInSectionDelegate(MAUICollectionView tableView, int section);
    /// <summary>
    /// 获取IndexPath对应的ViewHolder
    /// </summary>
    /// <param name="tableView"></param>
    /// <param name="indexPath"></param>
    /// <param name="oldViewHolder">依旧可见的, 仅被移动的ViewHolder, 其可能需要更新IndexPath信息</param>
    /// <param name="widthConstrain">对ViewHolder可能需要大小设置时, 这个宽度是一个参考值, 对于GridLayout, 其平分CollectionView的宽度, 对于ListLayout其等于CollectionView的宽</param>
    /// <returns></returns>
    public delegate MAUICollectionViewViewHolder cellForRowAtIndexPathDelegate(MAUICollectionView tableView, NSIndexPath indexPath, MAUICollectionViewViewHolder oldViewHolder, double widthConstrain);
    public delegate int numberOfSectionsInTableViewDelegate(MAUICollectionView tableView);

    /// <summary>
    /// 最新的Api见
    /// https://learn.microsoft.com/en-us/dotnet/api/uikit.uitableviewdatasource.caneditrow?view=xamarin-ios-sdk-12
    /// </summary>
    public interface IMAUICollectionViewSource
    {
        public Func<MAUICollectionView, int, int> NumberOfItems { get; }
        /// <summary>
        /// 获取对应IndexPath的ViewHolder
        /// </summary>
        public Func<MAUICollectionView, NSIndexPath, MAUICollectionViewViewHolder, double, MAUICollectionViewViewHolder> ViewHolderForItem { get; }
        public Func<MAUICollectionView, int> NumberOfSections { get; }

        Func<MAUICollectionView, NSIndexPath, NSIndexPath> WillSelectItem { get; }
        Func<MAUICollectionView, NSIndexPath, NSIndexPath> WillDeselectItem { get; }

        Action<MAUICollectionView, NSIndexPath> DidSelectItem { get; }
        Action<MAUICollectionView, NSIndexPath> DidDeselectItem { get; }

        Func<MAUICollectionView, NSIndexPath, double> HeightForItem { get; }
        Func<MAUICollectionView, NSIndexPath, string> ReuseIdForItem { get; }
        /// <summary>
        /// When running Drag operate, will still load it.
        /// </summary>
        public Action<MAUICollectionView, NSIndexPath, NSIndexPath> WantDragTo { get; }
        /// <summary>
        /// When finish Drag operate, will load it.
        /// </summary>
        public Action<MAUICollectionView, NSIndexPath, NSIndexPath> WantDropTo { get; }

        /// <summary>
        /// prepared items, will show, now you can modify some action of items
        /// </summary>
        public Action<MAUICollectionView, NSIndexPath, MAUICollectionViewViewHolder> DidPrepareItem { get; }
        public Action<MAUICollectionView> WillArrange { get; }
    }
}