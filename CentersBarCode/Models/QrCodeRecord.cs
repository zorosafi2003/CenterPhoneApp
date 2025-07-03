using SQLite;

namespace CentersBarCode.Models;

[Table("StudentAttendanceRecords")]
public class QrCodeRecord
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public Guid CenterId { get; set; }
    
    public string Code { get; set; } = string.Empty;

    public Guid? StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;

    public DateTime CreatedDateUtc { get; set; }

    public QrCodeRecord()
    {
        CreatedDateUtc = DateTime.Now;
    }

    public QrCodeRecord(Guid centerId, string code) : this()
    {
        CenterId = centerId;
        Code = code;
    }
}



// DTO for API response
public class CreateStudentAttendanceRequest
{
    public List<DataChildOfCreateStudentAttendanceRequest> Data { get; set; }
}

public class DataChildOfCreateStudentAttendanceRequest
{
    public Guid? StudentId { get; set; }
    public string StudentCode { get; set; }
    public Guid CenterId { get; set; }
    public Guid LocalId { get; set; }
    public DateTime CreateDate { get; set; }
}