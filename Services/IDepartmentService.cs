using FinanceSystem_Dotnet.DTOs;

namespace FinanceSystem_Dotnet.Services
{
    public interface IDepartmentService
    {
        Task<(bool Success, string Message)> CreateDepartmentAsync(DeptCreateDTO request);
        Task<List<DeptResponseDTO>> GetAllDepartmentsAsync();
        Task<DeptResponseDTO?> GetDepartmentByNameAsync(string name);
        Task<(bool Success, string Message)> UpdateDepartmentAsync(string name, DeptUpdateDTO request);
        Task<(bool Success, string Message)> DeleteDepartmentAsync(string name);
    }
}
