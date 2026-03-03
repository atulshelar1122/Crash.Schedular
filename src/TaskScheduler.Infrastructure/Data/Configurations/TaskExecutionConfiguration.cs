using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskScheduler.Core.Models;

namespace TaskScheduler.Infrastructure.Data.Configurations;

public class TaskExecutionConfiguration : IEntityTypeConfiguration<TaskExecution>
{
    public void Configure(EntityTypeBuilder<TaskExecution> builder)
    {
        builder.ToTable("TaskExecutions");

        builder.HasKey(e => e.ExecutionId);

        builder.Property(e => e.TaskId)
            .IsRequired();

        builder.Property(e => e.WorkerId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.StartTime)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("NOW()");

        // Relationship: TaskExecution belongs to TaskItem
        builder.HasOne(e => e.Task)
            .WithMany(t => t.Executions)
            .HasForeignKey(e => e.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index
        builder.HasIndex(e => new { e.TaskId, e.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_executions_task");
    }
}
