namespace EHMR.Resources.Controls.Charts;

    public class DonutSegment
    {
        public string Label { get; set; } = string.Empty;

        public double Value
        {
            get; set;
        }

        public Color Color { get; set; } = Colors.Gray;
    }
