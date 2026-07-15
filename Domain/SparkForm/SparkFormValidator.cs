namespace EHMR.Domain.SparkForm;

public static class SparkFormValidator
{
    public static bool Validate(SparkFormDefinition form)
    {
        bool isValid = true;

        foreach(var field in form.Fields)
        {
            field.ValidationMessage=null;

            if(field.IsRequired&&IsEmpty(field.Value))
            {
                field.ValidationMessage="Полето е задолжително.";
                isValid=false;
                continue;
            }

            foreach(var rule in field.ValidationRules.Where(r => r.Enabled))
            {
                var message = rule.ValidationType switch
                {
                    SparkValidationType.Regex when field.Value is string s
                        &&!System.Text.RegularExpressions.Regex.IsMatch(s, (string)rule.Parameter!)
                        => rule.Message,

                    SparkValidationType.Range when field.Value is IConvertible c
                        &&!InRange(Convert.ToDouble(c), rule.Parameter)
                        => rule.Message,

                    _ => null
                };

                if(message!=null)
                {
                    field.ValidationMessage=message;
                    isValid=false;
                    break;
                }
            }
        }

        return isValid;
    }

    private static bool IsEmpty(object? value) =>
        value==null||(value is string s&&string.IsNullOrWhiteSpace(s));

    private static bool InRange(double value, object? parameter)
    {
        if(parameter is not (double min, double max)) return true;
        return value>=min&&value<=max;
    }
}