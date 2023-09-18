namespace Yang.MAUICollectionView.Layouts
{
    public abstract partial class CollectionViewLayout
    {
        public class LayoutInfor
        {
            public NSIndexPath StartItem;
            public NSIndexPath EndItem;
            public Rect StartBounds;
            public Rect EndBounds;

            public LayoutInfor Copy()
            {
                return new LayoutInfor()
                {
                    StartItem = this.StartItem,
                    EndItem = this.EndItem,
                    StartBounds = this.StartBounds,
                    EndBounds = this.EndBounds
                };
            }
        }
    }
}