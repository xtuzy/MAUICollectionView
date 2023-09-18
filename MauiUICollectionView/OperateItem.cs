namespace Yang.MAUICollectionView
{
    #region 操作

    public class OperateItem
    {
        public enum OperateType
        {
            /// <summary>
            /// remove old item
            /// </summary>
            Remove,
            /// <summary>
            /// insert new item 
            /// </summary>
            Insert,
            /// <summary>
            /// move item, will change position
            /// </summary>
            Move,
            /// <summary>
            /// replace
            /// </summary>
            Update,
            /// <summary>
            /// don't change position, but change IndexPath. it be designed for AnimationManager, AnimationManager don't have animation for it.
            /// </summary>
            //MoveNow,
            /// <summary>
            /// it be designed for AnimationManager, AnimationManager don't have animation for it.
            /// </summary>
            //RemoveNow
        }

        /// <summary>
        /// old index
        /// </summary>
        public NSIndexPath source;

        /// <summary>
        /// new index
        /// </summary>
        public NSIndexPath target;

        /// <summary>
        /// operate type
        /// </summary>
        public OperateType operateType;

        /// <summary>
        /// when operate, some item don't need animation. it is related to <see cref="OperateType.MoveNow"/>,<see cref="OperateType.RemoveNow"/> 
        /// </summary>
        public bool operateAnimate = true;

        /// <summary>
        /// move count, if target < source, it is negative number.
        /// </summary>
        public int moveCount = 0;
    }
    #endregion
}
