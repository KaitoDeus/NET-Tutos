namespace NET_Tutos.Gamification.Application.Common.Exceptions;

public class GamificationNotFoundException : Exception
{
    public string EntityName { get; }
    public object Key { get; }

    public GamificationNotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key ({key}) was not found.")
    {
        EntityName = entityName;
        Key = key;
    }
}
