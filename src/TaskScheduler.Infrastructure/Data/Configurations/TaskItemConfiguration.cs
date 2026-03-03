using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskScheduler.Core.Models;

namespace TaskScheduler.Infrastructure.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.TaskId);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.TaskType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Payload)
            .IsRequired();

        builder.Property(t => t.ScheduledTime)
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasDefaultValue(5);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>()
            .HasDefaultValue(TaskItemStatus.Pending);

        builder.Property(t => t.MaxRetries)
            .HasDefaultValue(3);

        builder.Property(t => t.CurrentRetryCount)
            .HasDefaultValue(0);

        builder.Property(t => t.WorkerId)
            .HasMaxLength(100);

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(t => t.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(t => new { t.ScheduledTime, t.Status })
            .HasDatabaseName("idx_tasks_scheduling");

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("idx_tasks_status");

        builder.HasIndex(t => new { t.WorkerId, t.LastHeartbeat })
            .HasFilter("\"Status\" = 'Running'")
            .HasDatabaseName("idx_tasks_worker_heartbeat");
    }
}
