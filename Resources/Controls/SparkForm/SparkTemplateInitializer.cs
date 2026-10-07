using Microsoft.Maui.Controls.Shapes;

namespace EHMR.Domain.SparkForm;

#region INPUT


public sealed class TextTemplate : SparkTemplateBase
{
    public override View Build(SparkFieldContext context)
        => CreateEntry(context, Keyboard.Default);
}



public sealed class MultilineTemplate : SparkTemplateBase
{
    public override View Build(SparkFieldContext context)
        => CreateEditor(context);
}



public sealed class EmailTemplate : SparkTemplateBase
{
    public override View Build(SparkFieldContext context)
        => CreateEntry(context, Keyboard.Email);
}



public sealed class UrlTemplate : SparkTemplateBase
{
    public override View Build(SparkFieldContext context)
        => CreateEntry(context, Keyboard.Url);
}



public sealed class PhoneTemplate : SparkTemplateBase
{
    public override View Build(SparkFieldContext context)
        => CreateEntry(context, Keyboard.Telephone);
}



public sealed class PasswordTemplate : SparkTemplateBase
{
    public override View Build(SparkFieldContext context)
    {
        var entry = CreateEntry(
            context,
            Keyboard.Default);

        entry.IsPassword = true;

        return entry;
    }
}

#endregion



#region NUMERIC


public sealed class NumericTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
        => CreateEntry(
            context,
            Keyboard.Numeric);
}



public sealed class CurrencyTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
        => CreateEntry(
            context,
            Keyboard.Numeric);
}



public sealed class PercentageTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
        => CreateEntry(
            context,
            Keyboard.Numeric);
}



#endregion



#region DATE


public sealed class DateTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return BindValue(
            new DatePicker
            {
                Format = "dd/MM/yyyy"
            },
            DatePicker.DateProperty);
    }
}



public sealed class DateTimeTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return BindValue(
            new DatePicker
            {
                Format = "dd/MM/yyyy HH:mm"
            },
            DatePicker.DateProperty);
    }
}



public sealed class TimeTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return BindValue(
            new TimePicker(),
            TimePicker.TimeProperty);
    }
}


#endregion



#region SELECTION


public sealed class ComboTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        var picker = new Picker
        {
            Title = context.Label
        };


        picker.SetBinding(
            Picker.ItemsSourceProperty,
            nameof(SparkFormField.Options));


        picker.SetBinding(
            Picker.SelectedItemProperty,
            nameof(SparkFormField.Value),
            BindingMode.TwoWay);


        return picker;
    }
}



public sealed class LookupTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        var search = new SearchBar
        {
            Placeholder = context.Label
        };


        search.SetBinding(
            SearchBar.TextProperty,
            nameof(SparkFormField.Value),
            BindingMode.TwoWay);


        return search;
    }
}



public sealed class SearchTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        var search = new SearchBar
        {
            Placeholder =
                context.Placeholder ?? context.Label
        };


        search.SetBinding(
            SearchBar.TextProperty,
            nameof(SparkFormField.Value),
            BindingMode.TwoWay);


        return search;
    }
}



public sealed class AutoCompleteTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return new SearchBar
        {
            Placeholder = context.Label
        };
    }
}



public sealed class MultiSelectTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        var list = new CollectionView();


        list.SetBinding(
            CollectionView.ItemsSourceProperty,
            nameof(SparkFormField.Options));


        return list;
    }
}


#endregion



#region BOOLEAN


public sealed class ToggleTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return BindValue(
            new Switch(),
            Switch.IsToggledProperty);
    }
}



public sealed class CheckBoxTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return BindValue(
            new CheckBox
            {
                HorizontalOptions =
                    LayoutOptions.Start
            },
            CheckBox.IsCheckedProperty);
    }
}



public sealed class RadioTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return BindValue(
            new RadioButton
            {
                Content = context.Label
            },
            RadioButton.IsCheckedProperty);
    }
}


#endregion



#region MEDIA


public sealed class AttachmentTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return new Button
        {
            Text = "Attach File"
        };
    }
}



public sealed class ImageTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        var image = new Image();


        image.SetBinding(
            Image.SourceProperty,
            nameof(SparkFormField.Value));


        return image;
    }
}



public sealed class SignatureTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return new GraphicsView();
    }
}



