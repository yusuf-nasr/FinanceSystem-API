using FinanceSystem_Dotnet.DTOs;

namespace FinanceSystem_Dotnet.Services
{
    public interface IDepartmentService
    {
        Task<DeptResponseDTO> CreateDepartmentAsync(DeptCreateDTO request);
        Task<PaginatedResult<DeptResponseDTO>> GetAllDepartmentsPaginatedAsync(DeptQueryDTO query);
        Task<DeptResponseDTO?> GetDepartmentByNameAsync(string name);
        Task<DeptResponseDTO> UpdateDepartmentAsync(string name, DeptUpdateDTO request);
        Task<DeptResponseDTO> DeleteDepartmentAsync(string name);
    }
}
