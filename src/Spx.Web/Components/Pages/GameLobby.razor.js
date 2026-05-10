const timelineObservers = new WeakMap();
const composerListeners = new WeakMap();

export function setTimelineObserver(container, sentinel, dotNetRef) {
    disposeTimelineObserver(sentinel);

    if (!container || !sentinel || !dotNetRef) {
        return;
    }

    const observer = new IntersectionObserver(entries => {
        if (entries.some(entry => entry.isIntersecting)) {
            dotNetRef.invokeMethodAsync("HandleTimelineNearTopAsync");
        }
    }, {
        root: container,
        threshold: 0.1
    });

    observer.observe(sentinel);
    timelineObservers.set(sentinel, observer);
}

export function disposeTimelineObserver(sentinel) {
    const observer = sentinel ? timelineObservers.get(sentinel) : null;
    if (!observer) {
        return;
    }

    observer.disconnect();
    timelineObservers.delete(sentinel);
}

export function setComposerSubmit(textarea, dotNetRef) {
    clearComposerSubmit(textarea);

    if (!textarea || !dotNetRef) {
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
    const listener = textarea ? composerListeners.get(textarea) : null;
    if (!listener) {
        return;
    }

    textarea.removeEventListener("keydown", listener);
    composerListeners.delete(textarea);
}

export function getScrollMetrics(container) {
    if (!container) {
        return {
            scrollTop: 0,
            scrollHeight: 0,
            clientHeight: 0
        };
    }

    return {
        scrollTop: container.scrollTop,
        scrollHeight: container.scrollHeight,
        clientHeight: container.clientHeight
    };
}

export function restoreScrollAfterPrepend(container, previousScrollHeight, previousScrollTop) {
    if (!container) {
        return;
    }

    container.scrollTop = container.scrollHeight - previousScrollHeight + previousScrollTop;
}

export function scrollToBottom(container) {
    if (!container) {
        return;
    }

    container.scrollTop = container.scrollHeight;
}

export function isNearBottom(container, threshold = 96) {
    if (!container) {
        return true;
    }

    return container.scrollHeight - container.scrollTop - container.clientHeight <= threshold;
}