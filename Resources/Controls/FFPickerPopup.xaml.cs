using System.Collections;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
namespace EHMR.Resources.Controls;
public partial class FFPickerPopup : Popup
{
 public FFPickerPopup(string title,IList items,BindingBase? displayBinding,object? selectedItem)
 {
  InitializeComponent(); Title=title; ItemsView.ItemsSource=items;
  ItemsView.ItemTemplate=new DataTemplate(()=>{var l=new Label{FontSize=13,TextColor=Color.FromArgb("#0F172A"),VerticalOptions=LayoutOptions.Center}; if(displayBinding!=null)l.SetBinding(Label.TextProperty,displayBinding);else l.SetBinding(Label.TextProperty,"."); return new Border{Margin=2,Padding=new Thickness(12,10),StrokeThickness=0,StrokeShape=new RoundRectangle{CornerRadius=new CornerRadius(8)},Content=l};});
  ItemsView.SelectionChanged+=async(_,e)=>{var v=e.CurrentSelection.FirstOrDefault();if(v!=null)await CloseAsync(v,CancellationToken.None);};
  if(selectedItem!=null)ItemsView.SelectedItem=selectedItem;
 }
 public string Title{get;}
}