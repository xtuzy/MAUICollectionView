using MauiUICollectionView;
using MauiUICollectionView.Layouts;
using TableView = MauiUICollectionView.TableView;

namespace DemoTest.Pages;

public partial class DefaultTestPage : ContentPage
{
    public DefaultTestPage()
    {
        InitializeComponent();
        var tableView = new TableView();
        Content = tableView;
        tableView.VerticalScrollBarVisibility = ScrollBarVisibility.Always;
        tableView.Source = new Source();
        tableView.ItemsLayout = new CollectionViewListLayout(tableView)
        {
        };

        var click = new TapGestureRecognizer();
        click.Tapped += (s, e) =>
        {
            var p = e.GetPosition(tableView);
#if IOS
            var indexPath = tableView.IndexPathForRowAtPointOfContentView(p.Value);
#else
            var indexPath = tableView.IndexPathForVisibaleRowAtPointOfTableView(p.Value);
#endif
            if (indexPath != null)
                tableView.SelectRowAtIndexPath(indexPath, false, TableViewScrollPosition.None);
        };
        tableView.Content.GestureRecognizers.Add(click);
        var headerButton = new Button() { Text = "Header", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        headerButton.Clicked += (s, e) =>
        {
            Console.WriteLine("Clicked Header");
        };
        var headerView = new TableViewViewHolder("Header")
        {
            HeightRequest = 50,
            BackgroundColor = Colors.Red,
            Children =
            {
                headerButton
            }
        };
        tableView.TableHeaderView = headerView;
        var footerButton = new Button() { Text = "Footer", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        footerButton.Clicked += (s, e) =>
        {
            Console.WriteLine("Clicked Footer");
        };
        tableView.TableFooterView = new TableViewViewHolder("Footer")
        {
            HeightRequest = 50,
            BackgroundColor = Colors.Red,
            Children =
            {
                footerButton
            }
        };
    }

    class Source : TableViewSource
    {
        public Source()
        {
            heightForRowAtIndexPath += heightForRowAtIndexPathMethod;
            cellTypeForRowAtIndexPath += cellTypeForRowAtIndexPathMethod;
            numberOfRowsInSection += numberOfRowsInSectionMethod;
            cellForRowAtIndexPath += cellForRowAtIndexPathMethod;
            sizeStrategyForRowAtIndexPath += sizeStrategyForRowAtIndexPathMethod;
            numberOfSectionsInTableView += numberOfSectionsInTableViewMethod;
        }

        public int numberOfSectionsInTableViewMethod(TableView tableView)
        {
            return 10;
        }

        public int numberOfRowsInSectionMethod(TableView tableView, int section)
        {
            return 50;
        }

        public string cellTypeForRowAtIndexPathMethod(TableView tableView, NSIndexPath indexPath)
        {
            if (indexPath.Row == 0)
            {
                return sectionCell;
            }
            if (indexPath.Row % 2 == 0)
            {
                return botCell;
            }
            else if (indexPath.Row % 3 == 0)
            {
                return youdaoCell;
            }
            else
            {
                return baiduCell;
            }
        }

        public SizeStrategy sizeStrategyForRowAtIndexPathMethod(TableView tableView, NSIndexPath indexPath)
        {
            var type = cellTypeForRowAtIndexPathMethod(tableView, indexPath);
            switch (type)
            {
                case sectionCell:
                    return SizeStrategy.FixedSize;
                case youdaoCell:
                    //return SizeStrategy.MeasureSelf;
                case baiduCell:
                    //return SizeStrategy.MeasureSelfAndMinFixedSize;
                default:
                    return SizeStrategy.FixedSize;
            }
        }

        public float heightForRowAtIndexPathMethod(TableView tableView, NSIndexPath indexPath)
        {
            var type = cellTypeForRowAtIndexPathMethod(tableView, indexPath);
            switch (type)
            {
                case sectionCell:
                    return 40;
                case botCell:
                    return 100;
                case youdaoCell:
                    return 100;
                case baiduCell:
                    return 80;
                default:
                    return 100;
            }
        }

        static int newCellCount = 0;
        //给每个cell设置ID号（重复利用时使用）
        const string sectionCell = "sectionCell";
        const string botCell = "botCell";
        const string youdaoCell = "youdaoCell";
        const string baiduCell = "baiduCell";
        public TableViewViewHolder cellForRowAtIndexPathMethod(TableView tableView, NSIndexPath indexPath, bool blank)
        {
            //从tableView的一个队列里获取一个cell
            var type = cellTypeForRowAtIndexPathMethod(tableView, indexPath);
            TableViewViewHolder cell = tableView.dequeueReusableCellWithIdentifier(type);

            if (type == botCell)
            {
                //判断队列里面是否有这个cell 没有自己创建，有直接使用
                if (cell == null)
                {
                    //没有,创建一个
                    cell = new ImageCell(type) { };
                    (cell as ImageCell).NewCellIndex = ++newCellCount;
                    Console.WriteLine($"newCell: {newCellCount}");
                    cell.BackgroundColor = Colors.Pink;
                }

                if (!blank)
                {
                    //使用cell
                    (cell as ImageCell).Image.Source = "dotnet_bot.png";
                    cell.IsEmpty = false;
                }
            }
            else if (type == youdaoCell)
            {
                //判断队列里面是否有这个cell 没有自己创建，有直接使用
                if (cell == null)
                {
                    //没有,创建一个
                    cell = new ImageCell(type) { };
                    cell.IsClippedToBounds = true;
                    (cell as Cell).NewCellIndex = ++newCellCount;
                    Console.WriteLine($"newCell: {newCellCount}");
                }

                if (!blank)
                {
                    //使用cell
                    (cell as ImageCell).Image.Source = "https://ydlunacommon-cdn.nosdn.127.net/cb776e6995f1c703706cf8c4c39a7520.png";
                    cell.IsEmpty = false;
                }
            }
            else if (type == baiduCell)
            {
                //判断队列里面是否有这个cell 没有自己创建，有直接使用
                if (cell == null)
                {
                    //没有,创建一个
                    cell = new ImageCell(type) { };
                    cell.IsClippedToBounds = true;
                    (cell as Cell).NewCellIndex = ++newCellCount;
                    Console.WriteLine($"newCell: {newCellCount}");
                }
                if (!blank)
                {
                    //使用cell
                    (cell as ImageCell).Image.Source = "https://www.baidu.com/img/PCtm_d9c8750bed0b3c7d089fa7d55720d6cf.png";
                    cell.IsEmpty = false;
                }
            }
            else if (type == sectionCell)
            {
                //判断队列里面是否有这个cell 没有自己创建，有直接使用
                if (cell == null)
                {
                    //没有,创建一个
                    cell = new Cell(type) { };
                    cell.IsClippedToBounds = true;
                    (cell as Cell).NewCellIndex = ++newCellCount;
                    Console.WriteLine($"newCell: {newCellCount}");
                }
                if (!blank)
                {
                    cell.IsEmpty = false;
                }
            }
            if (!blank)
            {
                cell.TextLabel.Text = $"Section={indexPath.Section} Row={indexPath.Row} newCellIndex={(cell as Cell).NewCellIndex}";
            }
            return cell;
        }
    }

    internal class Cell : TableViewViewHolder
    {
        public int NewCellIndex;

        public Cell(string reuseIdentifier) : base(reuseIdentifier)
        {
            this.BackgroundColor = Colors.Gray;
            IsClippedToBounds = true;
            this.SelectedBackgroundView = new Grid() { BackgroundColor = Colors.Red };
        }

        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
            TextLabel.Text = null;
        }
    }

    internal class ImageCell : Cell
    {
        public ImageCell(string reuseIdentifier) : base(reuseIdentifier)
        {
            Image = new Microsoft.Maui.Controls.Image() {HeightRequest=100, BackgroundColor = Colors.Green, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            var indicator = new ActivityIndicator { Color = new Color(0.5f), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            indicator.SetBinding(ActivityIndicator.IsRunningProperty, "IsLoading");
            indicator.BindingContext = Image;
            this.ContentView.Add(Image);
            this.ContentView.Add(indicator);
        }

        public Microsoft.Maui.Controls.Image Image;

        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
            Image.Source = null;
        }
    }
}