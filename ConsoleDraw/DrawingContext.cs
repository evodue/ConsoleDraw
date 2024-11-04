using Microsoft.EntityFrameworkCore;

public class DrawingContext : DbContext
{
    public DbSet<Drawing> Drawings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=rajzok;Database=DrawingsDB;Trusted_Connection=True;");
    }
}
