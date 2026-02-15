using System.Collections.Generic;

namespace FinanceSystem_Dotnet.Models
{
    public class Department
    {
        public string Name { get; set; }
        public int? ManagerId { get; set; }

        public virtual User Manager { get; set; }
        public virtual ICollection<User> Users { get; set; }
    }
}
