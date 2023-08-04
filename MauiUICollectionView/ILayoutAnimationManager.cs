using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiUICollectionView
{
    public interface ILayoutAnimationManager: IDisposable
    {
        /// <summary>
        /// operated item will be add to manager
        /// </summary>
        /// <param name="viewHolder"></param>
        void AddOperatedItem(MAUICollectionViewViewHolder viewHolder);
        /// <summary>
        /// run animation
        /// </summary>
        /// <param name="runScrollAnim">when scroll</param>
        /// <param name="runOperateAnim">when have operated item</param>
        void Run(bool runScrollAnim, bool runOperateAnim);
        /// <summary>
        /// stop operate animation, when next operate be done, or reload CollectionView
        /// </summary>
        void StopOperateAnim();
    }
}
