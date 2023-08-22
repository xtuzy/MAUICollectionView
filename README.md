# MAUICollectionView
[![NuGet version(Yang.MAUICollectionView)](https://img.shields.io/nuget/v/Yang.MAUICollectionView?label=Yang.MAUICollectionView)](https://www.nuget.org/packages/Yang.MAUICollectionView)

This is an custom CollectionView. It base on ScrollView, like FlatList in ReactNative, not use native UICollectionView/RecyclerView, it have high performance by recycle view.

Demo:
- Android
  
https://user-images.githubusercontent.com/17793881/248198954-316b2734-fbe6-4dfd-a87d-9feaa1abd68c.mp4
- Windows

https://user-images.githubusercontent.com/17793881/242142431-e5647e76-e297-4fc6-964f-78616592ca62.mp4


## Features
- Cross Platform, iOS/Android/Windows/Maccatalyst
- Support custom layout
- Support operation(Insert, Remove, ~Move~, Update) animation
- Support load more
- Support scrollto item
- ~Support drag-sort~
- Support select

*Features of Demo*
- RefreshView
- Context Menu

## Principle of high performance
When scrolling, there are three situations where the item needs to be set, visible becomes invisible, always visible, invisible becomes visible. We recycle the invisible item, the visible item reuses the recycled item to measure, and the always visible item does not need to be re-measured, so that we only need to measure a few items when scrolling.

## Changelog
- 0.0.1
  Show simple list should be ok.
  
## Awesome Resources
- [Chameleon](https://github.com/BigZaphod/Chameleon)
- [CollectionView](https://github.com/TheNounProject/CollectionView)
- [how-view-recycling-works](https://learn.microsoft.com/en-us/xamarin/android/user-interface/layouts/recycler-view/parts-and-functionality#how-view-recycling-works)
