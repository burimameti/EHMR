namespace EHMR.Domain.SparkForm;

public sealed class SparkFormBuilder : ISparkFormBuilder
{
    private readonly SparkReflectionBuilder _reflection;


    public SparkFormBuilder(
        SparkReflectionBuilder reflection)
    {
        _reflection=reflection;
    }



    public SparkFormDefinition Build<T>(
        SparkFormMode mode = SparkFormMode.Create)
    {
        return Build(
            typeof(T),
            mode);
    }



    public SparkFormDefinition Build(
        object entity,
        SparkFormMode mode = SparkFormMode.Edit)
    {
        var form = Build(
            entity.GetType(),
            mode);


        form.Entity=entity;



        foreach(var field in form.Fields)
        {
            var property =
                entity.GetType()
                .GetProperty(
                    field.PropertyName);


            if(property==null)
                continue;


            field.Value=
                property.GetValue(entity);
        }



        return form;
    }




    public SparkFormDefinition Build(
        Type entityType,
        SparkFormMode mode = SparkFormMode.Create)
    {
        var form = new SparkFormDefinition
        {
            Title=entityType.Name,

            Style=SparkFormStyle.Spark,

            Mode=mode
        };



        var fields =
            _reflection
            .BuildFields(entityType)
            .OrderBy(x => x.Order)
            .ToList();



        foreach(var sectionGroup in fields.GroupBy(x => x.Section))
        {
            form.Sections.Add(
                new SparkFormSection
                {
                    Name=sectionGroup.Key,

                    Header=sectionGroup.Key,

                    Fields=sectionGroup
                        .OrderBy(x => x.Order)
                        .ToList()
                });
        }



        return form;
    }
}