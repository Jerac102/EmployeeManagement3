namespace EmployeeManagement.Data.Data;

public record EmployeeChanges(
    IReadOnlyList<EmployeeData> NewEmployees,
    IReadOnlyList<EmployeeData> ModifiedEmployees,
    IReadOnlyList<int> DeletedEmployeeIds);