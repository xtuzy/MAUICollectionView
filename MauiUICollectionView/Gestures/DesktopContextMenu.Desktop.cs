using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yang.MAUICollectionView.Gestures
{
    public class DesktopContextMenu : IContextMenu
    {
        View View;
        public DesktopContextMenu(View v, MenuFlyout menu)
        {
            View = v;
            PlatformMenu = menu;
        }

        public MenuFlyout PlatformMenu { get; set; }

        bool isEnable = false;
        public bool IsEnable 
        {
            get 
            {
                return isEnable;
            }
            set
            {
                if (value == isEnable)
                    return;
                if(value == true)
                {
                    FlyoutBase.SetContextFlyout(View, PlatformMenu);
                }
                else
                {
                    FlyoutBase.SetContextFlyout(View, null);
                }
            } 
        }

        public void Show()
        {
            
        }
    }
}
