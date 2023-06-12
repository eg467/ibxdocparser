
(function(window) {
    var debugMode = true;


    function getLinks() {
        return [...document.querySelectorAll(`a[data-test="profile-link"]`)]
            .map(el => el.getAttribute("href"));
    }


    /**
     * @returns true if clicked, false if the next button doesn't exist
     */
    function advanceIfPossible() {
        const btn = document.querySelector(`button[data-test="next-page-button"]`);
        if (btn) {
            btn.click();
        }
        return !!btn;
    }

    /**
     * Waits for doctor listing links to populate and returns their href targets.
     * @param {any} prevResults The previous results, this function only after the available links have changed from these
     * @returns {Promise<string[]>} A promise containing an array of links
     */
    function waitForNewPageLinks(prevResults = []) {
        const interval = 250;
        const timeout = 3000;

        console.log("waitForNewPageLinks", prevResults);

        var lastLinksSet = new Set(prevResults);
        const anyCommonLinks = (newLinks) => !newLinks.every(l => !lastLinksSet.has(l));
        const arraysEqual = (a, b) => a && b && a.length === b.length && a.every((v, i) => b[i] === v);
        let newLinks = [];

        return new Promise((res, rej) => {
            const handle = window.setInterval(() => {
          
                var tmpLinks = getLinks();

                // Wait until links exist, stabilize, and contain no elements of the previous result set
                if (tmpLinks.length && arraysEqual(newLinks, tmpLinks) && !anyCommonLinks(tmpLinks)) {
                    console.log("waitForNewPageLinks", "success", tmpLinks);

                    res(tmpLinks);
                    window.clearTimeout(cancelHandle);
                    window.clearInterval(handle);
                }

                newLinks = tmpLinks;
            }, interval);

            var cancelHandle = setTimeout(() => {
                console.error("Timing out.");
                rej(timeout);
                window.clearInterval(handle);
            }, timeout);
        });
    }

    async function tryAddPageLinks(allLinks) {
        try {
            const prevResults = allLinks.length ? allLinks[allLinks.length - 1] : [];
            const pageLinks = await waitForNewPageLinks(prevResults);
            allLinks.push(pageLinks);
            return true;
        } catch (e) {
            console.error(e);
            return false;
        }
    }

    async function getAllLinks() {

        const allLinks = [];
        do {

            console.log("Page links parsed.");
        } while (await tryAddPageLinks(allLinks) && advanceIfPossible());

        window.chrome.webview.postMessage({
            type: "ProfileLinks",
            data: allLinks
        });
    }

    if (document.readyState === 'complete' || document.readyState === 'interactive') {
        // Run code immediately
        getAllLinks();
    } else {
        // Add an event listener to run your code when DOMContentLoaded fires
        document.addEventListener('DOMContentLoaded', getAllLinks);
    }
})(window);
