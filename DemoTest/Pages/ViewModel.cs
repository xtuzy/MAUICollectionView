using Bogus;
using MauiUICollectionView;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using System.Collections.ObjectModel;
using System.Diagnostics;
using The49.Maui.ContextMenu;

namespace DemoTest.Pages
{
    internal class ViewModel
    {
        static ViewModel instance;
        internal static ViewModel Instance
        {
            get
            {
                if (instance == null)
                    instance = new ViewModel();
                return instance;
            }
        }

        public static Stopwatch Stopwatch = new Stopwatch();

        public static List<long> Times = new List<long>();
        public static int LimitCount = 300;
        public static void CalculateMeanMeasureTimeAsync(long time)
        {
            if (Times.Count >= LimitCount)
            {
                long count = 0;
                long max = 0;
                long min = 100;
                foreach (long t in Times)
                {
                    if (t > max)
                        max = t;
                    if (t < min)
                        min = t;
                    count += t;
                }
                Times.Clear();
                Task.Run(() =>
                {
                    Shell.Current.CurrentPage?.Dispatcher.Dispatch(async () =>
                    {
                        await Shell.Current.CurrentPage?.DisplayAlert("Alert", $" Measure {LimitCount} Items: All-{count} Mean-{count * 1.0 / LimitCount} Max-{max} Min-{min} ms", "OK");
                    });
                });
            }
            else
            {
                Times.Add(time);
            }
        }

        public List<List<Model>> models;
        public ObservableCollection<Model> ObservableModels = new ObservableCollection<Model>();
        private Faker<Model> testModel;
        ViewModel()
        {
            testModel = new Faker<Model>();
            testModel
                //.RuleFor(m => m.PersonIconUrl, f => f.Person.Avatar)
                .RuleFor(m => m.PersonName, f => f.Person.FullName)
                .RuleFor(m => m.PersonGender, f => f.Person.Gender.ToString())
                .RuleFor(m => m.PersonPhone, f => f.Person.Phone)
                .RuleFor(m => m.PersonTextBlogTitle, f => f.WaffleText(1, false))
                .RuleFor(m => m.PersonTextBlog, f => f.WaffleText(1, false))
                //.RuleFor(m => m.PersonImageBlogUrl, f => f.Image.PicsumUrl())
                .RuleFor(m => m.FirstComment, f => f.WaffleText(1, false))
                //.RuleFor(m => m.LikeIconUrl, f => f.Person.Avatar)
                //.RuleFor(m => m.CommentIconUrl, f => f.Person.Avatar)
                //.RuleFor(m => m.ShareIconUrl, f => f.Person.Avatar)
                ;
            var modelsList = testModel.Generate(10000);
            var itenmCountInSection = 20;
            models = new List<List<Model>>();
            for (var index = 0; index < modelsList.Count / itenmCountInSection; index++)
            {
                var list = new List<Model>();
                var lineStart = index * itenmCountInSection;
                for (var i = 0; i < itenmCountInSection; i++)
                {
                    var itemIndex = lineStart + i;
                    list.Add(modelsList[itemIndex]);
                }
                models.Add(list);
            }
            for (var index = 0; index < modelsList.Count; index++)
            {
                var m = modelsList[index];
                m.Index = index.ToString();
                ObservableModels.Add(m);
            }
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

            HeightForItem += HeightForItemMethod;
            NumberOfItems += NumberOfItemsMethod;
            //ViewHolderForItem += ViewHolderForItemMethod;
            ViewHolderForItem += BindingItemMethod;
            NumberOfSections += NumberOfSectionsMethod;
            ReuseIdForItem += ReuseIdForItemMethod;
            WantDragTo += WillDragToMethod;
            DidPrepareItem += DidPrepareItemMethod;
            IsSectionItem += IsSectionItemMethod;
        }

        bool IsSectionItemMethod(MAUICollectionView view, NSIndexPath indexPath)
        {
            var id = ReuseIdForItemMethod(view, indexPath);
            if (id == sectionCell)
                return true;
            else
                return false;
        }

