using SQLite;

namespace CentersBarCode.Models;

[Table("StudentsTable")]
public class Student
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public string StudentCode { get; set; } = string.Empty;
    
    public string StudentName { get; set; } = string.Empty;
    public Guid? StudentGroupId { get; set; } 
    public string StudentGroupName { get; set; } = string.Empty;
    
    public DateTime CreatedOn { get; set; }
    
    public string PhoneNumber { get; set; } = string.Empty;
    
    public string ParentPhone1 { get; set; } = string.Empty;
    
    public string ParentPhone2 { get; set; } = string.Empty;

    public DateTime? LastAttendance { get; set; }

    public decimal? PaymentValue { get; set; }

    public Student()
    {
        CreatedOn = DateTime.Now;
    }
}

// DTO for API response
public class StudentApiResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ParentPhone1 { get; set; } = string.Empty;
    public string ParentPhone2 { get; set; } = string.Empty;
    public DateTime? LastAttendance { get; set; }
    public decimal? PaymentValue { get; set; }

}

// Configuration model for API settings
public class ApiConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ImportDataEndpoint { get; set; } = string.Empty;
    public string ExportDataEndPoint { get; set; } = string.Empty;
    public string AuthenticationEndpoint { get; set; } = string.Empty;
}

public class DataAndCountDto<T>
{
    public List<T> Data { get; set; } = new();
    public int Count { get; set; }
}

public class Result
{
    public bool IsSuccess { get; set; }
    public Error Error { get; set; } = new();
}

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public Error Error { get; set; } = new();
    public T Value { get; set; } = default!;
}

public class Error
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ValidateAuthenticationResponse
{
    public string FullName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
}

public class ExportStudentAttendanceResponse
{
    public List<Guid> InsertedLocalIdArr { get; set; } = new();
}
