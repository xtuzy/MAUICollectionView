using MauiUICollectionView;
using MauiUICollectionView.Layouts;
using Microsoft.Maui.Controls;
using static System.Collections.Specialized.BitVector32;

namespace DemoTest.Pages;

public partial class ManyItemsVisiableTestPage : ContentPage
{
    public ManyItemsVisiableTestPage()
    {
        InitializeComponent();
        var viewModel = ViewModel.Instance;
        var tableView = new MAUICollectionView()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Always,
            SelectionMode = SelectionMode.Multiple,
            CanDrag = true,
        };
        content.Content = tableView;
        tableView.ItemsLayout = new CollectionViewFlatListLayout(tableView)
        {
        };
        SetSource.Clicked += (sender, e) =>
        {
            tableView.Source = new Source(viewModel);
        };
        RemoveSource.Clicked += (sender, e) =>
        {
            tableView.Source = null;
        };
    }

    class Source : MAUICollectionViewSource
    {
        ViewModel ViewModel;

        public void RemoveData(int section, int dataRow, int count = 3)
        {
            ViewModel.models[section].RemoveRange(dataRow, count);
        }

        public void InsertData(int section, int dataRow, int count = 3)
        {
            ViewModel.models[section].InsertRange(dataRow, ViewModel.Generate(count));
        }

        public Source(ViewModel viewModel)
        {
            ViewModel = viewModel;

            HeightForItem += heightForRowAtIndexPathMethod;
            NumberOfItems += numberOfRowsInSectionMethod;
            ViewHolderForItem += cellForRowAtIndexPathMethod;
            NumberOfSections += numberOfSectionsInTableViewMethod;
            ReuseIdForItem += reuseIdentifierForRowAtIndexPathMethod;
            OnDragOver += DragTo;
            OnDrop += DropTo;
        }

        private void DropTo(MAUICollectionView view, NSIndexPath path1, NSIndexPath path2)
        {
            var data = ViewModel.models[path1.Section][path1.Row];
            ViewModel.models[path1.Section].Remove(data);
            ViewModel.models[path2.Section].Insert(path2.Row, data);
            view.MoveItem(path1, path2);
        }

        private void DragTo(MAUICollectionView view, NSIndexPath path1, NSIndexPath path2)
        {
            var data = ViewModel.models[path1.Section][path1.Row];
            ViewModel.models[path1.Section].Remove(data);
            ViewModel.models[path2.Section].Insert(path2.Row, data);
            view.MoveItem(path1, path2);
        }

        public int numberOfSectionsInTableViewMethod(MAUICollectionView tableView)
        {
            return ViewModel.models.Count;
        }

        public int numberOfRowsInSectionMethod(MAUICollectionView tableView, int section)
        {
            return ViewModel.models[section].Count;
        }

        public string reuseIdentifierForRowAtIndexPathMethod(MAUICollectionView tableView, NSIndexPath indexPath)
        {
            return itemCell;
        }

        public double heightForRowAtIndexPathMethod(MAUICollectionView tableView, NSIndexPath indexPath)
        {
            var type = reuseIdentifierForRowAtIndexPathMethod(tableView, indexPath);
            switch (type)
            {
                case itemCell:
                    return MAUICollectionViewViewHolder.AutoSize;
                default:
                    return 100;
            }
        }

        int newCellCount = 0;
        //给每个cell设置ID号（重复利用时使用）
        const string itemCell = "itemCell";
        public MAUICollectionViewViewHolder cellForRowAtIndexPathMethod(MAUICollectionView tableView, NSIndexPath indexPath, MAUICollectionViewViewHolder oldViewHolder, double widthConstrain)
        {
            //从tableView的一个队列里获取一个cell
            var type = reuseIdentifierForRowAtIndexPathMethod(tableView, indexPath);
            MAUICollectionViewViewHolder cell;
            if (oldViewHolder != null)//只需局部刷新
            {
                cell = oldViewHolder;
            }
            else
            {
                cell = tableView.DequeueRecycledViewHolderWithIdentifier(type);

                if (type == itemCell)
                {
                    var textCell = cell as ItemViewHolder;
                    //判断队列里面是否有这个cell 没有自己创建，有直接使用
                    if (textCell == null)
                    {
                        //没有,创建一个
                        textCell = new ItemViewHolder(new Grid(), type) { };
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
                            tableView.NotifyItemRangeInserted(NSIndexPath.FromRowSection(arg.Row + 1, arg.Section), count);
                            tableView.ReMeasure();
                        });
                        //textCell.InitMenu(deleteCommand, insertCommand, insertAfterCommand);
                    }

                    cell = textCell;
                }
            }
            if (cell.ContextMenu != null)
                cell.ContextMenu.IsEnable = true;
            if (cell is ItemViewHolder)
            {
                (cell as ItemViewHolder).Id.Text = indexPath.ToString();
                (cell as ItemViewHolder).Name.Text = ViewModel.models[indexPath.Section][indexPath.Row].PersonName;
                (cell as ItemViewHolder).Phone.Text = ViewModel.models[indexPath.Section][indexPath.Row].PersonPhone;
            }
            return cell;
        }

        internal class ItemViewHolder : MAUICollectionViewViewHolder
        {
            public int NewCellIndex;

            public Label Name;
            public Label Phone;
            public Label Id;
            public ItemViewHolder(View itemView, string reuseIdentifier) : base(itemView, reuseIdentifier)
            {
                var grid = itemView as Grid;
                grid.ColumnDefinitions = new ColumnDefinitionCollection()
                {
                    new ColumnDefinition(){ Width = GridLength.Star},
                    new ColumnDefinition(){ Width = GridLength.Star},
                    new ColumnDefinition(){ Width = GridLength.Star}
                };
                Name = new Label()
                {
                    VerticalTextAlignment =TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Center
                };
                Phone = new Label()
                {
                    VerticalTextAlignment =TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Center
                };
                Id = new Label()
                {
                    VerticalTextAlignment =TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Center
                };
                grid.Add(Name);
                grid.Add(Phone);
                grid.Add(Id);

                Grid.SetColumn(Name, 0);
                Grid.SetColumn(Phone, 1);
                Grid.SetColumn(Id, 2);

                //Id.SetBinding(Label.TextProperty, new Binding(nameof(IndexPath), source: this));
            }

            public override void UpdateSelectionState(SelectStatus status)
            {
                base.UpdateSelectionState(status);
                if(status == SelectStatus.Selected)
                {
                    BackgroundColor = Colors.Gray;
                }else if(status == SelectStatus.WillSelect)
                {
                    BackgroundColor = Colors.AliceBlue;
                }
                else
                {
                    BackgroundColor = Colors.Transparent;
                }
            }

            public override void PrepareForReuse()
            {
                base.PrepareForReuse();
                Name.Text = string.Empty;
                Phone.Text = string.Empty;
                UpdateSelectionState(SelectStatus.CancelWillSelect);
            }

            internal void InitMenu(Command deleteCommand, Command insertCommand, Command insertAfterCommand)
            {
#if WINDOWS || MACCATALYST
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
                deleteMenuItem.Clicked += (sender ,e)=>
                {
                    MenuFlyoutItem menuItem = sender as MenuFlyoutItem;
                    var repo = menuItem.CommandParameter as MAUICollectionViewViewHolder;
                    deleteCommand.Execute(repo.IndexPath);
                };
                insertMenuItem.Clicked += (sender, e) =>
                {
                    MenuFlyoutItem menuItem = sender as MenuFlyoutItem;
                    var repo = menuItem.CommandParameter as MAUICollectionViewViewHolder;
                    insertCommand.Execute(repo.IndexPath);
                };
                insertAfterMenuItem.Clicked += (sender, e) =>
                {
                    MenuFlyoutItem menuItem = sender as MenuFlyoutItem;
                    var repo = menuItem.CommandParameter as MAUICollectionViewViewHolder;
                    insertAfterCommand.Execute(repo.IndexPath);
                };
                deleteMenuItem.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding(nameof(IndexPath), source: this));
                insertMenuItem.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding(nameof(IndexPath), source: this));
                insertAfterMenuItem.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding(nameof(IndexPath), source: this));
                menu.Add(deleteMenuItem);
                menu.Add(insertMenuItem);
                menu.Add(insertAfterMenuItem);
                ContextMenu = new MauiUICollectionView.Gestures.DesktopContextMenu(this, menu);
#endif
            }
        }
    }
}