        /// <summary>
        /// modify action of items that will show
        /// </summary>
        /// <param name="view"></param>
        /// <param name="indexPath"></param>
        /// <param name="viewHolder"></param>
        private void DidPrepareItemMethod(MAUICollectionView view, NSIndexPath indexPath, MAUICollectionViewViewHolder viewHolder)
        {

        }

        private void WillDragToMethod(MAUICollectionView collectionView, NSIndexPath path1, NSIndexPath path2)
        {
            if (path1.Row == 0 || path2.Row == 0)//section的header不处理, 不然会出错
                return;
            //collectionView.MoveItem(path1, path2);
            MoveData(path1.Row, path2.Row);
        }

        public void RemoveData(int section, int dataRow, int count = 3)
        {
            ViewModel.models[section].RemoveRange(dataRow, count);
        }

        public void InsertData(int section, int dataRow, int count = 3)
        {
            ViewModel.models[section].InsertRange(dataRow, ViewModel.Generate(count));
        }

        public void ChangeData(int index)
        {
            ViewModel.models[0][index] = ViewModel.Generate();
        }

        public void MoveData(int index, int toIndex)
        {
            var item = ViewModel.models[0][index];
            ViewModel.models[0].RemoveAt(index);
            ViewModel.models[0].Insert(toIndex, item);
        }

