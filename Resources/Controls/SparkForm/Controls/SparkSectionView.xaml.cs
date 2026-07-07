using EHMR.Domain.SparkForm;

namespace EHMR.Resources.Controls.SparkForms.Controls;

public partial class SparkSectionView : ContentView
{
    public SparkSectionView()
    {
        InitializeComponent();
    }



    // ======================================================
    // SECTION
    // ======================================================

    public static readonly BindableProperty SectionProperty =
        BindableProperty.Create(
            nameof(Section),
            typeof(SparkFormSection),
            typeof(SparkSectionView),
            null,
            propertyChanged: OnSectionChanged);



    public SparkFormSection? Section
    {
        get =>
            (SparkFormSection?)GetValue(SectionProperty);

        set =>
            SetValue(SectionProperty, value);
    }



    // ======================================================
    // FIELD MODE
    // ======================================================

    public static readonly BindableProperty FieldModeProperty =
        BindableProperty.Create(
            nameof(FieldMode),
            typeof(SparkFieldMode),
            typeof(SparkSectionView),
            SparkFieldMode.Create,
            propertyChanged: OnFieldModeChanged);



    public SparkFieldMode FieldMode
    {
        get =>
            (SparkFieldMode)GetValue(FieldModeProperty);

        set =>
            SetValue(FieldModeProperty, value);
    }



    // ======================================================
    // EVENTS
    // ======================================================

    private static void OnSectionChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        ((SparkSectionView)bindable)
            .Build();
    }



    private static void OnFieldModeChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        ((SparkSectionView)bindable)
            .Build();
    }



    // ======================================================
    // BUILD
    // ======================================================

    private void Build()
    {
        PART_FieldGrid.Children.Clear();

        PART_FieldGrid.RowDefinitions.Clear();

        PART_FieldGrid.ColumnDefinitions.Clear();



        if(Section==null)
            return;



        var columns =
            ResolveColumns();



        CreateColumns(columns);



        int row = 0;

        int column = 0;



        foreach(var field in Section.Fields
            .Where(x => x.IsVisible)
            .OrderBy(x => x.Order))
        {

            var span =
                Math.Min(
                    Math.Max(field.ColumnSpan, 1),
                    columns);



            if(column+span>columns)
            {
                column=0;

                row++;
            }



            EnsureRow(row);



            var fieldView =
                new SparkFieldView
                {
                    Field=field,

                    Mode=FieldMode,

                    IsEnabled=field.IsEnabled,

                    BindingContext=BindingContext
                };



            PART_FieldGrid.Children.Add(
                fieldView);



            Grid.SetRow(
                fieldView,
                row);



            Grid.SetColumn(
                fieldView,
                column);



            if(span>1)
            {
                Grid.SetColumnSpan(
                    fieldView,
                    span);
            }



            column+=span;
        }
    }



    // ======================================================
    // GRID
    // ======================================================

    private void CreateColumns(
        int count)
    {
        for(int i = 0; i<count; i++)
        {
            PART_FieldGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width=GridLength.Star
                });
        }
    }



    private void EnsureRow(
        int row)
    {
        while(PART_FieldGrid.RowDefinitions.Count<=row)
        {
            PART_FieldGrid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height=GridLength.Auto
                });
        }
    }



    // ======================================================
    // RESPONSIVE
    // ======================================================

    private int ResolveColumns()
    {
        if(Section?.Layout==SparkSectionLayout.Stack)
            return 1;



        var width =
            DeviceDisplay.MainDisplayInfo.Width/
            DeviceDisplay.MainDisplayInfo.Density;



        return width switch
        {
            <700 => 1,

            <1100 => 2,

            _ => 3
        };
    }
}