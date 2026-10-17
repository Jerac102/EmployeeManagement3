using EmployeeManagement.Data.Entities;

namespace EmployeeManagament.Data.Test.Data
{
    internal class TestData
    {
        // create test data in constructor
        internal TestData()
        {
            Employees = new List<Employee>
            {
                new Employee
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@example.com"
                },
                new Employee
                {
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@example.com"
                },
                new Employee
                {
                    FirstName = "Alice",
                    LastName = "Johnson",
                    Email = "alice.johnson@example.com"
                }
            };

            Departments = new List<Department>
            {
                new Department
                {
                    Name = "Human Resources"
                },
                new Department
                {
                    Name = "Finance"
                },
                new Department
                {
                    Name = "Engineering"
                }
            };
        }

        internal List<Employee> Employees { get; }
        internal List<Department> Departments { get; }
    }
}
