using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HtmlAgilityPack;
using System.Net;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace ParserConcertTour
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static WebClient wClient;
        public static string wClientHtml;
        public static string url = "http://www.e1.ru/afisha/events/gastroli/2016/07/13";

        public MainWindow()
        {
            InitializeComponent();

            Parse();
        }
        private void Parse()
        {
            wClient = new WebClient();

            wClient.Encoding = Encoding.GetEncoding(1251);
            wClientHtml = wClient.DownloadString(url);
            HtmlDocument html = new HtmlDocument();

            html.LoadHtml(wClient.DownloadString(url));
            List<MyTable> source = new List<MyTable>();
            grid.ItemsSource = source;
            HtmlNodeCollection nodes = html.DocumentNode.SelectNodes("//table");

            foreach (var node in nodes)
            {
                // Bad code
                if ((node.OuterHtml.IndexOf("#") != -1 || node.OuterHtml.IndexOf("big_orange") == -1) && node.OuterHtml.IndexOf("#F4F4F4") == -1 && node.OuterHtml.IndexOf("span style") == -1)
                    continue;
                if (node.OuterHtml.IndexOf("EAEAEA") != -1 || node.OuterHtml.IndexOf("FFFF") != -1)
                    continue;
                // Bad code
                if (node.OuterHtml.IndexOf("big_orange") != -1)
                {
                    string eventName = GetEventName(node.OuterHtml);
                    string eventDate = GetEventDate(wClientHtml, eventName);
                    Place eventPlace = GetEventPlace(wClientHtml, eventDate);

                    // TODO: в некоторых случаях выводит на одну запись больше
                    MyTable tour = new MyTable(eventDate, eventPlace.Name, eventPlace.Address, eventName);
                    source.Add(tour);
                }
            }
            for (int i = 0; i < source.Count; i++)
            {
                try
                {
                    if (source[i].EventName == source[i - 1].EventName)
                        source[i].EventDate = ReplaceDate(source[i-1].EventDate);
                }
                catch { }

            }

            grid.ItemsSource = source;
        }

        private void grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MyTable path = grid.SelectedItem as MyTable;
            try
            {
                statusBar.Content = path.EventPlace.Address;
            }
            catch
            {
                statusBar.Content = "Что-то пошло не так";
            }
        }

        private void inWeb_Click(object sender, RoutedEventArgs e)
        {
            MyTable path = grid.SelectedItem as MyTable;
            try
            {
                System.Diagnostics.Process.Start(GetWebSource(wClientHtml, path.EventDate));
            }
            catch
            {
                statusBar.Content = "Вы не выбрали представление";
            }
        }

        // TODO: не работает
        private void goUrl_Click(object sender, RoutedEventArgs e)
        {
            url = urlAddress.Text;
            Parse();
        }

        private string GetEventName(string line)
        {
            int startIndex = line.IndexOf("<b class=\"big_orange\">");
            int endIndex = line.IndexOf("</b><br>", startIndex);
            string eventName = DeleteTags(line.Substring(startIndex, endIndex - startIndex));

            return eventName;
        }

        private Place GetEventPlace(string html, string date)
        {
            html = html.Substring(html.IndexOf(date));

            string pattern = @"<td.+F4F4F4.+";
            Match match = Regex.Match(html, pattern);

            Place place = new Place();
            place.Name = DeleteTags(match.ToString());
            place.Address = GetPlaceAddress(html);

            return place;
        }

        private string GetEventDate(string html, string eventName)
        {
            string pattern = @"[<-]+\s\d+[-]\d+[-]\d+\s[->]+";
            MatchCollection matchs = Regex.Matches(html, pattern);

            for(int i=0; i < matchs.Count; i++)
            {
                try
                {
                    //TODO: Если Несколько одинаковых мероприятий подряд, то берет только первую дату
                    if (html.IndexOf(matchs[i].ToString()) <= html.IndexOf(eventName) && html.IndexOf(eventName) <= html.IndexOf(matchs[i + 1].ToString()))
                        return matchs[i].ToString().Substring(3, 10);
                }
                catch
                {
                    return matchs[i].ToString().Substring(3, 10);
                }
                
            }

            return "";
        }

        private string DeleteTags(string line)
        {
            while (line.IndexOf("<") != -1)
                try
                {
                    line = line.Replace(line.Substring(line.IndexOf("<"), line.IndexOf(">") - (line.IndexOf("<") - 1)), "");
                }
                catch
                {
                    line = line.Replace(line.Substring(line.IndexOf("<")), "");
                }
            return line.Trim();
        }

        private string ConvertDate(DateTime date)
        {
            return date.ToString("<!-- yyyy-MM-dd -->");
        }

        private string ConvertDate(int y, int m, int d)
        {
            string day = d.ToString(), month = m.ToString();
            if (day.Length == 1)
                day = "0" + day;
            if (month.Length == 1)
                month = "0" + month;
            return String.Format("<!-- {0}-{1}-{2} -->", y, month, day);
        }

        // При наличии нескольких одинаковых записей дата берется только первая
        // Это своеобразный костыль
        private string ReplaceDate(string date)
        {
            string dayString = date.Substring(8, 2);
            string dateYearMonth = date.Substring(0, 8);
            int dayInt = Int32.Parse(dayString) +1;

            return dateYearMonth + dayInt.ToString();
        }

        private string GetPlaceAddress(string place)
        {
            string pattern = @"<span class=.small.+";
            Match match = Regex.Match(place, pattern,RegexOptions.Singleline);

            string t = DeleteTags(match.ToString().Substring(0, match.ToString().IndexOf("br")));
            while (t.IndexOf("  ") != -1)
                t = t.Replace("  ", " ");
            return t;
        }

        private string GetWebSource(string html, string date)
        {
            html = html.Substring(html.IndexOf(date));

            string pattern = @".afisha.events.gastroli.\d+";
            Match match = Regex.Match(html, pattern);

            return "http://e1.ru" + match.ToString();
        }

        

    }
}
