using Bogus;
using MauiUICollectionView;
using MauiUICollectionView.Layouts;
using Microsoft.Maui.Controls.Shapes;
using SharpConstraintLayout.Maui.Widget;
using Yang.Maui.Helper.Image;
using MAUICollectionView = MauiUICollectionView.MAUICollectionView;
namespace DemoTest.Pages;

public partial class GridLayoutTestPage : ContentPage
{
    public GridLayoutTestPage()
    {
        InitializeComponent();
        var tableView = new MAUICollectionView();
        content.Content = tableView;
        tableView.VerticalScrollBarVisibility = ScrollBarVisibility.Always;
        tableView.Source = new Source(DefaultTestPage.viewModel);
        tableView.ItemsLayout = new CollectionViewGridLayout(tableView)
        {
        };

        var click = new TapGestureRecognizer();
        click.Tapped += (s, e) =>
        {
            var p = e.GetPosition(tableView);
#if IOS
            var indexPath = tableView.ItemsLayout.ItemAtPoint(p.Value);
#else
            var indexPath = tableView.ItemsLayout.ItemAtPoint(p.Value, false);
#endif
            if (indexPath != null)
                tableView.SelectItem(indexPath, false, ScrollPosition.None);
        };
        tableView.Content.GestureRecognizers.Add(click);
        var headerButton = new Button() { Text = "Header", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        headerButton.Clicked += (s, e) =>
        {
            if ((tableView.ItemsLayout as CollectionViewGridLayout).ColumnCount == 1)
                (tableView.ItemsLayout as CollectionViewGridLayout).ColumnCount = 2;
            else
                (tableView.ItemsLayout as CollectionViewGridLayout).ColumnCount = 1;
            tableView.ReMeasure();
        };
        var headerView = new MAUICollectionViewViewHolder(headerButton, "Header");
        tableView.HeaderView = headerView;
        var footerButton = new Button() { Text = "Footer GoTo20", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        footerButton.Clicked += (s, e) =>
        {
            tableView.ScrollToItem(NSIndexPath.FromRowSection(20, 0), ScrollPosition.Top, true);
            Console.WriteLine("Clicked Footer");
        };
        tableView.FooterView = new MAUICollectionViewViewHolder(footerButton, "Footer");
        this.Loaded += (sender, e) =>
        {
            Console.WriteLine("Loaded");
        };
        this.Appearing += (sender, e) =>
        {
            tableView.ReAppear();
            Console.WriteLine("Appearing");
        };

        //Add
        Add.Clicked += (sender, e) =>
        {
            var index = 3;
            var count = 3;
            (tableView.Source as Source).InsertData(0, index, count);
            tableView.NotifyItemRangeInserted(NSIndexPath.FromRowSection(index, 0), count);
        };

        Remove.Clicked += (sender, e) =>
        {
            var index = 3;
            var count = 3;
            (tableView.Source as Source).RemoveData(0, index, count);
            tableView.NotifyItemRangeRemoved(NSIndexPath.FromRowSection(index, 0), count);
        };

        Move.Clicked += (sender, e) =>
        {
            var index = 3;
            var target = 1;
            (tableView.Source as Source).MoveData(index, target);
            tableView.MoveItem(NSIndexPath.FromRowSection(index, 0), NSIndexPath.FromRowSection(target, 0));
            tableView.ReMeasure();
        };

        Change.Clicked += (sender, e) =>
        {
            var index = 2;
            (tableView.Source as Source).ChangeData(index);
            tableView.NotifyItemRangeChanged(NSIndexPath.FromRowSection(index, 0));
            tableView.ReMeasure();
        };
    }
}