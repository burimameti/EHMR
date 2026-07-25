namespace EHMR.ViewModels.Dashboards.Models;

public static class ChartEngine
{
    /// <summary>Fills NormalizedValue (0–1) on each chart point relative to the max in the set.</summary>
    public static List<AnalyticsChart> Normalize(IEnumerable<AnalyticsChart> source)
    {
        var data = source.ToList();
        if(data.Count==0) return data;

        var max = data.Max(x => x.Value);
        if(max<=0)
        {
            foreach(var item in data) item.NormalizedValue=0;
            return data;
        }

        foreach(var item in data)
            item.NormalizedValue=Math.Round(item.Value/max, 4);

        return data;
    }

    public static List<double> Smooth(IList<double> values)
    {
        if(values.Count<3) return values.ToList();

        var result = new List<double> { values[0] };
        for(int i = 1; i<values.Count-1; i++)
            result.Add((values[i-1]+values[i]+values[i+1])/3);
        result.Add(values[^1]);
        return result;
    }
}