        public void LoadMoreOnFirst()
        {
            var models = ViewModel.Generate(20);
            ViewModel.models[0].InsertRange(0, models);
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
                ViewModel.models[ViewModel.models.Count - 1].AddRange(models);

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

        public int NumberOfSectionsMethod(MAUICollectionView tableView)
        {
            return ViewModel.models.Count;
        }

        public int NumberOfItemsMethod(MAUICollectionView tableView, int section)
        {
            return ViewModel.models[section].Count + 1; //+1 is add section header
        }

        public string ReuseIdForItemMethod(MAUICollectionView tableView, NSIndexPath indexPath)
        {
            if (indexPath.Row == 0)
            {
                return sectionCell;
            }
            return itemCellSimple;
        }

        public double HeightForItemMethod(MAUICollectionView tableView, NSIndexPath indexPath)
        {
            var type = ReuseIdForItemMethod(tableView, indexPath);
            switch (type)
            {
                case sectionCell:
                    return 40;
                case itemCellSimple:
                    return MAUICollectionViewViewHolder.AutoSize;
                default:
                    return 100;
            }
        }

        Model GetItemData(NSIndexPath indexPath)
        {
            return ViewModel.models[indexPath.Section][indexPath.Row - 1];
        }

        //给每个cell设置ID号（重复利用时使用）
        const string sectionCell = "sectionCell";
        const string itemCellSimple = "itemCellSimple";
        public MAUICollectionViewViewHolder ViewHolderForItemMethod(MAUICollectionView tableView, NSIndexPath indexPath, MAUICollectionViewViewHolder oldViewHolder, double widthConstrain)
        {
            //从tableView的一个队列里获取一个cell
            var type = ReuseIdForItemMethod(tableView, indexPath);
            MAUICollectionViewViewHolder cell;
            if (oldViewHolder != null)//只需局部刷新
            {
                cell = oldViewHolder;
                if (cell is ItemViewHolderSimple itemcellsimple)
                {
                    if (itemcellsimple != null)
                        itemcellsimple.ModelView.TestButton.Text = $"B Item Id={indexPath.Section}-{indexPath.Row}";
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
                else if (type == itemCellSimple)
                {
                    var simpleCell = cell as ItemViewHolderSimple;
                    if (simpleCell == null)
                    {
                        //没有,创建一个
                        simpleCell = new ItemViewHolderSimple(new ModelViewSimple() { }, type) { };
                        var deleteCommand = new Command<NSIndexPath>(execute: (NSIndexPath arg) =>
                        {
                            var count = 2;
                            var distance = arg.Row - 1 + count - ViewModel.models[arg.Section].Count;
                            if (distance < 0)
                                RemoveData(arg.Section, arg.Row - 1, count);
                            else
                            {
                                count = ViewModel.models[arg.Section].Count - (arg.Row - 1);
                                RemoveData(arg.Section, arg.Row - 1, count);
                            }
                            tableView.NotifyItemRangeRemoved(arg, count);
                            tableView.ReMeasure();
                        });
                        var insertCommand = new Command<NSIndexPath>(execute: (NSIndexPath arg) =>
                        {
                            var count = 2;
                            InsertData(arg.Section, arg.Row - 1, count);//section header occupy a row
                            tableView.NotifyItemRangeInserted(arg, count);
                            tableView.ReMeasure();
                        });
                        var insertAfterCommand = new Command<NSIndexPath>(execute: (NSIndexPath arg) =>
                        {
                            var count = 2;
                            InsertData(arg.Section, arg.Row, count);//section header occupy a row
                            tableView.NotifyItemRangeInserted(NSIndexPath.FromRowSection( arg.Row+1, arg.Section), count);
                            tableView.ReMeasure();
                        });
                        simpleCell.InitMenu(deleteCommand, insertCommand, insertAfterCommand);
                        simpleCell.ModelView.TestButton.Clicked += async (sender, e) =>
                        {
                            await Shell.Current.CurrentPage?.DisplayAlert("Alert", $"Section={simpleCell.IndexPath.Section} Row={simpleCell.IndexPath.Row}", "OK");
                        };
                    }

                    var data = GetItemData(indexPath);
                    simpleCell.ModelView.PersonName.Text = data.PersonName;
                    simpleCell.ModelView.PersonGender.Text = data.PersonGender;
                    simpleCell.ModelView.PersonPhone.Text = data.PersonPhone;
                    simpleCell.ModelView.PersonTextBlogTitle.Text = data.PersonTextBlogTitle;
                    simpleCell.ModelView.PersonImageBlog.Source = data.PersonImageBlogUrl;
                    simpleCell.ModelView.PersonTextBlog.Text = data.PersonTextBlog;
                    simpleCell.ModelView.TestButton.Text = $"AId={indexPath.ToString()}";

                    cell = simpleCell;
                }
            }
            if (cell.ContextMenu != null)
                cell.ContextMenu.IsEnable = tableView.CanContextMenu;
            return cell;
        }

        public MAUICollectionViewViewHolder BindingItemMethod(MAUICollectionView tableView, NSIndexPath indexPath, MAUICollectionViewViewHolder oldViewHolder, double widthConstrain)
        {
            //从tableView的一个队列里获取一个cell
            var type = ReuseIdForItemMethod(tableView, indexPath);
            MAUICollectionViewViewHolder cell ;
            if (oldViewHolder != null)//只需局部刷新
            {
                cell = oldViewHolder;
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
                else if (type == itemCellSimple)
                {
                    var simpleCell = cell as ItemViewHolderSimple;
                    if (simpleCell == null)
                    {
                        var content = new ModelViewSimple() { };
                        content.BindingData();

                        simpleCell = new ItemViewHolderSimple(content, type);
                        var deleteCommand = new Command<NSIndexPath>(execute: (NSIndexPath arg) =>
                        {
                            var count = 2;
                            var distance = arg.Row - 1 + count - ViewModel.models[arg.Section].Count;
                            if (distance < 0)
                                RemoveData(arg.Section, arg.Row - 1, count);
                            else
                            {
                                count = ViewModel.models[arg.Section].Count - (arg.Row - 1);
                                RemoveData(arg.Section, arg.Row - 1, count);
                            }
                            tableView.NotifyItemRangeRemoved(arg, count);
                            tableView.ReMeasure();
                        });
                        var insertCommand = new Command<NSIndexPath>(execute: (NSIndexPath arg) =>
                        {
                            var count = 2;
                            InsertData(arg.Section, arg.Row - 1, count);//section header occupy a row
                            tableView.NotifyItemRangeInserted(arg, count);
                            tableView.ReMeasure();
                        });
                        var insertAfterCommand = new Command<NSIndexPath>(execute: (NSIndexPath arg) =>
                        {
                            var count = 2;
                            InsertData(arg.Section, arg.Row, count);//section header occupy a row
                            tableView.NotifyItemRangeInserted(NSIndexPath.FromRowSection(arg.Row+1,arg.Section), count);
                            tableView.ReMeasure();
                        });
                        simpleCell.InitMenu(deleteCommand, insertCommand, insertAfterCommand);
                        simpleCell.ModelView.TestButton.Clicked += async (sender, e) =>
                        {
                            await Shell.Current.CurrentPage?.DisplayAlert("Alert", $"Section={simpleCell.IndexPath.Section} Row={simpleCell.IndexPath.Row}", "OK");
                        };
                    }
                    cell = simpleCell;
                }
            }
            if (cell is ItemViewHolderSimple)
            {
                var data = GetItemData(indexPath);
                //data.Index = indexPath.ToString();
                cell.BindingContext = data;
                (cell as ItemViewHolderSimple).ModelView.TestButton.Text = indexPath.ToString();
            }
            if (cell.ContextMenu != null)
                cell.ContextMenu.IsEnable = tableView.CanContextMenu;

            return cell;
        }
    }

    public class Model
    {
        public string PersonIconUrl { get; set; }
        public string PersonName { get; set; }
        public string PersonGender { get; set; }
        public string PersonPhone { get; set; }
        public string PersonTextBlogTitle { get; set; }
        public string PersonTextBlog { get; set; }
        public string PersonImageBlogUrl { get; set; }
        public string FirstComment { get; set; }
        public string LikeIconUrl { get; set; }
        public string CommentIconUrl { get; set; }
        public string ShareIconUrl { get; set; }
        public string Index { get; set; }
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
            TextView.BackgroundColor = Colors.Gray;
            TextView.TextColor = Colors.White;
            TextView.FontAttributes = FontAttributes.Bold;
        }

        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
            TextView.Text = string.Empty;
            UpdateSelectionState(SelectStatus.CancelWillSelect);
        }

