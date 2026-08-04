const buttons = document.getElementsByClassName('sectionbutton');

for (let i = 0; i < buttons.length; i++) {
    buttons[i].addEventListener('click', function onSectionButtonClick() {
        const expandoText = this.getElementsByClassName('buttonExpandoText')[0];
        this.classList.toggle('active');

        const content = this.nextElementSibling;
        if (expandoText.innerHTML === '-') {
            content.style.maxHeight = '0';
            expandoText.innerHTML = '+';
        }
        else {
            content.style.maxHeight = content.scrollHeight + 'px';
            expandoText.innerHTML = '-';
        }
    });
}

const thumbnail = document.getElementById('screenshotThumbnail');
const modal = document.getElementById('modal');
const modalImage = document.getElementById('modalimage');

if (thumbnail && modal && modalImage) {
    thumbnail.addEventListener('click', () => {
        modal.style.display = 'flex';
        modalImage.src = thumbnail.currentSrc || thumbnail.src;
        modalImage.alt = thumbnail.alt;
    });

    modal.addEventListener('click', () => {
        modal.style.display = 'none';
        modalImage.src = '';
        modalImage.alt = '';
    });
}
