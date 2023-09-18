using System.Windows.Input;
#if ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#else
using PlatformView = UIKit.UIView;
#endif
namespace Yang.MAUICollectionView
{
    public interface IGestureManager
    {
        ICommand SelectPointCommand { get; set; }
        ICommand DragPointCommand { get; set; }
        ICommand LongPressPointCommand { get; set; }

        void CancleSubscribeGesture();
        void SetCanDrag(bool can = false);
        void SubscribeGesture(View view);
    }
}