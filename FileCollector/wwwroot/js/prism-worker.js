// File: C:\Users\Administrator\RiderProjects\FileCollector\FileCollector\wwwroot\js\prism-worker.js
//--------------------------------------------------
// Attempt to import PrismJS scripts.
// Note: importScripts base path is relative to the worker's location.
// If Prism is already loaded globally in the main thread, it's not automatically available here.
// We must load it specifically for the worker's scope.
try {
    self.importScripts(
        'https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-core.min.js',
        'https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/plugins/autoloader/prism-autoloader.min.js'
        // Line numbers plugin is not directly used here as we construct HTML manually
    );

    // Configure autoloader path for Prism within the worker
    if (self.Prism && self.Prism.plugins && self.Prism.plugins.autoloader) {
        self.Prism.plugins.autoloader.languages_path = 'https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/';
    } else {
        console.warn('Prism autoloader not available in worker.');
    }
} catch (e) {
    console.error('Error importing scripts in prism-worker:', e);
    // Post an error back for any pending requests if the worker fails to initialize
    self.postMessage({ error: "Worker script import failed: " + e.message });
}


self.onmessage = function (event) {
    const { uniqueId, code, language } = event.data;

    if (!self.Prism) {
        self.postMessage({ uniqueId, html: null, error: "Prism.js not loaded in worker." });
        return;
    }

    try {
        // Ensure the language is available. Autoloader should handle this.
        // If a language is not found, Prism.highlight might throw or return unhighlighted code.
        const grammar = self.Prism.languages[language] || self.Prism.languages.clike; // Fallback to clike
        const highlightedCode = self.Prism.highlight(code, grammar, language);

        // Manually generate line numbers HTML structure
        const lines = code.split('\n');
        // Prism's line-numbers plugin expects the final line not to be empty if it had content,
        // or it might miscount. Our `content.TrimEnd('\r', '\n')` in C# helps.
        const linesNum = lines.length;
        let lineNumbersWrapper = '';

        // Add line numbers if content is not empty and has more than one line or if it's a single non-empty line.
        if (code.length > 0) {
            lineNumbersWrapper = '<span aria-hidden="true" class="line-numbers-rows">';
            for (let i = 0; i < linesNum; i++) {
                lineNumbersWrapper += '<span></span>';
            }
            lineNumbersWrapper += '</span>';
        }

        const finalHtml = highlightedCode + lineNumbersWrapper;
        self.postMessage({ uniqueId, html: finalHtml, error: null });

    } catch (e) {
        console.error(`Worker error highlighting ${uniqueId} (lang: ${language}):`, e);
        self.postMessage({ uniqueId, html: null, error: e.message || "Unknown error during highlighting in worker" });
    }
};
//--------------------------------------------------
// End of file: C:\Users\Administrator\RiderProjects\FileCollector\FileCollector\wwwroot\js\prism-worker.js