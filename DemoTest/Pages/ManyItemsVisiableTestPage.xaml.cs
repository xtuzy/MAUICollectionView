using MauiUICollectionView;
using MauiUICollectionView.Layouts;

namespace DemoTest.Pages;

public partial class ManyItemsVisiableTestPage : ContentPage
{
    public ManyItemsVisiableTestPage()
    {
        InitializeComponent();
        var viewModel = new ViewModel();
        var tableView = new MAUICollectionView()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Always,
            Source = new Source(viewModel),
            SelectionMode = SelectionMode.Multiple,
            //CanDrag = true,
            CanContextMenu = true,
        };
        Content = tableView;
        tableView.ItemsLayout = new CollectionViewListLayout(tableView)
        {
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
            return itemCell;
        }

        public float heightForRowAtIndexPathMethod(MAUICollectionView tableView, NSIndexPath indexPath)
        {
            var type = reuseIdentifierForRowAtIndexPathMethod(tableView, indexPath);
            switch (type)
            {
                case itemCell:
                    return MAUICollectionViewViewHolder.MeasureSelf;
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

                    textCell.Name.Text = ViewModel.models[indexPath.Row].PersonName;
                    textCell.Phone.Text = ViewModel.models[indexPath.Row].PersonPhone;

                    cell = textCell;
                }
            }
            if (cell.ContextMenu != null)
                cell.ContextMenu.IsEnable = tableView.CanContextMenu;
            return cell;
        }

        internal class ItemViewHolder : MAUICollectionViewViewHolder
        {
            public int NewCellIndex;

            public Label Name;
            public Label Phone;
            public ItemViewHolder(View itemView, string reuseIdentifier) : base(itemView, reuseIdentifier)
            {
                var grid = itemView as Grid;
                grid.ColumnDefinitions = new ColumnDefinitionCollection()
                {
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
                grid.Add(Name);
                grid.Add(Phone);

                Grid.SetColumn(Name, 0);
                Grid.SetColumn(Phone, 1);

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
                ContextMenu = new MauiUICollectionView.Gestures.DesktopContextMenu(this, menu);
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