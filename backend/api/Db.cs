namespace Company.Function;

/// <summary>Cosmos DB names and the connection app-setting key, in one place.</summary>
internal static class Db
{
    public const string ConnectionSetting = "CloudResume";
    public const string Database = "CloudResume";
    public const string CounterContainer = "Counter";
    public const string MessagesContainer = "Messages";
    public const string CounterId = "index";
}
