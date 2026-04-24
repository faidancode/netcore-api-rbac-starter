using Microsoft.EntityFrameworkCore;

namespace netcore_api_rbac_starter.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // optional (biar ada tabel dummy)
    public DbSet<Dummy> Dummies => Set<Dummy>();
}

public class Dummy
{
    public int Id { get; set; }
}