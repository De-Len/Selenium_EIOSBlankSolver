using System;
using System.Net.Mail;
using System.Net;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    private const int SPI_SETDESKWALLPAPER = 20;
    private const int SPIF_UPDATEINIFILE = 0x01 | 0x02;

    static async Task Main()
    {
        // Логин и пароль от ЭИОС
        string login = "login"; 
        string password = "password";
        // Укажите путь к chromedriver, если он не в PATH
        ChromeDriverService service = ChromeDriverService.CreateDefaultService();
        IWebDriver driver = new ChromeDriver(service);


        try
        {
            // Открываем страницу
            driver.Navigate().GoToUrl("https://eios.kemsu.ru/a/eios");
            Console.WriteLine("Страница загружена.");

            // Ожидание появления полей логина и пароля
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            IWebElement loginElement = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id='root']/div/div/div[3]/main/div/div[2]/div/div[2]/div/div/div[1]/div/div[1]/div[2]/div/form/div[1]/input[1]")));
            IWebElement passwordElement = driver.FindElement(By.XPath("//*[@id='root']/div/div/div[3]/main/div/div[2]/div/div[2]/div/div/div[1]/div/div[1]/div[2]/div/form/div[1]/input[2]"));

            Console.WriteLine("Поля логина и пароля найдены.");

            // Ввод логина и пароля
            loginElement.SendKeys(login);
            Console.WriteLine("Логин введен.");

            passwordElement.SendKeys(password); // Укажите правильный пароль
            Console.WriteLine("Пароль введен.");

            // Ожидание и нажатие кнопки входа
            IWebElement signInButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id='root']/div/div/div[3]/main/div/div[2]/div/div[2]/div/div/div[1]/div/div[1]/div[2]/div/form/div[2]/button/div")));
            Thread.Sleep(1000);
            signInButton.Click();
            Console.WriteLine("Вход выполнен.");

            // Ожидание и клик по "Форма дисциплины"
            IWebElement formDiscipline = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id='root']/div/div/div[3]/main/div/div[2]/div[1]/div[1]/div[8]/a")));
            Thread.Sleep(1000);
            formDiscipline.Click();
            Console.WriteLine("Форма дисциплины открыта.");

            Thread.Sleep(10000); // долго грузит страницу бланков  МОЖНО УМЕНЬШИТЬ ЕСЛИ ИНЕТ НОРМ

            string imageUrl = await GetCatImageUrl(); 
            string filePath = await DownloadImage(imageUrl);
            SetWallpaper(filePath); 

            // Перебираем анкеты (30 штук)
            for (int selectIndex = 9; selectIndex < 40; ++selectIndex)
            {
                Thread.Sleep(1000);
                IWebElement select = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id='root']/div[3]/select")));
                select.Click();
                Console.WriteLine("Выпадающий список открыт.");

                Thread.Sleep(1000);
                IWebElement definiteForm = driver.FindElement(By.XPath($"//*[@id='root']/div[3]/select/option[{selectIndex}]"));
                Thread.Sleep(1000);
                definiteForm.Click();
                Console.WriteLine($"Выбрана анкета {selectIndex}.");

                // Обрабатываем вопросы (10 штук)
                for (int i = 1; i <= 10; ++i)
                {
                    Thread.Sleep(1000); // МОЖНО УМЕНЬШИТЬ ЕСЛИ ИНЕТ НОРМ
                    IWebElement question = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath($"//*[@id='root']/div[4]/div[{i}]/div[2]/select")));
                    question.Click();
                    Console.WriteLine($"Вопрос {i} открыт.");

                    Thread.Sleep(1000); // МОЖНО УМЕНЬШИТЬ ЕСЛИ ИНЕТ НОРМ
                    IWebElement questionFiveMark = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath($"//*[@id='root']/div[4]/div[{i}]/div[2]/select/option[7]")));
                    questionFiveMark.Click();
                    Console.WriteLine($"Оценка 5 выбрана для вопроса {i}.");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка: " + e.Message);
        }
        finally
        {
            Console.WriteLine("Завершение работы WebDriver.");
            driver.Quit(); // Закрываем браузер
        }
    }

    static async Task<string> GetCatImageUrl()
    {
        string apiUrl = "https://api.thecatapi.com/v1/images/search";
        using (HttpClient client = new HttpClient())
        {
            var response = await client.GetStringAsync(apiUrl);
            int urlStart = response.IndexOf("\"url\":\"") + 7;
            int urlEnd = response.IndexOf("\"", urlStart);
            string imageUrl = response.Substring(urlStart, urlEnd - urlStart);
            return imageUrl;
        }
    }

    static async Task<string> DownloadImage(string imageUrl)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath = Path.Combine(desktopPath, "cat_wallpaper.jpg");

        using (HttpClient client = new HttpClient())
        {
            byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);
            await File.WriteAllBytesAsync(filePath, imageBytes);
        }

        Console.WriteLine($"Картинка сохранена: {filePath}");
        return filePath;
    }

    static void SetWallpaper(string filePath)
    {
        SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, filePath, SPIF_UPDATEINIFILE);
        Console.WriteLine("Обои обновлены!");
    }
}