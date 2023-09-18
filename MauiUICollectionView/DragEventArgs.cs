namespace Yang.MAUICollectionView
{
    public class DragEventArgs
    {
        public GestureDevice Device;
        public GestureStatus status;
        public Point point;

        public DragEventArgs(GestureStatus status, Point point)
        {
            this.status = status;
            this.point = point;
        }

        public bool CancelGesture { get; internal set; }
    }
}