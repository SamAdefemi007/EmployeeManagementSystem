using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EmployeeManagementSystem.DataAccess;
using EmployeeManagementSystem.Functions;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyModel;

namespace EmployeeManagementSystem.Tests
{
    public class EmployeeFunctionsTests
    {
        private readonly Mock<ILogger<EmployeeFunctions>> _mockLogger;
        private readonly Mock<IEmployeeManagementRepository> _mockRepository;
        private readonly EmployeeFunctions _functions;

        public EmployeeFunctionsTests()
        {
            // mocking the logger and repository
            _mockLogger = new Mock<ILogger<EmployeeFunctions>>();
            _mockRepository = new Mock<IEmployeeManagementRepository>();

            // Setup default behavior for CreateEmployeeAsync
            _mockRepository
                .Setup(repo => repo.CreateEmployeeAsync(It.IsAny<Employee>()))
                .ReturnsAsync((Employee e) =>
                {
                    e.Id = "connectfirst-id-001";
                    return e;
                });

            _functions = new EmployeeFunctions(_mockLogger.Object, _mockRepository.Object);
        }

        [Fact]
        public async Task GetEmployeeById_Success()
        {
            // Arrange
            var departmentId = "HR";
            var employeeId = "emp123";
            var fakeEmployee = new Employee
            {
                Id = employeeId,
                FirstName = "Joe",
                LastName = "Shenfield",
                Department = new Department { DepartmentId = departmentId }
            };

            // Setup the repository to return the fake employee
            _mockRepository
                .Setup(repo => repo.GetEmployeeByIdAsync(employeeId, departmentId))
                .ReturnsAsync(fakeEmployee);
            var mockRequest = new Mock<HttpRequest>();

            // Act
            IActionResult result = await _functions.GetEmployeeById(mockRequest.Object, departmentId, employeeId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedEmployee = Assert.IsType<Employee>(okResult.Value);
            Assert.Equal(employeeId, returnedEmployee.Id);
            Assert.Equal("Joe", returnedEmployee.FirstName);
            Assert.Equal("Shenfield", returnedEmployee.LastName);

            // checking repository method was called exactly once
            _mockRepository.Verify(r => r.GetEmployeeByIdAsync(employeeId, departmentId), Times.Once);
        }


        [Fact]
        public async Task GetEmployeeById_NotFound()
        {
            // Arrange
            var departmentId = "HR";
            var employeeId = "emp123";

            // Mocking the repository to return null
            _mockRepository
                .Setup(repo => repo.GetEmployeeByIdAsync(employeeId, departmentId))
                .ReturnsAsync((Employee?)null);

            var mockRequest = new Mock<HttpRequest>();

            // Act
            IActionResult result = await _functions.GetEmployeeById(mockRequest.Object, departmentId, employeeId);

            
            Assert.IsType<NotFoundResult>(result);
            Assert.Null((result as OkObjectResult)?.Value);

            _mockRepository.Verify(r => r.GetEmployeeByIdAsync(employeeId, departmentId), Times.Once);
        }

        [Fact]
        public async Task CreateEmployee_Success()
        {
            // Arrange
            var newEmp = new Employee
            {
                //Id will be automatically generated if it is set as empty string
                Id = "",
                FirstName = "Joe",
                LastName = "Shenfield",
                Position = "Manager",
                Department = new Department { DepartmentId = "ENG", DepartmentName = "Software Engineering" },
                Address = new Address { Street = "123 Elm St", City = "Springfield", State = "IL", PostalCode = "62701" }
            };

            // Mock the repository to return a newly created Employee
            _mockRepository
                .Setup(repo => repo.CreateEmployeeAsync(It.IsAny<Employee>()))
                .ReturnsAsync((Employee e) =>
                {
                    e.Id = "connectfirst-id-001";
                    return e;
                });

            // Creating a JSON object for the new employee
            string jsonPayload = JsonConvert.SerializeObject(newEmp);
            var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(jsonPayload));
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Body).Returns(requestBody);

            // Act
            IActionResult result = await _functions.CreateEmployee(mockRequest.Object);

