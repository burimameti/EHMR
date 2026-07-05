using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Extensions
{
    public class MenuItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate HeaderTemplate { get; set; } = null!;
        public DataTemplate GroupTemplate { get; set; } = null!;
        public DataTemplate ItemTemplate { get; set; } = null!;

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var isHeaderProp = item.GetType().GetProperty("IsHeader");
            if(isHeaderProp!=null&&isHeaderProp.GetValue(item) is true)
                return HeaderTemplate;

            var itemsProp = item.GetType().GetProperty("Items");
            if(itemsProp?.GetValue(item) is IEnumerable children&&children.Cast<object>().Any())
                return GroupTemplate;

            return ItemTemplate;
        }
    }

}
