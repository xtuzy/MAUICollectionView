using Bogus;
using MauiUICollectionView;
using Microsoft.Maui.Controls.Shapes;
using SharpConstraintLayout.Maui.Widget;
using Yang.Maui.Helper.Image;

namespace DemoTest.Pages
{
    internal class ViewModel
    {
        public List<Model> models;
        private Faker<Model> testModel;
        public ViewModel()
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
            models = testModel.Generate(50);
        }

        public Model Generate()
        {
            return testModel.Generate(1)[0];
        }
    }

    class Source : MAUICollectionViewSource
    {
        ViewModel ViewModel;
        public Source(ViewModel viewModel)
        {
            ViewModel = viewModel;

            heightForRowAtIndexPath += heightForRowAtIndexPathMethod;
            numberOfItemsInSection += numberOfRowsInSectionMethod;
            cellForRowAtIndexPath += cellForRowAtIndexPathMethod;
            numberOfSectionsInCollectionView += numberOfSectionsInTableViewMethod;
            reuseIdentifierForRowAtIndexPath += reuseIdentifierForRowAtIndexPathMethod;
        }

        public void RemoveData(int index)
        {
            ViewModel.models.RemoveAt(index);
        }

        public void InsertData(int index)
        {
            ViewModel.models.Insert(index, ViewModel.Generate());
        }

        public void ChangeData(int index)
        {
            ViewModel.models[index] = ViewModel.Generate();
        }

        public void MoveData(int index, int toIndex)
        {
            var item = ViewModel.models[index];
            ViewModel.models.RemoveAt(index);
            ViewModel.models.Insert(toIndex, item);
        }

        public int numberOfSectionsInTableViewMethod(MAUICollectionView tableView)
        {
            return 1;
        }

        public int numberOfRowsInSectionMethod(MAUICollectionView tableView, int section)
        {
            return ViewModel.models.Count;
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
        public MAUICollectionViewViewHolder cellForRowAtIndexPathMethod(MAUICollectionView tableView, NSIndexPath indexPath, double widthConstrain, bool needEmpty)
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
                    textCell = new TextCell(new Grid(), type) { };
                }
                if (!needEmpty)
                {
                    textCell.IsEmpty = false;
                    textCell.TextView.Text = $"Section={indexPath.Section} Row={indexPath.Row}";
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
                    imageCell.ModelView.ViewHolderIndex.Text = imageCell.NewCellIndex.ToString();
                }
                if (!needEmpty)
                {
                    if (type == botCell)
                    {
                        //imageCell.ModelView.PersonIcon.Source = ViewModel.models[indexPath.Row].PersonIconUrl;
                        imageCell.ModelView.PersonName.Text = ViewModel.models[indexPath.Row].PersonName;
                        imageCell.ModelView.PersonPhone.Text = ViewModel.models[indexPath.Row].PersonPhone;
                        imageCell.ModelView.PersonTextBlog.Text = ViewModel.models[indexPath.Row].PersonTextBlog;
                        //imageCell.ModelView.PersonImageBlog.Source = ViewModel.models[indexPath.Row].PersonImageBlogUrl;
                        imageCell.ModelView.LikeIcon.Source = new FontImageSource() { Glyph = FontAwesomeIcons.ThumbsUp, FontFamily = "FontAwesome6FreeSolid900" };
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

        public Entry TextView;
        public TextCell(View itemView, string reuseIdentifier) : base(itemView, reuseIdentifier)
        {
            var grid = itemView as Grid;
            TextView = new Entry();
            grid.Add(TextView);
            TextView.HorizontalOptions = LayoutOptions.Center;
            TextView.VerticalOptions = LayoutOptions.Center;
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
        public Label ViewHolderIndex;
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

            rootLayout = new ConstraintLayout() { ConstrainHeight = ConstraintSet.WrapContent, ConstrainWidth = ConstraintSet.MatchParent, ConstrainPaddingLeftDp = 5, ConstrainPaddingRightDp = 5 };
            Content = rootLayout;
            var PersonIconContainer = new Border() { StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(20) } };
            PersonIcon = new Image() { };
            PersonIconContainer.Content = PersonIcon;
            PersonName = new Label() { TextColor = Colors.White };
            PersonPhone = new Label() { TextColor = Colors.White };
            ViewHolderIndex = new Label() { TextColor = Colors.White };
            PersonTextBlog = new Label() { LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 3, TextColor = Colors.White };
            PersonImageBlog = new Image();
            FirstComment = new Label();
            LikeIcon = new Image() { };
            var tab = new TapGestureRecognizer();
            tab.Tapped += LikeIcon_Clicked;
            LikeIcon.GestureRecognizers.Add(tab);
            CommentIcon = new Image() { Source = new FontImageSource() { Glyph = FontAwesomeIcons.Comment, FontFamily = "FontAwesome6FreeSolid900" } };
            ShareIcon = new Image() { Source = new FontImageSource() { Glyph = FontAwesomeIcons.Share, FontFamily = "FontAwesome6FreeSolid900" } };
            rootLayout.AddElement(PersonIconContainer, PersonName, PersonPhone, ViewHolderIndex, PersonTextBlog, PersonImageBlog,
                FirstComment, LikeIcon, CommentIcon, ShareIcon);
            using (var set = new FluentConstraintSet())
            {
                set.Clone(rootLayout);
                set.Select(PersonIconContainer).LeftToLeft().TopToTop(null, 5).Width(40).Height(40)
                    .Select(PersonName).LeftToRight(PersonIconContainer, 5).TopToTop(PersonIconContainer)
                    .Select(PersonPhone).LeftToLeft(PersonName).BottomToBottom(PersonIconContainer)
                    .Select(ViewHolderIndex).RightToRight(null, 5).TopToTop(null, 5)
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
