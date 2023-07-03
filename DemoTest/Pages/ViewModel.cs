using Bogus;
using MauiUICollectionView;
using Microsoft.Maui.Controls;
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
            willDragTo += WillDragToMethod;
        }

        private void WillDragToMethod(MAUICollectionView collectionView, NSIndexPath path1, NSIndexPath path2)
        {
            if (path1.Row == 0 || path2.Row == 0)//section的header不处理, 不然会出错
                return;
            collectionView.MoveItem(path1, path2);
            MoveData(path1.Row, path2.Row);
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
                if (collectionView.FooterView.Content is VerticalStackLayout)
                {
                    loading = (collectionView.FooterView.Content as VerticalStackLayout)?.Children[0] as ActivityIndicator;
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
            //return itemCell;
            return itemCellSimple;
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
                case itemCellSimple:
                    return MAUICollectionViewViewHolder.MeasureSelf;
                default:
                    return 100;
            }
        }

        int newCellCount = 0;
        //给每个cell设置ID号（重复利用时使用）
        const string sectionCell = "sectionCell";
        const string itemCell = "itemCell";
        const string itemCellSimple = "itemCellSimple";
        public MAUICollectionViewViewHolder cellForRowAtIndexPathMethod(MAUICollectionView tableView, NSIndexPath indexPath, MAUICollectionViewViewHolder oldViewHolder, double widthConstrain)
        {
            //从tableView的一个队列里获取一个cell
            var type = reuseIdentifierForRowAtIndexPathMethod(tableView, indexPath);
            MAUICollectionViewViewHolder cell;
            if (oldViewHolder != null)//只需局部刷新
            {
                cell = oldViewHolder;
                if (cell is ItemViewHolder itemcell)
                {
                    if (itemcell != null)
                    {
                        itemcell.ModelView.PersonPhone.Text = $"Item Id={indexPath.Section}-{indexPath.Row}";
                        itemcell.ModelView.TestButton.Text = $"Item Id={indexPath.Section}-{indexPath.Row}";
                    }
                }
                else if (cell is ItemViewHolderSimple itemcellsimple)
                {
                    //if (itemcellsimple != null) itemcellsimple.ModelView.TestButton.Text = $"Item Id={indexPath.Section}-{indexPath.Row}";
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

                    textCell.TextView.Text = $"Section={indexPath.Section} Row={indexPath.Row}";

                    cell = textCell;
                }
                else if (type == itemCell)
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
                        imageCell.InitMenu(command);
                        imageCell.ModelView.TestButton.Clicked += async (sender, e) =>
                        {
                            await Shell.Current.CurrentPage?.DisplayAlert("Alert", $"Section={imageCell.IndexPath.Section} Row={imageCell.IndexPath.Row}", "OK");
                        };
                    }

                    //imageCell.ModelView.PersonIcon.Source = ViewModel.models[indexPath.Row].PersonIconUrl;
                    imageCell.ModelView.PersonName.Text = ViewModel.models[indexPath.Row].PersonName;
                    imageCell.ModelView.PersonPhone.Text = $"Item Id={indexPath.Section}-{indexPath.Row}";
                    imageCell.ModelView.PersonTextBlog.Text = ViewModel.models[indexPath.Row].PersonTextBlog;
                    imageCell.ModelView.TestButton.Text = $"Item Id={indexPath.Section}-{indexPath.Row}";

                    cell = imageCell;
                }
                else if (type == itemCellSimple)
                {
                    var simpleCell = cell as ItemViewHolderSimple;
                    if (simpleCell == null)
                    {
                        //没有,创建一个
                        simpleCell = new ItemViewHolderSimple(new ModelViewSimple() { }, type) { };
                        var command = new Command<NSIndexPath>(execute: (NSIndexPath arg) =>
                        {
                            RemoveData(arg.Row);
                            tableView.RemoveItems(arg);
                            tableView.ReMeasure();
                        });
                        simpleCell.InitMenu(command);
                        simpleCell.ModelView.TestButton.Clicked += async (sender, e) =>
                        {
                            await Shell.Current.CurrentPage?.DisplayAlert("Alert", $"Section={simpleCell.IndexPath.Section} Row={simpleCell.IndexPath.Row}", "OK");
                        };
                    }

                    simpleCell.ModelView.PersonName.Text = ViewModel.models[indexPath.Row].PersonName;
                    simpleCell.ModelView.PersonPhone.Text = ViewModel.models[indexPath.Row].PersonPhone;
                    simpleCell.ModelView.PersonTextBlog.Text = ViewModel.models[indexPath.Row].PersonTextBlog;
                    simpleCell.ModelView.TestButton.Text = $"Item Id={indexPath.Section}-{indexPath.Row}";

                    cell = simpleCell;
                }
            }
            cell.IndexPath = indexPath;
            if (cell.ContextMenu != null)
                cell.ContextMenu.IsEnable = tableView.CanContextMenu;
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
                DefaultColor = Content.BackgroundColor;
            }
            base.UpdateSelectionState(shouldHighlight);
            if (shouldHighlight)
                Content.BackgroundColor = Colors.LightGrey;
            else
                Content.BackgroundColor = DefaultColor;
        }
    }

    internal class ItemViewHolder : MAUICollectionViewViewHolder
    {
        public int NewCellIndex;

        public ItemViewHolder(View itemView, string reuseIdentifier) : base(itemView, reuseIdentifier)
        {
            this.IsClippedToBounds = true;
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
                DefaultColor = Content.BackgroundColor;
            }
            base.UpdateSelectionState(shouldHighlight);
            if (shouldHighlight)
                Content.BackgroundColor = Colors.Grey.WithAlpha(100);
            else
                Content.BackgroundColor = DefaultColor;
        }


        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
