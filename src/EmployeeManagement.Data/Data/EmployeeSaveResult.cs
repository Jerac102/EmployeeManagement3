namespace EmployeeManagement.Data.Data;

public record EmployeeSaveResult(
    IReadOnlyList<EmployeeData> NewEmployees,
    IReadOnlyList<EmployeeData> ModifiedEmployees);