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
        var headerView = new TableViewViewHolder(headerButton, "Header");
        tableView.TableHeaderView = headerView;
        var footerButton = new Button() { Text = "Footer", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        footerButton.Clicked += (s, e) =>
        {
            Console.WriteLine("Clicked Footer");
        };
        tableView.TableFooterView = new TableViewViewHolder(footerButton, "Footer");
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
                    //return SizeStrategy.FixedSize;
                case youdaoCell:
                    //return SizeStrategy.MeasureSelf;
                case baiduCell:
                    //return SizeStrategy.MeasureSelfGreaterThanMinFixedSize;
                default:
                    //return SizeStrategy.FixedSize;
                    return SizeStrategy.MeasureSelf;
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

            if (type == sectionCell)
            {
                var textCell = cell as TextCell;
                //判断队列里面是否有这个cell 没有自己创建，有直接使用
                if (textCell == null)
                {
                    //没有,创建一个
                    textCell = new TextCell(new Label(), type) { };
                    textCell.NewCellIndex = ++newCellCount;
                    Console.WriteLine($"newCell: {newCellCount}");
                }
                if (!blank)
                {
                    textCell.IsEmpty = false;
                    textCell.TextView.Text = $"Section={indexPath.Section} Row={indexPath.Row} newCellIndex={textCell.NewCellIndex}";
                }
                cell = textCell;
            }
            else
            {
                var imageCell = cell as ImageCell;
                if (imageCell == null)
                {
                    //没有,创建一个
                    imageCell = new ImageCell(new Image() { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }, type) { };
                    imageCell.NewCellIndex = ++newCellCount;
                    Console.WriteLine($"newCell: {newCellCount}");
                }
                if (!blank)
                {
                    if (type == botCell)
                    {
                        imageCell.ImageView.Source = "dotnet_bot.png";
                    }
                    else if (type == youdaoCell)
                    {
                        imageCell.ImageView.Source = "https://ydlunacommon-cdn.nosdn.127.net/cb776e6995f1c703706cf8c4c39a7520.png";
                    }
                    else if (type == baiduCell)
                    {
                        imageCell.ImageView.Source = "https://www.baidu.com/img/PCtm_d9c8750bed0b3c7d089fa7d55720d6cf.png";
                    }
                    imageCell.IsEmpty = false;
                }

                cell = imageCell;
            }

            return cell;
        }
    }

    internal class TextCell : TableViewViewHolder
    {
        public int NewCellIndex;

        public Label TextView;
        public TextCell(View itemView, string reuseIdentifier) : base(itemView, reuseIdentifier)
        {
            TextView = itemView as Label;
        }

        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
            TextView.Text = string.Empty;
        }

        public override void UpdateSelectionState(bool shouldHighlight)
        {
            base.UpdateSelectionState(shouldHighlight);
            if (shouldHighlight)
                TextView.BackgroundColor = Colors.LightGrey;
            else
                TextView.BackgroundColor = Colors.White;
        }
    }

    internal class ImageCell : TableViewViewHolder
    {
        public int NewCellIndex;

        public ImageCell(View itemView, string reuseIdentifier) : base(itemView, reuseIdentifier)
        {
            ImageView = itemView as Image;
        }

        public Microsoft.Maui.Controls.Image ImageView;

        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
            ImageView.Source = null;
        }
    }
}