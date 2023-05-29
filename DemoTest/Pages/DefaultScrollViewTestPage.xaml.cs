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
            var botCell = new DefaultTestPage.ImageCell("bot");
            botCell.Image.Source = "dotnet_bot.png";
            botCell.Image.HeightRequest = 100;
            botCell.Image.WidthRequest = 100;
            var baiduCell = new DefaultTestPage.ImageCell("bot");
            baiduCell.Image.Source = "https://www.baidu.com/img/PCtm_d9c8750bed0b3c7d089fa7d55720d6cf.png";
            baiduCell.Image.HeightRequest = 100;
            baiduCell.Image.WidthRequest = 100;
            var youdaoCell = new DefaultTestPage.ImageCell("bot");
            youdaoCell.Image.Source = "https://ydlunacommon-cdn.nosdn.127.net/cb776e6995f1c703706cf8c4c39a7520.png";
            youdaoCell.Image.HeightRequest = 100;
            youdaoCell.Image.WidthRequest = 100;
            rootLayout.Children.Add(botCell);
            rootLayout.Children.Add(baiduCell);
            rootLayout.Children.Add(youdaoCell);
            rootLayout.Children.Add(new Label() { HeightRequest = 20, Text = index.ToString() });
        }
    }
}