#if ANDROID
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yang.MAUICollectionView
{
    public class MyScrollViewHandler : ScrollViewHandler
    {
        protected override MauiScrollView CreatePlatformView()
        {
			var scrollView = new MyScrollView(
				new Android.Views.ContextThemeWrapper(MauiContext!.Context, Resource.Style.scrollViewTheme), null!,
                    Resource.Attribute.scrollViewStyle);

			scrollView.ClipToOutline = true;
			scrollView.FillViewport = true;

			return scrollView;
		}
    }
}
#endif