public sealed class BarcodeTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return new Border
        {
            StrokeShape = new Rectangle(),

            Content = new Label
            {
                Text = "Barcode"
            }
        };
    }
}



public sealed class QrCodeTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return new Border
        {
            StrokeShape = new Rectangle(),

            Content = new Label
            {
                Text = "QR Code"
            }
        };
    }
}


#endregion



#region DISPLAY


public sealed class LabelTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return new Label
        {
            Text = context.Label
        };
    }
}



public sealed class BadgeTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return new Border
        {
            StrokeShape = new Rectangle(),

            Padding = 8,

            Content = new Label
            {
                Text = context.Label
            }
        };
    }
}



public sealed class ReadOnlyTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return new Label
        {
            Text = context.Value?.ToString()
        };
    }
}



public sealed class ProgressTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return BindValue(
            new ProgressBar(),
            ProgressBar.ProgressProperty);
    }
}



public sealed class RatingTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return BindValue(
            new Slider
            {
                Minimum = 0,
                Maximum = 5
            },
            Slider.ValueProperty);
    }
}



public sealed class SliderTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return BindValue(
            new Slider
            {
                Minimum = 0,
                Maximum = 100
            },
            Slider.ValueProperty);
    }
}


#endregion



#region ADVANCED


public sealed class RichTextTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
        => CreateEditor(context);
}



public sealed class HtmlTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return new WebView();
    }
}



public sealed class ContentTemplate : SparkTemplateBase
{
    public override View Build(
        SparkFieldContext context)
    {
        return new ContentView();
    }
}


#endregion
public static class SparkTemplateInitializer
{
    public static void Register()
    {
        SparkTemplateRegistry.Clear();

        RegisterInput();
        RegisterNumeric();
        RegisterDate();
        RegisterSelection();
        RegisterBoolean();
        RegisterMedia();
        RegisterDisplay();
        RegisterAdvanced();
    }



    private static void RegisterInput()
    {
        SparkTemplateRegistry.Register(
            SparkFieldType.Text,
            new TextTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Multiline,
            new MultilineTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Password,
            new PasswordTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Email,
            new EmailTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Phone,
            new PhoneTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Url,
            new UrlTemplate());
    }



    private static void RegisterNumeric()
    {
        SparkTemplateRegistry.Register(
            SparkFieldType.Integer,
            new NumericTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Long,
            new NumericTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Decimal,
            new NumericTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Double,
            new NumericTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Currency,
            new CurrencyTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Percentage,
            new PercentageTemplate());
    }



    private static void RegisterDate()
    {
        SparkTemplateRegistry.Register(
            SparkFieldType.Date,
            new DateTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.DateTime,
            new DateTimeTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Time,
            new TimeTemplate());
    }



    private static void RegisterSelection()
    {
        SparkTemplateRegistry.Register(
            SparkFieldType.Combo,
            new ComboTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Lookup,
            new LookupTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Search,
            new SearchTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.AutoComplete,
            new AutoCompleteTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Token,
            new MultiSelectTemplate());
    }



    private static void RegisterBoolean()
    {
        SparkTemplateRegistry.Register(
            SparkFieldType.Boolean,
            new ToggleTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Toggle,
            new ToggleTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.CheckBox,
            new CheckBoxTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Radio,
            new RadioTemplate());
    }



    private static void RegisterMedia()
    {
        SparkTemplateRegistry.Register(
            SparkFieldType.Image,
            new ImageTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Avatar,
            new ImageTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Attachment,
            new AttachmentTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Signature,
            new SignatureTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Barcode,
            new BarcodeTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.QrCode,
            new QrCodeTemplate());
    }



    private static void RegisterDisplay()
    {
        SparkTemplateRegistry.Register(
            SparkFieldType.Label,
            new LabelTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Badge,
            new BadgeTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.ReadOnly,
            new ReadOnlyTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Progress,
            new ProgressTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Rating,
            new RatingTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Slider,
            new NumericTemplate());
    }



    private static void RegisterAdvanced()
    {
        SparkTemplateRegistry.Register(
            SparkFieldType.RichText,
            new RichTextTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Html,
            new HtmlTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Markdown,
            new RichTextTemplate());


        SparkTemplateRegistry.Register(
            SparkFieldType.Custom,
            new ContentTemplate());
    }
}

