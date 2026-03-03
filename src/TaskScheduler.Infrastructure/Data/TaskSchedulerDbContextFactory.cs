using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskScheduler.Infrastructure.Data;

public class TaskSchedulerDbContextFactory : IDesignTimeDbContextFactory<TaskSchedulerDbContext>
{
    public TaskSchedulerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TaskSchedulerDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=taskscheduler;Username=admin;Password=admin123");
        
        return new TaskSchedulerDbContext(optionsBuilder.Options);
    }
}
