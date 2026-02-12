using Microsoft.EntityFrameworkCore;
using Nexa.Api.Models;

namespace Nexa.Api.Data;

public class NexaDbContext : DbContext
{
    public NexaDbContext(DbContextOptions<NexaDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<UserInteraction> UserInteractions => Set<UserInteraction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Location).HasColumnType("geography (point)");
            entity.HasOne(e => e.Business)
                  .WithOne(b => b.User)
                  .HasForeignKey<Business>(b => b.UserId);
        });

        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Location).HasColumnType("geography (point)");
            entity.HasMany(e => e.Events).WithOne(ev => ev.Business).HasForeignKey(ev => ev.BusinessId);
            entity.HasMany(e => e.Listings).WithOne(l => l.Business).HasForeignKey(l => l.BusinessId);
            entity.HasMany(e => e.Offers).WithOne(o => o.Business).HasForeignKey(o => o.BusinessId);
        });

        modelBuilder.Entity<Listing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Location).HasColumnType("geography (point)");
            entity.Property(e => e.PriceMin).HasPrecision(18, 2);
            entity.Property(e => e.PriceMax).HasPrecision(18, 2);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasOne(e => e.Seller).WithMany(u => u.Listings).HasForeignKey(e => e.SellerId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Location).HasColumnType("geography (point)");
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.StartDate);
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Location).HasColumnType("geography (point)");
            entity.Property(e => e.OriginalPrice).HasPrecision(18, 2);
            entity.Property(e => e.DiscountedPrice).HasPrecision(18, 2);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.EndDate);
        });

        modelBuilder.Entity<UserInteraction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User).WithMany(u => u.Interactions).HasForeignKey(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.ContentType, e.ContentId });
        });
    }
}
