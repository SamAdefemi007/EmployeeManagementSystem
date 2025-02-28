using EmployeeManagementSystem.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace EmployeeManagementSystem.DataAccess
{
    /// <summary>
    /// Data Access repository for managing Employee Data in Cosmos DB
    /// </summary>
    public class EmployeeManagementRepository
    {
        private readonly CosmosClient _CosmosClient;
        private Container _Container;
        private readonly ILogger<EmployeeManagementRepository> _logger;
        

        public EmployeeManagementRepository( string connectionString, ILogger<EmployeeManagementRepository> logger)
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
       /// 
       /// </summary>
       /// <param name="databaseName"></param>
       /// <param name="containerName"></param>
       /// <param name="partitionKey"></param>
       /// <returns></returns>
       /// <exception cref="ArgumentNullException"></exception>
        public async Task InitializeConnectionAsync(string databaseName, string containerName, string partitionKey)
        {
            //Error handling to check that the database name, container name and partition key have all been provided
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentNullException("Database name cannot be null or empty.", nameof(databaseName));
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentNullException("Container name cannot be null or empty.", nameof(containerName));
            if (string.IsNullOrWhiteSpace(partitionKey))
                throw new ArgumentNullException("Partition key path cannot be null or empty.", nameof(partitionKey));

            // Create the database if it doesn't exist.
            DatabaseResponse databaseResponse= await _CosmosClient.CreateDatabaseIfNotExistsAsync(id:databaseName);
            _logger.LogInformation("Database '{DatabaseName}' verified/created.", databaseName);

            // Create the container if it doesn't exist.
            ContainerResponse containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(id:containerName, partitionKeyPath: partitionKey, throughput:400);

            _Container = containerResponse.Container;

            _logger.LogInformation("Container '{ContainerName}' verified/created with partition key '{PartitionKey}'.", containerName, partitionKey);
        }




        /// <summary>
        /// Repository method to get Employees from the cosmos db. 
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
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
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
        /// 
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>

        public async Task<Employee> CreateEmployeeAsync(Employee employee)
        {
            // Validate the employee object.
            if (employee == null)
            {
                _logger.LogError("Attempt to create a null employee.");
                throw new ArgumentNullException(nameof(employee), "Employee cannot be null.");
            }

            // Generate a new unique identifier if not provided.
            if (string.IsNullOrWhiteSpace(employee.Id))
            {
                employee.Id= Guid.NewGuid().ToString();
              
                _logger.LogInformation("Generated new Person with Id: {EmployeeId}", employee.Id);
            }

            // Validate the employee's Department details.
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
                _logger.LogInformation("Creating an employee with Id {EmployeeId}  in department {DepartmentId}.", employee.Id, employee.Department.DepartmentId);

               // Create the employee record in Cosmos DB, using the DepartmentId as the partition key.
               ItemResponse <Employee> response = await _Container.CreateItemAsync(
                    employee,
                    new PartitionKey(employee.Department.DepartmentId)
                );

                _logger.LogInformation("Employee with Id {EmployeeId} successfully created in department {DepartmentId}.",
                    employee.Id, employee.Department.DepartmentId);

                return response.Resource;
            }
            catch (CosmosException cosmosEx)
            {
                // Log Cosmos-specific exceptions with status code information.
                _logger.LogError(cosmosEx, "CosmosException when creating employee with Id {EmployeeId} in department {DepartmentId}. Status Code: {StatusCode}",
                    employee.Id, employee.Department.DepartmentId, cosmosEx.StatusCode);
                // Depending on your application's needs, you could implement retries here.
                throw;
            }
            catch (Exception ex)
            {
                // Logging any other unexpected exceptions.
                _logger.LogError(ex, "Unexpected error while creating employee with Id {EmployeeId}.", employee.Id);
                throw;
            }
        }





    }
}
