import path from 'path';
import process from 'process';
import fs from 'node:fs/promises';
import { Builder, By, WebDriver, WebElement, until, Key }  from 'selenium-webdriver';
import chrome from 'selenium-webdriver/chrome.js';
import { writeFile } from 'node:fs/promises'

function _exists(path) {
    return fs.stat(path).then(() => true, () => false);
}

/**
 * Logs the page source to the console.
 * @param driver {WebDriver} - The WebDriver instance.
 * @returns {Promise<void>}
 * @private
 */
async function _logSource(driver) {
    const html = await driver.getPageSource();
    console.log('HTML:', html.replace(/\s*[\n\r]+\s*/g, ' '));
}

/**
 * Takes a screenshot of the current page and saves it to the provided file.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param file {string} - The file path where the screenshot should be saved.
 * @returns {Promise<void>}
 * @private
 */
async function _takeScreenshot(driver, file){
    let image = await driver.takeScreenshot()
    await writeFile(file, image, 'base64')
}

/**
 * Waits for the provided condition to be true, with a timeout.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param condition {function():Promise<boolean>} - The condition to wait for.
 * @param timeout {number} - The maximum time to wait for the condition to be true.
 * @returns {Promise<*>}
 */
function safeWait(driver, condition, timeout = 10000) {
    return driver.wait(condition, timeout);
}

/**
 * Waits for the URL to be the provided value.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param url {string} - The URL to wait for.
 * @param timeout {number} - The maximum time to wait for the URL to be the provided value.
 * @returns {Promise<*>}
 */
function waitForUrl(driver, url, timeout = 10000)
{
    return safeWait(driver, until.urlIs(url), timeout);
}

/**
 * Waits for the input element to have a non-empty value. Useful e.g. when you need to wait for the editor to load.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param element {WebElement} - The input element to wait for.
 * @param timeout {number} - The maximum time to wait for the input to have a non-empty value.
 * @returns {Promise<void>}
 */
function waitUntilInputValueIsNotEmpty(driver, element, timeout = 5000) {
    return safeWait(
        driver,
        async function () {
            const currentValue = await element.getAttribute('value');
            return currentValue?.length > 0;
        },
        timeout);
}

/**
 * Waits for the class to be removed from the element.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param by {By} - The By selector used to find the element.
 * @param className {string} - The class name to wait for.
 * @param timeout {number} - The maximum time to wait for the class to be removed.
 * @returns {Promise<void>}
 */
function waitUntilClassIsRemoved(driver, by, className, timeout = 5000) {
    return safeWait(driver, async function () {
        const element = await driver.findElement(by);
        const classList = await element.getAttribute('class');

        // Wait until class is NOT present
        return !classList.includes(className);
    }, timeout);
}

/**
 * Finds an element using the provided selector.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param by {By} - The By selector used to find the element.
 * @returns {Promise<*>}
 */
function findElementBy(driver, by)
{
    return driver.findElement(by);
}

/**
 * Finds a link in an email by its text and clicks on it. Should be used on the Email UI.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param text {string} - The text of the link to find.
 * @returns {Promise<void>}
 */
function findLinkByTextInEmailThenClick(driver, text) {
    return findByThenClick(driver, By.linkText(text), true);
}

/**
 * Finds a link by its text and clicks on it.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param text {string} - The text of the link to find.
 * @returns {Promise<void>}
 */
function findLinkByTextThenClick(driver, text) {
    return findByThenClick(driver, By.linkText(text));
}

/**
 * Finds an element by its XPath.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param xpath {string} - The XPath of the element to find.
 * @returns {Promise<*>}
 */
function findElementByXpath(driver, xpath)
{
    return driver.findElement(By.xpath(xpath));
}

/**
 * Finds an input element by its value.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param value {string} - The value of the input element to find.
 * @returns {Promise<*>}
 */
function findElementByInputValue(driver, value)
{
    return findElementByXpath(driver, `//input[@value=${JSON.stringify(value)}]`);
}

