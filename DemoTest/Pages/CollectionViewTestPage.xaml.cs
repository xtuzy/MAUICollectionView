using Bogus;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using Yang.Maui.Helper.Device.Screen;

namespace DemoTest.Pages;

public partial class CollectionViewTestPage : ContentPage
{
#if WINDOWS || __ANDROID__ || __IOS__
    FrameRateCalculator fr;
#endif
    public CollectionViewTestPage()
    {
        InitializeComponent();

#if WINDOWS || __ANDROID__ || __IOS__
        if (fr == null)
        {
            fr = new FrameRateCalculator();
            fr.FrameRateUpdated += (value) =>
            {
                this.Dispatcher.Dispatch(() => fpsLabel.Text = value.Frames.ToString());
            };
            fr.Start();
        }
#endif
        var viewModel = new ViewModel();
        collectionView.BindingContext = viewModel;
        collectionView.ItemTemplate = new DataTemplate(() =>
        {
            var view = new ModelViewSimple();
            view.BindingData();
            return view;
        });
        collectionView.ItemsSource = viewModel.models;
        collectionView.SelectionMode = SelectionMode.Single;

        Add.Clicked += (sender, e) =>
        {
            var index = 2;
            viewModel.models.Insert(index, viewModel.Generate(1)[0]);
        };

        Remove.Clicked += (sender, e) =>
        {
            var index = 2;
            viewModel.models.RemoveAt(index);
        };

        Move.Clicked += (sender, e) =>
        {
            var index = 3;
            var target = 1;
            var item = viewModel.models[index];
            viewModel.models.RemoveAt(index);
            viewModel.models.Insert(target, item);
        };

        Change.Clicked += (sender, e) =>
        {
            var index = 2;
            viewModel.models[index] = viewModel.Generate(1)[0];
        };
    }
}