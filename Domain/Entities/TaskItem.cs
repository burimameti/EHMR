namespace EHMR.Domain.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    public class TaskItem : BaseEntity
    {
        public Guid PatientId
        {
            get; set;
        }

        public Guid? TreatmentPlanId
        {
            get; set;
        }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public Guid? DoctorId
        {
            get; set;
        }

        public DateTime DueDate
        {
            get; set;
        }

        public TaskState Status
        {
            get; set;
        }

        public TaskPriority Priority
        {
            get; set;
        }

        public DateTime? CompletedAt
        {
            get; set;
        }
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum TaskState
    {
        Pending,
        InProgress,
        Done,
        Cancelled
    }
}