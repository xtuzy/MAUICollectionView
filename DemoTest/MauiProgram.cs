using MauiUICollectionView;
using Microsoft.Extensions.Logging;

namespace DemoTest
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiUICollectionView()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Font Awesome 6 Free-Solid-900.otf", "FontAwesome6FreeSolid900");
                });
#if IOS
            //see https://github.com/dotnet/maui/issues/14257#issuecomment-1646408225
            Microsoft.Maui.Handlers.ScrollViewHandler.Mapper.AppendToMapping(nameof(IScrollView.ContentSize), (h, v) =>
                {
                    var contentSize = h.VirtualView.ContentSize;

                    if (contentSize.IsZero)
                        return;

                    UIKit.UIScrollView uiScrollView = h.PlatformView;
                    var container = uiScrollView.Subviews.FirstOrDefault(x => x.Tag == 0x845fed);

                    if (container != null && container.Bounds.Height != contentSize.Height)
                    {
                        container.Bounds = new CoreGraphics.CGRect(
                            container.Bounds.X,
                            container.Bounds.Y,
                            contentSize.Width,
                            contentSize.Height);

                        (h.VirtualView as IView).InvalidateMeasure();
                    }
                });
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}