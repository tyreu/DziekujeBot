using DziekujeBot.AppData;
using DziekujeBot.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DziekujeBot.Services
{
    public class DziekujeService : IDziekujeService
    {
        private IWebDriver driver = null;
        private ChromeDriverService driverService;
        public TelegramBotClient Bot { get; set; }
        private BotData.Account? SelectedAccount { get; set; }

        public DziekujeService()
        {
            driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;
        }

        public async Task OpenLink(string? link, string? account, long chatId, Ad ad)
        {
            var accountData = account?.Split("/");
            SelectedAccount = BotData.Accounts.FirstOrDefault(acc => acc.Name == accountData?[0] && acc.Email == accountData?[1]);
            driver ??= new ChromeDriver(driverService);

            driver.Manage().Window.Maximize();
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(5);
            driver.Navigate().GoToUrl("https://www.stoteledekui.lt/lt/skelbimas/44549");
            Thread.Sleep(2500);

            try
            {
                var isAvailable = driver.FindElement(By.CssSelector("div.product-preview span.label")).Text == "Laisva";

                if (isAvailable)
                {
                    var reservationButton = driver.FindElements(By.CssSelector("div.reserve-container button")).FirstOrDefault();
                    if (reservationButton is not null)
                    {
                        reservationButton.Click();//click on "Rezervuoti"
                        Thread.Sleep(500);
                        driver.FindElement(By.Id("recipient-name")).SendKeys(SelectedAccount?.Name);//fill Name
                        driver.FindElement(By.Id("recipient-email")).SendKeys(SelectedAccount?.Email);//fill E-mail
                        driver.FindElement(By.Id("defaultCheck1")).Click();//click on checkbox
                        driver.FindElement(By.Id("defaultCheck1")).Submit();//submit form
                        await Bot.SendTextMessageAsync(chatId, $"Товар {ad.Text} успешно забронирован! Подтверждение отправлено на {SelectedAccount?.Email}");
                    }
                }
            }
            catch (NoSuchElementException ex)
            {
                await Bot.SendTextMessageAsync(chatId, $"К сожалению, товар уже зарезервирован.\n{link}");
            }
        }

        public InlineKeyboardMarkup GetListAccount()
        {
            var accounts = (from account in BotData.Accounts
                            select InlineKeyboardButton.WithCallbackData(account.Name, $"{account.Name}/{account.Email}")).ToList();
            return new InlineKeyboardMarkup(accounts);
        }

        public async Task CreatePost(TelegramBotClient bot, Ad ad, long chatId)
        {
            try
            {
                List<InlineKeyboardButton> buttons = new() { InlineKeyboardButton.WithCallbackData("✅", ad.Link) };
                InlineKeyboardMarkup keyboard = new(buttons);
                await bot.SendPhotoAsync(chatId, InputFile.FromUri(ad.Image), caption: $"{ad.Date:f}\n{ad.Text}\n{ad.Link}", replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"Create post error: {ex.Message}");
            }
        }

        public async Task<string> TranslateAsync(string word, string fromLanguage = "lt", string toLanguage = "ru")
        {
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(word)}";
            var httpClient = new HttpClient();
            var result = await httpClient.GetStringAsync(url);
            try
            {
                return result[4..result.IndexOf("\"", 4, StringComparison.Ordinal)];
            }
            catch
            {
                return word;
            }
        }

        public async Task SelectAccount(TelegramBotClient bot, Update update, long chatId, CancellationToken cancellationToken)
        {
            await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Выберите аккаунт для резервации", cancellationToken: cancellationToken);
            await bot.EditMessageReplyMarkupAsync(chatId, update.CallbackQuery.Message.MessageId, GetListAccount(), cancellationToken);
        }
    }
}
