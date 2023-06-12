(function (window) {

    window.chrome.webview.postMessage({
        type: "Test",
        data: "Hello, World!"
    });
    window.console.log("TEST LOG 2");

})(window);

