namespace EHMR.Domain.SparkForm;

public static class SparkTemplateRegistry
{
    private static readonly Dictionary<SparkFieldType, ISparkFieldTemplate> Templates = new();


    public static void Register(
        SparkFieldType type,
        ISparkFieldTemplate template)
    {
        if(template==null)
            throw new ArgumentNullException(nameof(template));


        Templates[type]=template;
    }



    public static ISparkFieldTemplate Resolve(
        SparkFieldType type)
    {
        if(Templates.TryGetValue(type, out var template))
            return template;


        if(Templates.TryGetValue(
            SparkFieldType.Text,
            out var fallback))
            return fallback;


        throw new InvalidOperationException(
            $"No Spark template registered for {type}");
    }



    public static bool Exists(
        SparkFieldType type)
    {
        return Templates.ContainsKey(type);
    }



    public static IReadOnlyDictionary<SparkFieldType, ISparkFieldTemplate> All()
    {
        return Templates;
    }



    public static void Clear()
    {
        Templates.Clear();
    }
}