using EmployeeManagement.Data.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EmployeeManagement.Shared.Models
{
    public class EmployeeRow : INotifyPropertyChanged
    {
        private EmployeeData? original;
        private int id;
        private string firstName = string.Empty;
        private string lastName = string.Empty;
        private string? email;
        private DateOnly hireDate = DateOnly.FromDateTime(DateTime.Today);
        private int departmentId;
        private bool isMarkedForDelete;

        public int Id { get => id; private set => Set(ref id, value); }
        public string FirstName { get => firstName; set => Set(ref firstName, value); }
        public string LastName { get => lastName; set => Set(ref lastName, value); }
        public string? Email { get => email; set => Set(ref email, value); }
        public DateOnly HireDate { get => hireDate; set => Set(ref hireDate, value); }
        public int DepartmentId { get => departmentId; set => Set(ref departmentId, value); }
        public bool IsMarkedForDelete { get => isMarkedForDelete; set => Set(ref isMarkedForDelete, value); }
        public bool IsNew => Id == 0;
        public bool HasChanges => IsMarkedForDelete || original is null || ToData() != original;
        public event PropertyChangedEventHandler? PropertyChanged;

        public EmployeeData ToData() => new(Id, FirstName, LastName, Email, HireDate, DepartmentId);

        public static EmployeeRow FromData(EmployeeData data)
        {
            var row = new EmployeeRow();

            row.AcceptChanges(data);

            return row;
        }

        public void AcceptChanges(EmployeeData savedEmployee)
        {
            Id = savedEmployee.Id;
            FirstName = savedEmployee.FirstName;
            LastName = savedEmployee.LastName;
            Email = savedEmployee.Email;
            HireDate = savedEmployee.HireDate;
            DepartmentId = savedEmployee.DepartmentId;

            IsMarkedForDelete = false;

            original = ToData();

            PropertyChanged?.Invoke(this, new(nameof(HasChanges)));
            PropertyChanged?.Invoke(this, new(nameof(IsNew)));
        }

        public void DiscardChanges()
        {
            if (original is not null)
            {
                AcceptChanges(original);
            }
        }

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new(name));
            PropertyChanged?.Invoke(this, new(nameof(HasChanges)));
        }
    }
}
