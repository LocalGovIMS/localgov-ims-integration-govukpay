using Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class GovUkPayDbContext : DbContext
    {
        public GovUkPayDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Payment> Payments { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        public DbSet<ApplicationLog> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            SetupIndexes(modelBuilder);
            SetupColumnTypes(modelBuilder);
        }

        private static void SetupIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>()
                .HasIndex(x => x.Identifier);

            modelBuilder.Entity<Payment>()
                .HasIndex(x => x.Reference);

            modelBuilder.Entity<Payment>()
                .HasIndex(x => x.Finished)
                .IncludeProperties(x => x.Status);

            modelBuilder.Entity<Refund>()
                .HasIndex(x => x.PaymentReference);

            modelBuilder.Entity<Refund>()
                .HasIndex(x => x.RefundReference);

            modelBuilder.Entity<Refund>()
                .HasIndex(x => x.Finished);
        }

        private static void SetupColumnTypes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");
            
            modelBuilder.Entity<Refund>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");
        }
    }
}
