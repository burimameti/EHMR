using System.Collections;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
namespace EHMR.Resources.Controls;
public partial class FFPicker2 : ContentView
{
 public FFPicker2(){InitializeComponent();}
 public static readonly BindableProperty LabelProperty=BindableProperty.Create(nameof(Label),typeof(string),typeof(FFPicker2),string.Empty);
 public string Label{get=>(string)GetValue(LabelProperty);set=>SetValue(LabelProperty,value);}
 public static readonly BindableProperty PlaceholderProperty=BindableProperty.Create(nameof(Placeholder),typeof(string),typeof(FFPicker2),string.Empty);
 public string Placeholder{get=>(string)GetValue(PlaceholderProperty);set=>SetValue(PlaceholderProperty,value);}
 public static readonly BindableProperty ItemsSourceProperty=BindableProperty.Create(nameof(ItemsSource),typeof(IList),typeof(FFPicker2),null);
 public IList ItemsSource{get=>(IList)GetValue(ItemsSourceProperty);set=>SetValue(ItemsSourceProperty,value);}
 public static readonly BindableProperty ItemDisplayBindingProperty=BindableProperty.Create(nameof(ItemDisplayBinding),typeof(BindingBase),typeof(FFPicker2),null);
 public BindingBase? ItemDisplayBinding{get=>(BindingBase?)GetValue(ItemDisplayBindingProperty);set=>SetValue(ItemDisplayBindingProperty,value);}
 public static readonly BindableProperty SelectedItemProperty=BindableProperty.Create(nameof(SelectedItem),typeof(object),typeof(FFPicker2),null,BindingMode.TwoWay);
 public object? SelectedItem{get=>GetValue(SelectedItemProperty);set=>SetValue(SelectedItemProperty,value);}
 public static readonly BindableProperty SelectedIndexProperty=BindableProperty.Create(nameof(SelectedIndex),typeof(int),typeof(FFPicker2),-1,BindingMode.TwoWay);
 public int SelectedIndex{get=>(int)GetValue(SelectedIndexProperty);set=>SetValue(SelectedIndexProperty,value);}
 private async void OnPickerTapped(object? sender,TappedEventArgs e)
 {
  if(ItemsSource==null||ItemsSource.Count==0)return;
  var page=Application.Current?.Windows.FirstOrDefault()?.Page??Application.Current?.MainPage;
  if(page==null)return;
  var popup=new FFPickerPopup(string.IsNullOrWhiteSpace(Label) ? "Избери" : Label,ItemsSource,ItemDisplayBinding,SelectedItem);
  var result=await page.ShowPopupAsync(popup);
  if(result==null)return;
  SelectedItem=result;
  for(var i=0;i<ItemsSource.Count;i++)if(Equals(ItemsSource[i],result)){SelectedIndex=i;break;}
 }
}