using EHMR.Domain.SparkForm;

namespace EHMR.Resources.Controls.SparkForms.Controls;

public partial class SparkFormView : ContentView
{
    private readonly ISparkFormBuilder _builder;


    public SparkFormView(
        ISparkFormBuilder builder)
    {
        InitializeComponent();

        _builder=builder;
    }



    // ======================================================
    // ENTITY
    // ======================================================

    public static readonly BindableProperty EntityProperty =
        BindableProperty.Create(
            nameof(Entity),
            typeof(object),
            typeof(SparkFormView),
            null,
            propertyChanged: OnEntityChanged);



    public object? Entity
    {
        get =>
            GetValue(EntityProperty);

        set =>
            SetValue(EntityProperty, value);
    }



    // ======================================================
    // DEFINITION
    // ======================================================

    public static readonly BindableProperty DefinitionProperty =
        BindableProperty.Create(
            nameof(Definition),
            typeof(SparkFormDefinition),
            typeof(SparkFormView),
            null,
            propertyChanged: OnDefinitionChanged);



    public SparkFormDefinition? Definition
    {
        get =>
            (SparkFormDefinition?)GetValue(DefinitionProperty);

        set =>
            SetValue(DefinitionProperty, value);
    }



    // ======================================================
    // FORM MODE
    // ======================================================

    public static readonly BindableProperty ModeProperty =
        BindableProperty.Create(
            nameof(Mode),
            typeof(SparkFormMode),
            typeof(SparkFormView),
            SparkFormMode.Create,
            propertyChanged: OnModeChanged);



    public SparkFormMode Mode
    {
        get =>
            (SparkFormMode)GetValue(ModeProperty);

        set =>
            SetValue(ModeProperty, value);
    }



    // ======================================================
    // EVENTS
    // ======================================================

    private static void OnEntityChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        var control =
            (SparkFormView)bindable;


        control.GenerateDefinition();
    }



    private static void OnModeChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        var control =
            (SparkFormView)bindable;


        control.GenerateDefinition();
    }



    private static void OnDefinitionChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        var control =
            (SparkFormView)bindable;


        control.Build();
    }




    // ======================================================
    // GENERATE
    // ======================================================

    private void GenerateDefinition()
    {
        if(Entity==null)
            return;


        Definition=
            _builder.Build(
                Entity,
                Mode);
    }



    // ======================================================
    // BUILD
    // ======================================================

    private void Build()
    {
        PART_FormHost.Children.Clear();


        if(Definition==null)
            return;



        var fieldMode =
            ConvertFieldMode(
                Mode);



        foreach(var section in Definition.Sections
            .OrderBy(x => x.Order))
        {
            var sectionView =
                new SparkSectionView
                {
                    Section=section,

                    FieldMode=fieldMode,

                    BindingContext=BindingContext
                };


            PART_FormHost.Children.Add(
                sectionView);
        }
    }



    // ======================================================
    // MODE CONVERSION
    // ======================================================

    private static SparkFieldMode ConvertFieldMode(
        SparkFormMode mode)
    {
        return mode switch
        {
            SparkFormMode.Create
                => SparkFieldMode.Create,


            SparkFormMode.Edit
                => SparkFieldMode.Edit,


            SparkFormMode.Detail
                => SparkFieldMode.View,


            SparkFormMode.ReadOnly
                => SparkFieldMode.View,


            SparkFormMode.Search
                => SparkFieldMode.Search,


            SparkFormMode.Filter
                => SparkFieldMode.Filter,


            _ => SparkFieldMode.Edit
        };
    }



    // ======================================================
    // PUBLIC API
    // ======================================================

    public void Refresh()
    {
        GenerateDefinition();
    }
}