            // Assert that the result is an OkObjectResult containing the created employee
            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdEmp = Assert.IsType<Employee>(okResult.Value);
            Assert.Equal("connectfirst-id-001", createdEmp.Id);
            Assert.Equal("Joe", createdEmp.FirstName);
            Assert.Equal("Shenfield", createdEmp.LastName);
            Assert.Equal("Manager", createdEmp.Position);

            _mockRepository.Verify(r => r.CreateEmployeeAsync(It.IsAny<Employee>()), Times.Once);
        }

        [Fact]
        public async Task CreateEmployee_Invalid()
        {
            
            string invalidJson = ""; 
            var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Body).Returns(requestBody);

            // Act
            IActionResult result = await _functions.CreateEmployee(mockRequest.Object);

            // Assert
            //The code returns a badobjectresult if the employee object is null or invalid
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid Employee data.", badRequestResult.Value);

            // The repository method should not be called at all
            _mockRepository.Verify(r => r.CreateEmployeeAsync(It.IsAny<Employee>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEmployee_Success()
        {
            // Arrange: Create an employee object representing updated data
            var updatedEmp = new Employee
            {
                Id = "connectfirst-id-001",
                FirstName = "Joe",
                LastName = "Shenfield",
                Position = "Manager",
                Department = new Department { DepartmentId = "OPS", DepartmentName = "Operations" }
            };

            _mockRepository
                .Setup(repo => repo.UpdateEmployeeAsync(It.IsAny<Employee>()))
                .ReturnsAsync((Employee e) => e); 

            string jsonPayload = JsonConvert.SerializeObject(updatedEmp);
            var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(jsonPayload));
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Body).Returns(requestBody);

            // Actions
            IActionResult result = await _functions.UpdateEmployee(mockRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedEmp = Assert.IsType<Employee>(okResult.Value);
            Assert.Equal("connectfirst-id-001", returnedEmp.Id);
            Assert.Equal("Joe", returnedEmp.FirstName);

            _mockRepository.Verify(r => r.UpdateEmployeeAsync(It.IsAny<Employee>()), Times.Once);
        }

        [Fact]
        public async Task DeleteEmployee_Success()
        {
            // Setting up department and employee IDs for deletion
            var departmentId = "OPS";
            var employeeId = "connectfirst-id-001";

            // DeleteEmployeeAsync should return a completed task
            _mockRepository
                .Setup(repo => repo.DeleteEmployeeAsync(employeeId, departmentId))
                .Returns(Task.CompletedTask);

            var mockRequest = new Mock<HttpRequest>();

            // Act
            IActionResult result = await _functions.DeleteEmployee(mockRequest.Object, departmentId, employeeId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            string message = okResult.Value?.ToString() ?? "";
            Assert.Contains(employeeId, message);
            Assert.Contains(departmentId, message);

            _mockRepository.Verify(r => r.DeleteEmployeeAsync(employeeId, departmentId), Times.Once);
        }

        [Fact]
        public async Task GetEmployeesByDepartment_Success()
        {
            // Arrange: Set up a list of employees in a department
            var departmentId = "ENG";
            var employeesList = new List<Employee>
            {
                new Employee { Id = "connectfirst-id-001", FirstName = "Joe", LastName = "Shenfield", Department = new Department { DepartmentId = departmentId } },
                new Employee { Id = "connectfirst-id-002", FirstName = "Bryson", LastName = "Pullukatt", Department = new Department { DepartmentId = departmentId } }
            };

            _mockRepository
                .Setup(repo => repo.GetEmployeesByDepartmentAsync(departmentId))
                .ReturnsAsync(employeesList);

            var mockRequest = new Mock<HttpRequest>();

            // Act
            IActionResult result = await _functions.GetEmployeesByDepartment(mockRequest.Object, departmentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedList = Assert.IsType<List<Employee>>(okResult.Value);
            Assert.Equal(2, returnedList.Count);
            Assert.Equal("Joe", returnedList[0].FirstName);
            Assert.Equal("Bryson", returnedList[1].FirstName);


            _mockRepository.Verify(r => r.GetEmployeesByDepartmentAsync(departmentId), Times.Once);
        }
    }
}
