using Microsoft.Maui;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace EHMR.Resources.Controls
{
    public static class SparkBadgeToneHelper
    {
        public static SparkBadgeTone StatusToTone(string status)
    {
        return status?.ToLowerInvariant() switch
        {
            "active" => SparkBadgeTone.Neutral,
            "inactive" => SparkBadgeTone.Danger,
            "discharged" => SparkBadgeTone.Danger,
            "missed" => SparkBadgeTone.Danger,
            "completed" => SparkBadgeTone.Success,
            "scheduled" => SparkBadgeTone.Neutral,
            _ => SparkBadgeTone.Neutral
        };
    } }
    /// <summary>How a cell's value should be rendered.</summary>
    public enum SparkGridCellType
    {
        Text,
        Badge,
        Currency,
        Number,
        Avatar, Hyperlink,
        /// <summary>Row-level action icons (e.g. view/edit). Value is ignored; icons are wired via
        /// SparkDataGridView.RowTappedCommand (view) and EditRowCommand (edit).</summary>
        Actions, Button, QuickPreview
    }

    /// <summary>Semantic color for a badge cell (Predicted risk %, trend, status...).</summary>
    public enum SparkBadgeTone
    {
        Neutral,
        Success,
        Danger, 
        Warning
    }
    public abstract class SparkBindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if(Equals(field, value)) return false;
            field=value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }


    /// <summary>
    /// A single row of data. Backed by a dictionary so grids can be built dynamically
    /// from any data source without generating per-model XAML. Access via row["Key"].
    /// For badge cells, store a SparkBadgeValue as the value.
    /// </summary>
    public class SparkGridRow : Dictionary<string, object>
    {
        /// <summary>Arbitrary payload (e.g. the underlying customer id/entity) for use in RowTappedCommand.</summary>
        public object Tag
        {
            get; set;
        }
    }

    /// <summary>Value + tone for a badge cell, e.g. { Text = "42%", Tone = Danger }.</summary>
    public class SparkBadgeValue
    {
        public string Text
        {
            get; set;
        }
        public SparkBadgeTone Tone { get; set; } = SparkBadgeTone.Neutral;

        public SparkBadgeValue()
        {
        }

        public SparkBadgeValue(string text, SparkBadgeTone tone)
        {
            Text=text;
            Tone=tone;
        }
    }

    /// <summary>
    /// One tab in the header row, e.g. "All Users  200,401" / "Churned  12,904".
    /// Title / Value / Command / IsSelected are all bindable so a dashboard
    /// definition can build any number of these (4, 5, 6, ...).
    /// </summary>
    public class SparkTabItem : SparkBindableBase
    {
        private string _title;
        private string _value;          // e.g. "200,401" — shown as a small badge next to the title
        private bool _isSelected;
        private ICommand _command;
        private object _commandParameter;

        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        public string Value
        {
            get => _value;
            set => Set(ref _value, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => Set(ref _isSelected, value);
        }

        /// <summary>Fired when the tab is clicked/selected.</summary>
        public ICommand Command
        {
            get => _command;
            set => Set(ref _command, value);
        }

        public object CommandParameter
        {
            get => _commandParameter;
            set => Set(ref _commandParameter, value);
        }
    }

    /// <summary>
    /// One dropdown/picker placed after the search box, e.g. "Prioritized Searches".
    /// </summary>
    public enum SparkFilterControlType
    {
        Picker,
        DatePicker,
        CheckBox
    }

    public class SparkPickerItem : SparkBindableBase
    {
        private string _placeholder;
        private ObservableCollection<string> _items = new();
        private int _selectedIndex = -1;
        private string _selectedItem;
        private bool _syncing;
        private SparkFilterControlType _controlType = SparkFilterControlType.Picker;
        private DateTime _selectedDate = DateTime.Today;
        private bool _isChecked;
        private bool _isVisible = true;
        private bool _isEnabled = true;
        private bool _isInlineWithSearch;

        public string Placeholder
        {
            get => _placeholder;
            set => Set(ref _placeholder, value);
        }

        public ObservableCollection<string> Items
        {
            get => _items;
            set => Set(ref _items, value);
        }

        public SparkFilterControlType ControlType
        {
            get => _controlType;
            set
            {
                if(!Set(ref _controlType, value)) return;
                OnPropertyChanged(nameof(IsPicker));
                OnPropertyChanged(nameof(IsDatePicker));
                OnPropertyChanged(nameof(IsCheckBox));
            }
        }

        public bool IsPicker => ControlType == SparkFilterControlType.Picker;
        public bool IsDatePicker => ControlType == SparkFilterControlType.DatePicker;
        public bool IsCheckBox => ControlType == SparkFilterControlType.CheckBox;

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set => Set(ref _selectedDate, value);
        }

        public bool IsChecked
        {
            get => _isChecked;
            set => Set(ref _isChecked, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if(!Set(ref _isVisible, value)) return;
                OnPropertyChanged(nameof(IsFilterRowVisible));
                OnPropertyChanged(nameof(IsInlineVisible));
            }
        }

        /// <summary>Renders this filter beside the search field instead of in the wrapped filter row.</summary>
        public bool IsInlineWithSearch
        {
            get => _isInlineWithSearch;
            set
            {
                if(!Set(ref _isInlineWithSearch, value)) return;
                OnPropertyChanged(nameof(IsFilterRowVisible));
                OnPropertyChanged(nameof(IsInlineVisible));
            }
        }

        public bool IsFilterRowVisible => IsVisible && !IsInlineWithSearch;
        public bool IsInlineVisible => IsVisible && IsInlineWithSearch;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => Set(ref _isEnabled, value);
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if(!Set(ref _selectedIndex, value)) return;
                if(_syncing) return;

                _syncing=true;
                SelectedItem=value>=0&&value<Items.Count ? Items[value] : null;
                _syncing=false;
            }
        }

        public string SelectedItem
        {
            get => _selectedItem;
            set
            {
                if(!Set(ref _selectedItem, value)) return;
                if(_syncing) return;

                _syncing=true;
                SelectedIndex=value!=null ? Items.IndexOf(value) : -1;
                _syncing=false;
            }
        }
    }

    /// <summary>
    /// One button on the right of the row (e.g. "Search", "New", the funnel icon).
    /// IconGlyph is optional path data (Segoe MDL2 / any Path.Data string) so an
    /// icon-only button can be built without extra XAML per instance.
    /// </summary>
    public class SparkButtonItem : SparkBindableBase
    {
        private string _label;
        private string _iconGlyph;
        private bool _isPrimary;
        private ICommand _command;
        private object _commandParameter;
        private bool _isEnabled = true;

        public string Label
        {
            get => _label;
            set => Set(ref _label, value);
        }

        /// <summary>Optional icon-only glyph (Path Data). Leave null for a text button.</summary>
        public string IconGlyph
        {
            get => _iconGlyph;
            set => Set(ref _iconGlyph, value);
        }

        /// <summary>True = solid teal button (Search), False = outline button (New).</summary>
        public bool IsPrimary
        {
            get => _isPrimary;
            set => Set(ref _isPrimary, value);
        }

        public ICommand Command
        {
            get => _command;
            set => Set(ref _command, value);
        }

        public object CommandParameter
        {
            get => _commandParameter;
            set => Set(ref _commandParameter, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => Set(ref _isEnabled, value);
        }
    }
    /// <summary>One stat card on the detail page, e.g. { Label = "Predicted LTV", Value = "$12,345" }.</summary>
    public class SparkMetricItem : SparkBindableBase
    {
        private string _label;
        private string _value;
        private SparkBadgeTone _tone = SparkBadgeTone.Neutral;

        public string Label
        {
            get => _label;
            set => Set(ref _label, value);
        }

        public string Value
        {
            get => _value;
            set => Set(ref _value, value);
        }

        /// <summary>Optional accent (e.g. Danger for a high-risk score) — colors just the value text.</summary>
        public SparkBadgeTone Tone
        {
            get => _tone;
            set => Set(ref _tone, value);
        }
    }

    /// <summary>One row in the activity/timeline list on the detail page.</summary>
    public class SparkActivityItem : SparkBindableBase
    {
        private string _title;
        private string _timestamp;
        private string _description;
        private string _iconGlyph;

        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        public string Timestamp
        {
            get => _timestamp;
            set => Set(ref _timestamp, value);
        }

        public string Description
        {
            get => _description;
            set => Set(ref _description, value);
        }

        /// <summary>Optional small icon glyph (Path data) shown to the left of the entry.</summary>
        public string IconGlyph
        {
            get => _iconGlyph;
            set => Set(ref _iconGlyph, value);
        }
    }
    public class SparkGridColumn : SparkBindableBase
    {
        /// <summary>Header text.</summary>
        public string Header
        {
            get; set;
        }

        /// <summary>Dictionary key used to read the value from SparkGridRow.</summary>
        public string Key
        {
            get; set;
        }

        /// <summary>How the cell is rendered.</summary>
        public SparkGridCellType CellType { get; set; } = SparkGridCellType.Text;

        /// <summary>
        /// Column width.
        /// Use GridLength.Star or new GridLength(x, GridUnitType.Star)
        /// for responsive layouts.
        /// Use Absolute only when a fixed width is really required.
        /// </summary>
        public GridLength Width { get; set; } = GridLength.Star;

        /// <summary>Automatically right-align numeric columns.</summary>
        public bool RightAlign =>
            CellType==SparkGridCellType.Currency||
            CellType==SparkGridCellType.Number;

        /// <summary>Whether clicking the header sorts this column.</summary>
        public bool Sortable
        {
            get; set;
        } = true;

        /// <summary>Executed when the header is clicked.</summary>
        public ICommand HeaderTapCommand
        {
            get; set;
        }
    }

    /// <summary>
    /// A single row of data. Backed by a dictionary so grids can be built dynamically
    /// from any data source without generating per-model XAML. Access via row["Key"].
    /// For badge cells, store a SparkBadgeValue as the value.
    /// </summary>


    /// <summary>Value + tone for a badge cell, e.g. { Text = "42%", Tone = Danger }.</summary>

}