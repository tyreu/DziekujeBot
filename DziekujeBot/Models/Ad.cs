namespace DziekujeBot.Models
{
    public class Ad
    {
        public Ad(string text, DateTime date, string link, string image)
        {
            Text = text;
            Date = date;
            Link = link;
            Image = image;
        }

        public string Text { get; set; }
        public DateTime Date { get; set; }
        public string Link { get; set; }
        public string Image { get; set; }
    }
}
