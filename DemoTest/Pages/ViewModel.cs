using Bogus;
using Maui.BindableProperty.Generator.Core;
using MauiUICollectionView;
using Microsoft.Maui.Controls.Shapes;
using SharpConstraintLayout.Maui.Widget;
using The49.Maui.ContextMenu;
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
                .RuleFor(m => m.PersonTextBlog, f => f.WaffleText(1, false))
                .RuleFor(m => m.PersonImageBlogUrl, f => f.Image.PicsumUrl())
                .RuleFor(m => m.FirstComment, f => f.WaffleText(1, false))
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

        public List<Model> Generate(int count)
        {
            return testModel.Generate(count);
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
            lastItemWillShow += lastItemWillShowMethod;
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

        public void LoadMoreOnFirst()
        {
            var models = ViewModel.Generate(20);
            ViewModel.models.InsertRange(0, models);
        }

        public void lastItemWillShowMethod(MAUICollectionView collectionView, NSIndexPath indexPath)
        {
            Task.Run(async () =>
            {
                ActivityIndicator loading = null;
                if (collectionView.FooterView.ContentView is VerticalStackLayout)
                {
                    loading = (collectionView.FooterView.ContentView as VerticalStackLayout).Children[0] as ActivityIndicator;
                }
                if (loading != null)
                {
                    collectionView.Dispatcher.Dispatch(() =>
                    {
                        loading.IsVisible = true;
                        loading.IsRunning = true;
                    });
                }
                await Task.Delay(2000);
                var models = ViewModel.Generate(20);
                ViewModel.models.AddRange(models);

                collectionView.ReloadDataCount();
                if (loading != null)
                {
                    collectionView.Dispatcher.Dispatch(() =>
                    {
                        loading.IsVisible = false;
                        loading.IsRunning = false;
                    });
                }
            });
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
            return itemCell;
        }

        public float heightForRowAtIndexPathMethod(MAUICollectionView tableView, NSIndexPath indexPath)
        {
            var type = reuseIdentifierForRowAtIndexPathMethod(tableView, indexPath);
            switch (type)
            {
                case sectionCell:
                    return 40;
                case itemCell:
                    return MAUICollectionViewViewHolder.MeasureSelf;
                default:
                    return 100;
            }
        }

        int newCellCount = 0;
        //给每个cell设置ID号（重复利用时使用）
        const string sectionCell = "sectionCell";
        const string itemCell = "itemCell";
        public MAUICollectionViewViewHolder cellForRowAtIndexPathMethod(MAUICollectionView tableView, NSIndexPath indexPath, MAUICollectionViewViewHolder oldViewHolder, double widthConstrain)
        {
            //从tableView的一个队列里获取一个cell
            var type = reuseIdentifierForRowAtIndexPathMethod(tableView, indexPath);
            MAUICollectionViewViewHolder cell;
            if (oldViewHolder != null)//只需局部刷新
            {
                cell = oldViewHolder;
                var imageCell = cell as ItemViewHolder;
                if (imageCell != null)
                {
                    imageCell.ModelView.PersonPhone.Text = $"Item Id={indexPath.Section}-{indexPath.Row}";
                    imageCell.ModelView.IndexPath = indexPath;
                }
            }
            else
            {
                cell = tableView.DequeueRecycledViewHolderWithIdentifier(type);

                if (type == sectionCell)
                {
                    var textCell = cell as SectionViewHolder;
                    //判断队列里面是否有这个cell 没有自己创建，有直接使用
                    if (textCell == null)
                    {
                        //没有,创建一个
                        textCell = new SectionViewHolder(new Grid(), type) { };
                    }

                    textCell.IsEmpty = false;
                    textCell.TextView.Text = $"Section={indexPath.Section} Row={indexPath.Row}";

                    cell = textCell;
                }
                else
                {
                    var imageCell = cell as ItemViewHolder;
                    if (imageCell == null)
                    {
                        //没有,创建一个
                        imageCell = new ItemViewHolder(new ModelView() { }, type) { };
                        //if (!System.OperatingSystem.IsWindows()) imageCell.ImageView.MinimumHeightRequest = widthConstrain;
                        imageCell.NewCellIndex = ++newCellCount;
                        imageCell.ModelView.ViewHolderIndex.Text = imageCell.NewCellIndex.ToString();
                        var command = new Command<NSIndexPath>(execute: (NSIndexPath arg) =>
                        {
                            RemoveData(arg.Row);
                            tableView.RemoveItems(arg);
                            tableView.ReMeasure();
                        });
                        imageCell.ModelView.InitMenu(command);
                    }

                    if (type == itemCell)
                    {
                        //imageCell.ModelView.PersonIcon.Source = ViewModel.models[indexPath.Row].PersonIconUrl;
                        imageCell.ModelView.PersonName.Text = ViewModel.models[indexPath.Row].PersonName;
                        imageCell.ModelView.PersonPhone.Text = $"Item Id={indexPath.Section}-{indexPath.Row}";
                        imageCell.ModelView.PersonTextBlog.Text = ViewModel.models[indexPath.Row].PersonTextBlog;
                        //imageCell.ModelView.PersonImageBlog.Source = ViewModel.models[indexPath.Row].PersonImageBlogUrl;
                        imageCell.ModelView.LikeIcon.Source = new FontImageSource() { Glyph = FontAwesomeIcons.ThumbsUp, FontFamily = "FontAwesome6FreeSolid900" };
                    }
                    imageCell.IsEmpty = false;
                    imageCell.ModelView.IndexPath = indexPath;

                    cell = imageCell;
                }
            }
            cell.NSIndexPath = indexPath;

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

    internal class SectionViewHolder : MAUICollectionViewViewHolder
    {
        public int NewCellIndex;

        public Label TextView;
        public SectionViewHolder(View itemView, string reuseIdentifier) : base(itemView, reuseIdentifier)
        {
            var grid = itemView as Grid;
            TextView = new Label();
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

    internal class ItemViewHolder : MAUICollectionViewViewHolder
    {
        public int NewCellIndex;

        public ItemViewHolder(View itemView, string reuseIdentifier) : base(itemView, reuseIdentifier)
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

    public partial class ModelView : Border
    {
        [AutoBindable]
        NSIndexPath _indexPath;

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
            PersonTextBlog = new Label() { LineBreakMode = LineBreakMode.WordWrap, MaxLines = 3, TextColor = Colors.White, BackgroundColor = Colors.SlateGray };
            PersonImageBlog = new Image() { BackgroundColor = Colors.AliceBlue };
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
                    .Select(PersonTextBlog).LeftToLeft(PersonIconContainer).TopToBottom(PersonIconContainer)
                    .Select(PersonImageBlog).LeftToLeft(PersonTextBlog).TopToBottom(PersonTextBlog).Width(100).Height(100)
                    .Select(LikeIcon, CommentIcon, ShareIcon).CreateXChain(rootLayout, Edge.Left, rootLayout, Edge.Right, ChainStyle.Spread, new (View, float)[] { (LikeIcon, 1), (CommentIcon, 1), (ShareIcon, 1) })
                    .TopToBottom(PersonImageBlog).BottomToBottom(null, 5).Width(20).Height(20)
                    ;
                set.ApplyTo(rootLayout);
            }
        }

        public void InitMenu(Command command)
        {
#if IOS
            var template = new DataTemplate(() =>
            {
                var menu = new Menu();
                var menuItem = new The49.Maui.ContextMenu.Action()
                {
                    Title = "Delete",
                    Command = command,
                };
                menuItem.SetBinding(The49.Maui.ContextMenu.Action.CommandParameterProperty, new Binding(nameof(IndexPath), source: this));
                menu.Children = new System.Collections.ObjectModel.ObservableCollection<MenuElement>()
                {
                    menuItem
                };
                return menu;
            });
            ContextMenu.SetMenu(this, template);
#elif ANDROID
            this.command = command;
#elif WINDOWS || MACCATALYST
            var menu = new MenuFlyout();
            var menuItem = new MenuFlyoutItem()
            {
                Text = "Delete",
                Command = command,
                CommandParameter = IndexPath
            };
            menuItem.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding(nameof(IndexPath), source: this));
            menu.Add(menuItem);
            FlyoutBase.SetContextFlyout(this, menu);
#endif

#if !ANDROID
            var gesture = new TapGestureRecognizer();
            gesture.Tapped += (sender, args) =>
            {
                (this.Parent?.Parent as MAUICollectionView)?.SelectRowAtIndexPath(IndexPath, false, ScrollPosition.None);
            };
            this.GestureRecognizers.Add(gesture);
#endif
        }
#if ANDROID
        Command command { get; set; }
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            var av = this.Handler.PlatformView as Android.Views.View;
            var menu = new AndroidX.AppCompat.Widget.PopupMenu(av.Context, av);
            menu.Inflate(Resource.Menu.popup_menu);
            av.LongClick += (sender, args) =>
            {
                menu.Show();
            };
            menu.MenuItemClick += (s1, arg1) =>
            {
                command.Execute(IndexPath);
            };

            menu.DismissEvent += (s2, arg2) =>
            {
                Console.WriteLine("menu dismissed");
            };
            av.Click += (s1, arg1) =>
            {
                (this.Parent?.Parent as MAUICollectionView)?.SelectRowAtIndexPath(IndexPath, false, ScrollPosition.None);
            };
        }
#endif

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
