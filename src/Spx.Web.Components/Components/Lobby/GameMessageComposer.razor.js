const composerListeners = new WeakMap();

export function setComposerSubmit(textarea, dotNetRef) {
    clearComposerSubmit(textarea);

    if (!(textarea instanceof HTMLTextAreaElement) || !dotNetRef) {
        return;
    }

    const listener = event => {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            dotNetRef.invokeMethodAsync("HandleComposerEnterAsync");
        }
    };

    textarea.addEventListener("keydown", listener);
    composerListeners.set(textarea, listener);
}

export function clearComposerSubmit(textarea) {
    if (!(textarea instanceof HTMLTextAreaElement)) {
        return;
    }

    const listener = composerListeners.get(textarea);
    if (!listener) {
        return;
    }

    textarea.removeEventListener("keydown", listener);
    composerListeners.delete(textarea);
}