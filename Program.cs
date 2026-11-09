using System.Linq.Expressions;
using System.Reflection.Emit;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

class Program
{
    static void Main(string[] args)
    {
        string url = args.Length > 0 ? args[0] : "https://app.cloudqa.io/home/AutomationPracticeForm";
        IWebDriver driver = new ChromeDriver();
        WebDriverWait wait = new WebDriverWait(
            driver,
            TimeSpan.FromSeconds(10)
        );
        driver.Navigate().GoToUrl(url);

        Console.WriteLine("Page title: " + driver.Title);
        try
        {
            IWebElement firstName = wait.Until(d => FindControlByLabel(d, "First Name",
        "Your First Name",
        "Given Name"));
            firstName.SendKeys("Tanish");
            if (string.Equals(firstName.GetDomProperty("value"), "Tanish", StringComparison.Ordinal)) Console.WriteLine("First Name Test : " + "PASS");
            else Console.WriteLine("First Name Test : " + "FAIL");

            IWebElement stateDropDown = wait.Until(d => FindControlByLabel(d, "State",
        "Select State",
        "Your State"));
            SelectElement selectState = new SelectElement(stateDropDown);
            selectState.SelectByValue("India");
            if (selectState.SelectedOption.Text.Equals("India")) Console.WriteLine("State Selection Test : " + "PASS");
            else Console.WriteLine("State Selection Test : " + "FAIL");

            string genderToTest = "Transgender";
            IWebElement gender = wait.Until(d => FindRadioByOptionText(d, genderToTest));
            gender.Click();
            if (gender.Selected) Console.WriteLine($"{genderToTest} Radio Button Test : " + "PASS");
            else Console.WriteLine($"{genderToTest} Radio Button Test : " + "FAIL");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        // driver.Quit();
    }
    static IWebElement FindControlByLabel(IWebDriver d, params string[] acceptedLabels)
    {
        var labels = d.FindElements(By.TagName("label"));

        foreach (var label in labels)
        {
            string actualText = label.Text.Trim();

            bool matches = acceptedLabels.Any(t =>
            actualText.Equals(
                t, StringComparison.OrdinalIgnoreCase
            )
            );
            if (!matches) continue;

            //Label Found
            Console.WriteLine(actualText + " label found");
            string? controlId = label.GetAttribute("for");
            if (!string.IsNullOrWhiteSpace(controlId))
            {
                var associated = d.FindElements(By.Id(controlId)).FirstOrDefault();
                if (associated != null) return associated;
            }
            //Attribute 'for' missing, change approach
            //Find nested Controls
            Console.WriteLine("Attribute \"for\" is missing, changing the approach to nested controls.");
            var nestedControls = label.FindElements(
                By.CssSelector("input,select,textarea")
            );
            if (nestedControls.Count == 1) return nestedControls[0];

            if (nestedControls.Count > 1)
                throw new Exception(
                    "Multiple Controls inside form label" + actualText
                );
        }
        throw new NoSuchElementException(
            "Could not identify field."
        );
    }
    static IWebElement FindRadioButton(
        IWebDriver driver,
        string groupName,
        string optionValue
    )
    {
        Console.WriteLine("Finding radio buttons...");
        var radios = driver.FindElements(By.CssSelector("input[type='radio']"));
        Console.WriteLine("All radio buttons found. Filtering based on name and value.");
        foreach (var radio in radios)
        {
            string name = radio.GetAttribute("name") ?? "";
            string value = radio.GetAttribute("value") ?? "";

            if (string.Equals(name, groupName, StringComparison.OrdinalIgnoreCase)
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
    static IWebElement FindRadioByOptionText(
    IWebDriver driver,
    string optionText)
    {
        var options = driver.FindElements(
            By.XPath("//span[normalize-space()='" + optionText + "']"));
        Console.WriteLine($"Found all spans with {optionText}");
        foreach (var option in options)
        {
            var radios = option.FindElements(
                By.XPath("./preceding-sibling::input[@type='radio'][1]"));
            Console.WriteLine($"Found all radios preceding span {optionText}");
            if (radios.Count == 1)
                return radios[0];
        }

        throw new NoSuchElementException(
            "Radio option not found: " + optionText);
    }
}
