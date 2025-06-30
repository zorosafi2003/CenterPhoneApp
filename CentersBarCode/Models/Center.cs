using SQLite;

namespace CentersBarCode.Models;

[Table("Centers")]
public class Center
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedOn { get; set; }

    public Center()
    {
        Id = string.Empty;
        CreatedOn = DateTime.UtcNow;
    }

    public Center(string id, string name) : this()
    {
        Id = id;
        Name = name;
    }

    public override string ToString()
    {
        return Name;
    }
}

// DTO for Centers API response
public class CenterApiResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}