namespace MauiUICollectionView
{
    public delegate NSIndexPath willXRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath);
    public delegate void didXRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath);
    public delegate float heightForRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath);
    public delegate SizeStrategy sizeStrategyForRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath);
    public delegate float heightForXInSectionDelegate(TableView tableView, int section);
    public delegate View viewForHeaderInSectionDelegate(TableView tableView, int section);
    public delegate void XRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath);
    public delegate string titleForDeleteConfirmationButtonForRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath);

    public delegate int numberOfRowsInSectionDelegate(TableView tableView, int section);
    public delegate TableViewViewHolder cellForRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath, bool isEmpty);
    public delegate int numberOfSectionsInTableViewDelegate(TableView tableView);
    public delegate string titleForXInSectionDelegate(TableView tableView, int section);
    public delegate string cellTypeForRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath);
    public delegate string EditintitleForFooterInSectionDelegate(TableView tableView, int section);
    public delegate void commitEditingStyleDelegate(TableView tableView, TableViewCellEditingStyle editingStyle, NSIndexPath indexPath);
    public delegate bool canEditRowAtIndexPathDelegate(TableView tableView, NSIndexPath indexPath);

    /// <summary>
    /// 最新的Api见
    /// https://learn.microsoft.com/en-us/dotnet/api/uikit.uitableviewdatasource.caneditrow?view=xamarin-ios-sdk-12
    /// </summary>
    public interface ITableViewSource
    {
        public numberOfRowsInSectionDelegate numberOfRowsInSection { get; }
        public cellForRowAtIndexPathDelegate cellForRowAtIndexPath { get; }
        public numberOfSectionsInTableViewDelegate numberOfSectionsInTableView { get; }
        public titleForXInSectionDelegate titleForHeaderInSection { get; }
        public titleForXInSectionDelegate titleForFooterInSection { get; }
        public commitEditingStyleDelegate commitEditingStyle { get; }
        public canEditRowAtIndexPathDelegate canEditRowAtIndexPath { get; }
        public cellTypeForRowAtIndexPathDelegate cellTypeForRowAtIndexPath { get; }

        willXRowAtIndexPathDelegate willSelectRowAtIndexPath { get; }
        willXRowAtIndexPathDelegate willDeselectRowAtIndexPath { get; }

        didXRowAtIndexPathDelegate didSelectRowAtIndexPath { get; }
        didXRowAtIndexPathDelegate didDeselectRowAtIndexPath { get; }

        heightForRowAtIndexPathDelegate heightForRowAtIndexPath { get; }
        sizeStrategyForRowAtIndexPathDelegate sizeStrategyForRowAtIndexPath { get; }
        heightForXInSectionDelegate heightForHeaderInSection { get; }
        heightForXInSectionDelegate heightForFooterInSection { get; }

        viewForHeaderInSectionDelegate viewForHeaderInSection { get; }
        viewForHeaderInSectionDelegate viewForFooterInSection { get; }

        XRowAtIndexPathDelegate willBeginEditingRowAtIndexPath { get; }
        XRowAtIndexPathDelegate didEndEditingRowAtIndexPath { get; }

        titleForDeleteConfirmationButtonForRowAtIndexPathDelegate titleForDeleteConfirmationButtonForRowAtIndexPath { get; }
    }
}