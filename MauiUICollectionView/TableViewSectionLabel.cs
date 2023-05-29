using System.Runtime.InteropServices;

namespace MauiUICollectionView
{
    internal class TableViewSectionLabel : Label
    {
        private string headerTitle;

        public TableViewSectionLabel(string title)
        {
            this.headerTitle = title;
            Text = $"  {title}";
            FontAttributes = FontAttributes.Bold;
            FontSize = 17;
            TextColor = Colors.White;
            //ShadowColor = new UIColor(100 / 255.0f, green: 105 / 255.0f, blue: 110 / 255.0f, alpha: 1);
            //ShadowOffset = new CGSize(0, 1);
        }
    }
}