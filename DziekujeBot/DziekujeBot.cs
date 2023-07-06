using AngleSharp;
using DziekujeBot.AppData;
using DziekujeBot.Models;
using DziekujeBot.Services;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DziekujeBot
{
    public class Dziekuje
    {
        private readonly TelegramBotClient Bot = new(BotData.Token);
        private readonly CancellationTokenSource cts = new();
        private long? ChatId = null;

        private delegate void DziekujeHandler();
        private event DziekujeHandler RunScan;

        private const int MAX_COUNT_ADS = 10;
        private const string REGEX_LINK = @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()!@:%_\+.~#?&\/\/=]*)";
        private static List<Ad> NewAds { get; set; } = new List<Ad>(10);
        private Ad? SelectedAd { get; set; } = null;

        private readonly IDziekujeService _dziekujeService;

        public Dziekuje(IDziekujeService dziekujeService)
        {
            _dziekujeService = dziekujeService;
            _dziekujeService.Bot = Bot;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            Bot.StartReceiving(updateHandler: HandleUpdateAsync,
                               pollingErrorHandler: HandlePollingErrorAsync,
                               receiverOptions: receiverOptions,
                               cancellationToken: cts.Token);
            RunScan += StartScanAds;
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.CallbackQuery is null)
            {
                //если прислали текстовое сообщение
                ChatId ??= update.Message?.Chat.Id;
                RunScan?.Invoke();
                return;
            }
            else
            {
                //если нажали на кнопку
                ChatId ??= update.CallbackQuery.Message?.Chat.Id;
                await (update.CallbackQuery.Data switch
                {
                    //если строка = ссылка, значит нажали на галочку и предоставляем выбор аккаунтов
                    { Length: > 0 } when Regex.IsMatch(update.CallbackQuery.Data, REGEX_LINK)
                        => _dziekujeService.SelectAccount(Bot, update, ChatId.Value, cancellationToken),
                    //если это строка-аккаунт, открываем браузер
                    { Length: > 0 }
                        => _dziekujeService.OpenLink(SelectedAd?.Link, update.CallbackQuery.Data, ChatId.Value, SelectedAd),
                    "" or null or _
                        => Task.CompletedTask
                });

                if (Regex.IsMatch(update.CallbackQuery.Data, REGEX_LINK))
                {
                    SelectedAd = NewAds.FirstOrDefault(ad => ad.Link == update.CallbackQuery.Data);
                }
            }
        }

        private async void StartScanAds()
        {
            while (true)
            {
                try
                {
                    //скачать html страницы
                    var config = Configuration.Default.WithDefaultLoader();
                    using var context = BrowsingContext.New(config);
                    using var document = await context.OpenAsync(BotData.TargetUrl);
                    //получить элементы списка объявлений
                    var ads = document.QuerySelectorAll($"div.product-list__item:nth-child(-n+{MAX_COUNT_ADS}) a").OrderBy(x => DateTime.Parse(x.Children[1].TextContent));
                    //цикл по первым MAX_COUNT_ADS объявлениям
                    foreach (var ad in ads)
                    {
                        var date = DateTime.Parse(ad.Children[1].TextContent);
                        var text = await _dziekujeService.TranslateAsync(ad.Children[2].TextContent.Split("\n")[1]);
                        var image = Regex.Match(ad.InnerHtml, REGEX_LINK).Value; 
                        var link = ad.Attributes["href"].Value;
                        //если текст ИЛИ время объявления не совпадает ни с одним, создать сообщение и отправить его
                        if (NewAds.All(ad => ad.Link != link))
                        {
                            var newAd = new Ad(text, date, link, image);
                            NewAds.Add(newAd);
                            await _dziekujeService.CreatePost(Bot, newAd, ChatId.Value);
                        }
                    }
                    NewAds = NewAds.OrderByDescending(ad => ad.Date).Take(MAX_COUNT_ADS).ToList();
                    RunScan -= new DziekujeHandler(delegate { });
                    Thread.Sleep(5000);
                }
                catch (Exception ex) { Console.WriteLine($"{ex.StackTrace}\n{ex.Message}"); continue; }
            }
        }
    }
}
