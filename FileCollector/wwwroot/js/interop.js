// File: wwwroot/js/interop.js

// Ensure Prism.js is loaded before this script, or at least before these functions are called.
window.blazorInterop = {
    highlightElementById: function (elementId) {
        // This function will return a Promise, allowing Blazor to await it if needed,
        // but the actual highlighting is deferred, preventing UI blocking.
        return new Promise((resolve, reject) => {
            requestAnimationFrame(() => { // Defer to the next animation frame
                try {
                    var element = document.getElementById(elementId);
                    if (element) {
                        if (typeof Prism !== 'undefined' && typeof Prism.highlightElement === 'function') {
                            Prism.highlightElement(element);
                            // console.log("Syntax highlighting applied to: " + elementId);
                            resolve(true); // Indicate success
                        } else {
                            console.warn("Prism.js or Prism.highlightElement is not available. Cannot highlight: " + elementId);
                            resolve(false); // Indicate Prism not available
                        }
                    } else {
                        // console.warn("Element not found for highlighting: " + elementId);
                        resolve(false); // Indicate element not found
                    }
                } catch (e) {
                    console.error("Error in blazorInterop.highlightElementById for " + elementId + ":", e);
                    reject(e); // Propagate the error
                }
            });
        });
    },

    copyToClipboard: async function (text, feedbackElementId) {
        // navigator.clipboard.writeText is already asynchronous and non-blocking.
        // We make this function async to use await for cleaner promise handling.
        try {
            await navigator.clipboard.writeText(text);
            if (feedbackElementId) {
                var feedbackEl = document.getElementById(feedbackElementId);
                if (feedbackEl) {
                    feedbackEl.innerText = "Copied!";
                    feedbackEl.style.color = "green";
                    setTimeout(function () {
                        if (document.getElementById(feedbackElementId) === feedbackEl) { // Check if element still exists and is the same
                            feedbackEl.innerText = "";
                        }
                    }, 2000);
                }
            }
            return true; // Indicate success
        } catch (err) {
            console.error('Error in blazorInterop.copyToClipboard: ', err);
            if (feedbackElementId) {
                var feedbackEl = document.getElementById(feedbackElementId);
                if (feedbackEl) {
                    feedbackEl.innerText = "Copy failed!";
                    feedbackEl.style.color = "red";
                    setTimeout(function () {
                        if (document.getElementById(feedbackElementId) === feedbackEl) { // Check if element still exists and is the same
                            feedbackEl.innerText = "";
                        }
                    }, 3000);
                }
            }
            // It's good practice to either throw the error or return a status
            // so the calling Blazor code can know about the failure.
            // For now, we'll just return false, but throwing err might be better
            // if Blazor needs to specifically handle the error.
            return false; // Indicate failure
        }
    },

    highlightAllSyntax: function () {
        // Similar to highlightElementById, defer using requestAnimationFrame.
        return new Promise((resolve, reject) => {
            requestAnimationFrame(() => { // Defer to the next animation frame
                try {
                    if (typeof Prism !== 'undefined' && typeof Prism.highlightAll === 'function') {
                        Prism.highlightAll();
                        // console.log("Prism.highlightAll() called.");
                        resolve(true);
                    } else {
                        console.warn("Prism.js or Prism.highlightAll is not available.");
                        resolve(false);
                    }
                } catch (e) {
                    console.error("Error in blazorInterop.highlightAllSyntax:", e);
                    reject(e);
                }
            });
        });
    }
};