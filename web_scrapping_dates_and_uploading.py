import lxml.html as lh
import requests
import time
from bs4 import BeautifulSoup
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.firefox.options import Options

def save_content(URL):
    law_page = requests.get(URL)
    law_go_kr_soup = BeautifulSoup(law_page.content, "html.parser")
    status_code = law_go_kr_soup.find("div", id="error500")
    if status_code != None:
        return "error500"
    else:
        doc = lh.fromstring(law_page.content)
        elt = doc.xpath('//iframe[@id="lawService"]')
        url_data = elt[0].attrib.get('src')
        data = requests.get("https://www.law.go.kr/"+url_data)
        law_go_kr_soup = BeautifulSoup(data.content, "html.parser")
        body = law_go_kr_soup.find("div", id="contentBody")
        for script in body(["script", "style"]):
            script.extract()
        text = body.get_text()
        lines = (line.strip() for line in text.splitlines())
        chunks = (phrase.strip() for line in lines for phrase in line.split("  "))
        text = '\n'.join(chunk for chunk in chunks if chunk)
        return text


def upload_content(driver, date_element, text):
    URL_f = "https://ko-wikisource-org.translate.goog/w/index.php?title="
    URL_t = "&action=edit&redlink=1&_x_tr_sl=en&_x_tr_tl=ru&_x_tr_hl=en&_x_tr_pto=wapp"
    driver.get(URL_f + date_element.text + URL_t)
    driver.find_element(By.TAG_NAME, "textarea").send_keys(text)
    elem = driver.find_element(By.XPATH, "//input[@type='submit']")
    elem.click()


def main():
    URL_wiki = "https://ko-wikisource-org.translate.goog/wiki/%EC%82%AC%EC%9A%A9%EC%9E%90:MyReceivership?_x_tr_sl=en&_x_tr_tl=ru&_x_tr_hl=en&_x_tr_pto=wapp#%EB%AF%BC%EB%B2%95_%ED%8C%90%EB%A1%80"
    wiki_page = requests.get(URL_wiki)

    wiki_soup = BeautifulSoup(wiki_page.content, "html.parser")

    results = wiki_soup.find("ol")
    date_elements = results.find_all("a", class_="new")

    URL_law = "https://www.law.go.kr/%ED%8C%90%EB%A1%80/("

    options = Options()
    options.add_argument('--headless')
    driver = webdriver.Firefox(options=options)

    for date_element in date_elements:
        time.sleep(5)
        print(date_element.text)
        print("!Checking information!")
        text = save_content(URL_law + date_element.text + ")")
        if text != "error500":
            print("!Uploading content!")
            upload_content(driver, date_element, text)
            print("DONE")
        else:
            print("!WEB SITE ERROR: 500!")
            print("!NO DATA WAS FOUND ABOUT" + date_element.text + "!")

    print("!Uploading is complete!")
    print("!Shutting down WebDriver!")
    driver.quit()

if __name__ == '__main__':
    main()
