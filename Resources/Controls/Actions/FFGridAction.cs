using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EHMR.Resources.Controls.Actions
{
    public class FFGridAction
    {
        public string Glyph { get; set; } = "...";
        public ICommand? Command
        {
            get; set;
        }

        public Color BackgroundColor { get; set; } = Colors.Transparent;
        public Color ForegroundColor { get; set; } = Colors.Black;

        public string? Tooltip
        {
            get; set;
        }
    }
}
