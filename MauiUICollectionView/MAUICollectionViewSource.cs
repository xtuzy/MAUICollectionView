using MauiUICollectionView.Layouts;

namespace MauiUICollectionView
{
    public class MAUICollectionViewSource : IMAUICollectionViewSource
    {
        public Func<MAUICollectionView, int, int> NumberOfItems { get; set; }

        public Func<MAUICollectionView, NSIndexPath, MAUICollectionViewViewHolder, double, MAUICollectionViewViewHolder> ViewHolderForItem { get; set; }

        public Func<MAUICollectionView, int> NumberOfSections { get; set; }

        //public Func<MAUICollectionView, NSIndexPath, NSIndexPath> WillSelectItem { get; set; }

        //public Func<MAUICollectionView, NSIndexPath, NSIndexPath> WillDeselectItem { get; set; }

        public Action<MAUICollectionView, NSIndexPath> DidSelectItem { get; set; }

        public Action<MAUICollectionView, NSIndexPath> DidDeselectItem { get; set; }

        public Func<MAUICollectionView, NSIndexPath, double> HeightForItem { get; set; }

        public Func<MAUICollectionView, NSIndexPath, string> ReuseIdForItem { get; set; }

        public Func<MAUICollectionView, NSIndexPath, bool> IsSectionItem { get; set; }
        
        public Action<MAUICollectionView, NSIndexPath> OnDragStart { get; set; }

        public Action<MAUICollectionView, NSIndexPath, NSIndexPath> OnDragOver { get; set; }

        public Action<MAUICollectionView, NSIndexPath, NSIndexPath> OnDrop { get; set; }

        public Action<MAUICollectionView, NSIndexPath, MAUICollectionViewViewHolder, Edge> DidPrepareItem { get; set; }

        public Action<MAUICollectionView> WillArrange { get; set; }
    }
}
