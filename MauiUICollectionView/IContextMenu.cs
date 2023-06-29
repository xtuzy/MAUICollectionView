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
        void Show();
    }
}
