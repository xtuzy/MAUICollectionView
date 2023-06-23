# MauiUICollectionView
- This is an experiment with a custom CollectionView, which does not use the native UICollectionView/RecyclerView, but is based on Maui's ScrollView. This idea from FlatList in ReactNative and iOS's TableView, they are base on ScrollView.

- We know UICollectionView/RecyclerView is a high performance way to show list, they get high performance by recycle view. In this library, i also do it.
- And most importantly, we can control all detail about CollectionView, you can custom a layout to get best performance to show your data.

Demo:
- Android
  
https://user-images.githubusercontent.com/17793881/248198954-316b2734-fbe6-4dfd-a87d-9feaa1abd68c.mp4
- Windows

https://user-images.githubusercontent.com/17793881/242142431-e5647e76-e297-4fc6-964f-78616592ca62.mp4


## Features
- Cross Platform, as long as the ScrollView behaves consistently
- Support for custom layouts

## How it works
If we have 1000 data, we can create 1000 views, and add to Content, then we can scroll and see all data, but it is slow, because create/measure/layout 1000 views need a lot of time. So we need try to not create 1000 views.

When ContentSize is larger than Bounds of ScrollView, we can scroll. So we can give ContentSize a large value, ScrollView always can scroll. When we scroll, some views will be visiable, some will be invisiable, we can recycle views of will be invisiable and provide them to will be visible Views. Then we adjust position of these views, it also can show all data.

## Awesome Libraries
- [Chameleon](https://github.com/BigZaphod/Chameleon)
- [CollectionView](https://github.com/TheNounProject/CollectionView)
