using SQLite;

namespace CentersBarCode.Models;

[Table("Groups")]
public class Group
{
    [PrimaryKey]
    public Guid Id { get; set; } 
    
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedOn { get; set; }

    public Group() { 
        CreatedOn = DateTime.Now;
    }
    public Group(Guid id, string name)
    {
        Id = id;
        Name = name;
        CreatedOn = DateTime.Now;
    }

    public override string ToString()
    {
        return Name;
    }
}

// DTO for Centers API response
public class GroupApiResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}