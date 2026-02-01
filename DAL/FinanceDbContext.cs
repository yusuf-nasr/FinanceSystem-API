using FinanceSystem_Dotnet.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceSystem_Dotnet.DAL
{
    public class FinanceDbContext : DbContext
    {
        public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionType> TransactionTypes { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<TransactionForward> TransactionForwards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Name);
                entity.Property(e => e.HashedPassword).IsRequired();
                entity.Property(e => e.Role).IsRequired();

                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Users)
                    .HasForeignKey(e => e.DepartmentName);
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.Name);

                entity.HasOne(e => e.Manager)
                    .WithMany(u => u.ManagedDepartments)
                    .HasForeignKey(e => e.ManagerName);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Priority).IsRequired();

                entity.HasOne(e => e.Creator)
                    .WithMany(u => u.CreatedTransactions)
                    .HasForeignKey(e => e.CreatorName);

                entity.HasOne(e => e.TransactionType)
                    .WithMany(tt => tt.Transactions)
                    .HasForeignKey(e => e.TransactionTypeName);
            });

            modelBuilder.Entity<TransactionType>(entity =>
            {
                entity.HasKey(e => e.Name);

                entity.HasOne(e => e.Creator)
                    .WithMany(u => u.CreatedTransactionTypes)
                    .HasForeignKey(e => e.CreatorName);
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();

                entity.HasOne(e => e.Uploader)
                    .WithMany(u => u.UploadedDocuments)
                    .HasForeignKey(e => e.UploaderName);

                entity.HasOne(e => e.Transaction)
                    .WithMany(t => t.Documents)
                    .HasForeignKey(e => e.TransactionId);
            });

            modelBuilder.Entity<TransactionForward>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired();

                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.SentForwards)
                    .HasForeignKey(e => e.SenderName)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Receiver)
                    .WithMany(u => u.ReceivedForwards)
                    .HasForeignKey(e => e.ReceiverName)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Transaction)
                    .WithMany(t => t.Forwards)
                    .HasForeignKey(e => e.TransactionId);
            });
        }
    }
}
