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
        public numberOfRowsInSectionDelegate NumberOfItems { get; }
        /// <summary>
        /// 获取对应IndexPath的ViewHolder
        /// </summary>
        public cellForRowAtIndexPathDelegate ViewHolderForItem { get; }
        public numberOfSectionsInTableViewDelegate NumberOfSections { get; }

        willXRowAtIndexPathDelegate WillSelectItem { get; }
        willXRowAtIndexPathDelegate WillDeselectItem { get; }

        didXRowAtIndexPathDelegate DidSelectItem { get; }
        didXRowAtIndexPathDelegate DidDeselectItem { get; }

        heightForRowAtIndexPathDelegate HeightForItem { get; }
        reuseIdentifierForRowAtIndexPathDelegate ReuseIdForItem { get; }

        public Action<MAUICollectionView, NSIndexPath, NSIndexPath> WantDragTo { get; }

        /// <summary>
        /// prepared items, will show, now you can modify some action of items
        /// </summary>
        public Action<MAUICollectionView, NSIndexPath, MAUICollectionViewViewHolder> DidPrepareItem { get; }
        public Action<MAUICollectionView> WillArrange { get; }
    }
}