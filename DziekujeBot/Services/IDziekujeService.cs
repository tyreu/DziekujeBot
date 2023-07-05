using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using DziekujeBot.Models;

namespace DziekujeBot.Services
{
    public interface IDziekujeService
    {
        public TelegramBotClient Bot { get; set; }

        Task CreatePost(TelegramBotClient bot, Ad ad, long chatId);
        InlineKeyboardMarkup GetListAccount();
        Task OpenLink(string? link, string? account, long chatId, Ad ad);
        Task<string> TranslateAsync(string word, string fromLanguage = "lt", string toLanguage = "ru");
        Task SelectAccount(TelegramBotClient bot, Update update, long chatId, CancellationToken cancellationToken);
    }
}
