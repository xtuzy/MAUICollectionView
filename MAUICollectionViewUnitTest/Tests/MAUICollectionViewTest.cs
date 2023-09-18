using Yang.MAUICollectionView;
using Yang.MAUICollectionView.Layouts;
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
        [InlineData(1, 0, -16, 0, 5)]
        [InlineData(1, 1, -17, 0, 5)]
        [InlineData(0, 5, 15+21, 1, 20)]
        [InlineData(1, 20, -(15+21), 0, 5)]
        [InlineData(2, 0, -(15+21+1), 0, 5)]
        [InlineData(2, 1, -(15+21+2), 0, 5)]
        [InlineData(0, 0, 10489, 499, 10)]
        [InlineData(499, 10, -10489, 0, 0 )]
        [InlineData(499, 14, (int)(-3186.832), 347, 20)]
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
        [InlineData(0, 0, 10488)]
        [InlineData(0, 5, 10000+500-6)]
        public void NextItemTest_WhenLargeCount(int sourceSection, int sourceRow, int count)
        {
            CollectionView.ReloadDataCount();
            var target = CollectionView.NextItem(NSIndexPath.FromRowSection(sourceRow, sourceSection), count);
            var targetCount = CollectionView.ItemCountInRange(NSIndexPath.FromRowSection(sourceRow, sourceSection), target);
            Assert.True(target.Row <= 20);
            _output.WriteLine($"NextItem of {sourceSection}-{sourceRow} is {target}");
            _output.WriteLine($"ItemCountInRange of {sourceSection}-{sourceRow} and {target} is {targetCount}");
            Assert.Equal(count, targetCount + 1);
        }

        [Theory]
        [InlineData(0, 5, 15 + 21 -2 , 1, 20)]
        [InlineData(0, 0, 10487 , 499, 10)]
        public void NextItemTest_WhenRemove(int sourceSection, int sourceRow, int count, int targetSection, int targetRow)
        {
            var collectionView = new MAUICollectionView();
            var viewModel = new ViewModel();
            viewModel.models[0].RemoveRange(3, 2);
            viewModel.models[viewModel.models.Count-1].RemoveRange(3, 2);
            collectionView.Source = new Source(viewModel);
            collectionView.ReloadDataCount();
            var sourceIndex = NSIndexPath.FromRowSection(sourceRow, sourceSection);
            var targetIndex = NSIndexPath.FromRowSection(targetRow, targetSection);
            var resultIndex = collectionView.NextItem(sourceIndex, count);
            var resultCount = collectionView.ItemCountInRange(sourceIndex, targetIndex);
            _output.WriteLine($"NextItem of {sourceIndex} is {resultIndex}");
            _output.WriteLine($"ItemCountInRange of {sourceIndex} and {targetIndex} is {resultCount}");
            Assert.True(targetRow == resultIndex.Row && targetSection== resultIndex.Section);
            Assert.Equal(count, resultCount+1);
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
