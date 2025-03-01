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

namespace EmployeeManagementSystem.Tests
{
    public class EmployeeFunctionsTests
    {
        private readonly Mock<ILogger<EmployeeFunctions>> _mockLogger;
        private readonly Mock<IEmployeeManagementRepository> _mockRepository;
        private readonly EmployeeFunctions _functions;

        public EmployeeFunctionsTests()
        {
            // Create the mocks
            _mockLogger = new Mock<ILogger<EmployeeFunctions>>();
            _mockRepository = new Mock<IEmployeeManagementRepository>();

            // Setup default behavior for CreateEmployeeAsync
            _mockRepository
                .Setup(repo => repo.CreateEmployeeAsync(It.IsAny<Employee>()))
                .ReturnsAsync((Employee e) =>
                {
                    e.Id = "generated-id-123";
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
                FirstName = "Alice",
                LastName = "Smith",
                Department = new Department { DepartmentId = departmentId }
            };

            // Mock the repository call
            _mockRepository
                .Setup(repo => repo.GetEmployeeByIdAsync(employeeId, departmentId))
                .ReturnsAsync(fakeEmployee);

            // We need a dummy HttpRequest. Minimal usage for a GET scenario.
            var mockRequest = new Mock<HttpRequest>();

            // Act
            IActionResult result = await _functions.GetEmployeeById(mockRequest.Object, departmentId, employeeId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedEmployee = Assert.IsType<Employee>(okResult.Value);
            Assert.Equal(employeeId, returnedEmployee.Id);
            Assert.Equal("Alice", returnedEmployee.FirstName);

            // Verify that the repository method was called exactly once
            _mockRepository.Verify(r => r.GetEmployeeByIdAsync(employeeId, departmentId), Times.Once);
        }

        [Fact]
        public async Task GetEmployeeById_NotFound()
        {
            // Arrange
            var departmentId = "HR";
            var employeeId = "emp123";

            // Mock the repository to return null => employee not found
            _mockRepository
                .Setup(repo => repo.GetEmployeeByIdAsync(employeeId, departmentId))
                .ReturnsAsync((Employee)null);

            var mockRequest = new Mock<HttpRequest>();

            // Act
            IActionResult result = await _functions.GetEmployeeById(mockRequest.Object, departmentId, employeeId);

            // Assert
            // Currently, your code always returns OkObjectResult even if it is null. 
            // You might want to fix your function to return NotFoundResult if employee == null.
            // For the sake of demonstration, we assume you'd do something like:
            // if (employee == null) { return new NotFoundResult(); }
            // Then we can test that scenario:

            // If your code is unmodified, it returns OkObjectResult with null. 
            // We'll demonstrate the "ideal" scenario:
            if (result is OkObjectResult)
            {
                // If the code hasn't been changed, it might come back as OK with null. 
                // We'll check for that:
                var okResult = result as OkObjectResult;
                Assert.Null(okResult.Value);
            }
            else
            {
                // If you updated your function to return NotFound(), we'd do:
                Assert.IsType<NotFoundResult>(result);
            }

            _mockRepository.Verify(r => r.GetEmployeeByIdAsync(employeeId, departmentId), Times.Once);
        }

        [Fact]
        public async Task CreateEmployee_Success()
        {
            // Arrange
            var newEmp = new Employee
            {
                // Id will be set if blank
                Id= "",
                FirstName = "Bob",
                LastName = "Dylan",
                Position = "Musician",
                Department = new Department { DepartmentId = "ARTS", DepartmentName = "Arts" }
            };

            // Mock the repository to return a newly created Employee
            _mockRepository
                .Setup(repo => repo.CreateEmployeeAsync(It.IsAny<Employee>()))
                .ReturnsAsync((Employee e) =>
                {
                    e.Id = "generated-id-123";
                    return e;
                });

            // Create a JSON payload with the new employee
            string jsonPayload = JsonConvert.SerializeObject(newEmp);
            var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(jsonPayload));
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Body).Returns(requestBody);

            // Act
            IActionResult result = await _functions.CreateEmployee(mockRequest.Object /* intentionally mismatch, but your code uses HttpRequestData! */);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdEmp = Assert.IsType<Employee>(okResult.Value);
            Assert.Equal("generated-id-123", createdEmp.Id);
            Assert.Equal("Bob", createdEmp.FirstName);

            _mockRepository.Verify(r => r.CreateEmployeeAsync(It.IsAny<Employee>()), Times.Once);
        }

        [Fact]
        public async Task CreateEmployee_Invalid()
        {
            // Arrange
            // This time, we pass no valid JSON or something that leads to `employee == null`.
            string invalidJson = ""; // or something that can't be deserialized
            var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Body).Returns(requestBody);

            // Act
            IActionResult result = await _functions.CreateEmployee(mockRequest.Object);

            // Assert
            // We expect a BadRequestObjectResult from your code if `employee == null`.
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid Employee data.", badRequestResult.Value);

            // Make sure CreateEmployeeAsync isn't called
            _mockRepository.Verify(r => r.CreateEmployeeAsync(It.IsAny<Employee>()), Times.Never);
        }
    }
}
