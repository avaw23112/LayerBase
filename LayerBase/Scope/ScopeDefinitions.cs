namespace LayerBase.Scope;

public interface IScopeDefinition
{
    ScopeOptions Options { get; }
}

public sealed class MainScope : IScopeDefinition
{
    public const int ScopeId = 0;

    public ScopeOptions Options => ScopeOptions.Main;
}

internal static class ScopeDefinitionIds
{
    public const int Main = MainScope.ScopeId;

    public const string MainIdentity =
        "scope:LayerBase:LayerBase.Scope.MainScope";

    public static int FromType(Type scopeType)
    {
        if (scopeType == typeof(MainScope))
            return Main;

        if (!typeof(IScopeDefinition).IsAssignableFrom(scopeType))
            throw new InvalidOperationException(
                $"Scope type `{scopeType.FullName}` must implement {nameof(IScopeDefinition)}.");

        var field = scopeType.GetField("ScopeId");
        if (field != null && field.FieldType == typeof(int) && field.IsStatic)
            return (int)field.GetValue(null)!;

        string identity = BuildFallbackIdentity(scopeType);
        return ComputeScopeId(identity);
    }

    private static int ComputeScopeId(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
            throw new ArgumentException("Scope identity is required.", nameof(identity));

        for (int attempt = 0; attempt < 32; attempt++)
        {
            string candidate = attempt == 0
                ? identity
                : identity + "#" + attempt.ToString(System.Globalization.CultureInfo.InvariantCulture);

            byte[] input = System.Text.Encoding.UTF8.GetBytes(candidate);
            byte[] digest;

            using (var sha256 = System.Security.Cryptography.SHA256.Create())
                digest = sha256.ComputeHash(input);

            int scopeId =
                ((digest[0] & 0x7F) << 24) |
                (digest[1] << 16) |
                (digest[2] << 8) |
                digest[3];

            if (scopeId != 0)
                return scopeId;
        }

        throw new InvalidOperationException(
            $"Unable to derive a non-zero Scope ID for identity '{identity}'.");
    }

    private static string BuildFallbackIdentity(Type scopeType)
    {
        var identityAttr = scopeType.GetCustomAttributes(typeof(ScopeIdentityAttribute), false);
        if (identityAttr.Length > 0 && identityAttr[0] is ScopeIdentityAttribute sia)
            return "scope-key:" + sia.Value;

        return $"scope:{scopeType.Assembly.GetName().Name}:{scopeType.FullName}";
    }
}
