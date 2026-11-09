using System.Linq.Expressions;
using System.Reflection.Emit;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools.V154.Animation;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Support.UI;

class Program
{
    static void Main(string[] args)
    {   DotNetEnv.Env.Load();

        //Custom File Path
        string customFilePath="./iframe-test.html";
        string filePath = new Uri(Path.GetFullPath(customFilePath)).AbsoluteUri;

        //URL
        string? url =
            args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("PATH");


        IWebDriver driver = new ChromeDriver();
        driver.Navigate().GoToUrl(url);

        Console.WriteLine("Page title: " + driver.Title);
        try
        {
            TestAcrossFrames(
                driver,
                current => FindControlsByLabel(
                    current,
                    "First Name",
                    "Your First Name",
                    "Given Name"
                ),
                element =>
                {
                    element.SendKeys("Tanish");
                    return string.Equals(
                        element.GetDomProperty("value"),
                        "Tanish",
                        StringComparison.Ordinal
                    );
                },
                "First Name"
            );

            string genderToTest = "male";
            TestAcrossFrames(
                driver,
                current => FindRadiosByOptionText(current, genderToTest),
                radio =>
                {
                    radio.Click();
                    return radio.Selected;
                },
                $"{genderToTest} Radio Button"
            );

            TestAcrossFrames(
                driver,
                current => FindControlsByLabel(
                    current,
                    "State",
                    "Select State",
                    "Your State",
                    "Indian State"
                ),
                element =>
                {
                    var selectState = new SelectElement(element);
                    selectState.SelectByValue("India");
                    return string.Equals(
                        selectState.SelectedOption.Text,
                        "India",
                        StringComparison.Ordinal
                    );
                },
                "State Selection"
            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        // driver.Quit();
    }

    static IReadOnlyCollection<IWebElement> FindControlsByLabel(
        IWebDriver d,
        params string[] acceptedLabels)
    {
        var controls = new List<IWebElement>();
        var labels = d.FindElements(By.TagName("label"));

        foreach (var label in labels)
        {
            string actualText = label.Text.Trim();

            bool matches = acceptedLabels.Any(t =>
                actualText.Equals(t, StringComparison.OrdinalIgnoreCase)
            );
            if (!matches)
                continue;

            //Label Found
            Console.WriteLine(actualText + " label found");
            string? controlId = label.GetAttribute("for");
            if (!string.IsNullOrWhiteSpace(controlId))
            {
                var associated = d.FindElements(By.Id(controlId))
                    .FirstOrDefault(element =>
                        element.TagName.Equals("input", StringComparison.OrdinalIgnoreCase)
                        || element.TagName.Equals("select", StringComparison.OrdinalIgnoreCase)
                        || element.TagName.Equals("textarea", StringComparison.OrdinalIgnoreCase)
                    );
                if (associated != null)
                {
                    controls.Add(associated);
                    continue;
                }
            }

            var nestedControls = label.FindElements(By.CssSelector("input,select,textarea"));
            if (nestedControls.Count == 1)
                controls.Add(nestedControls[0]);

            if (nestedControls.Count > 1)
                throw new Exception("Multiple Controls inside form label" + actualText);
        }
        return controls;
    }

    static IWebElement FindRadioButton(IWebDriver driver, string groupName, string optionValue)
    {
        Console.WriteLine("Finding radio buttons...");
        var radios = driver.FindElements(By.CssSelector("input[type='radio']"));
        Console.WriteLine("All radio buttons found. Filtering based on name and value.");
        foreach (var radio in radios)
        {
            string name = radio.GetAttribute("name") ?? "";
            string value = radio.GetAttribute("value") ?? "";

            if (
                string.Equals(name, groupName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(value, optionValue, StringComparison.OrdinalIgnoreCase)
            )
            {
                return radio;
            }
        }
        throw new NoSuchElementException(
            $"Radio button {optionValue} not found in {groupName} group."
        );
    }

    static IReadOnlyCollection<IWebElement> FindRadiosByOptionText(
        IWebDriver driver,
        string optionText)
    {
        var radios = new List<IWebElement>();

        foreach (var label in driver.FindElements(By.TagName("label")))
        {
            if (!string.Equals(label.Text.Trim(), optionText, StringComparison.OrdinalIgnoreCase))
                continue;

            var nestedRadio = label.FindElements(By.CssSelector("input[type='radio']"))
                .FirstOrDefault();
            if (nestedRadio != null)
            {
                radios.Add(nestedRadio);
                continue;
            }

            string? radioId = label.GetAttribute("for");
            if (!string.IsNullOrWhiteSpace(radioId))
            {
                var associatedRadio = driver.FindElements(By.Id(radioId))
                    .FirstOrDefault(element =>
                        string.Equals(
                            element.GetAttribute("type"),
                            "radio",
                            StringComparison.OrdinalIgnoreCase
                        )
                    );
                if (associatedRadio != null)
                {
                    radios.Add(associatedRadio);
                    continue;
                }
            }
        }

        foreach (var span in driver.FindElements(By.TagName("span")))
        {
            if (!string.Equals(span.Text.Trim(), optionText, StringComparison.OrdinalIgnoreCase))
                continue;

            var precedingRadio = span.FindElements(
                By.XPath("./preceding-sibling::input[@type='radio'][1]")
            ).FirstOrDefault();
            if (precedingRadio != null)
                radios.Add(precedingRadio);
        }

        return radios;
    }

    static int TestAcrossFrames(
        IWebDriver driver,
        Func<IWebDriver, IReadOnlyCollection<IWebElement>> find,
        Func<IWebElement, bool> test,
        string testName,
        IWebElement? frame = null,
        string framePath = "default content")
    {
        if (frame == null)
            driver.SwitchTo().DefaultContent();
        else
            driver.SwitchTo().Frame(frame);

        int testedCount = 0;
        var matches = find(driver);
        for (int i = 0; i < matches.Count; i++)
        {
            var element = matches.ElementAt(i);
            string matchPath = $"{framePath} / match[{i + 1}]";
            testedCount++;
            try
            {
                bool passed = test(element);
                Console.WriteLine(
                    $"{testName} Test at {matchPath} : {(passed ? "PASS" : "FAIL")}"
                );
            }
            catch (Exception e)
            {
                Console.WriteLine($"{testName} Test at {matchPath} : FAIL ({e.Message})");
            }
        }

        var frames = driver.FindElements(By.TagName("iframe"));
        for (int i = 0; i < frames.Count; i++)
        {
            int childTestedCount;
            try
            {
                childTestedCount = TestAcrossFrames(
                    driver,
                    find,
                    test,
                    testName,
                    frames[i],
                    $"{framePath} / iframe[{i + 1}]"
                );
            }
            finally
            {
                driver.SwitchTo().ParentFrame();
            }
            testedCount += childTestedCount;
        }

        if (frame == null && testedCount == 0)
            throw new NoSuchElementException(
                $"No elements matching the {testName} condition were found in the document or its nested iframes."
            );

        return testedCount;
    }
    }
