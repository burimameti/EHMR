using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Infrastructure.Persistence.Configs
{
    public sealed class ReportHistoryConfiguration
     : IEntityTypeConfiguration<ReportHistory>
    {
        public void Configure(EntityTypeBuilder<ReportHistory> builder)
        {
            builder.ToTable("ReportHistory");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ReportKey)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.ReportTitle)
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(x => x.Format)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.FileName)
                .HasMaxLength(500);

            builder.Property(x => x.FilePath)
                .HasMaxLength(1000);

            builder.Property(x => x.GeneratedBy)
                .HasMaxLength(100);

            builder.Property(x => x.MachineName)
                .HasMaxLength(100);

            builder.HasIndex(x => x.GeneratedOn);

            builder.HasIndex(x => x.ReportKey);

            builder.HasIndex(x => x.GeneratedBy);
        }
    }
}
