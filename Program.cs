using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace WebDriverScraping
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    StartKvazars();
                }
                catch (Exception)
                {
                    Console.WriteLine("Произошла неведомая фигня, стартуем опять");
                }
            }     
        }

        public static void StartKvazars()
        {
            // Initialize the Chrome Driver
            using (var driver = new ChromeDriver())
            {
                // Go to the home page
                driver.Navigate().GoToUrl("http://wap.mgates.ru/login/");

                // Get User Name field, Password field and Login Button
                var userNameField = driver.FindElementByName("login");
                var userPasswordField = driver.FindElementByName("password");
                var loginButton = driver.FindElementByName("enter");

                // Type user name and password
                userNameField.SendKeys("kvakusha");
                userPasswordField.SendKeys("100500z");

                // and click the login button
                loginButton.Click();
                int numberGood = 0;
                int numberBad = 0;
                while (true)
                {
                    driver.Navigate().GoToUrl("http://quasars.mgates.ru/expedition.php");
                    var expiditionTypeDropDown = driver.FindElementByName("exp_type");
                    expiditionTypeDropDown.FindElement(By.CssSelector("option[value='1']")).Click();

                    var expiditionTimeDropDown = driver.FindElementByName("exp_time");
                    expiditionTimeDropDown.FindElement(By.CssSelector("option[value='3']")).Click();

                    if (driver.FindElementsByName("send").Count > 0)
                    {
                        numberGood++;
                        var sendButton = driver.FindElementByName("send");
                        sendButton.Click();
                        Console.WriteLine("Отправил опять эту фигню, попытка #" + numberGood);
                    }
                    else
                    {
                        numberBad++;
                        Console.WriteLine("Фигня не отправилась, попытка #" + numberBad);
                    }

                    Thread.Sleep(10000);
                }
            }
        }
    }
}
