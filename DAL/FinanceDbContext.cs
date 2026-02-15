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
        public DbSet<TransactionDocument> TransactionDocuments { get; set; }// for explicit join entity

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.HashedPassword).IsRequired();
                entity.Property(e => e.Role).IsRequired();

                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Users)
                    .HasForeignKey(e => e.DepartmentName)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.Name);

                entity.HasOne(e => e.Manager)
                    .WithOne(u => u.ManagedDepartment)
                    .HasForeignKey<Department>(e => e.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Priority).IsRequired();

                entity.HasOne(e => e.Creator)
                    .WithMany(u => u.CreatedTransactions)
                    .HasForeignKey(e => e.CreatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.TransactionType)
                    .WithMany(tt => tt.Transactions)
                    .HasForeignKey(e => e.TransactionTypeName)
                    .OnDelete(DeleteBehavior.Restrict);

                // Use the CLR join entity so the "AttachedBy" and "AttachedAt" are first-class properties
                entity.HasMany(t => t.Documents)
                    .WithMany(d => d.Transactions)
                    .UsingEntity<TransactionDocument>(
                        right => right
                            .HasOne(j => j.Document)
                            .WithMany()
                            .HasForeignKey(j => j.DocumentId)
                            .OnDelete(DeleteBehavior.Restrict),
                        left => left
                            .HasOne(j => j.Transaction)
                            .WithMany()
                            .HasForeignKey(j => j.TransactionId)
                            .OnDelete(DeleteBehavior.Cascade),
                        join =>
                        {
                            join.HasKey(j => new { j.TransactionId, j.DocumentId });
                            join.Property(j => j.AttachedBy).IsRequired();
                            join.Property(j => j.AttachedAt).IsRequired();

                            join.HasOne(j => j.AttachedByUser)
                                .WithMany()
                                .HasForeignKey(j => j.AttachedBy)
                                .OnDelete(DeleteBehavior.Restrict);

                            join.ToTable("TransactionDocument");
                        });
            });

            modelBuilder.Entity<TransactionType>(entity =>
            {
                entity.HasKey(e => e.Name);

                entity.HasOne(e => e.Creator)
                    .WithMany(u => u.CreatedTransactionTypes)
                    .HasForeignKey(e => e.CreatorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();

                entity.HasOne(e => e.Uploader)
                    .WithMany(u => u.UploadedDocuments)
                    .HasForeignKey(e => e.UploaderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TransactionForward>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired();

                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.SentForwards)
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Receiver)
                    .WithMany(u => u.ReceivedForwards)
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Transaction)
                    .WithMany(t => t.Forwards)
                    .HasForeignKey(e => e.TransactionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
