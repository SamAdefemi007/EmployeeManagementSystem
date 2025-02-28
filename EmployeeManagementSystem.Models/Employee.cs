using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeManagementSystem.Models
{
    /// <summary>
    /// Employee class inheriting from the Person base class containing fields (PersonId, FirstName, LastName) and Virtual Method(getRoleDescription)
    /// For the class, the EmployeeId and Department is required because the Database partition Key will be on the department. The relationship between the 
    /// employee class and department and address class is composition (Has-a) relationship while Employee and person class represent the Inheritance (Is-a) relationship
    /// </summary>
    public class Employee :Person
    {
        public required int EmployeeId { get; set; }
        
        

        //Position can be an enum if we have a set of valid positions, but It's been kept as a string for flexibility
        public string Position { get; set; } = string.Empty;

        public required Department Department { get; set; }

        public Address? Address { get; set; }


       //Method overriding to demonstrate polymorphism
        public override string getRecordDescription()
        {
            return "Employee";
        }


    }
}
