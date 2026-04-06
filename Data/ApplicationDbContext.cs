using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models;
using Salamaty.API.Models.HomeModels;
using Salamaty.API.Models.ProfileModels;

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

        // خلي سطر الـ Facilities ده من غير علامات Nancy
        public DbSet<Facility> Facilities { get; set; }
        public DbSet<MedicalProduct> MedicalProducts { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // لو عندك أي Fluent API configuration ضيفيها هنا
        }
    }
}