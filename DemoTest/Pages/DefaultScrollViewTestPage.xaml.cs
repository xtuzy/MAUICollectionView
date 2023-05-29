namespace DemoTest.Pages;

public partial class DefaultScrollViewTestPage : ContentPage
{
    public DefaultScrollViewTestPage()
    {
        InitializeComponent();
        var index = 0;
        while (index < 100)
        {
            index++;
            var botCell = new Image();
            botCell.Source = "dotnet_bot.png";
            var baiduCell = new Image();
            baiduCell.Source = "https://www.baidu.com/img/PCtm_d9c8750bed0b3c7d089fa7d55720d6cf.png";

            var youdaoCell = new Image();
            youdaoCell.Source = "https://ydlunacommon-cdn.nosdn.127.net/cb776e6995f1c703706cf8c4c39a7520.png";

            rootLayout.Children.Add(botCell);
            rootLayout.Children.Add(baiduCell);
            rootLayout.Children.Add(youdaoCell);
            rootLayout.Children.Add(new Label() { HeightRequest = 20, Text = index.ToString() });
        }
    }
}