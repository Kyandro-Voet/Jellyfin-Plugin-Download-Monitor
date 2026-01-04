(function () {
    'use strict';

    let retryCount = 0;
    const MAX_RETRIES = 30;

    // --- HELPER 1: URL Parameter Checker ---
    function isDownloadPage() {
        return window.location.hash.includes('view=downloadmonitor');
    }

    // --- HELPER 2: Sluit het menu ---
    function closeDrawer() {
        const backdrop = document.querySelector('.mainDrawer-backdrop');
        if (backdrop) backdrop.click();

        document.body.classList.remove('mainDrawer-open');
        const drawer = document.querySelector('.mainDrawer');
        if (drawer) drawer.classList.remove('mainDrawer-open');
    }

    // --- HELPER 3: Schakel tussen Jellyfin Home en Onze Page ---
    function toggleViews() {
        const viewContainer = document.querySelector('.mainAnimatedPages');
        if (!viewContainer) return;

        const myPage = document.getElementById('downloadMonitorPage');
        const jellyfinPages = viewContainer.querySelectorAll('.mainAnimatedPage');

        if (isDownloadPage()) {
            // ---> We zijn op de Download Pagina

            // 1. Forceer ALLES anders onzichtbaar met inline styles
            // Dit is veiliger dan classes weghalen/toevoegen
            jellyfinPages.forEach(p => {
                if (p.id !== 'downloadMonitorPage') {
                    p.style.display = 'none';
                    p.setAttribute('aria-hidden', 'true');
                }
            });

            // 2. Toon onze pagina
            if (myPage) {
                myPage.style.display = 'block';
                myPage.removeAttribute('aria-hidden');
            } else {
                loadDownloadsHtml(viewContainer);
            }

        } else {
            // ---> We zijn op een normale Jellyfin pagina (Home, Movies, etc.)

            // 1. Verberg onze pagina
            if (myPage) {
                myPage.style.display = 'none';
                myPage.setAttribute('aria-hidden', 'true');
            }

            // 2. HERSTEL de andere pagina's
            // We halen onze geforceerde 'none' weg.
            // Als Jellyfin de pagina zelf verborgen wil houden (met class 'hide'), blijft hij nu verborgen!
            jellyfinPages.forEach(p => {
                if (p.id !== 'downloadMonitorPage') {
                    p.style.display = '';
                    // We raken aria-hidden niet aan, dat doet Jellyfin zelf
                }
            });
        }
    }

    function loadDownloadsHtml(container) {
        fetch('/Plugins/DownloadMonitor/Page')
            .then(response => response.text())
            .then(html => {
                const tempDiv = document.createElement('div');
                tempDiv.innerHTML = html;
                const newPage = tempDiv.querySelector('[data-role="page"]');

                if (newPage) {
                    newPage.id = 'downloadMonitorPage';
                    newPage.classList.add('mainAnimatedPage', 'libraryPage', 'page', 'type-interior');

                    // Zorg dat hij standaard verborgen is tot toggleViews draait
                    newPage.style.display = 'none';

                    container.appendChild(newPage);

                    const scripts = newPage.querySelectorAll('script');
                    scripts.forEach(oldScript => {
                        const newScript = document.createElement('script');
                        newScript.textContent = oldScript.textContent;
                        oldScript.parentNode.replaceChild(newScript, oldScript);
                    });

                    // Check direct de URL om te zien of we hem moeten tonen
                    toggleViews();
                }
            })
            .catch(err => console.error('Failed to load downloads page:', err));
    }

    function addDownloadsMenuItem() {
        const drawerScroll = document.querySelector('.mainDrawer-scrollContainer');
        if (!drawerScroll) {
            if (++retryCount < MAX_RETRIES) setTimeout(addDownloadsMenuItem, 500);
            return;
        }

        if (document.querySelector('#nav-downloadmonitor')) return;

        const allLinks = drawerScroll.querySelectorAll('a');
        let anchorLink = null;
        for (let i = 0; i < allLinks.length; i++) {
            const text = (allLinks[i].textContent || '').trim().toLowerCase();
            if (text.includes('movie') || text.includes('film') || text.includes('lib')) {
                anchorLink = allLinks[i];
            }
        }

        const navItem = document.createElement('a');
        navItem.id = 'nav-downloadmonitor';
        navItem.href = '#!/home.html?view=downloadmonitor';
        navItem.className = anchorLink ? anchorLink.className : 'navMenuOption navMenuOption-withIcon';

        navItem.addEventListener('click', function(e) {
            e.preventDefault();
            closeDrawer();
            window.location.hash = '#!/home.html?view=downloadmonitor';
        });

        navItem.innerHTML = `
            <span class="material-icons navMenuOption-icon" style="margin-right: 1em;">download</span>
            <span class="navMenuOptionText">Downloads</span>
        `;

        if (anchorLink) {
            anchorLink.insertAdjacentElement('afterend', navItem);
        } else {
            const parent = drawerScroll.querySelector('.navMenuOptions') || drawerScroll;
            parent.appendChild(navItem);
        }
    }

    // --- EVENT LISTENERS ---

    window.addEventListener('hashchange', toggleViews);

    document.addEventListener('viewshow', function() {
        setTimeout(toggleViews, 50);
        setTimeout(addDownloadsMenuItem, 200);
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            addDownloadsMenuItem();
            toggleViews();
        });
    } else {
        addDownloadsMenuItem();
        toggleViews();
    }
})();
