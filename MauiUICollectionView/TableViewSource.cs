namespace MauiUICollectionView
{
    public class TableViewSource : ITableViewSource
    {
        public numberOfRowsInSectionDelegate numberOfRowsInSection { get; set; }

        public cellForRowAtIndexPathDelegate cellForRowAtIndexPath { get; set; }

        public numberOfSectionsInTableViewDelegate numberOfSectionsInTableView { get; set; }

        public titleForXInSectionDelegate titleForHeaderInSection { get; set; }

        public titleForXInSectionDelegate titleForFooterInSection { get; set; }

        public commitEditingStyleDelegate commitEditingStyle { get; set; }

        public canEditRowAtIndexPathDelegate canEditRowAtIndexPath { get; set; }

        public cellTypeForRowAtIndexPathDelegate cellTypeForRowAtIndexPath { get; set; }

        public willXRowAtIndexPathDelegate willSelectRowAtIndexPath { get; set; }

        public willXRowAtIndexPathDelegate willDeselectRowAtIndexPath { get; set; }

        public didXRowAtIndexPathDelegate didSelectRowAtIndexPath { get; set; }

        public didXRowAtIndexPathDelegate didDeselectRowAtIndexPath { get; set; }

        public heightForRowAtIndexPathDelegate heightForRowAtIndexPath { get; set; }

        public heightForXInSectionDelegate heightForHeaderInSection { get; set; }

        public heightForXInSectionDelegate heightForFooterInSection { get; set; }

        public viewForHeaderInSectionDelegate viewForHeaderInSection { get; set; }

        public viewForHeaderInSectionDelegate viewForFooterInSection { get; set; }

        public XRowAtIndexPathDelegate willBeginEditingRowAtIndexPath { get; set; }

        public XRowAtIndexPathDelegate didEndEditingRowAtIndexPath { get; set; }

        public titleForDeleteConfirmationButtonForRowAtIndexPathDelegate titleForDeleteConfirmationButtonForRowAtIndexPath { get; set; }

        public sizeStrategyForRowAtIndexPathDelegate sizeStrategyForRowAtIndexPath { get; set; }
    }
}
