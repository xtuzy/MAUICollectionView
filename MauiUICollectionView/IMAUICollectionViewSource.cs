namespace MauiUICollectionView
{
    public interface IMAUICollectionViewSource
    {
        /// <summary>
        /// Items count in this section.
        /// </summary>
        public Func<MAUICollectionView, int, int> NumberOfItems { get; }
        
        /// <summary>
        /// Get and set ViewHolder.
        /// </summary>
        public Func<MAUICollectionView, NSIndexPath, MAUICollectionViewViewHolder, double, MAUICollectionViewViewHolder> ViewHolderForItem { get; }
        
        /// <summary>
        /// Section count.
        /// </summary>
        public Func<MAUICollectionView, int> NumberOfSections { get; }

        //Func<MAUICollectionView, NSIndexPath, NSIndexPath> WillSelectItem { get; }
        //Func<MAUICollectionView, NSIndexPath, NSIndexPath> WillDeselectItem { get; }

        Action<MAUICollectionView, NSIndexPath> DidSelectItem { get; }
        Action<MAUICollectionView, NSIndexPath> DidDeselectItem { get; }

        /// <summary>
        /// Tell layout how to calculate item's size. If it is not fix value, return <see cref="MAUICollectionViewViewHolder.AutoSize"/>.
        /// </summary>
        Func<MAUICollectionView, NSIndexPath, double> HeightForItem { get; }

        /// <summary>
        /// Use the same ID for the same type of item.
        /// </summary>
        Func<MAUICollectionView, NSIndexPath, string> ReuseIdForItem { get; }
        
        /// <summary>
        /// Some complex layout need know which item is section's header or footer.
        /// Advice header's row use 0, footer's row use data's count+1.
        /// </summary>
        Func<MAUICollectionView, NSIndexPath, bool> IsSectionItem { get; }
        
        /// <summary>
        /// When drag gesture start work, it will be called.
        /// </summary>
        public Action<MAUICollectionView, NSIndexPath> OnDragStart { get; }

        /// <summary>
        /// When drag gesture still running, it will be called.
        /// </summary>
        public Action<MAUICollectionView, NSIndexPath, NSIndexPath> OnDragOver { get; }

        /// <summary>
        /// When finish drag gesture, it will be called.
        /// </summary>
        public Action<MAUICollectionView, NSIndexPath, NSIndexPath> OnDrop { get; }

        /// <summary>
        /// The default bounds has been calculated, it is stored in <see cref="MAUICollectionViewViewHolder.BoundsInLayout"/>, you can change size and remeasure it to implement some layout animation.
        /// </summary>
        public Action<MAUICollectionView, NSIndexPath, MAUICollectionViewViewHolder> DidPrepareItem { get; }

        /// <summary>
        /// All the prepared items have been calculated. Before arrange, we can do something based on these items’ data, such as loading more.
        /// </summary>
        public Action<MAUICollectionView> WillArrange { get; }
    }
}