        Color DefaultColor = Colors.LightYellow;
        public override void UpdateSelectionState(SelectStatus status)
        {
            if (DefaultColor == Colors.LightYellow)
            {
                DefaultColor = Content.BackgroundColor;
            }
            base.UpdateSelectionState(status);
            if (status == SelectStatus.Selected)
                Content.BackgroundColor = Colors.LightGrey;
            else
                Content.BackgroundColor = DefaultColor;
        }
    }

    internal class ItemViewHolderSimple : MAUICollectionViewViewHolder
    {
        public ItemViewHolderSimple(View itemView, string reuseIdentifier) : base(itemView, reuseIdentifier)
        {
            ModelView = itemView as ModelViewSimple;
        }

        public ModelViewSimple ModelView;

        public override void PrepareForReuse()
        {
            BindingContext = null;
            base.PrepareForReuse();
            ModelView.PersonIcon.Source = null;
            ModelView.PersonImageBlog.Source = null;
            //ModelView.CommentIcon.Source = null;
            //ModelView.ShareIcon.Source = null;
            UpdateSelectionState(SelectStatus.CancelWillSelect);
        }

        Color DefaultColor = Colors.LightYellow;
        public override void UpdateSelectionState(SelectStatus status)
        {
            if (DefaultColor == Colors.LightYellow)
            {
                DefaultColor = Content.BackgroundColor;
            }
            base.UpdateSelectionState(status);
            if (status == SelectStatus.Selected)
                Content.BackgroundColor = Colors.Grey.WithAlpha(100);
            else
                Content.BackgroundColor = DefaultColor;
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
#if ANDROID
            var av = this.Handler.PlatformView as Android.Views.View;
            var aContextMenu = ContextMenu as MauiUICollectionView.Gestures.AndroidContextMenu;
            aContextMenu.Init(av.Context, av);

            //设置PopupMenu样式, see https://learn.microsoft.com/en-us/xamarin/android/user-interface/controls/popup-menu
            aContextMenu.PlatformMenu.Inflate(Resource.Menu.popup_menu);
            aContextMenu.PlatformMenu.MenuItemClick += (s1, arg1) =>
            {
                switch (arg1.Item.ItemId)
                {
                    case Resource.Id.delete_item:
                        DeleteMenuCommand.Execute(IndexPath);
                        break;
                    case Resource.Id.insert_item:
                        InsertMenuCommand.Execute(IndexPath);
                        break;
                    case Resource.Id.insert_after_item:
                        InsertAfterMenuCommand.Execute(IndexPath);
                        break;
                }
            };
            ContextMenu = aContextMenu;
#endif
        }


