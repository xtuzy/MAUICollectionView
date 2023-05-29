using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls;

namespace MauiUICollectionView
{
    public class MyScrollView 
    {
        public MyScrollView() 
        {
            var s = new ScrollViewer();
            s.ViewChanging += S_ViewChanging;
        }

        private void S_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}