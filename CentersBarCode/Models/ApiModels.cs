namespace CentersBarCode.Models;

// Phone App API Models - GET /centers-api/phone-app/data
public class GetDataPhoneAppQueryResult
{
    public List<CenterBaseDto> Centers { get; set; } = new();
    public List<StudentWithLastAttendanceDto> Students { get; set; } = new();
    public List<GroupBaseDto> Groups { get; set; } = new();
}

public class CenterBaseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class StudentWithLastAttendanceDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ParentPhone1 { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime? LastAttendance { get; set; }
    public decimal? PaymentValue { get; set; }
}

public class GroupBaseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Phone App API Models - POST /centers-api/phone-app/data
public class SetDataPhoneAppCommandRequest
{
    public List<AttendanceChildOfSetDataPhoneAppCommandRequest> AttendanceData { get; set; } = new();
    public List<ExamChildOfSetDataPhoneAppCommandRequest> ExamData { get; set; } = new();
}

public class AttendanceChildOfSetDataPhoneAppCommandRequest
{
    public Guid? StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public Guid CenterId { get; set; }
    public Guid LocalId { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class ExamChildOfSetDataPhoneAppCommandRequest
{
    public Guid StudentId { get; set; }
    public Guid? GroupId { get; set; }
    public decimal? FinalDegree { get; set; }
    public decimal? StudentDegree { get; set; }
    public string? Notes { get; set; } = string.Empty;
    public bool? IsNotDoDutties { get; set; }
    public Guid LocalId { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class SetDataPhoneAppCommandResult
{
    public List<Guid> AttendanceSavedLocalIds { get; set; } = new();
    public List<Guid> ExamSavedLocalIds { get; set; } = new();
}
