namespace MauiUICollectionView
{
    public delegate NSIndexPath willXRowAtIndexPathDelegate(MAUICollectionView tableView, NSIndexPath indexPath);
    public delegate void didXRowAtIndexPathDelegate(MAUICollectionView tableView, NSIndexPath indexPath);
    public delegate float heightForRowAtIndexPathDelegate(MAUICollectionView tableView, NSIndexPath indexPath);
    public delegate string reuseIdentifierForRowAtIndexPathDelegate(MAUICollectionView tableView, NSIndexPath indexPath);

    public delegate int numberOfRowsInSectionDelegate(MAUICollectionView tableView, int section);
    public delegate MAUICollectionViewViewHolder cellForRowAtIndexPathDelegate(MAUICollectionView tableView, NSIndexPath indexPath, double widthConstrain, bool needEmpty);
    public delegate int numberOfSectionsInTableViewDelegate(MAUICollectionView tableView);

    /// <summary>
    /// 最新的Api见
    /// https://learn.microsoft.com/en-us/dotnet/api/uikit.uitableviewdatasource.caneditrow?view=xamarin-ios-sdk-12
    /// </summary>
    public interface IMAUICollectionViewSource
    {
        public numberOfRowsInSectionDelegate numberOfItemsInSection { get; }
        /// <summary>
        /// 获取对应IndexPath的ViewHolder
        /// </summary>
        public cellForRowAtIndexPathDelegate cellForRowAtIndexPath { get; }
        public numberOfSectionsInTableViewDelegate numberOfSectionsInCollectionView { get; }

        willXRowAtIndexPathDelegate willSelectRowAtIndexPath { get; }
        willXRowAtIndexPathDelegate willDeselectRowAtIndexPath { get; }

        didXRowAtIndexPathDelegate didSelectRowAtIndexPath { get; }
        didXRowAtIndexPathDelegate didDeselectRowAtIndexPath { get; }

        heightForRowAtIndexPathDelegate heightForRowAtIndexPath { get; }
        reuseIdentifierForRowAtIndexPathDelegate reuseIdentifierForRowAtIndexPath { get; }

        public Action<MAUICollectionView, NSIndexPath> lastItemWillShow { get; }
    }
}