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
        private const int defaultTimeout = 100000;
        private static int timeout = defaultTimeout;

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
                    Console.WriteLine(DateTime.Now.ToShortTimeString() + exception.Message);
                    try
                    {
                        var message = "Я упал: ";
                        if (exception.Message != null)
                        {
                            message += exception.ToString() + exception.Message;
                            if (message.Length >= 45)
                            {
                                message += message.Remove(45);
                            }
                        }

                        SendSms(message);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    Thread.Sleep(timeout);
                    timeout *= 2;
                }
            }
        }

        public static void StartKvazars(string site, string login, string password)
        {
            // Initialize the Chrome Driver
            using (var driver = new ChromeDriver())
            {
                driver.Manage().Timeouts().SetPageLoadTimeout(new TimeSpan(0, 2, 0, 0));
                driver.Manage().Timeouts().SetScriptTimeout(new TimeSpan(0, 0, 0, 40));
                driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 0, 1));
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
                var myHP = GetMyHp(driver);

                driver.Navigate().GoToUrl(site + "profile/");
                var myRate = GetRate(driver, site);

                driver.Navigate().GoToUrl(site + "duel/");
                try
                {
                    myHP = GetMyHp(driver);
                }
                catch { }

                while (thereisDuel)
                {    
                    thereisDuel = StartDuel(driver, site, myHP, myRate);
                    Thread.Sleep(5000);
                    driver.Navigate().GoToUrl(site);
                }

                driver.Quit();
            }

            timeout = defaultTimeout;
            Thread.Sleep(900000);
        }

        public static bool StartDuel(ChromeDriver driver, string site, int myHP, int myRate)
        {
            //find a enemy
            int maxAttempt = 300;
            int maxAttemptToFindWIthRate = -3;
            int attemptToFindWIthRate = 0;
            int attempt = 0;
            var enemyRate = 0;
            var enemyHp = int.MaxValue;
            
            if (!IsActiveDuelNow(driver))
            {
                do
                {
                    do
                    {
                        driver.Navigate().GoToUrl(site + "duel/");
                        if (!IsAvalibleDuel(driver))
                        {
                            return false;
                        }
                        else
                        {
                            enemyHp = GetEnemyHp(driver);
                        }

                        attempt++;

                        if (attempt > maxAttempt)
                        {
                            RaiseFailure("Не могу найти противника!! Попыток:" + attempt, driver);
                            attempt = 0;
                        }
                    }
                    while (!IsEnemyAttracted(enemyHp, myHP));


                    //var enemyName = GetEnemyName(driver);
                    //enemyRate = GetEnemyRate(driver, enemyName, site);
                    attemptToFindWIthRate++;

                }
                while (myRate > enemyRate && maxAttemptToFindWIthRate > attemptToFindWIthRate);

                //attack
                driver.Navigate().GoToUrl(site + "duel/tobattle/");
            }

            do
            {
                var elemToClick = GetAttackLinkCard(driver);
                elemToClick.Click();
            }
            while (!IsEndOfAttack(driver));

            return true;
        }

        private static bool IsEnemyAttracted(int enemyHp, int myHP)
        {
            var maxHpForEnemy = myHP * 1.08;
            return maxHpForEnemy > (enemyHp * 1.0);
        }

        private static bool IsAvalibleDuel(ChromeDriver driver)
        {
            var restoreTime = driver.FindElementsById("duels_restore_time");
            return !restoreTime.Any();
        }

        private static void RaiseFailure(string failReason, ChromeDriver driver)
        {
            //senSMS
            SendSms(failReason, driver);
            Thread.Sleep(timeout);
        }

        private static bool IsActiveDuelNow(ChromeDriver driver)
        {
            var cards = driver.FindElementsByCssSelector(".w3card");
            return cards.Any();
        }

        private static bool IsEndOfAttack(ChromeDriver driver)
        {
            var infoBlocks = driver.FindElementsByCssSelector(".w3card");
            return !infoBlocks.Any();
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

                var damage = myAttack * coeff - enemyAttack * (2.0 - coeff);
                IWebElement cardLink = card.FindElement(By.ClassName("card"));

                attackPower.Add(new KeyValuePair<double, IWebElement>(damage, cardLink));
            }

            var max = attackPower.Max(x => x.Key);
            var elemToClick = attackPower.First(x => (x.Key - max) < 0.1 && (x.Key - max) > -0.1);
            return elemToClick.Value;
        }


        private static int GetMyHp(ChromeDriver driver)
        {
            //run at duel
            var myHp = driver.FindElementsByCssSelector(".c_da").First().Text.Replace(" ", string.Empty);
            return Convert.ToInt32(myHp);
        }

        private static int GetRate(ChromeDriver driver, string site)
        {
            //run at main page            
            var rate = driver.FindElementsByCssSelector(".small.c_99.mt10.ml8 .c_da").First().Text.Replace(" ", string.Empty);
            return Convert.ToInt32(rate);
        }

        private static string GetEnemyName(ChromeDriver driver)
        {
            //run at main page
            string enemyHp = driver.FindElementByCssSelector(".c_rose.nwr.mb5").Text;
            return enemyHp;
        }

        private static int GetEnemyRate(ChromeDriver driver, string enemyName, string site)
        {
            driver.Navigate().GoToUrl(site + "online/");
            var findInput = driver.FindElementByName("slogin");
            findInput.SendKeys(enemyName);

            findInput.Submit();
            return GetRate(driver, site);
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
