namespace EHMR.Domain.SparkForm;

public static class SparkTemplateRegistry
{
    private static readonly Dictionary<SparkFieldType, ISparkFieldTemplate> _templates = new();


    public static void Register(
        SparkFieldType type,
        ISparkFieldTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        _templates[type]=template;
    }



    public static ISparkFieldTemplate Resolve(
        SparkFieldType type)
    {
        if(_templates.TryGetValue(type, out var template))
            return template;


        if(_templates.TryGetValue(
            SparkFieldType.Text,
            out var fallback))
            return fallback;


        throw new InvalidOperationException(
            $"No Spark template registered for {type}");
    }



    public static bool Exists(
        SparkFieldType type)
    {
        return _templates.ContainsKey(type);
    }



    public static IReadOnlyDictionary<SparkFieldType, ISparkFieldTemplate> All()
    {
        return _templates;
    }



    public static void Clear()
    {
        _templates.Clear();
    }
}