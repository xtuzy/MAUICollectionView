using Bogus;
using MauiUICollectionView;
using MauiUICollectionView.Layouts;
using Microsoft.Maui.Controls.Shapes;
using SharpConstraintLayout.Maui.Widget;
using Yang.Maui.Helper.Image;
using MAUICollectionView = MauiUICollectionView.MAUICollectionView;
namespace DemoTest.Pages;

public partial class DefaultTestPage : ContentPage
{
    internal static ViewModel viewModel;
    public DefaultTestPage()
    {
        viewModel = new ViewModel();

        InitializeComponent();
        var tableView = new MAUICollectionView();
        content.Content = tableView;
        tableView.VerticalScrollBarVisibility = ScrollBarVisibility.Always;
        tableView.Source = new Source(viewModel);
        tableView.ItemsLayout = new CollectionViewListLayout(tableView)
        {
        };

        //ѡ��Item
        var click = new TapGestureRecognizer();
        click.Tapped += (s, e) =>
        {
            var p = e.GetPosition(tableView);
#if IOS
            var indexPath = tableView.ItemsLayout.IndexPathForRowAtPointOfContentView(p.Value);
#else
            var indexPath = tableView.ItemsLayout.IndexPathForVisibaleRowAtPointOfCollectionView(p.Value);
#endif
            if (indexPath != null)
                tableView.SelectRowAtIndexPath(indexPath, false, ScrollPosition.None);
        };
        //tableView.Content.GestureRecognizers.Add(click);

        //Header
        var headerButton = new Button() { Text = "Header GoTo20", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        headerButton.Clicked += (s, e) =>
        {
            tableView.ScrollToRowAtIndexPath(NSIndexPath.FromRowSection(20, 0), ScrollPosition.Top, true);
            Console.WriteLine("Clicked Header");
        };
        var headerView = new MAUICollectionViewViewHolder(headerButton, "Header");
        tableView.HeaderView = headerView;

        //Footer
        var footer = new VerticalStackLayout();
        var footerButton = new Button() { Text = "Footer GoTo20", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        footerButton.Clicked += (s, e) =>
        {
            tableView.ScrollToRowAtIndexPath(NSIndexPath.FromRowSection(20, 0), ScrollPosition.Top, true);
            Console.WriteLine("Clicked Footer");
        };
        var footActivityIndicator = new ActivityIndicator() { Color = Colors.Red, IsVisible = false, IsRunning = false, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        footer.Add(footActivityIndicator);
        footer.Add(footerButton);

        tableView.FooterView = new MAUICollectionViewViewHolder(footer, "foot");

        tableView.BackgroundView = new Grid() { BackgroundColor = Colors.LightPink };

        this.Loaded += (sender, e) =>
        {
            Console.WriteLine("Loaded");
        };
        this.Appearing += (sender, e) =>
        {
            tableView.ReAppear();//�л�Pageʱ����Item���ɼ�, ��Ҫ���¼���
            Console.WriteLine("Appearing");
        };

        //Add
        Add.Clicked += (sender, e) =>
        {
            var index = 2;
            (tableView.Source as Source).InsertData(index);
            tableView.InsertItems(NSIndexPath.FromRowSection(index, 0));
            tableView.ReMeasure();
        };

        Remove.Clicked += (sender, e) =>
        {
            var index = 2;
            (tableView.Source as Source).RemoveData(index);
            tableView.RemoveItems(NSIndexPath.FromRowSection(index, 0));
            tableView.ReMeasure();
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
            tableView.ChangeItem(new[] { NSIndexPath.FromRowSection(index, 0) });
            tableView.ReMeasure();
        };

        Reload.Clicked += (sender, e) =>
        {
            tableView.ReloadData();
        };

        ChangeLayout.Clicked += (sender, e) =>
        {
            if (tableView.ItemsLayout is CollectionViewListLayout)
                tableView.ItemsLayout = new CollectionViewGridLayout(tableView);
            else
                tableView.ItemsLayout = new CollectionViewListLayout(tableView);
            tableView.ReMeasure();
        };

        content.Command = new Command(() =>
        {
            (tableView.Source as Source).LoadMoreOnFirst();
            tableView.ReloadData();
            content.IsRefreshing = false;
        });
    }
}