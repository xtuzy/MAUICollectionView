using MauiUICollectionView;
using MauiUICollectionView.Layouts;
using Xunit;
using Xunit.Abstractions;

namespace MAUICollectionViewUnitTest.Tests
{
    public class MAUICollectionViewTest
    {
        readonly ITestOutputHelper _output;

        public MAUICollectionViewTest(ITestOutputHelper output)
        {
            _output = output;
        }

        static MAUICollectionView collectionView;
        static MAUICollectionView CollectionView
        {
            get
            {
                if (collectionView == null)
                {
                    collectionView = new MAUICollectionView();
                    collectionView.Source = new Source(new ViewModel());
                    var layout = new CollectionViewFlatListLayout(collectionView);
                    collectionView.ItemsLayout = layout;
                }
                return collectionView;
            }
        }

        [Theory]
        [InlineData(0, 1, 10, 0, 11)]
        [InlineData(0, 11, -10, 0, 1)]
        [InlineData(0, 5, 15, 0, 20)]
        [InlineData(0, 20, -15, 0, 5)]
        [InlineData(0, 5, 15+21, 1, 20)]
        [InlineData(1, 20, -(15+21), 0, 5)]
        public void NextItemTest(int sourceSection, int sourceRow, int count, int targetSection, int targetRow)
        {
            CollectionView.ReloadDataCount();
            var target = CollectionView.NextItem(NSIndexPath.FromRowSection(sourceRow, sourceSection), count);
            Assert.Equal(targetRow, target.Row);
            Assert.Equal(targetSection, target.Section);
        }

        [Theory]
        [InlineData(0, 5, 4866)]
        [InlineData(0, 5, 5244)]
        public void NextItemTest1(int sourceSection, int sourceRow, int count)
        {
            CollectionView.ReloadDataCount();
            var target = CollectionView.NextItem(NSIndexPath.FromRowSection(sourceRow, sourceSection), count);
            Assert.True(target.Row <= 20);
            _output.WriteLine($"Result is {target}");
            
        }

        [Theory]
        [InlineData(0, 1, 10 -1, 0, 11)]
        [InlineData(0, 5, 15 - 1, 0, 20)]
        [InlineData(0, 5, 15 + 21 - 1, 1, 20)]
        public void ItemCountInRangeTest(int sourceSection, int sourceRow, int count, int targetSection, int targetRow)
        {
            CollectionView.ReloadDataCount();
            var targetCount = CollectionView.ItemCountInRange(NSIndexPath.FromRowSection(sourceRow, sourceSection), NSIndexPath.FromRowSection(targetRow, targetSection));
            Assert.Equal(count, targetCount);
        }
    }
}
