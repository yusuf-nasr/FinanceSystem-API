using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.Enums;

namespace FinanceSystem_Dotnet.Services
{
    public interface  IFinanceService
    {
        bool IsAdmin(int id);
        bool DeptExists(string name);
        bool IsManager(string deptName, int id);
    }
    public class Services : IFinanceService
    {
        private readonly FinanceDbContext context;

        public Services(FinanceDbContext context)
        {
            this.context = context;
        }

        public bool DeptExists(string name)
        {
            return context.Departments.Any(d => d.Name == name);
        }

        public bool IsAdmin(int id)
        {
            return context.Users.Find(id)?.Role == Role.ADMIN;
        }

        public bool IsManager(string deptName, int id)
        {
            return context.Departments.Find(deptName)?.ManagerId == id;
        }
    }
}
