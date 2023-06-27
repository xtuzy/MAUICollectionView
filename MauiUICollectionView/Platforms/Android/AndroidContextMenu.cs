using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiUICollectionView.Platforms.Android
{
    public class AndroidContextMenu : IContextMenu
    {
        public AndroidContextMenu(Context context, global::Android.Views.View av)
        {
            PlatformMenu = new AndroidX.AppCompat.Widget.PopupMenu(context, av);
        }

        public AndroidX.AppCompat.Widget.PopupMenu PlatformMenu { get; set; }
        public void Show()
        {
            PlatformMenu?.Show();
        }
    }
}
