using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebDriverScraping
{
    class Program
    {
        private static int numberGood = 0;
        private static int numberBad = 0;
        private const int defaultTimeout = 100000;
        private static int timeout = defaultTimeout;
        private static int botsCount = 0;
        private static bool statWasSent;
        private static int dungeonCount = 0;

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
                            if (message.Length >= 65)
                            {
                                message = message.Remove(65);
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

                //while (true)
                //{
                //    StartUrfin(driver, site);
                //    Thread.Sleep(GetSleepInterval(driver));
                //}

                
                StartDuelBlock(site, driver);
                StartDungeon(driver, site);
                StartSurvivalBlock(driver, site);
                StartDailyBlock(driver, site);
                Thread.Sleep(2000);
                SendStatistic(driver);
                Thread.Sleep(2000);
                driver.Quit();
            }

            timeout = defaultTimeout;
            Thread.Sleep(900000);
        }

        private static void StartDailyBlock(ChromeDriver driver, string site)
        {
            if (DateTime.Now.Hour > 22)
            {
                driver.Navigate().GoToUrl(site + "daily/");
                var elements = driver.FindElementsByCssSelector("a.btn.bli.orange.mlra.w180px.mt-15");
                foreach (var webElement in elements)
                {
                    webElement.Click();
                }
            }
        }

        private static void StartDuelBlock(string site, ChromeDriver driver)
        {
            bool thereisDuel = true;
            var myHP = GetMyHp(driver);

            driver.Navigate().GoToUrl(site + "profile/");
            var myRate = GetRate(driver, site);

            driver.Navigate().GoToUrl(site + "duel/");
            try
            {
                myHP = GetMyHp(driver);
            }
            catch
            {
            }

            while (thereisDuel)
            {
                thereisDuel = StartDuel(driver, site, myHP, myRate);
                Thread.Sleep(5000);
                driver.Navigate().GoToUrl(site);
            }
        }

        private static void StartSurvivalBlock(ChromeDriver driver, string site)
        {
            driver.Navigate().GoToUrl(site + "survival/");

            while (IsSurvivalRequered(driver))
            {
                try
                {
                    StartSurvival(driver, site);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Арена:" + DateTime.Now.ToShortTimeString() + exception.Message);
                }
                
            }
        }

        private static void StartSurvival(ChromeDriver driver, string site)
        {
            driver.Navigate().GoToUrl(site + "survival/join/");

            while (IsSurvivalJoined(driver) && !IsActiveDuelNow(driver))
            {
                Thread.Sleep(5000);
                driver.Navigate().GoToUrl(site + "survival/");
            }

            if (IsActiveDuelNow(driver))
            {
                var cartToAttack = GetSurvivalAttackLinkCard(driver);
                while (cartToAttack != null)
                {
                    cartToAttack.Click();
                    cartToAttack = GetSurvivalAttackLinkCard(driver);
                }
            }
            else
            {
                SurvivalAttempt--;
            }

            driver.Navigate().GoToUrl(site + "survival/");
        }

        private static void StartUrfin(ChromeDriver driver, string site)
        {
            driver.Navigate().GoToUrl(site + "urfin/start/");
            while (IsActiveDuelNow(driver))
            {
                var cards = driver.FindElementsByCssSelector(".w3card");
                foreach (var card in cards)
                {
                    card.Click();
                    Thread.Sleep(200);
                }
            }
        }

        private static int GetSleepInterval(ChromeDriver driver)
        {
            var timerText = string.Empty;
            var timers = driver.FindElementsByCssSelector("#time_till_next_invasion");
            if (timers.Any())
            {
                timerText = timers.First().Text;
            }
            else
            {
                var timers2 = driver.FindElementsByCssSelector("#time_till_boss_gen");
                if (timers2.Any())
                {
                    timerText = timers2.First().Text;
                }
            }

            if (string.IsNullOrEmpty(timerText))
            {
                return 10000;
            }
            else
            {
                if (timerText.Contains("д"))
                {
                    return 24 * 60 * 1000;
                }
                if (timerText.Contains("м"))
                {
                    return 60 * 1000;
                }
            }

            return 10000;
        }

        private static void SendStatistic(ChromeDriver driver)
        {
            if (DateTime.Now.Hour > 12)
            {
                if (!statWasSent)
                {
                    statWasSent = true;
                    SendSms("Дуэлей:" + numberGood + ", из них ботов:" + botsCount + "dungeons:" + dungeonCount, driver);
                }
            }
            else
            {
                statWasSent = false;
            }
        }

        private static void StartDungeon(ChromeDriver driver, string site)
        {
            driver.Navigate().GoToUrl(site + "dungeon/");

            if (IsActiveDuelNow(driver))
            {
                ProcessDungeon(driver);
                driver.Navigate().GoToUrl(site + "dungeon/");
            }

            var companyes = driver.FindElementsByCssSelector(".cpoint");
            foreach (var company in companyes)
            {
                var href = company.FindElements(By.CssSelector("a")).FirstOrDefault();
                if (href != null && href.GetAttribute("href") != "http://elem.mobi/story/")
                {
                    href.Click();
                    dungeonCount++;
                    ProcessDungeon(driver);
                    break;
                }
            }

        }

        private static void ProcessDungeon(ChromeDriver driver)
        {
            while (!IsEndOfAttack(driver))
            {
                var elemToClick = GetAttackLinkCardForDungeon(driver);
                if (elemToClick != null)
                {
                    elemToClick.Click();
                }
            }
        }

        public static bool StartDuel(ChromeDriver driver, string site, int myHP, int myRate)
        {
            //find a enemy
            int maxAttempt = 700;
            int maxAttemptToFindWIthRate = -3;
            int attemptToFindWIthRate = 0;
            int attempt = 0;
            var enemyRate = 0;
            var enemyHp = int.MaxValue;
            var isTopLiga = false;

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
                            RaiseFailure("Не могу найти противника!! Попыток:" + attempt + ". Ботов найдено: " + botsCount, driver);
                            attempt = 0;
                        }
                    }
                    while (!IsEnemyAttracted(enemyHp, myHP));

                    isTopLiga = false;//IsTopInLiga(driver);
                    var enemyName = GetEnemyName(driver);
                    enemyRate = int.MaxValue;
                    if (enemyName.Contains(" ")) //check at bot enemy
                    {
                        enemyRate = GetEnemyRate(driver, enemyName, site);
                    }

                    attemptToFindWIthRate++;

                }
                while ((isTopLiga && enemyRate != 0) && maxAttemptToFindWIthRate > attemptToFindWIthRate);

                //attack
                driver.Navigate().GoToUrl(site + "duel/tobattle/");
                numberGood++;
            }

            do
            {
                var elemToClick = GetAttackLinkCard(driver);
                elemToClick.Click();
            }
            while (!IsEndOfAttack(driver));

            return true;
        }

        private static bool IsTopInLiga(ChromeDriver driver)
        {
            try
            {
                var rateBar = driver.FindElementsByCssSelector(".rate.blue").FirstOrDefault();
                //style="width:34%;
                var ratePersent = rateBar.GetAttribute("style");
                var per = Regex.Match(ratePersent, @"\d+").Value;
                return Convert.ToInt32(per) > 75;
            }
            catch
            {
                return false;
            }

        }

        private static bool IsEnemyAttracted(int enemyHp, int myHP)
        {
            var maxHpForEnemy = myHP * 0.98;
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


        private static int SurvivalAttempt = 0;
        private static int MaxSurvivalAttempt = 10;
        private static int SurvivalDayNumber;
        private static bool IsSurvivalRequered(ChromeDriver driver)
        {
            if (SurvivalDayNumber == DateTime.Now.Day)
            {
                return false;
            }

            if (SurvivalAttempt > MaxSurvivalAttempt)
            {
                SurvivalDayNumber = DateTime.Now.Day;
                SurvivalAttempt = 0;
                return false;
            }

            SurvivalAttempt++;
            return true;

            var infoBlock = driver.FindElementsByCssSelector(".c_fe.pt10 .c_99.cntr.small");
            if (infoBlock.Any())
            {
                SurvivalAttempt++;
                return true;
            };

            return false;
        }

        private static IWebElement GetSurvivalAttackLinkCard(ChromeDriver driver)
        {
            var cards = driver.FindElementsByCssSelector(".w3card");
            var clickCount = 0;
            while (cards.Any())
            {
                foreach (var card in cards)
                {
                    var c_dmg15 = card.FindElements(By.CssSelector(".c_dmg15"));
                    if (c_dmg15.Any())
                    {
                        return card.FindElements(By.ClassName("card")).First(x => x.FindElements(By.ClassName("fade_out")).Any()); ;
                    }
                }

                var nextTarget = driver.FindElementsByCssSelector(".cntr a.ml5.btn.grey.w100px.mt5").FirstOrDefault();
                
                if (nextTarget != null && clickCount < 6)
                {
                    nextTarget.Click();
                    clickCount++;
                    cards = driver.FindElementsByCssSelector(".w3card");
                }
                else
                {
                    clickCount = 0;
                    while (driver.FindElementsByCssSelector(".w3card .time .fade_out").Any())
                    {
                        var refresh = driver.FindElementsByCssSelector(".cntr .btn.blue.w100px.mt5.mr5").FirstOrDefault();
                        if (refresh != null)
                        {
                            refresh.Click();
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            return null;
                        }
                        
                    }

                    return !IsEndOfAttack(driver) ? GetAttackLinkCard(driver) : null;
                }
            }

            return null;
        }

        private static bool IsSurvivalJoined(ChromeDriver driver)
        {
            return driver.FindElementsByCssSelector(".c_orange.pt10").Any() 
                || driver.FindElementsByCssSelector("div.brd.mr5.ml5.mb10.inbl").Any();
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
            if (!cards.Any())
            {
                return null;
            }

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
                IWebElement cardLink = card.FindElements(By.ClassName("card")).First(x => x.FindElements(By.ClassName("fade_out")).Any());

                attackPower.Add(new KeyValuePair<double, IWebElement>(damage, cardLink));
            }

            var max = attackPower.Max(x => x.Key);
            var elemToClick = attackPower.First(x => (x.Key - max) < 0.1 && (x.Key - max) > -0.1);
            return elemToClick.Value;
        }

        private static IWebElement GetAttackLinkCardForDungeon(ChromeDriver driver)
        {
            var maxStat = driver.FindElementsByCssSelector(".w3card .stat").Max(stat => Convert.ToInt32(stat.Text.Trim()));
            var result = driver.FindElementsByCssSelector(".w3card .stat").FirstOrDefault(stat => Convert.ToInt32(stat.Text.Trim()) >= maxStat);
            return result;
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

            if (!driver.Url.Contains("user"))
            {
                botsCount++;
                return 0;
            }
            else
            {
                return GetRate(driver, site);
            }

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
