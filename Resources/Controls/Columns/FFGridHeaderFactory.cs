using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Resources.Controls.Columns
{

    internal static class FFGridRenderer
    {
        internal static View BuildHeader(FFDataGridColumn column)
        {
            return column.CreateHeader();
        }

        internal static View BuildCell(
            FFDataGridColumn column,
            object item)
        {
            return column.CreateCell(item);
        }
    }

    internal static class FFGridHeaderFactory
    {
        internal static View Create(FFDataGridColumn column)
        {
            return new Label
            {
                Text=column.Header,
                FontSize=11,
                FontAttributes=FontAttributes.Bold,
                CharacterSpacing=.8,
                VerticalOptions=LayoutOptions.Center,
                VerticalTextAlignment=TextAlignment.Center
            };
        }
    }
}
