namespace MauiUICollectionView
{
    public class MAUICollectionViewSource : IMAUICollectionViewSource
    {
        public numberOfRowsInSectionDelegate numberOfItemsInSection { get; set; }

        public cellForRowAtIndexPathDelegate cellForRowAtIndexPath { get; set; }

        public numberOfSectionsInTableViewDelegate numberOfSectionsInCollectionView { get; set; }

        public willXRowAtIndexPathDelegate willSelectRowAtIndexPath { get; set; }

        public willXRowAtIndexPathDelegate willDeselectRowAtIndexPath { get; set; }

        public didXRowAtIndexPathDelegate didSelectRowAtIndexPath { get; set; }

        public didXRowAtIndexPathDelegate didDeselectRowAtIndexPath { get; set; }

        public heightForRowAtIndexPathDelegate heightForRowAtIndexPath { get; set; }

        public reuseIdentifierForRowAtIndexPathDelegate reuseIdentifierForRowAtIndexPath { get; set; }
    }
}
