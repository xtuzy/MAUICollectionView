namespace MauiUICollectionView
{
    public class SelectEventArgs
    {
        public SelectStatus status;
        public Point point;

        public SelectEventArgs(SelectStatus status, Point point)
        {
            this.status = status;
            this.point = point;
        }

        public bool CancelGesture { get; internal set; }
    }
}