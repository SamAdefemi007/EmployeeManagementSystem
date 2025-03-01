using EmployeeManagementSystem.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace EmployeeManagementSystem.DataAccess
{
    /// <summary>
    /// Data Access repository for managing Employee Data in Cosmos DB
    /// </summary>
    public class EmployeeManagementRepository:IEmployeeManagementRepository
    {
        private readonly CosmosClient _CosmosClient;
        private Container _Container;
        private readonly ILogger<EmployeeManagementRepository> _logger;

        public EmployeeManagementRepository(string connectionString, ILogger<EmployeeManagementRepository> logger)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException("Connection string cannot be null or empty, please check and try again", nameof(connectionString));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _CosmosClient = new CosmosClient(connectionString);
            _logger.LogInformation("Cosmos Client has been Initialized");
        }

        /// <summary>
        /// Initializes the connection by verifying/creating the database and container.
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="containerName"></param>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        public async Task InitializeConnectionAsync(string databaseName, string containerName, string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentNullException("Database name cannot be null or empty.", nameof(databaseName));
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentNullException("Container name cannot be null or empty.", nameof(containerName));
            if (string.IsNullOrWhiteSpace(partitionKey))
                throw new ArgumentNullException("Partition key path cannot be null or empty.", nameof(partitionKey));

            // Create the database if it doesn't exist.
            DatabaseResponse databaseResponse = await _CosmosClient.CreateDatabaseIfNotExistsAsync(id: databaseName);
            _logger.LogInformation("Database '{DatabaseName}' verified/created.", databaseName);

            // Create the container if it doesn't exist.
            ContainerResponse containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(id: containerName, partitionKeyPath: partitionKey);
            _Container = containerResponse.Container;
            _logger.LogInformation("Container '{ContainerName}' verified/created with partition key '{PartitionKey}'.", containerName, partitionKey);
        }

        /// <summary>
        /// Retrieves an employee record by employee Id and department Id.
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="departmentId"></param>
        /// <returns></returns>
        public async Task<Employee> GetEmployeeByIdAsync(string employeeId, string departmentId)
        {
            try
            {
                _logger.LogInformation("Attempting to read employee record with Id '{EmployeeId}' in department '{DepartmentId}'.", employeeId, departmentId);
                ItemResponse<Employee> employeeResponse = await _Container.ReadItemAsync<Employee>(id: employeeId, partitionKey: new PartitionKey(departmentId));
                _logger.LogInformation("Successfully retrieved employee record with Id '{EmployeeId}'.", employeeId);
                return employeeResponse.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Employee record with Id '{EmployeeId}' not found in department '{DepartmentId}'.", employeeId, departmentId);
                return null;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error reading employee record with Id '{EmployeeId}'.", employeeId);
                throw;
            }
        }

        /// <summary>
        /// Creates a new employee record.
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        public async Task<Employee> CreateEmployeeAsync(Employee employee)
        {
            if (employee == null)
            {
                _logger.LogError("Attempt to create a null employee.");
                throw new ArgumentNullException(nameof(employee), "Employee cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(employee.Id))
            {
                employee.Id = Guid.NewGuid().ToString();
                _logger.LogInformation("Generated new Employee with Id: {EmployeeId}", employee.Id);
            }

            if (employee.Department == null)
            {
                _logger.LogError("Employee with Id {EmployeeId} has null Department.", employee.Id);
                throw new ArgumentException("Department is required.", nameof(employee.Department));
            }

            if (string.IsNullOrWhiteSpace(employee.Department.DepartmentId))
            {
                _logger.LogError("Employee with Id {EmployeeId} has an invalid or missing DepartmentId.", employee.Id);
                throw new ArgumentException("DepartmentId is required.", "employee.Department.DepartmentId");
            }

            try
            {
                _logger.LogInformation("Creating an employee with Id {EmployeeId} in department {DepartmentId}.", employee.Id, employee.Department.DepartmentId);
                ItemResponse<Employee> response = await _Container.CreateItemAsync(employee, new PartitionKey(employee.Department.DepartmentId));
                _logger.LogInformation("Employee with Id {EmployeeId} successfully created in department {DepartmentId}.", employee.Id, employee.Department.DepartmentId);
                return response.Resource;
            }
            catch (CosmosException cosmosEx)
            {
                _logger.LogError(cosmosEx, "CosmosException when creating employee with Id {EmployeeId} in department {DepartmentId}. Status Code: {StatusCode}",
                    employee.Id, employee.Department.DepartmentId, cosmosEx.StatusCode);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating employee with Id {EmployeeId}.", employee.Id);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing employee record.
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        public async Task<Employee> UpdateEmployeeAsync(Employee employee)
        {
            if (employee == null)
            {
                _logger.LogError("Attempt to update a null employee.");
                throw new ArgumentNullException(nameof(employee), "Employee cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(employee.Id))
            {
                _logger.LogError("Employee Id is required for update.");
                throw new ArgumentException("Employee Id is required.", nameof(employee.Id));
            }

            if (employee.Department == null || string.IsNullOrWhiteSpace(employee.Department.DepartmentId))
            {
                _logger.LogError("Valid Department information is required for updating employee with Id {EmployeeId}.", employee.Id);
                throw new ArgumentException("Valid Department information is required.", nameof(employee.Department));
            }

            try
            {
                _logger.LogInformation("Attempting to update employee record with Id '{EmployeeId}' in department '{DepartmentId}'.", employee.Id, employee.Department.DepartmentId);
                ItemResponse<Employee> response = await _Container.ReplaceItemAsync(employee, employee.Id, new PartitionKey(employee.Department.DepartmentId));
                _logger.LogInformation("Successfully updated employee record with Id '{EmployeeId}'.", employee.Id);
                return response.Resource;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error updating employee record with Id '{EmployeeId}'.", employee.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating employee record with Id '{EmployeeId}'.", employee.Id);
                throw;
            }
        }

        /// <summary>
        /// Deletes an employee record.
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="departmentId"></param>
        public async Task DeleteEmployeeAsync(string employeeId, string departmentId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                _logger.LogError("Employee Id cannot be null or empty for deletion.");
                throw new ArgumentNullException(nameof(employeeId), "Employee Id is required.");
            }

            if (string.IsNullOrWhiteSpace(departmentId))
            {
                _logger.LogError("Department Id cannot be null or empty for deletion.");
                throw new ArgumentNullException(nameof(departmentId), "Department Id is required.");
            }

            try
            {
                _logger.LogInformation("Attempting to delete employee record with Id '{EmployeeId}' from department '{DepartmentId}'.", employeeId, departmentId);
                await _Container.DeleteItemAsync<Employee>(employeeId, new PartitionKey(departmentId));
                _logger.LogInformation("Successfully deleted employee record with Id '{EmployeeId}'.", employeeId);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Employee record with Id '{EmployeeId}' not found for deletion in department '{DepartmentId}'.", employeeId, departmentId);
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error deleting employee record with Id '{EmployeeId}'.", employeeId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting employee record with Id '{EmployeeId}'.", employeeId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all employees within a given department.
        /// </summary>
        /// <param name="departmentId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(string departmentId)
        {
            if (string.IsNullOrWhiteSpace(departmentId))
            {
                _logger.LogError("Department Id cannot be null or empty when querying employees.");
                throw new ArgumentNullException(nameof(departmentId), "Department Id is required.");
            }

            try
            {
                _logger.LogInformation("Querying employees in department '{DepartmentId}'.", departmentId);
                var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.Department.DepartmentId = @departmentId")
                    .WithParameter("@departmentId", departmentId);

                List<Employee> employees = new List<Employee>();
                FeedIterator<Employee> resultSet = _Container.GetItemQueryIterator<Employee>(queryDefinition, requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(departmentId)
                });

                while (resultSet.HasMoreResults)
                {
                    FeedResponse<Employee> response = await resultSet.ReadNextAsync();
                    employees.AddRange(response.ToList());
                }

                _logger.LogInformation("Retrieved {Count} employee(s) from department '{DepartmentId}'.", employees.Count, departmentId);
                return employees;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error querying employees in department '{DepartmentId}'.", departmentId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while querying employees in department '{DepartmentId}'.", departmentId);
                throw;
            }
        }
    }
}
