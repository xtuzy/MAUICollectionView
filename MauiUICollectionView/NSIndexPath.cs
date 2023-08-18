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
        /// <returns>if other is greater, return -1; if equal, return 0; if other is less, return 1</returns>
        public virtual int Compare(NSIndexPath other)
        {
            //相等
            if (Section == other.Section && Row == other.Row)
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
            if (other == null)
                return false;
            return Section == other.Section && Row == other.Row;
        }

        /// <summary>
        /// https://blog.csdn.net/nini_boom/article/details/78728129
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
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

        /// <summary>
        /// equal or in
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public bool IsInRange(NSIndexPath start, NSIndexPath end)
        {
            if(start == null || end == null)
                return false;
            if(start.Compare(end) > 1)// if start > end
            {
                var temp = end;
                end = start;
                start = temp;
            }
            if(this.Compare(start) >= 0 && this.Compare(end) <= 0)
            {
                return true;
            }
            return false;
        }

        public static List<NSIndexPath> InRange((int start, int end) range, int section)
        {
            var ips = new List<NSIndexPath>();
            for (var idx = range.start; idx <= range.end; idx++)
            {
                ips.Add(NSIndexPath.FromRowSection(idx, section));
            }
            return ips;
        }

        public static bool operator <(NSIndexPath lhs, NSIndexPath rhs)
        {
            return lhs.Compare(rhs) == -1;
        }

        public static bool operator >(NSIndexPath lhs, NSIndexPath rhs)
        {
            return lhs.Compare(rhs) == 1;
        }

        public override string ToString()
        {
            return $"{Section}-{Row}";
        }
    }
}
