using EHMR.Resources.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.ViewModels.Dashboards
{


    public static class ChartEngine
    {
        public static List<ChartPoint> Normalize(
            IEnumerable<ChartPoint> source,
            double maxHeight = 90)
        {
            var data = source.ToList();

            if(!data.Any())
                return data;

            var max = data.Max(x => x.Value);

            // avoid division crash + flat data handling
            if(max<=0)
            {
                //foreach(var item in data)
                //    item.Height=0;

                return data;
            }

            //foreach(var item in data)
            //{
            //    item.Height=Math.Round((item.Value/max)*maxHeight, 2);
            //}

            return data;
        }

        // 🔥 smooth trend (optional upgrade for line charts)
        public static List<double> Smooth(IList<double> values)
        {
            if(values.Count<3) return values.ToList();

            var result = new List<double> { values[0] };

            for(int i = 1; i<values.Count-1; i++)
            {
                result.Add((values[i-1]+values[i]+values[i+1])/3);
            }

            result.Add(values[^1]);

            return result;
        }
    }
}
