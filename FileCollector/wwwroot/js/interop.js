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
    },
    setCheckboxIndeterminate: function (element, isIndeterminate) {
        if (element) {
            element.indeterminate = isIndeterminate;
        }
    },
    initializeResizer: function (resizerId, panelId) {
        const resizer = document.getElementById(resizerId);
        const panel = document.getElementById(panelId);

        if (!resizer || !panel) {
            console.error("Resizer or panel element not found.", { resizerId, panelId });
            return;
        }

        const handleMouseMove = (e) => {
            // We calculate the new width based on the mouse's clientX and the container's starting position.
            const container = panel.parentElement;
            const containerRect = container.getBoundingClientRect();
            const newWidth = e.clientX - containerRect.left;

            // The browser will respect the min-width and max-width set in CSS when we set flex-basis.
            panel.style.flexBasis = `${newWidth}px`;
        };

        const handleMouseUp = () => {
            resizer.classList.remove('is-resizing');
            // Re-enable text selection and pointer events on the body
            document.body.style.userSelect = '';
            document.body.style.pointerEvents = '';

            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', handleMouseUp);
        };

        resizer.addEventListener('mousedown', (e) => {
            e.preventDefault();
            resizer.classList.add('is-resizing');
            // Disable text selection and pointer events on the body to prevent interference during drag
            document.body.style.userSelect = 'none';
            document.body.style.pointerEvents = 'none';

            document.addEventListener('mousemove', handleMouseMove);
            document.addEventListener('mouseup', handleMouseUp);
        });
    }
};