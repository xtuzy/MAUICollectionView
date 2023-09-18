using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using The49.Maui.ContextMenu;

namespace Yang.MAUICollectionView.Gestures
{
    public class iOSContextMenu : IContextMenu
    {
        View View;
        public iOSContextMenu(View v, Menu menu)
        {
            View = v;
            PlatformMenu = menu;
        }

        public Menu PlatformMenu { get; set; }

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
                    The49.Maui.ContextMenu.ContextMenu.SetMenu(View, new DataTemplate(() => PlatformMenu));
                }
                else
                {
                    The49.Maui.ContextMenu.ContextMenu.SetMenu(View, null);
                }
            } 
        }

        public void Show()
        {
            
        }
    }
}
