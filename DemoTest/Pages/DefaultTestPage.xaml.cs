using Bogus;
using MauiUICollectionView;
using MauiUICollectionView.Layouts;
using Microsoft.Maui.Controls.Shapes;
using SharpConstraintLayout.Maui.Widget;
using System.Diagnostics;
using Yang.Maui.Helper.Device.Screen;
using Yang.Maui.Helper.Image;
using MAUICollectionView = MauiUICollectionView.MAUICollectionView;
namespace DemoTest.Pages;

public partial class DefaultTestPage : ContentPage
{
#if WINDOWS || __ANDROID__ || __IOS__
    FrameRateCalculator fr;
#endif
    internal static ViewModel viewModel;
    public DefaultTestPage()
    {
        viewModel = new ViewModel();

        InitializeComponent();


#if WINDOWS || __ANDROID__ || __IOS__
        if (fr == null)
        {
            fr = new FrameRateCalculator();
            fr.FrameRateUpdated += (value) =>
            {
                this.Dispatcher.Dispatch(() => fpsLabel.Text = value.Frames.ToString());
            };
            fr.Start();
        }
#endif

        var tableView = new MAUICollectionView()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Always,
            Source = new Source(viewModel),
            SelectionMode = SelectionMode.Multiple,
            //CanDrag = true,
            CanContextMenu = true,
        };
        content.Content = tableView;
        tableView.ItemsLayout = new CollectionViewListLayout(tableView)
        {
        };

        //ѡ��Item
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
                tableView.SelectRowAtIndexPath(indexPath, false, ScrollPosition.None);
        };
        //tableView.Content.GestureRecognizers.Add(click);

        //Header
        var headerButton = new Button() { Text = "Header GoTo20", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        headerButton.Clicked += (s, e) =>
        {
            tableView.ScrollToRowAtIndexPath(NSIndexPath.FromRowSection(9, 1), ScrollPosition.Top, true);
            Debug.WriteLine("Clicked Header");
        };
        var headerView = new MAUICollectionViewViewHolder(headerButton, "Header");
        tableView.HeaderView = headerView;

        //Footer
        var footer = new VerticalStackLayout();
        var footerButton = new Button() { Text = "Footer GoTo20", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        footerButton.Clicked += (s, e) =>
        {
            tableView.ScrollToRowAtIndexPath(NSIndexPath.FromRowSection(9, 1), ScrollPosition.Top, true);
            Debug.WriteLine("Clicked Footer");
        };
        var footActivityIndicator = new ActivityIndicator() { Color = Colors.Red, IsVisible = false, IsRunning = false, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        footer.Add(footActivityIndicator);
        footer.Add(footerButton);

        tableView.FooterView = new MAUICollectionViewViewHolder(footer, "foot");

        tableView.BackgroundView = new Grid() { BackgroundColor = Colors.LightPink };

        this.Loaded += (sender, e) =>
        {
            Debug.WriteLine("Loaded");
        };
        this.Appearing += (sender, e) =>
        {
            tableView.ReAppear();//�л�Pageʱ����Item���ɼ�, ��Ҫ���¼���
            Debug.WriteLine("Appearing");
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
            var arg = NSIndexPath.FromRowSection(3, 0);
            var count = 3;
            var distance = arg.Row + count - viewModel.models[arg.Section].Count;
            if (distance < 0)
                (tableView.Source as Source).RemoveData(arg.Section, arg.Row, count);
            else
                (tableView.Source as Source).RemoveData(arg.Section, arg.Row, viewModel.models[arg.Section].Count - arg.Row);
            tableView.NotifyItemRangeRemoved(arg, count);
        };

        Move.Clicked += (sender, e) =>
        {
            var index = 3;
            var target = 1;
            (tableView.Source as Source).MoveData(index, target);
            tableView.MoveItem(NSIndexPath.FromRowSection(index, 0), NSIndexPath.FromRowSection(target, 0));
        };

        Change.Clicked += (sender, e) =>
        {
            var index = 2;
            (tableView.Source as Source).ChangeData(index);
            tableView.NotifyItemRangeChanged(new[] { NSIndexPath.FromRowSection(index + 1, 0) });
        };

        Reload.Clicked += (sender, e) =>
        {
            tableView.NotifyDataSetChanged();
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
            tableView.NotifyDataSetChanged();
            content.IsRefreshing = false;
        });
    }
}