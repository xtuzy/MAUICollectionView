using MauiUICollectionView;
using MauiUICollectionView.Layouts;
using System.Diagnostics;
using Yang.Maui.Helper.Device.Screen;
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
        var refreshview = new RefreshView();
        var tableView = new MAUICollectionView()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Always,
            HeightExpansionFactor = 0,
            SelectionMode = SelectionMode.Multiple,
            CanDrag = true,
            CanContextMenu = true,
        };
        tableView.ItemsLayout = new CollectionViewFlatListLayout(tableView)
        {
        };
#if WINDOWS
        content.Content = tableView;
#else
        content.Content = refreshview;
        refreshview.Content = tableView;
#endif
        this.SetSource.Clicked += (sender, e) =>
        {
            tableView.Source = new Source(viewModel);
        };
        this.RemoveSource.Clicked += (sender, e) =>
        {
            tableView.Source = null;
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
                tableView.SelectItem(indexPath, false, ScrollPosition.None);
        };
        //tableView.Content.GestureRecognizers.Add(click);

        //Header
        var headerButton = new Button() { Text = "Header GoTo 49-10", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        headerButton.Clicked += (s, e) =>
        {
            tableView.ScrollToItem(NSIndexPath.FromRowSection(10, 49), ScrollPosition.Top, true);
            Debug.WriteLine("Clicked Header");
        };
        var headerView = new MAUICollectionViewViewHolder(headerButton, "Header");
        tableView.HeaderView = headerView;

        //Footer
        var footer = new VerticalStackLayout();
        var footerButton = new Button() { Text = "Footer GoTo 1-9", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        footerButton.Clicked += (s, e) =>
        {
            tableView.ScrollToItem(NSIndexPath.FromRowSection(9, 1), ScrollPosition.Top, true);
            Debug.WriteLine("Clicked Footer");
        };
        var footActivityIndicator = new ActivityIndicator() { Color = Colors.Red, IsVisible = false, IsRunning = false, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        footer.Add(footActivityIndicator);
        footer.Add(footerButton);

        tableView.FooterView = new MAUICollectionViewViewHolder(footer, "foot");

        tableView.EmptyView = new Grid() { BackgroundColor = Colors.Yellow, Children = { new Label() { Text = "No data!", TextColor = Colors.Black, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center } } };
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
            var item = NSIndexPath.FromRowSection(3, 0);
            var count = 3;
            (tableView.Source as Source).InsertData(item.Section, item.Row - 1, count);//have a section item, so row need -1
            tableView.NotifyItemRangeInserted(item, count);
        };

        Remove.Clicked += (sender, e) =>
        {
            var item = NSIndexPath.FromRowSection(3, 0);
            var count = 3;
            var distance = item.Row + count - viewModel.models[item.Section].Count;
            if (distance < 0)
                (tableView.Source as Source).RemoveData(item.Section, item.Row - 1, count);
            else
                (tableView.Source as Source).RemoveData(item.Section, item.Row - 1, viewModel.models[item.Section].Count - item.Row);
            tableView.NotifyItemRangeRemoved(item, count);
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
            if (tableView.ItemsLayout is CollectionViewFlatListLayout)
                tableView.ItemsLayout = new CollectionViewGridLayout(tableView);
            else
                tableView.ItemsLayout = new CollectionViewFlatListLayout(tableView);
            tableView.ReMeasure();
        };

        refreshview.Command = new Command(() =>
        {
            if (OperatingSystem.IsWindows()) return;//RefreshView will load many times when drag scroll bar
            (tableView.Source as Source).LoadMoreOnFirst();
            tableView.NotifyDataSetChanged();
            refreshview.IsRefreshing = false;
        });

        this.SizeChanged += DefaultTestPage_SizeChanged;
    }

    private void DefaultTestPage_SizeChanged(object sender, EventArgs e)
    {
        Debug.WriteLine(nameof(DefaultTestPage_SizeChanged));
    }
}