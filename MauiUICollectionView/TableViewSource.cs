﻿namespace MauiUICollectionView
{
    public class TableViewSource : ITableViewSource
    {
        public numberOfRowsInSectionDelegate numberOfRowsInSection { get; set; }

        public cellForRowAtIndexPathDelegate cellForRowAtIndexPath { get; set; }

        public numberOfSectionsInTableViewDelegate numberOfSectionsInTableView { get; set; }

        public willXRowAtIndexPathDelegate willSelectRowAtIndexPath { get; set; }

        public willXRowAtIndexPathDelegate willDeselectRowAtIndexPath { get; set; }

        public didXRowAtIndexPathDelegate didSelectRowAtIndexPath { get; set; }

        public didXRowAtIndexPathDelegate didDeselectRowAtIndexPath { get; set; }

        public heightForRowAtIndexPathDelegate heightForRowAtIndexPath { get; set; }

        public reuseIdentifierForRowAtIndexPathDelegate reuseIdentifierForRowAtIndexPath { get; set; }
    }
}
