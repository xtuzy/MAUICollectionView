using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiUICollectionView
{
    public interface IContextMenu
    {
        bool IsEnable { get; set; }
        /// <summary>
        /// Android长按手势弹出ContextMenu, 需要实现这个方法并在长按时调用;
        /// 桌面上MAUI默认使用右键触发, 不需要实现该方法; 
        /// iOS上默认长按, 不能通过自定义的长按手势触发, 不需要实现该方法.
        /// </summary>
        void Show();
    }
}
