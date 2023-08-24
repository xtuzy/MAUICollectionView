namespace MauiUICollectionView
{
    /// <summary>
    /// 最新的Api见
    /// https://learn.microsoft.com/en-us/dotnet/api/uikit.uitableviewdatasource.caneditrow?view=xamarin-ios-sdk-12
    /// </summary>
    public interface IMAUICollectionViewSource
    {
        public Func<MAUICollectionView, int, int> NumberOfItems { get; }
        /// <summary>
        /// Get and set ViewHolder.
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
        /// If item is section's header or footer, item's Row of IndexPath must be 0 or last one, <see cref="Layouts.CollectionViewLayout"/> can layout according to it.
        /// </summary>
        Func<MAUICollectionView, NSIndexPath, bool> IsSectionItem { get; }

        public Action<MAUICollectionView, NSIndexPath> OnDragStart { get; }
        /// <summary>
        /// When running Drag operate, will still load it.
        /// </summary>
        public Action<MAUICollectionView, NSIndexPath, NSIndexPath> OnDragOver { get; }
        /// <summary>
        /// When finish Drag operate, will load it.
        /// </summary>
        public Action<MAUICollectionView, NSIndexPath, NSIndexPath> OnDrop { get; }

        /// <summary>
        /// prepared items, will show, now you can modify some action of items
        /// </summary>
        public Action<MAUICollectionView, NSIndexPath, MAUICollectionViewViewHolder> DidPrepareItem { get; }
        public Action<MAUICollectionView> WillArrange { get; }
    }
}