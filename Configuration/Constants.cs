namespace CStafford.MoneyTree.Configuration;

public class Constants
{
    public static bool LogSimulation = false;
    public static decimal FeeRate = 0.00417m;
    public static DateTime Epoch = new DateTime(2019, 9, 17);
    public static string ConnectionString = $"Server=127.0.0.1;Port=3306;Database=MoneyTree2;User=root;Password=qwe123;SSL Mode=None;AllowPublicKeyRetrieval=True;default command timeout=0;";
}
