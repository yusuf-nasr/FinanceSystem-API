namespace FinanceSystem_Dotnet.Models
{
    public class TransactionDocument
    {
        public int TransactionId { get; set; }
        public virtual Transaction Transaction { get; set; } = null!;

        public int DocumentId { get; set; }
        public virtual Document Document { get; set; } = null!;

        // Attached-by user name (FK to User.Name)
        public string AttachedBy { get; set; } = null!;
        public virtual User? AttachedByUser { get; set; }

        public DateTime AttachedAt { get; set; }
    }
}