#if ANDROID
            var av = this.Handler.PlatformView as Android.Views.View;
            var aContextMenu = new MauiUICollectionView.Gestures.AndroidContextMenu(av.Context, av);

            //设置PopupMenu样式, see https://learn.microsoft.com/en-us/xamarin/android/user-interface/controls/popup-menu
            aContextMenu.PlatformMenu.Inflate(Resource.Menu.popup_menu);
            aContextMenu.PlatformMenu.MenuItemClick += (s1, arg1) =>
            {
                MenuCommand.Execute(IndexPath);
            };
            ContextMenu = aContextMenu;
#endif
        }


        public Command MenuCommand;
        public void InitMenu(Command command)
        {
            MenuCommand = command;
#if IOS
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
            ContextMenu = new MauiUICollectionView.Gestures.iOSContextMenu(this, menu);
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
            ContextMenu = new MauiUICollectionView.Gestures.DesktopContextMenu(this, menu);
#endif
        }
    }

    public partial class ModelView : ConstraintLayout
    {
        ConstraintLayout rootLayout;
        public Image PersonIcon;
        public Label PersonName;
        public Label PersonPhone;
        public Label ViewHolderIndex;
        public Label PersonTextBlog;
        public Image PersonImageBlog;
        public Button TestButton;
        public Label FirstComment;
        public Image LikeIcon;
        public Image CommentIcon;
        public Image ShareIcon;

        public ModelView()
        {
            //this.StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(10) };
            //this.Margin = new Thickness(20, 0, 20, 20);
            this.BackgroundColor = new Color(30, 30, 30);

            //rootLayout = new ConstraintLayout() { ConstrainHeight = ConstraintSet.WrapContent, ConstrainWidth = ConstraintSet.MatchParent, ConstrainPaddingLeftDp = 5, ConstrainPaddingRightDp = 5 };
            //Content = rootLayout;
            rootLayout = this;
            this.ConstrainHeight = ConstraintSet.WrapContent;
            this.ConstrainWidth = ConstraintSet.MatchParent;
            var PersonIconContainer = new Border() { StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(20) } };
            PersonIcon = new Image() { };
            PersonIconContainer.Content = PersonIcon;
            PersonName = new Label() { TextColor = Colors.White };
            PersonPhone = new Label() { TextColor = Colors.White };
            ViewHolderIndex = new Label() { TextColor = Colors.White };
            PersonTextBlog = new Label() { LineBreakMode = LineBreakMode.WordWrap, MaxLines = 3, TextColor = Colors.White, BackgroundColor = Colors.SlateGray };
            PersonImageBlog = new Image() { BackgroundColor = Colors.AliceBlue };
            TestButton = new Button() { };
            FirstComment = new Label();
            LikeIcon = new Image() { };
            var tab = new TapGestureRecognizer();
            tab.Tapped += LikeIcon_Clicked;
            LikeIcon.GestureRecognizers.Add(tab);
            LikeIcon.Source = new FontImageSource() { Glyph = FontAwesomeIcons.ThumbsUp, FontFamily = "FontAwesome6FreeSolid900" };
            CommentIcon = new Image() { Source = new FontImageSource() { Glyph = FontAwesomeIcons.Comment, FontFamily = "FontAwesome6FreeSolid900" } };
            ShareIcon = new Image() { Source = new FontImageSource() { Glyph = FontAwesomeIcons.Share, FontFamily = "FontAwesome6FreeSolid900" } };
            rootLayout.AddElement(PersonIconContainer, PersonName, PersonPhone, ViewHolderIndex, PersonTextBlog, PersonImageBlog, TestButton,
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
                    .Select(TestButton).LeftToRight(PersonImageBlog).CenterYTo(PersonImageBlog)
                    .Select(LikeIcon, CommentIcon, ShareIcon).CreateXChain(rootLayout, Edge.Left, rootLayout, Edge.Right, ChainStyle.Spread, new (View, float)[] { (LikeIcon, 1), (CommentIcon, 1), (ShareIcon, 1) })
                    .TopToBottom(PersonImageBlog).BottomToBottom(null, 5).Width(20).Height(20)
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

    internal class ItemViewHolderSimple : MAUICollectionViewViewHolder
    {
        public int NewCellIndex;

        public ItemViewHolderSimple(View itemView, string reuseIdentifier) : base(itemView, reuseIdentifier)
        {
            ModelView = itemView as ModelViewSimple;
        }

        public ModelViewSimple ModelView;

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
                DefaultColor = Content.BackgroundColor;
            }
            base.UpdateSelectionState(shouldHighlight);
            if (shouldHighlight)
                Content.BackgroundColor = Colors.Grey.WithAlpha(100);
            else
                Content.BackgroundColor = DefaultColor;
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
#if ANDROID
            var av = this.Handler.PlatformView as Android.Views.View;
            var aContextMenu = new MauiUICollectionView.Gestures.AndroidContextMenu(av.Context, av);

            //设置PopupMenu样式, see https://learn.microsoft.com/en-us/xamarin/android/user-interface/controls/popup-menu
            aContextMenu.PlatformMenu.Inflate(Resource.Menu.popup_menu);
            aContextMenu.PlatformMenu.MenuItemClick += (s1, arg1) =>
            {
                MenuCommand.Execute(IndexPath);
            };
            ContextMenu = aContextMenu;
#endif
        }


        public Command MenuCommand;
        public void InitMenu(Command command)
        {
            MenuCommand = command;
#if IOS
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
            ContextMenu = new MauiUICollectionView.Gestures.iOSContextMenu(this, menu);
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
            ContextMenu = new MauiUICollectionView.Gestures.DesktopContextMenu(this, menu);
#endif
        }
    }

    class ModelViewSimple : Grid
    {
        public Image PersonIcon;
        public Label PersonName;
        public Label PersonPhone;
        public Label PersonTextBlog;
        public Image PersonImageBlog;
        public Button TestButton;
        public Label FirstComment;
        public Image LikeIcon;
        public Image CommentIcon;
        public Image ShareIcon;

        public ModelViewSimple()
        {
            this.BackgroundColor = Colors.Black;
            this.RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition(){ Height = GridLength.Auto },
                new RowDefinition(){ Height = GridLength.Auto },
                new RowDefinition(){ Height = GridLength.Auto },
                new RowDefinition(){ Height = GridLength.Star },
            };
            var infoContainer = new Grid()
            {
                Padding = new Thickness(5),
                ColumnDefinitions = new ColumnDefinitionCollection()
                {
                    new ColumnDefinition(){Width = GridLength.Auto},
                    new ColumnDefinition(){Width = GridLength.Star},
                }
            };
            PersonTextBlog = new Label() { LineBreakMode = LineBreakMode.WordWrap, MaxLines = 3, TextColor = Colors.White, BackgroundColor = Colors.SlateGray };
            var imageblogContainer = new HorizontalStackLayout() { Margin = new Thickness(0, 5, 0, 5) };
            var iconContainer = new Grid()
            {
                Margin = new Thickness(0, 5, 0, 5),
                ColumnDefinitions = new ColumnDefinitionCollection()
                {
                    new ColumnDefinition(){Width = GridLength.Star},
                    new ColumnDefinition(){Width = GridLength.Star},
                    new ColumnDefinition(){Width = GridLength.Star},
                }
            };
            this.Add(infoContainer);
            this.Add(PersonTextBlog);
            this.Add(imageblogContainer);
            this.Add(iconContainer);

            Grid.SetRow(infoContainer, 0);
            Grid.SetRow(PersonTextBlog, 1);
            Grid.SetRow(imageblogContainer, 2);
            Grid.SetRow(iconContainer, 3);

            PersonIcon = new Image() { WidthRequest = 40, HeightRequest = 40, BackgroundColor = Colors.AliceBlue };
            var textInfo = new VerticalStackLayout() { Margin = new Thickness(5, 0, 0, 0) };
            infoContainer.Add(PersonIcon);
            infoContainer.Add(textInfo);
            Grid.SetColumn(PersonIcon, 0);
            Grid.SetColumn(textInfo, 1);
            PersonName = new Label() { TextColor = Colors.White, };
            PersonPhone = new Label() { TextColor = Colors.White };
            textInfo.Add(PersonName);
            textInfo.Add(PersonPhone);

            PersonImageBlog = new Image() { WidthRequest = 100, HeightRequest = 100, BackgroundColor = Colors.AliceBlue };
            TestButton = new Button() { BackgroundColor = Colors.Blue, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
            imageblogContainer.Add(PersonImageBlog);
            imageblogContainer.Add(TestButton);

            LikeIcon = new Image() { WidthRequest = 20, Source = new FontImageSource() { Glyph = FontAwesomeIcons.ThumbsUp, FontFamily = "FontAwesome6FreeSolid900" }, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            var tab = new TapGestureRecognizer();
            tab.Tapped += LikeIcon_Clicked;
            LikeIcon.GestureRecognizers.Add(tab);
            CommentIcon = new Image() { WidthRequest = 20, Source = new FontImageSource() { Glyph = FontAwesomeIcons.Comment, FontFamily = "FontAwesome6FreeSolid900" }, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            ShareIcon = new Image() { WidthRequest = 20, Source = new FontImageSource() { Glyph = FontAwesomeIcons.Share, FontFamily = "FontAwesome6FreeSolid900" }, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };

            iconContainer.Add(LikeIcon);
            iconContainer.Add(CommentIcon);
            iconContainer.Add(ShareIcon);
            Grid.SetColumn(LikeIcon, 0);
            Grid.SetColumn(CommentIcon, 1);
            Grid.SetColumn(ShareIcon, 2);
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
