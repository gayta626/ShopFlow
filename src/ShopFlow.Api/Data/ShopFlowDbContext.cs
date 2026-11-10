using Microsoft.EntityFrameworkCore;
using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Data;

public class ShopFlowDbContext : DbContext
{

    public ShopFlowDbContext(DbContextOptions<ShopFlowDbContext> options) : base(options)
    {


    }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Cart> Carts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(category => category.NormalizedName)
                .IsRequired()
                .HasMaxLength(100);
            entity.HasIndex(category => category.NormalizedName)
                .IsUnique();

        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products", table =>
            {
                table.HasCheckConstraint("CK_Products_Price", "\"Price\" > 0");
                table.HasCheckConstraint("CK_Products_Stock", "\"Stock\" >= 0");
                table.HasCheckConstraint("CK_Products_Connectivity", "\"Connectivity\" BETWEEN 1 AND 7");
                table.HasCheckConstraint("CK_Products_SwitchType",
                    "\"SwitchType\" IN ('Unknown', 'Linear', 'Tactile', 'Clicky')");
            });
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Brand).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Color).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Description).IsRequired().HasMaxLength(2000)
                .HasDefaultValue(string.Empty);

            entity.Property(p => p.SwitchType)
                .HasConversion<string>().IsRequired().HasMaxLength(20);
            entity.Property(p => p.Connectivity).HasConversion<int>().IsRequired();
            entity.Property(p => p.IsHotSwappable).IsRequired(false);
            entity.Property(p => p.Price).HasPrecision(18, 2).IsRequired();
            entity.Property(p => p.Stock).HasColumnType("bigint").IsRequired();
            entity.Property(p => p.IsActive).IsRequired();
            entity.Property(p => p.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(p => p.RowVersion).IsRowVersion();

            entity.HasIndex(p => p.Sku).IsUnique();
            entity.HasIndex(p => p.Name);
            entity.HasIndex(p => p.CategoryId);

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {

            entity.ToTable("Users", table =>
            {
                table.HasCheckConstraint("CK_Role", "\"Role\" IN ('Customer' ,'Admin') ");
            });

            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(254);
            entity.Property(u => u.NormalizedEmail)
                .IsRequired()
                .HasMaxLength(254);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(u => u.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            entity.HasIndex(u => u.NormalizedEmail).IsUnique();
        });


        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");

            entity.HasKey(c => c.Id);
            entity.Property(c => c.UserId).IsRequired();
            entity.Property(c => c.Revision)
                .HasColumnType("bigint")
                .HasDefaultValue(0L)
                .IsConcurrencyToken();

            entity.HasIndex(c => c.UserId).IsUnique();

            entity.HasOne(c => c.User)
                  .WithOne(u => u.Cart)
                  .HasForeignKey<Cart>(c => c.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

    }

}
