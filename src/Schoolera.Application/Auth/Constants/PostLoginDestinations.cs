namespace Schoolera.Application.Auth.Constants;

public static class PostLoginDestinations
{
    public const string Parent = "/parent";
    public const string School = "/school";
    public const string Admin = "/admin";
    public const string Support = "/support";

    public static string ForRoles(IEnumerable<string> roles)
    {
        var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roleSet.Contains(SchooleraRoles.PlatformAdmin))
        {
            return Admin;
        }

        if (roleSet.Contains(SchooleraRoles.SupportAgent))
        {
            return Support;
        }

        if (roleSet.Contains(SchooleraRoles.SchoolOwner) || roleSet.Contains(SchooleraRoles.SchoolAdmin))
        {
            return School;
        }

        return Parent;
    }
}
