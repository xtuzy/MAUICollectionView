using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Yang.MAUICollectionView
{
    public static class ViewExtension
    {
        public static void RemoveFromSuperview(this View view)
        {
            (view.Parent as Layout)?.Remove(view);
        }

        public static SizeRequest MeasureSelf(this Element element, double widthConstraint, double heightConstraint)
        {
            return (element as IView).Measure(widthConstraint, heightConstraint);
        }

        public static void ArrangeSelf(this Element element, Rect rect)
        {
            (element as IView).Arrange(rect);
        }
    }
}
