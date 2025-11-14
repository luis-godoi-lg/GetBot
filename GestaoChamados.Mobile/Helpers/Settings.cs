namespace GestaoChamados.Mobile.Helpers;

public static class Settings
{
    private const string TokenKey = "auth_token";
    private const string UserEmailKey = "user_email";
    private const string UserNameKey = "user_name";
    private const string UserRoleKey = "user_role";
    
    // API Base URL - Usar HTTP para evitar problemas de certificado em desenvolvimento
    public const string ApiBaseUrl = "http://localhost:5142";

    public static string Token
    {
        get => Preferences.Get(TokenKey, string.Empty);
        set => Preferences.Set(TokenKey, value);
    }

    public static string UserEmail
    {
        get => Preferences.Get(UserEmailKey, string.Empty);
        set => Preferences.Set(UserEmailKey, value);
    }

    public static string UserName
    {
        get => Preferences.Get(UserNameKey, string.Empty);
        set => Preferences.Set(UserNameKey, value);
    }

    public static string UserRole
    {
        get => Preferences.Get(UserRoleKey, string.Empty);
        set => Preferences.Set(UserRoleKey, value);
    }

    public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public static bool IsTecnico => UserRole == "Tecnico" || UserRole == "Gerente" || UserRole == "Admin";

    public static bool IsAdmin => UserRole == "Admin" || UserRole == "Gerente";

    public static void ClearAll()
    {
        Preferences.Clear();
    }
}
