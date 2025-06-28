using SQLite;

namespace CentersBarCode.Models;

[Table("QrCodeRecords")]
public class QrCodeRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public Guid CenterId { get; set; }
    
    public string Code { get; set; } = string.Empty;
    
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