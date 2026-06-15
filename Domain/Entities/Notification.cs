using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Entities
{
    public class Notification
    {
        public Guid Id
        {
            get; set;
        }

        public Guid UserId
        {
            get; set;
        }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public bool IsRead
        {
            get; set;
        }

        public NotificationSeverity Severity
        {
            get; set;
        }

        public DateTime CreatedAt
        {
            get; set;
        }
    }
}