/**
 * Checks if the provided link should open in a new tab by its target attribute.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param element {WebElement} - The link element to check.
 * @returns {Promise<boolean>} - True if the link should open in a new tab, false otherwise.
 */
async function linkShouldOpenNewTab(driver, element)
{
    const targetAttr = await element.getAttribute('target');
    return targetAttr === '_blank';
}

/**
 * Creates a By selector for finding an element by its tag and containing text.
 * @param tag The tag of the element.
 * @param text The text to search for.
 * @returns {!By} The By selector.
 */
function byTagAndText(tag, text){
    return By.xpath(`//${tag}[contains(., ${JSON.stringify(text)})]`);
}

/**
 * Finds an element by its tag and containing text.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param tag {string} - The tag of the element.
 * @param text {string} - The text to search for.
 * @returns {Promise<*>}
 */
async function findElementByTagAndContainingText(driver, tag, text)
{
    const by = byTagAndText(tag, text);
    return driver.findElement(by);
}

/**
 * Finds an element by its tag and containing text, then clicks on it.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param tag {string} - The tag of the element.
 * @param text {string} - The text to search for.
 * @returns {Promise<void>}
 */
async function findByTagAndContainingTextThenClick(driver, tag, text)
{
    return findByThenClick(driver, byTagAndText(tag, text));
}

/**
 * Finds an element by the provided By selector and clicks on it. If the element is a link that should open in a new tab,
 * the function will wait for the new tab to open and switch to it.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param by {By} - The By selector used to find the element.
 * @param forceWaitNewTab {boolean} - If true, the function will wait for the new tab to open and switch to it.
 * @returns {Promise<void>}
 */
async function findByThenClick(driver, by, forceWaitNewTab = false)
{
    // Get current window handles before clicking
    let initialHandles = await driver.getAllWindowHandles();

    await safeWait(driver, until.elementsLocated(by));
    const element = await driver.findElement(by);
    await safeWait(driver, until.elementIsVisible(element));
    const shouldOpenNewTab = await linkShouldOpenNewTab(driver, element);
    await clickSafely(driver, element);

    // If the link should open in a new tab, wait for the new tab to open and switch to the last tab.
    if (shouldOpenNewTab || forceWaitNewTab) {
        let updatedHandles;
        await safeWait(driver, async () => {
            updatedHandles = await driver.getAllWindowHandles();
            return updatedHandles.length > initialHandles.length;
        }).catch(() => {});

        await driver.switchTo().window(updatedHandles[updatedHandles.length - 1]);
    }
}

/**
 * Switches to the tab with the provided index.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param tabIndex {number} - The index of the tab to switch to.
 * @returns {Promise<void>}
 */
async function switchToTab(driver, tabIndex = 0) {
    let handles = await driver.getAllWindowHandles();
    if (tabIndex < handles.length) {
        await driver.switchTo().window(handles[tabIndex]);
    } else {
        throw new Error('Tab index out of range');
    }
}

/**
 * Opens an email with the provided subject and email address.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param subject {string} - The subject of the email.
 * @param email {string} - The email address of the recipient.
 * @returns {Promise<void>}
 */
async function openEmailWithSubject(driver, subject, email) {
    await clickSafely(
        driver,
        await driver.findElement(
            By.xpath(`//tr[contains(@class,'el-table__row')][.//div[contains(@class,'cell')][contains(normalize-space(), '${subject}')] and
             .//div[contains(@class,'cell')][contains(normalize-space(), '${email}')]]`)));

    await driver.switchTo().frame(0)
}

/**
 * Clicks on the provided element after scrolling it into view, because it might not be on the screen and while it can
 * be found by Selenium even if it is not on screen, it cannot be clicked.
 * @param {WebDriver} driver The driver of the current browser.
 * @param {WebElement} element The element to be clicked.
 */
