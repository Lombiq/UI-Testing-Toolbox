import { By, until } from 'selenium-webdriver';

// This dependency is copied into the build directory by Lombiq.Tests.UI.
import { runTest, shouldContainText, navigate } from '../ui-testing-toolkit.mjs';

// This function automatically handles the command line arguments and sets up a Chrome driver.
await runTest(async (driver, startUrl) => {
    // Inside you can use all normal Selenium JavaScript code, e.g.:
    // - https://www.selenium.dev/selenium/docs/api/javascript/WebDriver.html
    // - https://www.selenium.dev/selenium/docs/api/javascript/By.html
    await driver.findElement(By.xpath("//a[@href = '/blog/post-1']")).click();

    // We also included a shortcut function to safely check text content.
    await shouldContainText(
        await driver.findElement(By.tagName("h1")),
        "Man must explore, and this is exploration at its greatest");
    await shouldContainText(
        await driver.findElement(By.className("field-name-blog-post-subtitle")),
        "Problems look mighty small from 150 miles up");

    // And another one to navigate and safely wait for the page to load.
    await navigate(driver, startUrl);
    await driver.findElement(By.xpath("id('footer')//a[@href='https://lombiq.com/']"));
});

// END OF TRAINING SECTION: Executing tests written in JavaScript.
