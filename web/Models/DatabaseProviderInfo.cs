namespace NET_Tutos.Models;

public class DatabaseProviderInfo
{
    public string Name { get; set; } = "SQL Server";
    public string NameEn { get; set; } = "SQL Server";

    public string GetDisplayName(bool isEn) => isEn ? (string.IsNullOrEmpty(NameEn) ? Name : NameEn) : Name;
}

