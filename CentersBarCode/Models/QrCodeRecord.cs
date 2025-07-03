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
        CreatedDateUtc = DateTime.UtcNow;
    }

    public QrCodeRecord(Guid centerId, string code) : this()
    {
        CenterId = centerId;
        Code = code;
    }
}