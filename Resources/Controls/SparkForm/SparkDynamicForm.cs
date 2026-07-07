using EHMR.Domain.SparkForm;

namespace EHMR.Resources.Controls.SparkForm;


public class SparkDynamicForm : ContentView
{
    private readonly VerticalStackLayout _layout;


    public SparkDynamicForm()
    {
        _layout=new VerticalStackLayout
        {
            Spacing=20,
            Padding=new Thickness(20)
        };


        Content=new ScrollView
        {
            Content=_layout
        };
    }



    public static readonly BindableProperty FormProperty =
        BindableProperty.Create(
            nameof(Form),
            typeof(SparkFormDefinition),
            typeof(SparkDynamicForm),
            null,
            propertyChanged: OnFormChanged);



    public SparkFormDefinition? Form
    {
        get => (SparkFormDefinition?)GetValue(FormProperty);
        set => SetValue(FormProperty, value);
    }



    private static void OnFormChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        ((SparkDynamicForm)bindable)
            .Build();
    }



    private void Build()
    {
        _layout.Children.Clear();


        if(Form==null)
            return;


        BuildHeader();


        foreach(var section in Form.Sections
            .OrderBy(x => x.Order))
        {
            _layout.Children.Add(
                BuildSection(section));
        }


        BuildFooterButtons();
    }




    private View BuildSection(
        SparkFormSection section)
    {
        var stack = new VerticalStackLayout
        {
            Spacing=12
        };


        if(!string.IsNullOrWhiteSpace(section.Header))
        {
            stack.Children.Add(
                new Label
                {
                    Text=section.Header,
                    FontSize=20,
                    FontAttributes=
                        FontAttributes.Bold
                });
        }


        foreach(var field in section.Fields
            .OrderBy(x => x.Order))
        {
            if(!field.IsVisible)
                continue;


            stack.Children.Add(
                BuildField(field));
        }


        return stack;
    }




    private View BuildField(
        SparkFormField field)
    {
        var context =
            new SparkFieldContext
            {
                Field=field,

                Mode=ConvertMode(),

                BindingContext=
                    Form?.Entity
            };


        var template =
            SparkTemplateRegistry
                .Resolve(field.FieldType);



        var control =
            template.Build(context);



        control.IsEnabled=
            field.IsEnabled;



        return CreateFieldContainer(
            field,
            control);
    }




    private View CreateFieldContainer(
        SparkFormField field,
        View control)
    {
        var stack =
            new VerticalStackLayout
            {
                Spacing=5
            };


        stack.Children.Add(
            new Label
            {
                Text=field.Label,
                FontAttributes=
                    FontAttributes.Bold
            });



        stack.Children.Add(control);



        if(field.IsRequired)
        {
            stack.Children.Add(
                new Label
                {
                    Text="* Required",
                    FontSize=11
                });
        }



        if(!string.IsNullOrWhiteSpace(field.HelpText))
        {
            stack.Children.Add(
                new Label
                {
                    Text=field.HelpText,
                    FontSize=12
                });
        }



        return stack;
    }




    private void BuildHeader()
    {
        if(Form==null)
            return;


        if(!string.IsNullOrWhiteSpace(Form.Title))
        {
            _layout.Children.Add(
                new Label
                {
                    Text=Form.Title,
                    FontSize=28,
                    FontAttributes=
                        FontAttributes.Bold
                });
        }
    }




    private void BuildFooterButtons()
    {
        if(Form==null||
           Form.FooterButtons.Count==0)
            return;


        var layout =
            new HorizontalStackLayout
            {
                Spacing=12
            };


        foreach(var button in Form.FooterButtons)
        {
            if(!button.IsVisible)
                continue;


            layout.Children.Add(
                new Button
                {
                    Text=button.Text,
                    Command=button.Command
                });
        }


        _layout.Children.Add(layout);
    }




    private SparkFieldMode ConvertMode()
    {
        return Form?.Mode switch
        {
            SparkFormMode.Create =>
                SparkFieldMode.Create,

            SparkFormMode.Edit =>
                SparkFieldMode.Edit,

            SparkFormMode.Detail =>
                SparkFieldMode.View,

            SparkFormMode.ReadOnly =>
                SparkFieldMode.View,

            _ =>
                SparkFieldMode.Create
        };
    }
}