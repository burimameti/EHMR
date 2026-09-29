using System;
using System.Collections.Generic;

namespace EHMR.Domain.Entities
{
    public class TherapyCycle : BaseEntity
    {
        public string TherapyCyleNumber { get; set; } = string.Empty;
        public Guid? PatientId
        {
            get; set;
        }

        public Patient? Patient
        {
            get; set;
        }

        //public int? CycleNumber
        //{
        //    get; set;
        //}

        public TherapyStatus? Status
        {
            get; set;
        }

        public DateTime? StartDate
        {
            get; set;
        }

        public DateTime? EndDate
        {
            get; set;
        }

        public string? Notes { get; set; } = "";

        /// <summary>
        /// Clinical decision / resolution recorded when this therapy is started or changed.
        /// </summary>
        public string? DecisionText { get; set; } = "";

        public ICollection<PatientDocument> Documents { get; set; } = [];

        public ICollection<Appointment>? Appointments { get; set; } = [];
        public override string ToString() =>      $"{Notes} ({Status})";
    }
}