async function clickSafely(driver, element){
    await driver.executeScript("arguments[0].scrollIntoView({ behavior: 'instant', block: 'center' });", element);
    await element.click();
}

/**
 * Verifies that the provided element's inner text contains the provided text.
 * @param {WebElement} element The web element whose inner text is examined.
 * @param {string} text The expected inner text fragment.
 * @returns {Promise<void>} Success when the element text contains the expected string, rejection if it does not or if
 * element is null or empty.
 */
async function shouldContainText(element, text) {
    if (!element) {
        throw new Error('The element is missing.');
    }

    if (element.then) {
        element = await element;
    }

    const actualText = (await element.getText())?.trim();

    if (actualText?.includes(text) !== true) {
        const url = await element.getDriver().getCurrentUrl();
        throw new Error(
            `Expected element at ${url} to contain text "${text}", but it does not. (Actual text: ${actualText})`);
    }
}

/**
 * Navigates the browser to the given URL and then verifies that the page has loaded with non-empty content.
 * @param {WebDriver} driver The driver whose current tab should be navigated.
 * @param {string} url The target URL.
 * @param {number} maxAttempts The maximum number of attempts. If exceeded, an error is thrown.
 * @returns {Promise<void>} Success when a non-empty page has been reached.
 */
async function navigate(driver, url, maxAttempts = 10) {
    for (let i = 0; i < maxAttempts; i++) {
        await driver.navigate().to(url);

        if(await waitForPageLoad(driver, url)){
            return;
        }
    }

    throw new Error(`Failed to navigate to a non-empty page at ${url} in ${maxAttempts} attempts.`)
}

/**
 * Waits for the page to load by checking the document.readyState property and then by checking if the body element has
 * any inner HTML. If the page is loaded, the function returns true, otherwise it returns false.
 * @param {WebDriver} driver The driver of the current browser.
 * @param {string} url The URL of the page to be checked.
 * @param {number} timeout The maximum time to wait for the page to load.
 */
async function waitForPageLoad(driver, url, timeout = 10000) {
    await safeWait(driver, () => driver
            .executeScript('return document.readyState')
            .then((readyState) => readyState === 'complete'),
        timeout);

    try {
        if ((await driver.findElement(By.xpath('//body')).getAttribute('innerHTML'))?.trim()) {
            console.log(`Successfully reached ${url}.`);
            return true;
        }
    }
    catch (exception) {
        // Nothing to do here, let's try again.
    }
    return false;
}

/**
 * Finds an input element using the provided selector, clicks on it, clears its contents, then types in the provided
 * value instead.
 * @param {WebDriver} driver The driver of the current browser.
 * @param by The By selector used to find the web element.
 * @param value The new value to be typed in as a human would.
 * @returns {Promise<void>} Success if the element was found and successfully updated.
 */
async function fillInput(driver, by, value) {
    const inputField = await driver.findElement(by);
    await clickSafely(driver, inputField);

    const currentValue = await inputField.getAttribute('value');
    if (currentValue?.length > 0) {
        await inputField.sendKeys(Key.chord(Key.CONTROL, 'a'), Key.DELETE);
    }
    await inputField.sendKeys(value);
}

/**
 * Finds an input element by its label text and fills it with the provided value.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param labelText {string} - The label text of the input field.
 * @param context {WebElement | WebDriver} - The context where the input field is located.
 * @returns {Promise<!By>}
 */
async function byInputByLabel(driver, labelText, context = driver){
    const label = await findElementByXpath(context, `//label[contains(., ${JSON.stringify(labelText)})]`);
    return By.id(await label.getAttribute('for'));
}

/**
 * Finds an input element by its label text.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param labelText {string} - The label text of the input field.
 * @param context {WebElement | WebDriver} - The context where the input field is located.
 * @returns {Promise<*>}
 */
async function findInputByLabel(driver, labelText, context = driver){
    return driver.findElement(await byInputByLabel(driver, labelText, context));
}

