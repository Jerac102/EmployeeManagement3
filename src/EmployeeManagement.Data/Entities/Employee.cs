namespace EmployeeManagement.Data.Entities;

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateOnly HireDate { get; set; }

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
}
