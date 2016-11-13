# AndroidLayoutToCSharp
Tiny prgram that converts android xml's into c# properties. It can save a few minutes of your life ^^
It runs on UWP.

All what it does is this:
###Input:
```
<?xml version="1.0" encoding="utf-8"?>
<LinearLayout xmlns:android="http://schemas.android.com/apk/res/android"
              xmlns:app="http://schemas.android.com/apk/res-auto"
              android:orientation="vertical"
              android:layout_width="match_parent"
              android:layout_height="match_parent"
              android:paddingTop="2dp" android:paddingBottom="2dp"
              android:background="?android:selectableItemBackground">
  <RelativeLayout android:layout_width="match_parent" android:layout_height="wrap_content" android:background="@drawable/border_accent_left_wide" android:paddingTop="2dp" android:paddingBottom="2dp" android:paddingStart="10dp">
    <LinearLayout android:layout_width="wrap_content" android:layout_height="wrap_content" android:orientation="vertical">
      <LinearLayout android:layout_width="wrap_content" android:layout_height="wrap_content" android:orientation="horizontal" android:layout_marginBottom="5dp">
        <FFImageLoading.Views.ImageViewAsync android:layout_width="35dp" android:layout_height="50dp" android:scaleType="fitXY"
                                             android:id="@+id/AnimeReviewItemLayoutAvatarImage"/>
        <LinearLayout android:layout_width="wrap_content" android:layout_gravity="center" android:orientation="vertical" android:layout_height="wrap_content" android:layout_marginStart="10dp">
          <TextView android:layout_width="wrap_content" android:fontFamily="@string/font_family_medium" android:layout_height="wrap_content" android:text="Author" android:textColor="@color/BrushText"
                    android:id="@+id/AnimeReviewItemLayoutAuthor"/>
          <TextView android:layout_width="wrap_content" android:fontFamily="@string/font_family_light" android:layout_height="wrap_content" android:text="Yesteday blaldfg" android:textColor="@color/BrushText"
                    android:id="@+id/AnimeReviewItemLayoutDate"/>
        </LinearLayout>
      </LinearLayout>
      <TextView android:layout_width="wrap_content" android:fontFamily="@string/font_family_light" android:layout_height="wrap_content" android:textColor="@color/BrushText" android:text="Overall: 9"
                android:id="@+id/AnimeReviewItemLayoutOverallScore"/>
      <TextView android:layout_width="wrap_content" android:fontFamily="@string/font_family_light" android:layout_height="wrap_content" android:textColor="@color/BrushText" android:text="9 of 20 eps seen"
                android:id="@+id/AnimeReviewItemLayoutEpsSeen"/>
      <TextView android:layout_width="wrap_content" android:fontFamily="@string/font_family_light" android:layout_height="wrap_content" android:textColor="@color/BrushText" android:text="12 found this helpful"
                android:id="@+id/AnimeReviewItemLayoutHelpfulCount"/>
    </LinearLayout>

    <LinearLayout android:id="@+id/AnimeReviewItemLayoutMarksList" android:layout_width="100dp" android:layout_marginTop="5dp" android:layout_height="wrap_content" android:layout_alignParentEnd="true" android:orientation="vertical" android:gravity="start"/>
  </RelativeLayout>

  <TextView android:layout_width="match_parent" android:layout_height="wrap_content" android:padding="10dp" android:text="Lorem" android:textColor="@color/BrushText"
            android:id="@+id/AnimeReviewItemLayoutReviewContent"/>
</LinearLayout>
```
###Output:
```
private ImageViewAsync _animeReviewItemLayoutAvatarImage;
private TextView _animeReviewItemLayoutAuthor;
private TextView _animeReviewItemLayoutDate;
private TextView _animeReviewItemLayoutOverallScore;
private TextView _animeReviewItemLayoutEpsSeen;
private TextView _animeReviewItemLayoutHelpfulCount;
private LinearLayout _animeReviewItemLayoutMarksList;
private TextView _animeReviewItemLayoutReviewContent;

public ImageViewAsync AnimeReviewItemLayoutAvatarImage => _animeReviewItemLayoutAvatarImage ?? (_animeReviewItemLayoutAvatarImage = FindViewById<ImageViewAsync>(Resource.Id.AnimeReviewItemLayoutAvatarImage));

public TextView AnimeReviewItemLayoutAuthor => _animeReviewItemLayoutAuthor ?? (_animeReviewItemLayoutAuthor = FindViewById<TextView>(Resource.Id.AnimeReviewItemLayoutAuthor));

public TextView AnimeReviewItemLayoutDate => _animeReviewItemLayoutDate ?? (_animeReviewItemLayoutDate = FindViewById<TextView>(Resource.Id.AnimeReviewItemLayoutDate));

public TextView AnimeReviewItemLayoutOverallScore => _animeReviewItemLayoutOverallScore ?? (_animeReviewItemLayoutOverallScore = FindViewById<TextView>(Resource.Id.AnimeReviewItemLayoutOverallScore));

public TextView AnimeReviewItemLayoutEpsSeen => _animeReviewItemLayoutEpsSeen ?? (_animeReviewItemLayoutEpsSeen = FindViewById<TextView>(Resource.Id.AnimeReviewItemLayoutEpsSeen));

public TextView AnimeReviewItemLayoutHelpfulCount => _animeReviewItemLayoutHelpfulCount ?? (_animeReviewItemLayoutHelpfulCount = FindViewById<TextView>(Resource.Id.AnimeReviewItemLayoutHelpfulCount));

public LinearLayout AnimeReviewItemLayoutMarksList => _animeReviewItemLayoutMarksList ?? (_animeReviewItemLayoutMarksList = FindViewById<LinearLayout>(Resource.Id.AnimeReviewItemLayoutMarksList));

public TextView AnimeReviewItemLayoutReviewContent => _animeReviewItemLayoutReviewContent ?? (_animeReviewItemLayoutReviewContent = FindViewById<TextView>(Resource.Id.AnimeReviewItemLayoutReviewContent));
```
