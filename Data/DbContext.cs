using Microsoft.EntityFrameworkCore;
using upload_architecture.Model;
namespace upload_architecture.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {}
    
    public DbSet<FileRecord> Files => Set<FileRecord>();
}