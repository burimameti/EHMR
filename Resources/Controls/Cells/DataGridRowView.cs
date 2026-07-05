//using EHMR.Resources.Controls.Columns;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace EHMR.Resources.Controls.Cells
//{
//    public class FFDataGridRowView : Grid
//    {
//        private IList<FFDataGridColumn> _columns;

//        public void SetColumns(IList<FFDataGridColumn> columns)
//        {
//            _columns=columns;
//        }

//        protected override void OnBindingContextChanged()
//        {
//            base.OnBindingContextChanged();

//            if(BindingContext==null||_columns==null)
//                return;

//            Children.Clear();
//            FFGridRenderer.BuildRows(this, _columns, BindingContext);
//        }
//    }
//}
