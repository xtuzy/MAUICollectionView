using MauiUICollectionView;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUICollectionViewUnitTest.Tests
{
    internal class ViewModel
    {
        public List<List<Model>> models = new();

        public ViewModel()
        {
            for(var row =0; row < 500; row++)
            {
                var modelList = new List<Model>();
                for(var colum =0; colum < 20;colum++)
                {
                    modelList.Add(new Model());
                }
                models.Add(modelList);
            }
        }
    }

    class Source : MAUICollectionViewSource
    {
        ViewModel ViewModel;
        public Source(ViewModel viewModel)
        {
            ViewModel = viewModel;
            NumberOfItems += NumberOfItemsMethod;
            NumberOfSections += NumberOfSectionsMethod;
            IsSectionItem += IsSectionItemMethod;
        }

        bool IsSectionItemMethod(MAUICollectionView view, NSIndexPath indexPath)
        {
            if (indexPath.Row == 0)
                return true;
            else
                return false;
        }
      
        public int NumberOfSectionsMethod(MAUICollectionView tableView)
        {
            return ViewModel.models.Count;
        }

        public int NumberOfItemsMethod(MAUICollectionView tableView, int section)
        {
            return ViewModel.models[section].Count + 1; //+1 is add section header
        }
    }

    class Model
    {

    }
}
