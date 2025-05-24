window.blazorInterop = {
    copyToClipboard: async function (text, feedbackElementId) {
        const feedbackEl = document.getElementById(feedbackElementId);
        try {
            await navigator.clipboard.writeText(text);
            if (feedbackElementId) {
                if (feedbackEl) {
                    feedbackEl.innerText = "Copied!";
                    feedbackEl.style.color = "green";
                    setTimeout(function () {
                        if (document.getElementById(feedbackElementId) === feedbackEl) {
                            feedbackEl.innerText = "";
                        }
                    }, 2000);
                }
            }
            return true;
        } catch (err) {
            console.error('Error in blazorInterop.copyToClipboard: ', err);
            if (feedbackElementId) {
                if (feedbackEl) {
                    feedbackEl.innerText = "Copy failed!";
                    feedbackEl.style.color = "red";
                    setTimeout(function () {
                        if (document.getElementById(feedbackElementId) === feedbackEl) {
                            feedbackEl.innerText = "";
                        }
                    }, 3000);
                }
            }
            return false;
        }
    }
};