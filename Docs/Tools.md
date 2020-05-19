# Tools we use



- The testing framework we use is [Selenium](https://www.selenium.dev/), extended with [Atata](https://atata.io/).
- Chrome needs to be installed on your machine. If you also want to test other browsers then:
  - For Edge only the new Chromium-based Edge is supported that you can download from [here](https://www.microsoft.com/en-us/edge).
  - For Firefox it needs to be installed too.
  - For IE you'll need to do [some manual configuration](https://github.com/SeleniumHQ/selenium/wiki/InternetExplorerDriver#required-configuration) in the browser.
- There are multiple recording tools available for Selenium but the "official" one which works pretty well is [Selenium IDE](https://www.selenium.dev/selenium-ide/) (which is a Chrome/Firefox extension). To fine-tune XPath queries and CSS selectors and also to record tests check out [ChroPath](https://chrome.google.com/webstore/detail/chropath/ljngjbnaijcbncmcnjfhigebomdlkcjo/).
- Recorded tests are then executed from xUnit tests.
