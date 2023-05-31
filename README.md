# MauiUICollectionView
This is an experiment with a custom CollectionView, which does not use the native UICollectionView/RecyclerView, but is based on Maui's ScrollView, and the FlatList in ReactNative is also based on ScrollView, so it is feasible.

We know UICollectionView/RecyclerView is a high performance way to show list, they get high performance by recycle view. In this library, i also do it.

## Features
- Cross Platform, as long as the ScrollView behaves consistently
- Support for custom layouts

## How it works
ScrollView's bounds is fixed, is little, but bounds of Content can be large, when Content is larger than ScrollView, we can scroll. So if we have 1000 data, we can create 1000 view, and add to Content, then we can scroll and see all data, but it is slow, because create/measure/layout 1000 view need a lot of time. 
When we scroll, some views will be visiable, some will be invisiable, why not recycle views of will be invisiable and provide them to will be visible Views. Then we adjust position, it also can show all data.
