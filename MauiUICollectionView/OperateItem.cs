namespace MauiUICollectionView
{
    #region 操作

    public class OperateItem
    {
        public enum OperateType
        {
            /// <summary>
            /// 移除de
            /// </summary>
            remove,
            /// <summary>
            /// 新增的
            /// </summary>
            insert,
            /// <summary>
            /// 移动的, 代表IndexPath改变的
            /// </summary>
            move,
            /// <summary>
            /// 内容更新的
            /// </summary>
            update
        }
        //旧Index
        public NSIndexPath source;
        //新的Index
        public NSIndexPath target;
        public OperateType operateType;

        public bool animate = true;
    }
    #endregion
}
