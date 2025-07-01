using SQLite;

namespace CentersBarCode.Models;

[Table("Students")]
public class Student
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public string StudentId { get; set; } = string.Empty;
    
    public string StudentCode { get; set; } = string.Empty;
    
    public string StudentName { get; set; } = string.Empty;
    
    public string StudentGroup { get; set; } = string.Empty;
    
    public DateTime CreatedOn { get; set; }
    
    public string PhoneNumber { get; set; } = string.Empty;
    
    public string ParentPhone1 { get; set; } = string.Empty;
    
    public string ParentPhone2 { get; set; } = string.Empty;

    public Student()
    {
        CreatedOn = DateTime.UtcNow;
    }
}

// DTO for API response
public class StudentApiResponse
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ParentPhone1 { get; set; } = string.Empty;
    public string ParentPhone2 { get; set; } = string.Empty;
}

// Configuration model for API settings
public class ApiConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string StudentsEndpoint { get; set; } = string.Empty;
    public string CentersEndpoint { get; set; } = string.Empty;
    public string AuthenticationEndpoint { get; set; } = string.Empty;
}

public class DataAndCountDto<T>
{
    public List<T> Data { get; set; }
    public int Count { get; set; }
}

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public Error Error { get; set; }
    public T Value { get; set; }
}

public class Error
{
    public string Code { get; set; }
    public string Description { get; set; }
}

public class ValidateAuthenticationResponse
{
    public string FullName { get; set; }
    public string Token { get; set; }
    public string TeacherName { get; set; }
}