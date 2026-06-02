using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

public static class SessionExtensions
{
    private static readonly JsonSerializerSettings SessionJsonSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    public static void SetObjectAsJson(this ISession session, string key, object value)
    {
        session.SetString(key, JsonConvert.SerializeObject(value, SessionJsonSettings));
    }

    public static T GetObjectFromJson<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value, SessionJsonSettings);
    }
}
