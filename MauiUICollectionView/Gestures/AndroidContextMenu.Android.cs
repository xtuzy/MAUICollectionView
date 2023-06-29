using Android.Content;

namespace MauiUICollectionView.Gestures
{
    public class AndroidContextMenu : IContextMenu
    {
        public AndroidContextMenu(Context context, global::Android.Views.View av)
        {
            PlatformMenu = new AndroidX.AppCompat.Widget.PopupMenu(context, av);
        }

        public AndroidX.AppCompat.Widget.PopupMenu PlatformMenu { get; set; }

        public bool IsEnable { get; set; } = false;

        public void Show()
        {
            if (IsEnable)
            {
                PlatformMenu?.Show();
            }
        }
    }
}
