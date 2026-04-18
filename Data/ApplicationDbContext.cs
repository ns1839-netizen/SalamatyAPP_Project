using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models; // تأكدي إن ده مسار كلاس Prescription
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
        public DbSet<Product> Products { get; set; }
        public DbSet<MedicalProduct> MedicalProducts { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<MedicalProvider> MedicalProviders { get; set; }
        public DbSet<Facility> Facilities { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // لو عندك أي Fluent API configuration ضيفيها هنا
        }
    }
}