
namespace Yang.MAUICollectionView.TouchEffects
{
    public class TabEffect : RoutingEffect
    {
        internal static TabEffect? PickFrom(BindableObject? bindable)
        {
            IEnumerable<TabEffect>? effects = (bindable as VisualElement)?.Effects?.OfType<TabEffect>();
            return effects?.FirstOrDefault();
        }

        public Color RippleColor { get; set; } = new Color(31, 31, 31);
    }

#if WINDOWS
    public class TabPlatformEffect : Microsoft.Maui.Controls.Platform.PlatformEffect
    {
        protected override void OnAttached()
        {
            this.Control.PointerEntered += Control_PointerEntered;
            this.Control.PointerExited += Control_PointerExited;
        }

        private void Control_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var viewholder = (this.Element as MAUICollectionViewViewHolder);
            if (!viewholder.Selected)
            {
                viewholder.SetSelected(SelectStatus.CancelWillSelect);
            }
        }

        private void Control_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var viewholder = (this.Element as MAUICollectionViewViewHolder);
            if (!viewholder.Selected)
                viewholder.SetSelected(SelectStatus.WillSelect);
        }

        protected override void OnDetached()
        {
            this.Control.PointerEntered -= Control_PointerEntered;
            this.Control.PointerExited -= Control_PointerExited;
        }
    }
#elif __ANDROID__
    public class TabPlatformEffect : Microsoft.Maui.Controls.Platform.PlatformEffect
    {
        Android.Graphics.Drawables.RippleDrawable rippleDrawable;

        protected override void OnAttached()
        {
            var effect = TabEffect.PickFrom(Element);
            var androidColor = Microsoft.Maui.Graphics.Platform.GraphicsExtensions.AsColor(effect.RippleColor);
            int[] colors = { androidColor, androidColor };
            var rippleColors = new Android.Content.Res.ColorStateList(new int[][] { new int[] { } }, colors);
            rippleDrawable = new Android.Graphics.Drawables.RippleDrawable(rippleColors, null, new Android.Graphics.Drawables.ShapeDrawable(new Android.Graphics.Drawables.Shapes.RectShape()));
            this.Control.Foreground = rippleDrawable;
        }

        protected override void OnDetached()
        {
            this.Control.Foreground = null;
            rippleDrawable.Dispose();
        }
    }
#elif __IOS__
    public class TabPlatformEffect : Microsoft.Maui.Controls.Platform.PlatformEffect
    {
        protected override void OnAttached()
        {
            //throw new NotImplementedException();
        }

        protected override void OnDetached()
        {
            //throw new NotImplementedException();
        }
    }
#endif
}
