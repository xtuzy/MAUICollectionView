namespace MauiUICollectionView
{
    public class MAUICollectionViewSource : IMAUICollectionViewSource
    {
        public numberOfRowsInSectionDelegate NumberOfItems { get; set; }

        public cellForRowAtIndexPathDelegate ViewHolderForItem { get; set; }

        public numberOfSectionsInTableViewDelegate NumberOfSections { get; set; }

        public willXRowAtIndexPathDelegate WillSelectItem { get; set; }

        public willXRowAtIndexPathDelegate WillDeselectItem { get; set; }

        public didXRowAtIndexPathDelegate DidSelectItem { get; set; }

        public didXRowAtIndexPathDelegate DidDeselectItem { get; set; }

        public heightForRowAtIndexPathDelegate HeightForItem { get; set; }

        public reuseIdentifierForRowAtIndexPathDelegate ReuseIdForItem { get; set; }

        public Action<MAUICollectionView, NSIndexPath, NSIndexPath> WantDragTo { get; set; }

        public Action<MAUICollectionView, NSIndexPath, MAUICollectionViewViewHolder> DidPrepareItem { get; set; }

        public Action<MAUICollectionView> WillArrange { get; set; }
    }
}
