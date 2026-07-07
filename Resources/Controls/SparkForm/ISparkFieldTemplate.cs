namespace EHMR.Domain.SparkForm;

public interface ISparkFieldTemplate
    {
        View Build(SparkFieldContext context);
    }

