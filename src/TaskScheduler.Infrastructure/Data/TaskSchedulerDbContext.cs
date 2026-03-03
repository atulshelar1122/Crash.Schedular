using Microsoft.EntityFrameworkCore;
using TaskScheduler.Core.Models;

namespace TaskScheduler.Infrastructure.Data;

public class TaskSchedulerDbContext : DbContext
{
    public TaskSchedulerDbContext(DbContextOptions<TaskSchedulerDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<RecurringTask> RecurringTasks => Set<RecurringTask>();
    public DbSet<TaskExecution> TaskExecutions => Set<TaskExecution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskSchedulerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
