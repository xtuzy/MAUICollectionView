namespace MauiUICollectionView
{
    public static class AppHostBuilderExtensions
    {
        public static MauiAppBuilder UseMauiUICollectionView(this MauiAppBuilder builder)
        {
            builder.ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler(typeof(TableView), typeof(MyScrollViewHandler));
#elif WINDOWS
                handlers.AddHandler(typeof(TableView), typeof(MyScrollViewHandler));
#endif
            });

            return builder;
        }
    }
}
