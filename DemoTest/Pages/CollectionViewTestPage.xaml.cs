using Bogus;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;

namespace DemoTest.Pages;

public partial class CollectionViewTestPage : ContentPage
{

    public CollectionViewTestPage()
    {
        InitializeComponent();
        var model = new ViewModel();
        collectionView.BindingContext = model;
        collectionView.ItemsSource = model.Models;
        collectionView.SelectionMode = SelectionMode.Single;

        Add.Clicked += (sender, e) =>
        {
            var index = 2;
            model.Models.Insert(index, model.testModel.Generate(1)[0]);
        };

        Remove.Clicked += (sender, e) =>
        {
            var index = 2;
            model.Models.RemoveAt(index);
        };

        Move.Clicked += (sender, e) =>
        {
            var index = 3;
            var target = 1;
            model.Models.Move(index, target);
        };

        Change.Clicked += (sender, e) =>
        {
            var index = 2;
            model.Models[index] = model.testModel.Generate(1)[0];
        };
    }

    class ViewModel
    {
        public Faker<Model> testModel;

        public ObservableCollection<Model> Models { get; set; } = new ObservableCollection<Model>();
        public ViewModel()
        {
            testModel = new Faker<Model>();
            testModel
                .RuleFor(m => m.PersonIconUrl, f => f.Person.Avatar)
                .RuleFor(m => m.PersonName, f => f.Person.FullName)
                .RuleFor(m => m.PersonPhone, f => f.Person.Phone)
                .RuleFor(m => m.PersonTextBlog, f => f.Random.String())
                .RuleFor(m => m.PersonImageBlogUrl, f => f.Image.PicsumUrl())
                .RuleFor(m => m.FirstComment, f => f.Random.String())
                //.RuleFor(m => m.LikeIconUrl, f => f.Person.Avatar)
                //.RuleFor(m => m.CommentIconUrl, f => f.Person.Avatar)
                //.RuleFor(m => m.ShareIconUrl, f => f.Person.Avatar)
                ;
            var models = testModel.Generate(100);
            for (var index =0; index< models.Count;index++ )
            {
                var model = models[index];
                model.FirstComment = index.ToString();
                Models.Add(model);
            }
        }
    }

    class Model
    {
        public string PersonIconUrl { get; set; }
        public string PersonName { get; set; }
        public string PersonPhone { get; set; }
        public string PersonTextBlog { get; set; }
        public string PersonImageBlogUrl { get; set; }
        public string FirstComment { get; set; }
        public string LikeIconUrl { get; set; }
        public string CommentIconUrl { get; set; }
        public string ShareIconUrl { get; set; }
    }
}