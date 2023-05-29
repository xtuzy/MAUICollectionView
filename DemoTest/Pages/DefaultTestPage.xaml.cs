using Bogus;
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
            var indexPath = tableView.ItemsLayout.IndexPathForRowAtPointOfContentView(p.Value);
#else
            var indexPath = tableView.ItemsLayout.IndexPathForVisibaleRowAtPointOfTableView(p.Value);
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
        this.Loaded += (sender, e) =>
        {
            Console.WriteLine("Loaded");
        };
        this.Appearing += (sender, e) =>
        {
            tableView.ReAppear();
            Console.WriteLine("Appearing");
        };
    }

    class Source : TableViewSource
    {
        List<Model> models;
        public Source()
        {
            var testModel = new Faker<Model>();
            testModel.RuleFor(u => u.Url, f => f.Image.PicsumUrl());
            models = testModel.Generate(100);

            heightForRowAtIndexPath += heightForRowAtIndexPathMethod;
            numberOfRowsInSection += numberOfRowsInSectionMethod;
            cellForRowAtIndexPath += cellForRowAtIndexPathMethod;
            numberOfSectionsInTableView += numberOfSectionsInTableViewMethod;
            reuseIdentifierForRowAtIndexPath += reuseIdentifierForRowAtIndexPathMethod;
        }

        public int numberOfSectionsInTableViewMethod(TableView tableView)
        {
            return 1;
        }

        public int numberOfRowsInSectionMethod(TableView tableView, int section)
        {
            return models.Count;
        }

        public string reuseIdentifierForRowAtIndexPathMethod(TableView tableView, NSIndexPath indexPath)
        {
            if (indexPath.Row == 0)
            {
                return sectionCell;
            }
            return botCell;
        }

        public float heightForRowAtIndexPathMethod(TableView tableView, NSIndexPath indexPath)
        {
            var type = reuseIdentifierForRowAtIndexPathMethod(tableView, indexPath);
            switch (type)
            {
                case sectionCell:
                    return 40;
                case botCell:
                    return TableViewViewHolder.MeasureSelf;
                default:
                    return 100;
            }
        }

        static int newCellCount = 0;
        //给每个cell设置ID号（重复利用时使用）
        const string sectionCell = "sectionCell";
        const string botCell = "botCell";
        public TableViewViewHolder cellForRowAtIndexPathMethod(TableView tableView, NSIndexPath indexPath, double widthConstrain, bool blank)
        {
            //从tableView的一个队列里获取一个cell
            var type = reuseIdentifierForRowAtIndexPathMethod(tableView, indexPath);
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
                    imageCell = new ImageCell(new Image() { Aspect = Aspect.Center, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }, type) { };
                    //if (!System.OperatingSystem.IsWindows()) imageCell.ImageView.MinimumHeightRequest = widthConstrain;
                    imageCell.NewCellIndex = ++newCellCount;
                    Console.WriteLine($"newCell: {newCellCount}");
                }
                if (!blank)
                {
                    if (type == botCell)
                    {
                        imageCell.ImageView.Source = models[indexPath.Row].Url;
                    }
                    imageCell.IsEmpty = false;
                }

                cell = imageCell;
            }

            return cell;
        }
    }

    class Model
    {
        public string Url { get; set; }
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