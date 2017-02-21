using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Collections.Generic;
using System.Linq;

namespace WebDriverScraping
{
    class Program
    {
        private static int numberGood = 0;
        private static int numberBad = 0;

        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    var site = "http://elem.mobi/";
                    var login = "";
                    var password = "";
                    StartKvazars(site, login, password);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                    try
                    {
                        SendSms("Я упал: " + exception.Message.Remove(45));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    Thread.Sleep(3600000);
                }
            }
        }

        public static void StartKvazars(string site, string login, string password)
        {
            // Initialize the Chrome Driver
            using (var driver = new ChromeDriver())
            {
                driver.Manage().Timeouts().SetPageLoadTimeout(new TimeSpan(0, 1, 0, 0));
                driver.Manage().Timeouts().SetScriptTimeout(new TimeSpan(0, 0, 0, 40));
                driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 0, 2));
                // Go to the home page
                driver.Navigate().GoToUrl(site + "login/");

                // Get User Name field, Password field and Login Button
                var userNameField = driver.FindElementByName("plogin");
                var userPasswordField = driver.FindElementByName("ppass");
                var loginButton = driver.FindElementByClassName("lbl");

                // Type user name and password
                userNameField.SendKeys(login);
                userPasswordField.SendKeys(password);

                // and click the login button
                loginButton.Click();

                bool thereisDuel = true;
                while (thereisDuel)
                {
                    var myHP = GetMyHp(driver);
                    thereisDuel = StartDuel(driver, site, myHP);
                    Thread.Sleep(10000);
                    driver.Navigate().GoToUrl(site);
                }

                driver.Quit();
                Thread.Sleep(600000);
            }
        }

        public static bool StartDuel(ChromeDriver driver, string site, int myHP)
        {
            //find a enemy
            int maxAttempt = 50;
            int attempt = 0;
            var enemyHp = int.MaxValue;
            do
            {
                driver.Navigate().GoToUrl(site + "duel/");
                if (!IsAvalibleDuel(driver))
                {
                    return false;
                }

                if (IsActiveDuelNow(driver))
                {
                    enemyHp = myHP;
                }
                else
                {
                    enemyHp = GetEnemyHp(driver);
                }

                attempt++;

                if (attempt > maxAttempt)
                {
                    RaiseFailure("Начальник всё пропало!! Не могу найти противника!!");
                    attempt = 0;
                }
            }
            while ((enemyHp - myHP) / (double)myHP * 100 > 10.0);

            //attack
            if (!IsActiveDuelNow(driver))
            {
                driver.Navigate().GoToUrl(site + "duel/tobattle/");
            }
            //driver.Navigate().GoToUrl(site + "duel/"); //remove

            do
            {
                var elemToClick = GetAttackLinkCard(driver);
                elemToClick.Click();
            }
            while (!IsEndOfAttack(driver));

            return true;
        }

        private static bool IsAvalibleDuel(ChromeDriver driver)
        {
            var restoreTime = driver.FindElementsById("duels_restore_time");
            return !restoreTime.Any();
        }

        private static void RaiseFailure(string failReason)
        {
            //senSMS
            SendSms(failReason);
            Thread.Sleep(3600000);
        }

        private static bool IsActiveDuelNow(ChromeDriver driver)
        {
            var cards = driver.FindElementsByCssSelector(".w3card");
            return cards.Any();
        }

        private static bool IsEndOfAttack(ChromeDriver driver)
        {
            var infoBlocks = driver.FindElementsByCssSelector(".c_silver.mb5");
            return infoBlocks.Any();
        }

        private static int GetEnemyHp(ChromeDriver driver)
        {
            var attackHP = driver.FindElementByCssSelector(".c_da.mt5.mr5").Text.Replace(" ", string.Empty);
            var enemyHp = Convert.ToInt32(attackHP);
            return enemyHp;
        }

        private static IWebElement GetAttackLinkCard(ChromeDriver driver)
        {
            var cards = driver.FindElementsByCssSelector(".w3card");
            var attackPower = new List<KeyValuePair<double, IWebElement>>();

            foreach (var card in cards)
            {
                var c_dmg15 = card.FindElements(By.CssSelector(".c_dmg15"));
                var c_dmg10 = card.FindElements(By.CssSelector(".c_dmg10"));

                var coeff = 0.5;
                if (c_dmg10.Any())
                {
                    coeff = 1.0;
                }
                if (c_dmg15.Any())
                {
                    coeff = 1.5;
                }

                var stats = card.FindElements(By.ClassName("stat"));

                var enemyAttack = Convert.ToInt32(stats.First().Text.Trim());
                var myAttack = Convert.ToInt32(stats.Last().Text.Trim());

                var damage = myAttack * coeff - enemyAttack;
                IWebElement cardLink = card.FindElement(By.ClassName("card"));

                attackPower.Add(new KeyValuePair<double, IWebElement>(damage, cardLink));
            }

            var max = attackPower.Max(x => x.Key);
            var elemToClick = attackPower.First(x => (x.Key - max) < 0.1 && (x.Key - max) > -0.1);
            return elemToClick.Value;
        }


        private static int GetMyHp(ChromeDriver driver)
        {
            //run at main page
            var myHp = driver.FindElementByCssSelector(".c_da").Text.Replace(" ", string.Empty);
            return Convert.ToInt32(myHp);
        }



        private static void SendSms(string message, ChromeDriver driver)
        {
            var url = string.Format("https://sms.ru/sms/send?api_id={0}&to={1}&text={2}", "", "", message);
            driver.Navigate().GoToUrl(url);
        }

        private static void SendSms(string message)
        {
            using (var driver = new ChromeDriver())
            {
                driver.Manage().Timeouts().SetPageLoadTimeout(new TimeSpan(0, 1, 0, 0));
                driver.Manage().Timeouts().SetScriptTimeout(new TimeSpan(0, 0, 0, 40));
                driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 0, 2));

                SendSms(message, driver);

                Thread.Sleep(1000);
            }

        }

    }
}
