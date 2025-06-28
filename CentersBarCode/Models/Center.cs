using SQLite;

namespace CentersBarCode.Models;

public class Center
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public Center()
    {
        Id = Guid.NewGuid();
    }

    public Center(string name) : this()
    {
        Name = name;
    }

    public override string ToString()
    {
        return Name;
    }
}