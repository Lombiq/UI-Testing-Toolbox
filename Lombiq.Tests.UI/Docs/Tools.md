# Tools we use



- The tests themselves are written in [xUnit](https://xunit.net/).
- The UI testing framework we use is [Selenium](https://www.selenium.dev/), extended with [Atata](https://atata.io/).
- Chrome needs to be installed on your machine, the latest version. If you also want to test other browsers then install the latest version of each of them:
  - For Edge only the new Chromium-based Edge is supported that you can download from [here](https://www.microsoft.com/en-us/edge).
  - For Firefox it needs to be installed too.
  - For IE you'll need to do [some manual configuration](https://github.com/SeleniumHQ/selenium/wiki/InternetExplorerDriver#required-configuration) in the browser.
- Browser driver setup is automated with [WebDriverManager.NET](https://github.com/rosolko/WebDriverManager.Net).
- There are multiple recording tools available for Selenium but the "official" one which works pretty well is [Selenium IDE](https://www.selenium.dev/selenium-ide/) (which is a Chrome/Firefox extension). To fine-tune XPath queries and CSS selectors and also to record tests check out [ChroPath](https://chrome.google.com/webstore/detail/chropath/ljngjbnaijcbncmcnjfhigebomdlkcjo/) (the [Xpath cheatsheet](https://devhints.io/xpath) is a great resource too, and [XmlToolBox](https://xmltoolbox.appspot.com/xpath_generator.html) can help you with quick XPath queries).
- Accessibility checking can be done with [axe](https://github.com/dequelabs/axe-core) via [Selenium.Axe for .NET](https://github.com/TroyWalshProf/SeleniumAxeDotnet).
- HTML markup validation can be done with [html-validate](https://gitlab.com/html-validate/html-validate) via [Atata.HtmlValidation](https://github.com/atata-framework/atata-htmlvalidation).
- Monkey testing is implemented using [Gremlins.js](https://github.com/marmelab/gremlins.js/) library.
