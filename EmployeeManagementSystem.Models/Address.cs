using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeManagementSystem.Models
{
    public class Address
    {
        public required string Street { get; set; }
        public required string City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
    }
}
