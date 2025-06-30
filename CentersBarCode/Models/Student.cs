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
    public string StudentId { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentGroup { get; set; } = string.Empty;
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
}