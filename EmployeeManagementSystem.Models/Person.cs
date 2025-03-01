using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace EmployeeManagementSystem.Models
{

    /// <summary>
    /// Base class Person representing unique characteristics common to all persons (First Name, last name and a Unique ID)
    /// </summary>
    public class Person
    {

        //The first name and last name will be made private  fields which will be exposed through a getter and setter to demonstrate encapsulation in OOP

        private string? _FirstName;
        private string? _LastName;

        //Unique Identifier for each person, this can be used to aggregate records for different classes common to that person

        [JsonProperty(PropertyName = "id")]
        public required string Id { get; set; }

        public string FirstName
        {
            get => _FirstName ?? string.Empty;
            set
            {
                //A simple Check if first name is not null, greater than 1 letter  before setting the value 
               ValidateName(value);
                _FirstName = value;



            }
        }

       

        public string LastName
        {
            get => _LastName ?? string.Empty;
            set
            {
              ValidateName(value);
                _LastName = value;
            }
        }
       

       public string GetFullName()
        {
            return this.FirstName+ " "+this.LastName;
        }



        /// <summary>
        /// This method was created to demonstrate the use of polymorphism as stated in the requirement. Derived classes can override this method.
        /// </summary>
        /// <returns></returns>
        public virtual string getRecordDescription()
        {
            return "Person";
        }


        /// <summary>
        /// Validates names to ensure it is not null or whitespace and contains 2 or more characters.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length < 2)
            {
                throw new ArgumentException("First Name must be at least 2 characters long.");
            }
           
        }


      

    }
}
