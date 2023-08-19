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
            CanContextMenu = true,
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
        public Source(ViewModel viewModel)
        {
            ViewModel = viewModel;

            HeightForItem += heightForRowAtIndexPathMethod;
            NumberOfItems += numberOfRowsInSectionMethod;
            ViewHolderForItem += cellForRowAtIndexPathMethod;
            NumberOfSections += numberOfSectionsInTableViewMethod;
            ReuseIdForItem += reuseIdentifierForRowAtIndexPathMethod;
            WantDragTo += DragTo;
            WantDropTo += DropTo;
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
                    }

                    cell = textCell;
                }
            }
            if (cell.ContextMenu != null)
                cell.ContextMenu.IsEnable = tableView.CanContextMenu;
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

#if WINDOWS || MACCATALYST
                var menu = new MenuFlyout();
                var menuItem = new MenuFlyoutItem()
                {
                    Text = "Delete",
                    Command = new Command(() => { }),
                    CommandParameter = IndexPath
                };
                menuItem.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding(nameof(IndexPath), source: this));
                menu.Add(menuItem);
                //ContextMenu = new MauiUICollectionView.Gestures.DesktopContextMenu(this, menu);
#endif
            }

            public override void UpdateSelectionState(bool shouldHighlight)
            {
                base.UpdateSelectionState(shouldHighlight);
                if(shouldHighlight)
                {
                    BackgroundColor = Colors.Gray;
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
                UpdateSelectionState(false);
            }
        }
    }
}