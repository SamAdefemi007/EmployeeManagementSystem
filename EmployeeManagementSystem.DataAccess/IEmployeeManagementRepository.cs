using EmployeeManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeManagementSystem.DataAccess
{
    public interface IEmployeeManagementRepository
    {
        Task<Employee> GetEmployeeByIdAsync(string employeeId, string departmentId);
        Task<Employee> CreateEmployeeAsync(Employee employee);
        Task<Employee> UpdateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(string employeeId, string departmentId);
        Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(string departmentId);
    }
}
