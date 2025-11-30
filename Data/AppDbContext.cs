using Microsoft.EntityFrameworkCore;
using EcomerceBE.Models;

namespace EcomerceBE.Data
{
    public class AppDbContext : DbContext
    {


        public void SeedAdminUsers()
        {
            if (!Users.Any(u => u.Email == "Hungv5996@gmail.com"))
            {
                var adminPass = BCrypt.Net.BCrypt.HashPassword("admin@1234");
                Users.Add(new User
                {
                    Email = "Hungv5996@gmail.com",
                    PasswordHash = adminPass,
                    Role = "Admin",
                    Name = "Hungadmin"
                });
            }

            if (!Users.Any(u => u.Email == "thaithanhphat323@gmail.com"))
            {
                var adminPass = BCrypt.Net.BCrypt.HashPassword("admin@1234");
                Users.Add(new User
                {
                    Email = "thaithanhphat323@gmail.com",
                    PasswordHash = adminPass,
                    Role = "Admin",
                    Name = "Phatadmin"
                });
            }

            SaveChanges();
        }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets
        public DbSet<OtpCode> OtpCodes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletItem> WalletItems { get; set; }

        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        public DbSet<WishList> Wishlists { get; set; }
        public DbSet<WishListItem> WishlistItems { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }

        public DbSet<Size> Sizes { get; set; }
        public DbSet<ProductSize> ProductSizes { get; set; }

        public DbSet<ProductColor> ProductColors { get; set; }

        public DbSet<CategorySize> CategorySizes { get; set; }

        public DbSet<Review> Reviews { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<FlashSale> FlashSales { get; set; }
        public DbSet<FlashSaleItem> FlashSaleItems { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<ProductCoupon> ProductCoupons { get; set; }
        public DbSet<UserCoupon> UserCoupons { get; set; }

        public DbSet<Brand> Brands { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);



  


        modelBuilder.Entity<ProductSize>()
     .HasIndex(ps => new { ps.ProductId, ps.SizeId, ps.CustomValue })
     .IsUnique(); // Một product không có 2 hàng kích cỡ trùng nhau (SizeId hoặc CustomValue) [web:80]

            modelBuilder.Entity<ProductColor>()
              .HasIndex(pc => new { pc.ProductSizeId, pc.ColorCode })
              .IsUnique(); // Một size chỉ có 1 colorCode duy nhất (chặn trùng color theo size) [web:80]
            // User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique(false); // nếu muốn unique email -> true

            // Category self relation
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict); // [web:218]

            // Product - Category
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull); // [web:218]

            // ProductImage (nếu có)
            modelBuilder.Entity<ProductImage>(e =>
            {
                e.ToTable("ProductImages"); // tên bảng mà EF sẽ dùng
                e.HasKey(pi => pi.ProductImageId);           // hoặc Id
                e.Property(pi => pi.ProductImageId).ValueGeneratedOnAdd();
                e.Property(pi => pi.ImageUrl).HasColumnType("LONGTEXT")   // hoặc "MEDIUMTEXT" nếu đủ
 .IsRequired();
                e.HasOne(pi => pi.Product)
                 .WithMany(p => p.Images)
                 .HasForeignKey(pi => pi.ProductId)
                 .OnDelete(DeleteBehavior.Cascade);
            });


            // Category 1 - n Brand
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Brands)
                .WithOne(b => b.Categories)
                .HasForeignKey(b => b.CategoryId)
                .IsRequired();


