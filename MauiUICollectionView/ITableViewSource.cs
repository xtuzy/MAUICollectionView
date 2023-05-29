namespace MauiUICollectionView
{
    public delegate NSIndexPath willXRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath);
    public delegate void didXRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath);
    public delegate float heightForRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath);
    public delegate string reuseIdentifierForRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath);

    public delegate int numberOfRowsInSectionDelegate(TableView tableView, int section);
    public delegate TableViewViewHolder cellForRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath, double widthConstrain, bool isEmpty);
    public delegate int numberOfSectionsInTableViewDelegate(TableView tableView);

    /// <summary>
    /// 最新的Api见
    /// https://learn.microsoft.com/en-us/dotnet/api/uikit.uitableviewdatasource.caneditrow?view=xamarin-ios-sdk-12
    /// </summary>
    public interface ITableViewSource
    {
        public numberOfRowsInSectionDelegate numberOfRowsInSection { get; }
        /// <summary>
        /// 获取对应IndexPath的ViewHolder
        /// </summary>
        public cellForRowAtIndexPathDelegate cellForRowAtIndexPath { get; }
        public numberOfSectionsInTableViewDelegate numberOfSectionsInTableView { get; }

        willXRowAtIndexPathDelegate willSelectRowAtIndexPath { get; }
        willXRowAtIndexPathDelegate willDeselectRowAtIndexPath { get; }

        didXRowAtIndexPathDelegate didSelectRowAtIndexPath { get; }
        didXRowAtIndexPathDelegate didDeselectRowAtIndexPath { get; }

        heightForRowAtIndexPathDelegate heightForRowAtIndexPath { get; }
        reuseIdentifierForRowAtIndexPathDelegate reuseIdentifierForRowAtIndexPath { get; }
    }
}