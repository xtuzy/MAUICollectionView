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
            Remove,
            /// <summary>
            /// 新增的
            /// </summary>
            Insert,
            /// <summary>
            /// 移动的, 代表IndexPath改变的, 位置也变的
            /// </summary>
            Move,
            /// <summary>
            /// 内容全部更新的, 可理解为替换, 内部实现为替换View
            /// </summary>
            Update,
            /// <summary>
            /// 位置不变, 但IndexPath变. 此tag专为AnimationManager设计, 在AnimationManager不对其进行动画.
            /// </summary>
            MoveNow,
            /// <summary>
            /// 此tag专为AnimationManager设计, 在AnimationManager不对其进行动画.
            /// </summary>
            RemoveNow
        }
        //旧Index
        public NSIndexPath source;
        //新的Index
        public NSIndexPath target;
        public OperateType operateType;

        public bool operateAnimate = true;
    }
    #endregion
}
