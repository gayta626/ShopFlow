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

    public DbSet<CartItem> CartItems { get; set; }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    public DbSet<ProductImage> ProductImages { get; set; }
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

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImages", table =>
            {
                table.HasCheckConstraint("CK_ProductImages_SortOrder", "\"SortOrder\" >= 0");
            });

            entity.HasKey(image => image.Id);
            entity.Property(image => image.FileName).IsRequired().HasMaxLength(100);
            entity.Property(image => image.AltText).IsRequired().HasMaxLength(200);
            entity.Property(image => image.SortOrder).IsRequired();

            entity.HasIndex(image => image.FileName).IsUnique();
            entity.HasIndex(image => new { image.ProductId, image.SortOrder });

            entity.HasOne(image => image.Product)
                  .WithMany(product => product.Images)
                  .HasForeignKey(image => image.ProductId)
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


        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems", table =>
            {
                // Mỗi dòng giỏ chỉ nhận từ 1 đến 99 sản phẩm.
                table.HasCheckConstraint("CK_CartItems_Quantity", "\"Quantity\" BETWEEN 1 AND 99");
            });

            entity.HasKey(item => item.Id);
            entity.Property(item => item.Quantity).IsRequired();

            // Một sản phẩm chỉ có một dòng trong cùng giỏ.
            entity.HasIndex(item => new { item.CartId, item.ProductID }).IsUnique();

            // Xóa giỏ thì xóa các dòng thuộc giỏ đó.
            entity.HasOne(item => item.Cart)
                  .WithMany(cart => cart.Items)
                  .HasForeignKey(item => item.CartId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Không cho xóa vật lý sản phẩm đang được giỏ tham chiếu.
            entity.HasOne(item => item.Product)
                  .WithMany()
                  .HasForeignKey(item => item.ProductID)
                  .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", table =>
            {
                table.HasCheckConstraint("CK_Orders_TotalAmount", "\"TotalAmount\" > 0");
                table.HasCheckConstraint("CK_Orders_Status",
                    "\"Status\" IN ('Pending', 'Paid', 'Shipped', 'Completed', 'Cancelled')");
            });

            entity.HasKey(order => order.Id);
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(order => order.TotalAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(order => order.ShippingAddress).HasMaxLength(500).IsRequired();
            entity.Property(order => order.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(order => order.PaidAt).HasColumnType("timestamp with time zone").IsRequired(false);
            entity.Property(order => order.ShippedAt).HasColumnType("timestamp with time zone").IsRequired(false);
            entity.Property(order => order.CompletedAt).HasColumnType("timestamp with time zone").IsRequired(false);
            entity.Property(order => order.CancelledAt).HasColumnType("timestamp with time zone").IsRequired(false);
            entity.Property(order => order.RowVersion).IsRowVersion();

            entity.HasIndex(order => new { order.UserId, order.CreatedAt });
            entity.HasIndex(order => order.Status);
            entity.HasOne(order => order.User)
                  .WithMany()
                  .HasForeignKey(order => order.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems", table =>
            {
                table.HasCheckConstraint("CK_OrderItems_UnitPrice", "\"UnitPrice\" > 0");
                table.HasCheckConstraint("CK_OrderItems_Quantity", "\"Quantity\" BETWEEN 1 AND 99");
            });

            entity.HasKey(item => item.Id);
            entity.Property(item => item.Sku).HasMaxLength(50).IsRequired();
            entity.Property(item => item.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(item => item.Quantity).IsRequired();
            entity.HasIndex(item => new { item.OrderId, item.ProductId }).IsUnique();
            entity.HasIndex(item => item.ProductId);

            entity.HasOne(item => item.Order)
                  .WithMany(order => order.Items)
                  .HasForeignKey(item => item.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Product)
                  .WithMany()
                  .HasForeignKey(item => item.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

    }

}