/**
 * Finds an input element by its label text and fills it with the provided value.
 * @param driver {WebDriver} - The WebDriver instance.
 * @param labelText {string} - The label text of the input field.
 * @param inputText {string} - The text to fill the input field with.
 * @param context {WebElement | WebDriver} - The context where the input field is located.
 * @returns {Promise<void>}
 */
async function findInputByLabelAndFill(driver, labelText, inputText, context = driver){
    const inputBy = await byInputByLabel(driver, labelText, context);
    await fillInput(driver, inputBy, inputText);
}

/**
 * Executes a test by connecting to an existing web driver using the information in the command line arguments.
 * @param {function(WebDriver, string, string):Promise<void>} test The test to run. The WebDriver, start URL are provided
 * every time. The EmailUIUrl is provided if it's available.
 * @param {function(chrome.Options):chrome.Options} configureOptions Update the configuration before the driver is built.
 * @returns {Promise<void>} Success if the driver is created and the test has run to completion.
 */
async function runTest(test, configureOptions = null) {
    const args = process.argv.slice(2);
    if (args.length < 4) throw new Error('Usage: node script.js driverPath startUrl tempDirectory browserName (EmailUIUrl)');
    const [driverPath, startUrl, tempDirectory, browserName, emailUIUrl] = args;

    if (browserName !== 'Chrome') throw new Error('Only Chrome is supported at this time.');

    let options = new chrome.Options().addArguments('ignore-certificate-errors');

    // The current versions can be retrieved here:
    // https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions.json. This version number is
    // updated automatically by Renovate.
    // If anything on this line is changed, be sure to adjust the regex in the renovate.json5 config file in the root
    // too.
    options.setBrowserVersion('136.0.7103.113');

    console.log(`Using Chrome version ${options.getBrowserVersion()}.`);

    const argumentsPath = path.join(tempDirectory, 'BrowserArguments.json');
    if (await _exists(argumentsPath)) {
        JSON.parse(await fs.readFile(argumentsPath, { encoding: 'utf8' }))
            .forEach(argument => {
                options = options.addArguments(argument);
            });
    }

    if (process.env.GITHUB_ENV) options = options.addArguments('headless');
    if (configureOptions) options = configureOptions(options) ?? options;

    const driver = await new Builder()
        .forBrowser('chrome')
        .setChromeOptions(options)
        .build();
    await driver.manage().setTimeouts({ implicit: 10000 });

    try {
        await navigate(driver, startUrl);

        await driver.manage().window().setRect({x: 0, y: 0, width: 1920, height: 1080});

        console.log('Starting test...');

        await test(driver, startUrl, emailUIUrl);

        console.log('Test ran successfully.')
    }
    catch (exception) {
        // Write out some context, doesn't matter if these fail.
        try { console.log('Title:', await driver.getTitle()); } catch (error) { console.error(error); }
        try { console.log('URL:', await driver.getCurrentUrl()); } catch (error) { console.error(error); }
        try { await _logSource(driver); } catch (error) { console.error(error); }

        const screenshotPath = path.join(tempDirectory, 'Screenshots', 'error.png');
        console.log(`Writing screenshot to ${screenshotPath}...`);
        await _takeScreenshot(driver, screenshotPath);
        console.log('Done.')

        throw exception;
    }
    finally {
        await driver.close();
    }
}

export {
    runTest,
    shouldContainText,
    navigate,
    fillInput,
    waitForPageLoad,
    safeWait,
    clickSafely,
    switchToTab,
    openEmailWithSubject,
    waitForUrl,
    waitUntilInputValueIsNotEmpty,
    waitUntilClassIsRemoved,
    findElementBy,
    findElementByXpath,
    findElementByInputValue,
    findInputByLabel,
    findInputByLabelAndFill,
    findElementByTagAndContainingText,
    findLinkByTextInEmailThenClick,
    findLinkByTextThenClick,
    findByThenClick,
    findByTagAndContainingTextThenClick
};
