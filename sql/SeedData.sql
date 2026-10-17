USE EmployeeManagement;
GO

INSERT INTO dbo.Departments (Name)
SELECT v.Name FROM (VALUES (N'Engineering'), (N'Human Resources'), (N'Sales'), (N'Finance'), (N'Marketing'), (N'IT Support'), (N'Operations')) v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Departments d WHERE d.Name = v.Name);
GO

INSERT INTO dbo.Employees (FirstName, LastName, Email, HireDate, DepartmentId)
SELECT v.FirstName, v.LastName, v.Email, v.HireDate,
	   (SELECT Id FROM dbo.Departments WHERE Name = v.Dept)
FROM (VALUES
	(N'John',   N'Smith',   N'john.smith@example.com',    CAST('2020-03-01' AS DATE),  N'Engineering'  ),
	(N'Emily',  N'Johnson', N'emily.johnson@example.com', CAST('2018-07-15' AS DATE), N'Engineering'     ),
	(N'Michael', N'Brown',  N'michael.brown@example.com', CAST('2021-01-10' AS DATE), N'Human Resources'),
	(N'Sarah',  N'Davis',   N'sarah.davis@example.com',   CAST('2022-05-23' AS DATE), N'Sales'          ),
	(N'David',  N'Wilson',  NULL,                         CAST('2019-11-04' AS DATE), N'Finance'        ),
	(N'Karen',  N'Lewis',   N'karen.lewis@example.com',   CAST('2017-02-20' AS DATE), N'Marketing'      ),
	(N'Peter',  N'Walker',  N'peter.walker@example.com',  CAST('2016-09-12' AS DATE), N'Operations'     )
) v(FirstName, LastName, Email, HireDate, Dept)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Employees e WHERE e.FirstName = v.FirstName AND e.LastName = v.LastName);
GO
