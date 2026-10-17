namespace EmployeeManagement.Data.Data
{
    public record EmployeeData(int Id, string FirstName, string LastName, string? Email, DateOnly HireDate, int DepartmentId );
}