        public Command DeleteMenuCommand;
        public Command InsertMenuCommand;
        public Command InsertAfterMenuCommand;
        public void InitMenu(Command deleteCommand, Command insertCommand, Command insertAfterCommand)
        {
            DeleteMenuCommand = deleteCommand;
            InsertMenuCommand = insertCommand;
            InsertAfterMenuCommand = insertAfterCommand;
#if IOS
            var menu = new Menu();
            var deleteMenuItem = new The49.Maui.ContextMenu.Action()
            {
                Title = "Delete",
                Command = deleteCommand,
            };
            var insertMenuItem = new The49.Maui.ContextMenu.Action()
            {
                Title = "Insert",
                Command = insertCommand,
            };
            var insertAfterMenuItem = new The49.Maui.ContextMenu.Action()
            {
                Title = "InsertAfter",
                Command = insertAfterCommand,
            };
            deleteMenuItem.SetBinding(The49.Maui.ContextMenu.Action.CommandParameterProperty, new Binding(nameof(IndexPath), source: this));
            insertMenuItem.SetBinding(The49.Maui.ContextMenu.Action.CommandParameterProperty, new Binding(nameof(IndexPath), source: this));
            insertAfterMenuItem.SetBinding(The49.Maui.ContextMenu.Action.CommandParameterProperty, new Binding(nameof(IndexPath), source: this));
            menu.Children = new System.Collections.ObjectModel.ObservableCollection<MenuElement>()
            {
                deleteMenuItem,
                insertMenuItem,
                insertAfterMenuItem
            };
            ContextMenu = new MauiUICollectionView.Gestures.iOSContextMenu(this, menu);
#elif WINDOWS || MACCATALYST
            var menu = new MenuFlyout();
            var deleteMenuItem = new MenuFlyoutItem()
            {
                Text = "Delete",
                //Command = deleteCommand,
                CommandParameter = this
            };
            var insertMenuItem = new MenuFlyoutItem()
            {
                Text = "Insert",
                // = insertCommand,
                CommandParameter = this
            };
            var insertAfterMenuItem = new MenuFlyoutItem()
            {
                Text = "InsertAfter",
                // = insertCommand,
                CommandParameter = this
            };
            deleteMenuItem.Clicked += DeleteMenuItem_Clicked;
            insertMenuItem.Clicked += InsertMenuItem_Clicked;
            insertAfterMenuItem.Clicked += InsertAfterMenuItem_Clicked;
            deleteMenuItem.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding(nameof(IndexPath), source: this));
            insertMenuItem.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding(nameof(IndexPath), source: this));
            insertAfterMenuItem.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding(nameof(IndexPath), source: this));
            menu.Add(deleteMenuItem);
            menu.Add(insertMenuItem);
            menu.Add(insertAfterMenuItem);
            ContextMenu = new MauiUICollectionView.Gestures.DesktopContextMenu(this, menu);
#elif ANDROID
            ContextMenu = new MauiUICollectionView.Gestures.AndroidContextMenu();
