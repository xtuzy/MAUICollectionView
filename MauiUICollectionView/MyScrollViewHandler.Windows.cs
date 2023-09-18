using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace Yang.MAUICollectionView
{
    public class MyScrollViewHandler : ScrollViewHandler
    {
        protected override void ConnectHandler(ScrollViewer platformView)
        {
            base.ConnectHandler(platformView);
            //platformView.ViewChanging += PlatformView_ViewChanging;
        }
    }
}
