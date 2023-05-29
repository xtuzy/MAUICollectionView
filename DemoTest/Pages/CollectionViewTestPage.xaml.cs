using Bogus;

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
            var testModel = new Faker<Model>();
            testModel.RuleFor(u => u.Url, f => f.Image.PicsumUrl());
            Urls = testModel.Generate(100);
        }
    }

    class Model
    {
        public string Url { get; set; }
    }
}