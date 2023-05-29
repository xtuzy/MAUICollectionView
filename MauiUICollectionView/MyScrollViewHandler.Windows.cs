using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace MauiUICollectionView
{
    public class MyScrollViewHandler : ScrollViewHandler
    {
        protected override void ConnectHandler(ScrollViewer platformView)
        {
            base.ConnectHandler(platformView);
            platformView.ViewChanging += PlatformView_ViewChanging;
            
        }

        private void PlatformView_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            VirtualView.VerticalOffset = PlatformView.VerticalOffset;
            VirtualView.HorizontalOffset = PlatformView.HorizontalOffset;
            Debug.WriteLine($"PlatformView.VerticalOffset {PlatformView.VerticalOffset}");
            var t = (PlatformView.Content as UIElement).RenderTransform;
            (VirtualView as TableView).ScrollFinished(new ScrolledEventArgs(PlatformView.HorizontalOffset, PlatformView.VerticalOffset));
        }

        protected override void DisconnectHandler(ScrollViewer platformView)
        {
            base.DisconnectHandler(platformView);
            platformView.ViewChanging -= PlatformView_ViewChanging;
        }
    }
}
