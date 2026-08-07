using EHMR.ViewModels;
using Microsoft.Maui.Controls;
using EHMR.ViewModels.Appointments;
using System;



#if WINDOWS

using WinUITextBox = Microsoft.UI.Xaml.Controls.TextBox;
using Microsoft.UI.Xaml.Input;

#endif

namespace EHMR.Behaviors;

/// <summary>
/// Bonus UX, not required for the dropdown to work: lets the search Entry
/// respond to Up / Down / Escape the way a native combo box would, using the
/// MoveNextCommand / MovePreviousCommand already exposed by
/// AppointmentListViewModel. Enter is handled separately via Entry.ReturnCommand
/// in XAML, and clicking a row already works regardless of this behavior.
/// Only wired up on Windows (the realistic desktop target here); attaching it
/// on any other platform is a harmless no-op.
/// </summary>
public partial class SearchSuggestionKeyboardBehavior : Behavior<Entry>
{
#if WINDOWS
    private Entry? _entry;
#endif

    protected override void OnAttachedTo(Entry bindable)
    {
        base.OnAttachedTo(bindable);
        bindable.HandlerChanged+=OnHandlerChanged;
    }

    protected override void OnDetachingFrom(Entry bindable)
    {
        bindable.HandlerChanged-=OnHandlerChanged;
#if WINDOWS
        if(bindable.Handler?.PlatformView is WinUITextBox textBox)
            textBox.PreviewKeyDown-=OnPreviewKeyDown;
        _entry=null;
#endif
        base.OnDetachingFrom(bindable);
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        if(sender is not Entry entry) return;
        _entry=entry;

        if(entry.Handler?.PlatformView is WinUITextBox textBox)
        {
            textBox.PreviewKeyDown-=OnPreviewKeyDown;
            textBox.PreviewKeyDown+=OnPreviewKeyDown;
        }
#endif
    }

#if WINDOWS

    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if(_entry?.BindingContext is not AppointmentListViewModel vm) return;

        switch(e.Key)
        {
            case Windows.System.VirtualKey.Down:
                vm.MoveNextCommand.Execute(null);
                e.Handled=true;
                break;

            case Windows.System.VirtualKey.Up:
                vm.MovePreviousCommand.Execute(null);
                e.Handled=true;
                break;

            case Windows.System.VirtualKey.Escape:
                vm.ShowSuggestions=false;
                vm.SelectedSuggestion=null;
                vm.SelectedIndex=-1;
                e.Handled=true;
                break;
        }
    }

#endif
}