namespace BtPlatform.Core.Entities;

public class DirectoryGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    public List<DirectoryUser> Members { get; set; } = new();
}