using EmployeeManagement.Data.Data;

namespace EmployeeManagement.Data.Validation;

public static class EmployeeValidator
{
    public static EmployeeData NormalizeAndValidate(EmployeeData employee)
    {
        var normalizedEmployee = Normalize(employee);

        Validate(normalizedEmployee);

        return normalizedEmployee;
    }

    public static EmployeeData Normalize(EmployeeData employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        return employee with
        {
            FirstName = employee.FirstName?.Trim() ?? string.Empty,
            LastName = employee.LastName?.Trim() ?? string.Empty,
            Email = string.IsNullOrWhiteSpace(employee.Email) ? null : employee.Email.Trim()
        };
    }

    public static void Validate(EmployeeData employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        var firstNameLength = employee.FirstName?.Trim().Length ?? 0;
        var lastNameLength = employee.LastName?.Trim().Length ?? 0;
        var emailLength = employee.Email?.Trim().Length ?? 0;

        if (firstNameLength is 0 or > 100 || lastNameLength is 0 or > 100)
        {
            throw new ArgumentException("First and last names must contain 1 to 100 characters.");
        }

        if (emailLength > 200)
        {
            throw new ArgumentException("Email must not exceed 200 characters.");
        }

        if (employee.DepartmentId <= 0)
        {
            throw new ArgumentException("Select a department.");
        }
    }
}