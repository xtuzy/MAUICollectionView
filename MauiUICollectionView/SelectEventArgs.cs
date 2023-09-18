namespace Yang.MAUICollectionView
{
    public class SelectEventArgs
    {
        public SelectStatus status;
        public Point point;
        public NSIndexPath item;
        public SelectEventArgs(SelectStatus status, Point point)
        {
            this.status = status;
            this.point = point;
        }
        
        public SelectEventArgs(SelectStatus status, NSIndexPath item)
        {
            this.status = status;
            this.item = item;
        }

        public bool CancelGesture { get; internal set; }
    }
}