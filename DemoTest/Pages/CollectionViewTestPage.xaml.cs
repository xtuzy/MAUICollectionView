using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DemoTest.Pages;

public partial class CollectionViewTestPage : ContentPage
{

    public CollectionViewTestPage()
    {
        InitializeComponent();
        var model = new ViewModel();
        collectionView.BindingContext = model;
        collectionView.ItemsSource = model.Urls;
    }

    class ViewModel 
    {
        public List<Model> Urls { get; set; } 
        public ViewModel()
        {
            Urls = new List<Model>();
            var index = 0;
            while (index < 100)
            {
                index++;
                Urls.Add(new Model() { Url = "dotnet_bot.png" });
                Urls.Add(new Model() { Url = "https://www.baidu.com/img/PCtm_d9c8750bed0b3c7d089fa7d55720d6cf.png" });
                Urls.Add(new Model() { Url = "https://ydlunacommon-cdn.nosdn.127.net/cb776e6995f1c703706cf8c4c39a7520.png" });
            }
        }
    }

    class Model
    {
        public string Url { get; set; }
    }
}