#endif
        }

        private void DeleteMenuItem_Clicked(object sender, EventArgs e)
        {
            MenuFlyoutItem menuItem = sender as MenuFlyoutItem;
            var repo = menuItem.CommandParameter as MAUICollectionViewViewHolder;
            DeleteMenuCommand.Execute(repo.IndexPath);
        }

        private void InsertMenuItem_Clicked(object sender, EventArgs e)
        {
            MenuFlyoutItem menuItem = sender as MenuFlyoutItem;
            var repo = menuItem.CommandParameter as MAUICollectionViewViewHolder;
            InsertMenuCommand.Execute(repo.IndexPath);
        }
        
        private void InsertAfterMenuItem_Clicked(object sender, EventArgs e)
        {
            MenuFlyoutItem menuItem = sender as MenuFlyoutItem;
            var repo = menuItem.CommandParameter as MAUICollectionViewViewHolder;
            InsertAfterMenuCommand.Execute(repo.IndexPath);
        }
    }

    class ModelViewSimple : ContainerLayout
    {
        static int newCellCount = 0;

        public int NewCellIndex;

        public Image PersonIcon;
        public Label PersonName;
        public Label PersonGender;
        public Label PersonPhone;
        public Label PersonTextBlogTitle;
        public Label PersonTextBlog;
        public Image PersonImageBlog;
        public Button TestButton;
        public Label FirstComment;
        public Image LikeIcon;
        private Label LikeCountLabel;
        public Image CommentIcon;
        public Label CommentCountLabel;
        public Image ShareIcon;
        private Label ShareCountLabel;

        public ModelViewSimple()
        {
            this.BackgroundColor = new Color(30, 30, 30);
            var root = this;
            var layout = new Grid()
            {
                RowDefinitions = new RowDefinitionCollection()
                {
                    new RowDefinition(){ Height = GridLength.Auto },
                    new RowDefinition(){ Height = GridLength.Auto },
                    new RowDefinition(){ Height = GridLength.Auto },
                    new RowDefinition(){ Height = GridLength.Auto },
                    new RowDefinition(){ Height = GridLength.Auto },
                }
            };
            root.Add(layout);
            var PersonIconContainer = new Border() { WidthRequest = 40, HeightRequest = 40, StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(20) } };
            PersonIcon = new Image() { BackgroundColor = Colors.AliceBlue };
            PersonIconContainer.Content = PersonIcon;
            var personInfoContainer = new HorizontalStackLayout();
            var personTextInfoContainer = new VerticalStackLayout();
            PersonName = new Label() { TextColor = Colors.White };
            var personOtherInfoContainer = new HorizontalStackLayout();
            PersonGender = new Label() { TextColor = Colors.White };
            PersonPhone = new Label() { Margin = new Thickness(5, 0, 0, 0), TextColor = Colors.White };
            personOtherInfoContainer.Add(PersonGender);
            personOtherInfoContainer.Add(PersonPhone);
            personTextInfoContainer.Add(PersonName);
            personTextInfoContainer.Add(personOtherInfoContainer);
            personInfoContainer.Add(PersonIconContainer);
            personInfoContainer.Add(personTextInfoContainer);
            PersonTextBlogTitle = new Label() { FontSize = 20, LineBreakMode = LineBreakMode.WordWrap, MaxLines = 2, TextColor = Colors.White, BackgroundColor = Colors.SlateGray };
            PersonTextBlog = new Label() { LineBreakMode = LineBreakMode.WordWrap, MaxLines = 3, TextColor = Colors.White, BackgroundColor = Colors.SlateGray };
            var imageInfoContainer = new HorizontalStackLayout();
            PersonImageBlog = new Image() { WidthRequest = 100, HeightRequest = 100, BackgroundColor = Colors.AliceBlue, HorizontalOptions = LayoutOptions.Start };
            TestButton = new Button() { Text = "Hello", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
            imageInfoContainer.Add(PersonImageBlog);
            imageInfoContainer.Add(TestButton);
            layout.Add(personInfoContainer);
            layout.Add(PersonTextBlogTitle);
            layout.Add(imageInfoContainer);
            layout.Add(PersonTextBlog);

            Grid.SetRow(personInfoContainer, 0);
            Grid.SetRow(PersonTextBlogTitle, 1);
            Grid.SetRow(imageInfoContainer, 2);
            Grid.SetRow(PersonTextBlog, 3);

            var bottomIconBar = new Grid()
            {
                ColumnDefinitions = new ColumnDefinitionCollection()
                {
                    new ColumnDefinition(){ Width = GridLength.Star },
                    new ColumnDefinition(){ Width = GridLength.Star },
                    new ColumnDefinition(){ Width = GridLength.Star },
                }
            };

            var likeContainer = new HorizontalStackLayout() { HorizontalOptions = LayoutOptions.Center };
            LikeIcon = new Image() { WidthRequest = 30, HeightRequest = 30, BackgroundColor = Colors.AliceBlue };
            LikeCountLabel = new Label { Text = "555", VerticalOptions = LayoutOptions.Center, TextColor = Colors.AliceBlue };
            likeContainer.Add(LikeIcon);
            likeContainer.Add(LikeCountLabel);
            var commentContainer = new HorizontalStackLayout() { HorizontalOptions = LayoutOptions.Center, BackgroundColor = Colors.Red };
            CommentIcon = new Image() { WidthRequest = 30, HeightRequest = 30, BackgroundColor = Colors.AliceBlue };
            CommentCountLabel = new Label { Text = "1000", VerticalOptions = LayoutOptions.Center, TextColor = Colors.AliceBlue };
            commentContainer.Add(CommentIcon);
            commentContainer.Add(CommentCountLabel);
            var shareContaner = new HorizontalStackLayout() { HorizontalOptions = LayoutOptions.Center };
            ShareIcon = new Image() { WidthRequest = 30, HeightRequest = 30, BackgroundColor = Colors.AliceBlue };
            ShareCountLabel = new Label { Text = "999", VerticalOptions = LayoutOptions.Center, TextColor = Colors.AliceBlue };
            shareContaner.Add(ShareIcon);
            shareContaner.Add(ShareCountLabel);

            Grid.SetColumn(likeContainer, 0);
            Grid.SetColumn(commentContainer, 1);
            Grid.SetColumn(shareContaner, 2);
            bottomIconBar.Add(likeContainer);
            bottomIconBar.Add(commentContainer);
            bottomIconBar.Add(shareContaner);

            layout.Add(bottomIconBar);
            Grid.SetRow(bottomIconBar, 4);

            NewCellIndex = ++newCellCount;
            CommentCountLabel.Text = NewCellIndex.ToString();
        }

        private void LikeIcon_Clicked(object sender, EventArgs e)
        {
            if ((LikeIcon.Source as FontImageSource)?.Color == Colors.Red)
                (LikeIcon.Source as FontImageSource).Color = Colors.White;
            else
                (LikeIcon.Source as FontImageSource).Color = Colors.Red;
            Console.WriteLine("Like Clicked");
        }

        public void BindingData()
        {
            PersonIcon.SetBinding(Image.SourceProperty, nameof(Model.PersonIconUrl));
            PersonName.SetBinding(Label.TextProperty, nameof(Model.PersonName));
            PersonGender.SetBinding(Label.TextProperty, nameof(Model.PersonGender));
            PersonPhone.SetBinding(Label.TextProperty, nameof(Model.PersonPhone));
            PersonTextBlogTitle.SetBinding(Label.TextProperty, nameof(Model.PersonTextBlogTitle));
            PersonTextBlog.SetBinding(Label.TextProperty, nameof(Model.PersonTextBlog));
            PersonImageBlog.SetBinding(Image.SourceProperty, nameof(Model.PersonImageBlogUrl));
        }
    }

    public class ContainerLayout : Layout
    {
        protected override ILayoutManager CreateLayoutManager()
        {
            return new ContainerLayoutManager(this);
        }
    }

    public class ContainerLayoutManager : LayoutManager
    {

        public ContainerLayoutManager(Microsoft.Maui.ILayout layout) : base(layout)
        {
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            var layout = Layout as Layout;
            (layout.Children[0] as IView).Arrange(bounds);
            return bounds.Size;
        }

        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            var layout = Layout as Layout;
            ViewModel.Stopwatch.Restart();
            var size = (layout.Children[0] as IView).Measure(widthConstraint, heightConstraint);
            ViewModel.Stopwatch.Stop();
            //ViewModel.CalculateMeanMeasureTimeAsync(ViewModel.Stopwatch.ElapsedMilliseconds);
            return size;
        }
    }
}
