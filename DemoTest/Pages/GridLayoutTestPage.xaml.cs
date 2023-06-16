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
        tableView.Source = new Source();
        tableView.ItemsLayout = new CollectionViewGridLayout(tableView)
        {
        };

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
        tableView.Content.GestureRecognizers.Add(click);
        var headerButton = new Button() { Text = "Header", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        headerButton.Clicked += (s, e) =>
        {
            if ((tableView.ItemsLayout as CollectionViewGridLayout).ColumnCount == 1)
                (tableView.ItemsLayout as CollectionViewGridLayout).ColumnCount = 2;
            else
                (tableView.ItemsLayout as CollectionViewGridLayout).ColumnCount = 1;
            tableView.ContentView.ReMeasure();
        };
        var headerView = new MAUICollectionViewViewHolder(headerButton, "Header");
        tableView.HeaderView = headerView;
        var footerButton = new Button() { Text = "Footer", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        footerButton.Clicked += (s, e) =>
        {
            tableView.ScrollToRowAtIndexPath(NSIndexPath.FromRowSection(20, 0), ScrollPosition.Top, true);
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
            var index = 2;
            (tableView.Source as Source).InsertData(index);
            tableView.InsertItems(NSIndexPath.FromRowSection(index, 0));
            tableView.ContentView.ReMeasure();
        };

        Remove.Clicked += (sender, e) =>
        {
            var index = 2;
            (tableView.Source as Source).RemoveData(index);
            tableView.RemoveItems(NSIndexPath.FromRowSection(index, 0));
            tableView.ContentView.ReMeasure();
        };

        Move.Clicked += (sender, e) =>
        {
            var index = 3;
            var target = 1;
            (tableView.Source as Source).MoveData(index, target);
            tableView.MoveItem(NSIndexPath.FromRowSection(index, 0), NSIndexPath.FromRowSection(target, 0));
            tableView.ContentView.ReMeasure();
        };

        Change.Clicked += (sender, e) =>
        {
            var index = 2;
            (tableView.Source as Source).ChangeData(index);
            tableView.ChangeItem(new[] { NSIndexPath.FromRowSection(index, 0) });
            tableView.ContentView.ReMeasure();
        };
    }

    class Source : MAUICollectionViewSource
    {
        Faker<Model> testModel;
        List<Model> models;
        public Source()
        {
             testModel = new Faker<Model>();
            testModel
                .RuleFor(m => m.PersonIconUrl, f => f.Person.Avatar)
                .RuleFor(m => m.PersonName, f => f.Person.FullName)
                .RuleFor(m => m.PersonPhone, f => f.Person.Phone)
                .RuleFor(m => m.PersonTextBlog, f => f.Random.String())
                .RuleFor(m => m.PersonImageBlogUrl, f => f.Image.PicsumUrl())
                .RuleFor(m => m.FirstComment, f => f.Random.String())
                //.RuleFor(m => m.LikeIconUrl, f => f.Person.Avatar)
                //.RuleFor(m => m.CommentIconUrl, f => f.Person.Avatar)
                //.RuleFor(m => m.ShareIconUrl, f => f.Person.Avatar)
                ;
            models = testModel.Generate(100);

            heightForRowAtIndexPath += heightForRowAtIndexPathMethod;
            numberOfItemsInSection += numberOfRowsInSectionMethod;
            cellForRowAtIndexPath += cellForRowAtIndexPathMethod;
            numberOfSectionsInCollectionView += numberOfSectionsInTableViewMethod;
            reuseIdentifierForRowAtIndexPath += reuseIdentifierForRowAtIndexPathMethod;
        }

        public void RemoveData(int index)
        {
            models.RemoveAt(index);
            //CollectionView.DeleteItems(new[] { NSIndexPath.FromRowSection(10, 1) }, false);
        }

        public void InsertData(int index)
        {
            models.Insert(index, testModel.Generate(1)[0]);
        }

        public void ChangeData(int index)
        {
            models[index] = testModel.Generate(1)[0];
        }

        public void MoveData(int index, int toIndex)
        {
            var item = models[index];
            models.RemoveAt(index);
            models.Insert(toIndex, item);
        }

        public int numberOfSectionsInTableViewMethod(MAUICollectionView tableView)
        {
            return 1;
        }

        public int numberOfRowsInSectionMethod(MAUICollectionView tableView, int section)
        {
            return models.Count;
        }

        public string reuseIdentifierForRowAtIndexPathMethod(MAUICollectionView tableView, NSIndexPath indexPath)
        {
            if (indexPath.Row == 0)
            {
                return sectionCell;
            }
            return botCell;
        }

        public float heightForRowAtIndexPathMethod(MAUICollectionView tableView, NSIndexPath indexPath)
        {
            var type = reuseIdentifierForRowAtIndexPathMethod(tableView, indexPath);
            switch (type)
            {
                case sectionCell:
                    return 40;
                case botCell:
                    return MAUICollectionViewViewHolder.MeasureSelf;
                default:
                    return 100;
            }
        }

        static int newCellCount = 0;
        //给每个cell设置ID号（重复利用时使用）
        const string sectionCell = "sectionCell";
        const string botCell = "botCell";
        public MAUICollectionViewViewHolder cellForRowAtIndexPathMethod(MAUICollectionView tableView, NSIndexPath indexPath, double widthConstrain, bool blank)
        {
            //从tableView的一个队列里获取一个cell
            var type = reuseIdentifierForRowAtIndexPathMethod(tableView, indexPath);
            MAUICollectionViewViewHolder cell = tableView.DequeueRecycledViewHolderWithIdentifier(type);

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
                    imageCell = new ImageCell(new ModelView() { }, type) { };
                    //if (!System.OperatingSystem.IsWindows()) imageCell.ImageView.MinimumHeightRequest = widthConstrain;
                    imageCell.NewCellIndex = ++newCellCount;
                    Console.WriteLine($"newCell: {newCellCount}");
                }
                if (!blank)
                {
                    if (type == botCell)
                    {
                        imageCell.ModelView.PersonIcon.Source = models[indexPath.Row].PersonIconUrl;
                        imageCell.ModelView.PersonName.Text = models[indexPath.Row].PersonName;
                        imageCell.ModelView.PersonPhone.Text = models[indexPath.Row].PersonPhone;
                        imageCell.ModelView.PersonTextBlog.Text = models[indexPath.Row].PersonTextBlog;
                        imageCell.ModelView.PersonImageBlog.Source = models[indexPath.Row].PersonImageBlogUrl;
                        imageCell.ModelView.LikeIcon.Source = new FontImageSource() { Glyph = FontAwesomeIcons.ThumbsUp, FontFamily = "FontAwesome6FreeSolid900" };
                    }
                    imageCell.IsEmpty = false;
                }

                cell = imageCell;
            }

            return cell;
        }

        class Model
        {
            public string PersonIconUrl { get; set; }
            public string PersonName { get; set; }
            public string PersonPhone { get; set; }
            public string PersonTextBlog { get; set; }
            public string PersonImageBlogUrl { get; set; }
            public string FirstComment { get; set; }
            public string LikeIconUrl { get; set; }
            public string CommentIconUrl { get; set; }
            public string ShareIconUrl { get; set; }
        }

        internal class TextCell : MAUICollectionViewViewHolder
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
                UpdateSelectionState(false);
            }

            Color DefaultColor = Colors.LightYellow;
            public override void UpdateSelectionState(bool shouldHighlight)
            {
                if (DefaultColor == Colors.LightYellow)
                {
                    DefaultColor = ContentView.BackgroundColor;
                }
                base.UpdateSelectionState(shouldHighlight);
                if (shouldHighlight)
                    ContentView.BackgroundColor = Colors.LightGrey;
                else
                    ContentView.BackgroundColor = DefaultColor;
            }
        }

        internal class ImageCell : MAUICollectionViewViewHolder
        {
            public int NewCellIndex;

            public ImageCell(View itemView, string reuseIdentifier) : base(itemView, reuseIdentifier)
            {
                ModelView = itemView as ModelView;
            }

            public ModelView ModelView;

            public override void PrepareForReuse()
            {
                base.PrepareForReuse();
                ModelView.PersonIcon.Source = null;
                ModelView.PersonName.Text = string.Empty;
                ModelView.PersonPhone.Text = string.Empty;
                ModelView.PersonTextBlog.Text = string.Empty;
                ModelView.PersonImageBlog.Source = null;
                //ModelView.CommentIcon.Source = null;
                //ModelView.ShareIcon.Source = null;
                UpdateSelectionState(false);
            }

            Color DefaultColor = Colors.LightYellow;
            public override void UpdateSelectionState(bool shouldHighlight)
            {
                if (DefaultColor == Colors.LightYellow)
                {
                    DefaultColor = ContentView.BackgroundColor;
                }
                base.UpdateSelectionState(shouldHighlight);
                if (shouldHighlight)
                    ContentView.BackgroundColor = Colors.Grey.WithAlpha(100);
                else
                    ContentView.BackgroundColor = DefaultColor;
            }
        }

        public class ModelView : Border
        {
            ConstraintLayout rootLayout;
            public Image PersonIcon;
            public Label PersonName;
            public Label PersonPhone;
            public Label PersonTextBlog;
            public Image PersonImageBlog;
            public Label FirstComment;
            public Image LikeIcon;
            public Image CommentIcon;
            public Image ShareIcon;
            public ModelView()
            {
                this.StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(10) };
                //this.Margin = new Thickness(20, 0, 20, 20);
                this.BackgroundColor = new Color(30, 30, 30);

                rootLayout = new ConstraintLayout() { ConstrainHeight = ConstraintSet.MatchParent, ConstrainWidth = ConstraintSet.MatchParent, ConstrainPaddingLeftDp = 5, ConstrainPaddingRightDp = 5 };
                Content = rootLayout;
                var PersonIconContainer = new Border() { StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(20) } };
                PersonIcon = new Image() { };
                PersonIconContainer.Content = PersonIcon;
                PersonName = new Label() { TextColor = Colors.White };
                PersonPhone = new Label() { TextColor = Colors.White };
                PersonTextBlog = new Label() { LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 3, TextColor = Colors.White };
                PersonImageBlog = new Image();
                FirstComment = new Label();
                LikeIcon = new Image() { };
                var tab = new TapGestureRecognizer();
                tab.Tapped += LikeIcon_Clicked;
                LikeIcon.GestureRecognizers.Add(tab);
                CommentIcon = new Image() { Source = new FontImageSource() { Glyph = FontAwesomeIcons.Comment, FontFamily = "FontAwesome6FreeSolid900" } };
                ShareIcon = new Image() { Source = new FontImageSource() { Glyph = FontAwesomeIcons.Share, FontFamily = "FontAwesome6FreeSolid900" } };
                rootLayout.AddElement(PersonIconContainer, PersonName, PersonPhone, PersonTextBlog, PersonImageBlog,
                    FirstComment, LikeIcon, CommentIcon, ShareIcon);
                using (var set = new FluentConstraintSet())
                {
                    set.Clone(rootLayout);
                    set.Select(PersonIconContainer).LeftToLeft().TopToTop(null, 5).Width(40).Height(40)
                        .Select(PersonName).LeftToRight(PersonIconContainer, 5).TopToTop(PersonIconContainer)
                        .Select(PersonPhone).LeftToLeft(PersonName).BottomToBottom(PersonIconContainer)
                        .Select(PersonTextBlog).LeftToLeft(PersonIconContainer).TopToBottom(PersonIconContainer, 5)
                        .Select(PersonImageBlog).LeftToLeft(PersonTextBlog).TopToBottom(PersonTextBlog, 5).Width(100).Height(100)
                        .Select(LikeIcon, CommentIcon, ShareIcon).CreateXChain(rootLayout, Edge.Left, rootLayout, Edge.Right, ChainStyle.Spread, new (View, float)[] { (LikeIcon, 1), (CommentIcon, 1), (ShareIcon, 1) })
                        .TopToBottom(PersonImageBlog, 5).BottomToBottom(null, 5).Width(20).Height(20)
                        ;
                    set.ApplyTo(rootLayout);
                }
            }

            private void LikeIcon_Clicked(object sender, EventArgs e)
            {
                if ((LikeIcon.Source as FontImageSource)?.Color == Colors.Red)
                    (LikeIcon.Source as FontImageSource).Color = Colors.White;
                else
                    (LikeIcon.Source as FontImageSource).Color = Colors.Red;
                Console.WriteLine("Like Clicked");
            }
        }
    }
}