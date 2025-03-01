using EmployeeManagementSystem.DataAccess;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Net;

namespace EmployeeManagementSystem.Functions
{
    public class EmployeeFunctions
    {
        private readonly ILogger<EmployeeFunctions> _logger;
        private readonly IEmployeeManagementRepository _Repository;

        public EmployeeFunctions(ILogger<EmployeeFunctions> logger, IEmployeeManagementRepository repository)
        {
            _logger = logger;
            _Repository = repository;


        }

        /// <summary>
        /// Get an employee by Id
        /// </summary>
        /// <param name="req"></param>
        /// <param name="departmentId"></param>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        [Function("GetEmployeeById")]
        [OpenApiOperation(operationId: "GetEmployeeById", tags: new[] { "employee" }, Summary = "Get an employee by Id", Description = "Retrieves a specific employee record by employee and department Id.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "departmentId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The department Id.")]
        [OpenApiParameter(name: "employeeId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The employee Id.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Employee), Description = "Successful operation")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Employee not found")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Query, Name = "code", Description = "Function API key")]

        public async Task<IActionResult> GetEmployeeById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "employee/{departmentId}/{employeeId}")] HttpRequest req,
                string departmentId,
                string employeeId)
        {
            _logger.LogInformation("Attempting to retrieve EmployeeId '{EmployeeId}' in Department '{DepartmentId}'.", employeeId, departmentId);
            var employee = await _Repository.GetEmployeeByIdAsync(employeeId, departmentId);

            return new OkObjectResult(employee);
        }



        [Function("CreateEmployee")]
        [OpenApiOperation(operationId: "CreateEmployee", tags: new[] { "employee" },
                Summary = "Create a new employee", Description = "Creates a new employee record.",
                Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Employee),
                Description = "Employee object", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json",
                bodyType: typeof(Employee), Description = "Employee created successfully")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Query, Name = "code",
                Description = "Function API key")]
        public async Task<IActionResult> CreateEmployee(
                [HttpTrigger(AuthorizationLevel.Function, "post", Route = "employee")]
            HttpRequest req)
        {
           
            _logger.LogInformation("Received request to create an employee.");

            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var employee = JsonConvert.DeserializeObject<Employee>(requestBody);

            if (employee == null)
            {
                _logger.LogWarning("No valid employee data in request body.");
                return new BadRequestObjectResult("Invalid Employee data.");

            }

            Employee newEmployee = await _Repository.CreateEmployeeAsync(employee);
            return new OkObjectResult(newEmployee);
        }


        [Function("UpdateEmployee")]
        [OpenApiOperation(operationId: "UpdateEmployee", tags: new[] { "employee" },
                Summary = "Update an employee", Description = "Updates an existing employee record.",
                Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Employee),
                Description = "Employee object", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
                bodyType: typeof(Employee), Description = "Employee updated successfully")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Employee not found")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Query, Name = "code",
                Description = "Function API key")]
        public async Task<IActionResult> UpdateEmployee(
                [HttpTrigger(AuthorizationLevel.Function, "put", Route = "employee")]
            HttpRequestData req)
        {
            
            _logger.LogInformation("Received request to update an employee.");
            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var employee = JsonConvert.DeserializeObject<Employee>(requestBody);
            if (employee == null)
            {
                _logger.LogWarning("No valid employee data in request body.");
                return new BadRequestObjectResult("Invalid Employee data.");
            }
            Employee updatedEmployee = await _Repository.UpdateEmployeeAsync(employee);
            return new OkObjectResult(updatedEmployee);
        }

        [Function("DeleteEmployee")]
        [OpenApiOperation(operationId: "DeleteEmployee", tags: new[] { "employee" },
                Summary = "Delete an employee", Description = "Deletes an existing employee record.",
                Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "departmentId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The department Id.")]

        [OpenApiParameter(name: "employeeId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The employee Id.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Description = "Employee deleted successfully")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Employee not found")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Query, Name = "code",
                Description = "Function API key")]
        public async Task<IActionResult> DeleteEmployee(
                [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "employee/{departmentId}/{employeeId}")]
            HttpRequestData req,
            string departmentId,
            string employeeId)
        {
            _logger.LogInformation("Received request to delete an employee.");
            await _Repository.DeleteEmployeeAsync(employeeId, departmentId);
            return new OkObjectResult($"Employee with ID = {employeeId} in department {departmentId} deleted.");
        }

        [Function("GetEmployeesByDepartment")]
        [OpenApiOperation(operationId: "GetEmployeesByDepartment", tags: new[] { "employee" },
                Summary = "Get employees by department", Description = "Retrieves all employee records in a department.",
                Visibility = OpenApiVisibilityType.Important)]

        [OpenApiParameter(name: "departmentId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The department Id.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
                bodyType: typeof(IEnumerable<Employee>), Description = "Successful operation")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "No employees found")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Query, Name = "code",
                Description = "Function API key")]
        public async Task<IActionResult> GetEmployeesByDepartment(

                [HttpTrigger(AuthorizationLevel.Function, "get", Route = "employee/{departmentId}")]
            HttpRequestData req,
            string departmentId)
        {
            _logger.LogInformation("Attempting to retrieve all employees in Department '{DepartmentId}'.", departmentId);
            var employees = await _Repository.GetEmployeesByDepartmentAsync(departmentId);
            return new OkObjectResult(employees);
        }
    }

}
