import path from 'path';
import process from 'process';
import { By, WebDriver, WebElement } from 'selenium-webdriver';
import chrome from 'selenium-webdriver/chrome.js';
import { writeFile } from 'node:fs/promises'

async function _logSource(driver) {
    const html = await driver.getPageSource();
    console.log('HTML:', html.replace(/\s*[\n\r]+\s*/g, ' '));
}

async function _takeScreenshot(driver, file){
    let image = await driver.takeScreenshot()
    await writeFile(file, image, 'base64')
}

/**
 * Verifies that the provided element's inner text contains the provided text.
 * @param {WebElement} element The web element whose inner text is examined.
 * @param {string} text The expected inner text fragment.
 * @returns {Promise<void>} Success when the element text contains the expected string, rejection if it does not or if
 *                          element is null or empty.
 */
async function shouldContainText(element, text) {
    if (element?.then) {
        element = await element;
    }

    if (!element) {
        throw new Error('The element is missing.');
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

        await driver.wait(() => driver
            .executeScript('return document.readyState')
            .then((readyState) => readyState === 'complete'));

        try {
            if ((await driver.findElement(By.xpath('//body')).getAttribute('innerHTML'))?.trim()) {
                console.log(`Successfully reached ${url}.`);
                return;
            }
        }
        catch (exception) {
            // Nothing to do here, let's try again.
        }
    }

    throw new Error(`Failed to navigate to a non-empty page at ${url} in ${maxAttempts} attempts.`)
}

/**
 * Executes a test by connecting to an existing web driver using the information in the command line arguments.
 * @param {function(WebDriver, string):Promise<void>} test
 * @param {function(chrome.Options):chrome.Options} configureOptions Update the configuration before the driver is built.
 * @returns {Promise<void>} Success if the driver is created and the test has run to completion.
 */
async function runTest(test, configureOptions = null) {
    const args = process.argv.slice(2);
    if (args.length !== 4) throw new Error('Usage: node script.js driverPath startUrl tempDirectory browserName');
    const [driverPath, startUrl, tempDirectory, browserName] = args;

    if (browserName !== 'Chrome') throw new Error("Only Chrome is supported at this time");

    let options = new chrome.Options()
        .addArguments('disable-dev-shm-usage')
        .addArguments('unsafely-disable-devtools-self-xss-warnings')
        .addArguments('disable-search-engine-choice-screen')
        .addArguments('--lang=en-US')
        .addArguments('disable-accelerated-2d-canvas')
        .addArguments('disable-gpu')
        .addArguments('force-color-profile=sRGB')
        .addArguments('force-device-scale-factor=1')
        .addArguments('high-dpi-support=1')
        .addArguments('disable-smooth-scrolling')
        .addArguments('ignore-certificate-errors')
        .addArguments('--ignore-certificate-errors')
        .addArguments('--no-sandbox')
        ;

    if (process.env.GITHUB_ENV) options = options.addArguments('headless');
    if (configureOptions) options = configureOptions(options) ?? options;

    const service = new chrome.ServiceBuilder(driverPath).build();
    const driver = chrome.Driver.createSession(options, service);
    await driver.manage().setTimeouts({ implicit: 10000 });

    try {
        await navigate(driver, startUrl);

        await test(driver, startUrl);
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
};
