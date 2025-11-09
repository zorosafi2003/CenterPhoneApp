using SQLite;

namespace CentersBarCode.Models;

[Table("ExamsTbl")]
public class Exam
{
    [PrimaryKey]
    public Guid Id { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public Guid StudentId { get; set; }
    
    public string StudentCode { get; set; } = string.Empty;
    
    public string StudentName { get; set; } = string.Empty;
    
    public decimal? Degree { get; set; }
    
    public decimal? TotalDegree { get; set; }
    
    public string Notes { get; set; } = string.Empty;
    
    public bool NotHaveDuties { get; set; }

    public Exam()
    {
        Id = Guid.NewGuid();
        CreatedDate = DateTime.Now;
    }
}
