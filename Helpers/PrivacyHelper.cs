using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Helpers
{
    public static class PrivacyMaskHelper
    {
        // 1234567890123 -> ******0123
        public static string MaskNationalId(string? nationalId)
        {
            if(string.IsNullOrWhiteSpace(nationalId))
                return "-";

            var visibleCount = Math.Min(4, nationalId.Length);
            var maskedCount = nationalId.Length-visibleCount;

            return new string('*', maskedCount)+nationalId[^visibleCount..];
        }

        public static string MaskPhone(string? phone)
        {
            if(string.IsNullOrWhiteSpace(phone))
                return "-";

            return phone.Length<=4
                ? phone
                : new string('*', phone.Length-4)+phone[^4..];
        }
    }

}
