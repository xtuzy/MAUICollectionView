namespace MauiUICollectionView
{
    public class NSIndexPath
    {
        public int Row { get; private set; }
        public int Section { get; private set; }

        /// <summary>
        /// https://developer.apple.com/documentation/foundation/nsindexpath/1407552-compare
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual int Compare(NSIndexPath other)
        {
            //相等
            if(Section == other.Section && Row == other.Row)
            {
                return 0;
            }
            //升序
            if (Section < other.Section || (Section == other.Section && Row < other.Row)) 
            {
                return -1;
            }
            return 1;
        }

        public static NSIndexPath FromRowSection(int row, int section)
        {
            return new NSIndexPath() { Row = row, Section = section };
        }

        public bool IsEqual(NSIndexPath other)
        {
            return Section == other.Section && Row == other.Row;
        }

        /// <summary>
        /// https://blog.csdn.net/nini_boom/article/details/78728129
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = obj as NSIndexPath;
            return Section == other.Section && Row == other.Row;
        }

        /// <summary>
        /// https://blog.csdn.net/nini_boom/article/details/78728129
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Section.GetHashCode() + Row.GetHashCode();
        }
    }
}
