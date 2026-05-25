using FinanceSystem_Dotnet.Enums;
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
        public DbSet<BudgetCategory> BudgetCategories { get; set; }
        public DbSet<BudgetEntry> BudgetEntries { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.HashedPassword).IsRequired();
                entity.Property(e => e.Role).IsRequired();
                entity.Property(e => e.Presence).HasDefaultValue(Enums.UserPresence.OFFLINE);

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

                entity.HasOne(e => e.BudgetCategory)
                    .WithMany(bc => bc.Transactions)
                    .HasForeignKey(e => e.BudgetName)
                    .IsRequired(false)
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

            modelBuilder.Entity<BudgetCategory>(entity =>
            {
                entity.HasKey(e => e.Name);
                entity.Property(e => e.Preallocation).HasDefaultValue(0);
            });

            modelBuilder.Entity<BudgetEntry>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Budget)
                    .WithMany(bc => bc.Entries)
                    .HasForeignKey(e => e.BudgetName)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Inputter)
                    .WithMany()
                    .HasForeignKey(e => e.InputterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired();
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Args).HasColumnType("jsonb");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.HasPostgresEnum<UserPresence>("UserPresence");
            modelBuilder.HasPostgresEnum<Role>("UserRole");
            modelBuilder.HasPostgresEnum<TransactionPriority>("TransactionPriority");
            modelBuilder.HasPostgresEnum<TransactionForwardStatus>("TransactionForwardStatus");
            modelBuilder.HasPostgresEnum<NotificationType>("NotificationType");

            // Map singular table names, camelCase columns, and enum column types to match Prisma schema
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(entity.ClrType.Name);

                foreach (var property in entity.GetProperties())
                {
                    var columnName = property.Name;
                    if (!string.IsNullOrEmpty(columnName) && char.IsUpper(columnName[0]))
                    {
                        columnName = char.ToLower(columnName[0]) + columnName.Substring(1);
                    }
                    property.SetColumnName(columnName);

                    var propType = property.ClrType;
                    var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;
                    if (underlyingType.IsEnum)
                    {
                        var enumName = underlyingType.Name;
                        if (enumName == "Role") enumName = "UserRole";

                        modelBuilder.Entity(entity.ClrType)
                            .Property(property.Name)
                            .HasColumnType(enumName);
                    }
                }
            }

            // Custom column mapping for Transaction type name FK
            modelBuilder.Entity<Transaction>()
                .Property(t => t.TransactionTypeName)
                .HasColumnName("typeName");
        }
    }
}

