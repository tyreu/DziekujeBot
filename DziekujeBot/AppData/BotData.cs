namespace DziekujeBot.AppData
{
    public static class BotData
    {
        public class Account
        {
            public string Name { get; set; }
            public string Email { get; set; }
        }

        public const string Token = "6208706189:AAGHAwnvRwVG-KYQYBN0feewxFOvMGMeNUQ";
        public const string TargetUrl = "https://www.stoteledekui.lt/lt/paieska?keywords=&category_id=&location_id=64";

        public static List<Account> Accounts { get; set; } = new List<Account>
        {
            new()
            {
                Email = "disipisd@gmail.com",
                Name = "Mykyta"
            }
        };
    }
}
