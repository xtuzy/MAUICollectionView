using Yang.MAUICollectionView.TouchEffects;

namespace Yang.MAUICollectionView
{
    public static class AppHostBuilderExtensions
    {
        public static MauiAppBuilder UseMAUICollectionView(this MauiAppBuilder builder)
        {
            builder.ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler(typeof(MAUICollectionView), typeof(MyScrollViewHandler));
#elif WINDOWS
                handlers.AddHandler(typeof(MAUICollectionView), typeof(MyScrollViewHandler));
#endif
            });
            builder.ConfigureEffects(effects =>
            {
                effects.Add<TabEffect, TabPlatformEffect>();
            });
            return builder;
        }
    }
}