            modelBuilder.Entity<Category>()
             .HasMany(c => c.Brands);
            // Cart
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany(u => u.Carts)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade); // [web:218]

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade); // [web:218]

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // [web:218]

            // Wishlist
            modelBuilder.Entity<WishList>()
                .HasOne(w => w.User)
                .WithMany(u => u.Wishlists)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade); // [web:218]

            modelBuilder.Entity<WishListItem>()
                .HasOne(wi => wi.Wishlist)
                .WithMany(w => w.WishlistItems)
                .HasForeignKey(wi => wi.WishlistId)
                .OnDelete(DeleteBehavior.Cascade); // [web:218]

            modelBuilder.Entity<WishListItem>()
                .HasOne(wi => wi.Product)
                .WithMany(p => p.WishlistItems)
                .HasForeignKey(wi => wi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // [web:218]

            // Order & OrderItem
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade); // [web:218]

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Address)
                .WithMany(a => a.Orders)
                .HasForeignKey(o => o.AddressId)
                .OnDelete(DeleteBehavior.SetNull); // [web:218]

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Coupon)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CouponId)
                .OnDelete(DeleteBehavior.SetNull); // [web:218]

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // [web:218]

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // [web:218]

            // Reviews
            modelBuilder.Entity(typeof(Review))
                .HasOne(nameof(Review.User))
                .WithMany(nameof(User.Reviews))
                .HasForeignKey(nameof(Review.UserId))
                .OnDelete(DeleteBehavior.Cascade); // [web:218]

            modelBuilder.Entity(typeof(Review))
                .HasOne(nameof(Review.Product))
                .WithMany(nameof(Product.Reviews))
                .HasForeignKey(nameof(Review.ProductId))
                .OnDelete(DeleteBehavior.Cascade); // [web:218]

            // Coupon
            modelBuilder.Entity<Coupon>()
                .HasIndex(c => c.Code)
                .IsUnique(true); // [web:218]

            // Wallet
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithOne(u => u.Wallet)
                .HasForeignKey<Wallet>(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade); // [web:218]

            modelBuilder.Entity<WalletItem>()
                .HasOne(wi => wi.Wallet)
                .WithMany(w => w.WalletItems)
                .HasForeignKey(wi => wi.WalletId)
                .OnDelete(DeleteBehavior.Cascade); // [web:218]

            // FlashSale
            modelBuilder.Entity<FlashSaleItem>()
                .HasOne(fsi => fsi.FlashSale)
                .WithMany(fs => fs.FlashSaleItems)
                .HasForeignKey(fsi => fsi.FlashSaleId)
                .OnDelete(DeleteBehavior.Cascade); // [web:218]

            modelBuilder.Entity<FlashSaleItem>()
                .HasOne(fsi => fsi.Product)
                .WithMany(p => p.FlashSaleItems)
                .HasForeignKey(fsi => fsi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // [web:218]

            // Decimal precision settings
            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>().Property(p => p.DiscountPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Coupon>().Property(c => c.DiscountValue).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<FlashSaleItem>().Property(f => f.DiscountPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Wallet>().Property(w => w.Balance).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<WalletItem>().Property(wi => wi.Amount).HasColumnType("decimal(18,2)"); // [web:218]

            // CategorySize (giữ composite)
            modelBuilder.Entity<CategorySize>()
                .HasKey(cs => new { cs.CategoryId, cs.SizeId });
            modelBuilder.Entity<CategorySize>()
                .HasOne(cs => cs.Category)
                .WithMany(c => c.CategorySizes)
                .HasForeignKey(cs => cs.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CategorySize>()
                .HasOne(cs => cs.Size)
                .WithMany(s => s.CategorySizes)
                .HasForeignKey(cs => cs.SizeId)
                .OnDelete(DeleteBehavior.Cascade); // [web:218]

            // ProductSize
            modelBuilder.Entity<ProductSize>(e =>
            {
                e.ToTable("ProductSizes");                      

                e.HasKey(ps => ps.ProductSizeId);

                e.Property(ps => ps.ProductSizeId)
                 .ValueGeneratedOnAdd();                        // AUTO_INCREMENT

                e.Property(ps => ps.CustomValue)
                 .HasMaxLength(64);

                e.HasOne(ps => ps.Product)
                 .WithMany(p => p.ProductSizes)
                 .HasForeignKey(ps => ps.ProductId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ps => ps.Size)
                 .WithMany(s => s.ProductSizes)
                 .HasForeignKey(ps => ps.SizeId)
                 .OnDelete(DeleteBehavior.Restrict);

            });

            // ProductColor
            modelBuilder.Entity<ProductColor>(e =>
            {
                e.ToTable("ProductColors");

                e.HasKey(pc => pc.ProductColorId);

                e.Property(pc => pc.ProductColorId)
                 .ValueGeneratedOnAdd();                        // AUTO_INCREMENT

                e.Property(pc => pc.ColorCode)
                 .HasMaxLength(7)
                 .IsRequired();

                e.HasOne(pc => pc.ProductSize)
                 .WithMany(ps => ps.ProductColors)
                 .HasForeignKey(pc => pc.ProductSizeId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(pc => new { pc.ProductSizeId, pc.ColorCode })
                 .IsUnique();

             
            });



            //coupon systems
            modelBuilder.Entity<ProductCoupon>()
        .HasKey(pc => new { pc.ProductId, pc.CouponId });

            modelBuilder.Entity<ProductCoupon>()
                .HasOne(pc => pc.Product)
                .WithMany(p => p.ProductCoupons)
                .HasForeignKey(pc => pc.ProductId);

            modelBuilder.Entity<ProductCoupon>()
                .HasOne(pc => pc.Coupon)
                .WithMany(c => c.ProductCoupons)
                .HasForeignKey(pc => pc.CouponId);

            modelBuilder.Entity<UserCoupon>()
                .HasKey(uc => new { uc.UserId, uc.CouponId });

            modelBuilder.Entity<UserCoupon>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserCoupons)
                .HasForeignKey(uc => uc.UserId);

            modelBuilder.Entity<UserCoupon>()
                .HasOne(uc => uc.Coupon)
                .WithMany(c => c.UserCoupons)
                .HasForeignKey(uc => uc.CouponId);


        }
}
}
