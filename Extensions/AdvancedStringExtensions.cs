using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Extensions
{
    public static class StringExtensions
    {
        public static string ToLowerScalarOrOrdinal(this string str) => str?.ToLower()??string.Empty;
    }
}