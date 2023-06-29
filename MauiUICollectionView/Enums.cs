using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiUICollectionView
{
    public enum ScrollPosition
    {
        None, Top, Middle, Bottom
    }

    public enum RowAnimation
    {
        Fade, Right, Left, Top, Bottom, None, Middle, Automatic = 100
    }

    public enum SelectionStyle
    {
        None, Blue, Gray
    }

    public enum SelectStatus
    {
        /// <summary>
        /// 开启选择动画
        /// </summary>
        WillSelect,
        /// <summary>
        /// 确认选择状态
        /// </summary>
        Selected,
        /// <summary>
        /// 取消选择动画, 设置默认状态
        /// </summary>
        CancelWillSelect
    }

    public enum GestureDevice
    {
        /// <summary>
        /// 鼠标
        /// </summary>
        Mouse,
        /// <summary>
        /// 手或者笔
        /// </summary>
        Touch
    }
}
