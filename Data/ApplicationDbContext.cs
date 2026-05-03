using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models;
using Salamaty.API.Models.HomeModels;
using Salamaty.API.Models.ProfileModels;
using SalamatyAPI.Models;

namespace SalamatyAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Banner> Banners { get; set; }
        public DbSet<MedicalProvider> MedicalProviders { get; set; }
        public DbSet<Facility> Facilities { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<MedicalProduct> MedicalProducts { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<ProductAlternative> ProductAlternatives { get; set; }
        public DbSet<Favorite> Favourites { get; set; }
        public DbSet<InsuranceProvider> InsuranceProviders { get; set; }
        public DbSet<InsuranceProfile> InsuranceProfiles { get; set; }
        public DbSet<InsuranceNetworkService> InsuranceNetworkServices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. إعدادات المفضلات
            modelBuilder.Entity<Favorite>().ToTable("Favourites");
            modelBuilder.Entity<Favorite>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(f => f.UserId);

            // 2. إعدادات ملف التأمين
            modelBuilder.Entity<InsuranceProfile>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<InsuranceProfile>()
                .HasOne(p => p.InsuranceProvider)
                .WithMany(i => i.InsuranceProfiles)
                .HasForeignKey(p => p.InsuranceProviderId);

            // 3. 🔥 تعديل إعدادات البدائل (الحل للأخطاء CS0029 و CS1662 و CS1061)
            modelBuilder.Entity<ProductAlternative>()
                .HasKey(pa => new { pa.ProductId, pa.AlternativeProductId });

            modelBuilder.Entity<ProductAlternative>()
                .HasOne(pa => pa.Product)
                .WithMany(p => p.ProductAlternatives) // استخدمي الاسم الجديد اللي في الموديل
                .HasForeignKey(pa => pa.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductAlternative>()
                .HasOne(pa => pa.AlternativeProduct)
                .WithMany() // شيلنا .WithMany(p => p.AlternativeTo) لأننا مسحنا البروبرتي دي
                .HasForeignKey(pa => pa.AlternativeProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. دقة سعر المنتج
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MedicalProduct>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // 5. علاقات شبكة التأمين
            modelBuilder.Entity<InsuranceNetworkService>()
                .HasOne(s => s.InsuranceProvider)
                .WithMany(p => p.NetworkServices)
                .HasForeignKey(s => s.InsuranceProviderId);
        }
    }
}