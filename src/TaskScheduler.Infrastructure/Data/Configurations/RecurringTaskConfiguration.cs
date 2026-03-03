using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskScheduler.Core.Models;

namespace TaskScheduler.Infrastructure.Data.Configurations;

public class RecurringTaskConfiguration : IEntityTypeConfiguration<RecurringTask>
{
    public void Configure(EntityTypeBuilder<RecurringTask> builder)
    {
        builder.ToTable("RecurringTasks");

        builder.HasKey(t => t.RecurringTaskId);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.TaskType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.CronExpression)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.IsEnabled)
            .HasDefaultValue(true);

        builder.Property(t => t.NextExecutionTime)
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasDefaultValue(5);

        builder.Property(t => t.MaxRetries)
            .HasDefaultValue(3);

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(t => t.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Index
        builder.HasIndex(t => t.NextExecutionTime)
            .HasFilter("\"IsEnabled\" = TRUE")
            .HasDatabaseName("idx_recurring_next_execution");
    }
}
