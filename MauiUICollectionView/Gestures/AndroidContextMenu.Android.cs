using Android.Content;
using System.Diagnostics;

namespace MauiUICollectionView.Gestures
{
    public class AndroidContextMenu : IContextMenu
    {
        public AndroidContextMenu()
        {

        }

        public void Init(Context context, global::Android.Views.View av)
        {
            PlatformMenu = new AndroidX.AppCompat.Widget.PopupMenu(context, av);
        }

        public AndroidX.AppCompat.Widget.PopupMenu PlatformMenu { get; set; }

        public bool IsEnable { get; set; } = false;

        public void Show()
        {
            Debug.WriteLine("Show");
            if (IsEnable)
            {
                PlatformMenu?.Show();
            }
        }
    }
}
