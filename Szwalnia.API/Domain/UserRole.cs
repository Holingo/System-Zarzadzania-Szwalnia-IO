namespace Szwalnia.API;

public static class UserRole
{
    public const string Admin = "Admin";
    public const string Cutting = "CuttingWorker";
    public const string Sewing = "SewingWorker";

    public const string CuttingAccess = Admin + "," + Cutting;
    public const string SewingAccess = Admin + "," + Sewing;
    public const string All = Admin + "," + Cutting + "," + Sewing;

    public const string ProductionPattern = "^(" + Cutting + "|" + Sewing + ")$";
    public const string AllPattern = "^(" + Admin + "|" + Cutting + "|" + Sewing + ")$";
}
