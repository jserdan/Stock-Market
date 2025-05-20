using danserdan.Models;
using Microsoft.EntityFrameworkCore;

namespace danserdan.Services
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options) { }

        public DbSet<Users> Users { get; set; }
        public DbSet<Stocks> Stocks { get; set; }
        public DbSet<Transaction> Transactions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Users>(entity =>
            {
                entity.ToTable("users");

                // Configure columns
                entity.Property(e => e.user_id).HasColumnName("user_id");
                entity.Property(e => e.username).HasColumnName("username");
                entity.Property(e => e.email).HasColumnName("email");
                entity.Property(e => e.password_hash).HasColumnName("password_hash");
                entity.Property(e => e.balance)
                      .HasColumnName("balance")
                      .HasColumnType("decimal(18, 2)");  // Precision of 18 and scale of 2
                entity.Property(e => e.created_at).HasColumnName("created_at");
            });
            
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("transactions");
                
                // Configure columns
                entity.Property(e => e.transaction_id).HasColumnName("transaction_id");
                entity.Property(e => e.user_id).HasColumnName("user_id");
                entity.Property(e => e.StockId).HasColumnName("stock_id");
                entity.Property(e => e.Price)
                      .HasColumnName("price")
                      .HasColumnType("decimal(18, 2)");
                entity.Property(e => e.quantity).HasColumnName("quantity");
                entity.Property(e => e.TransactionTime).HasColumnName("transaction_time");
                
                // Configure relationships
                entity.HasOne(d => d.User)
                      .WithMany()
                      .HasForeignKey(d => d.user_id);
            });
        }
    }
}