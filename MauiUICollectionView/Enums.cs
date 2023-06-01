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
}
