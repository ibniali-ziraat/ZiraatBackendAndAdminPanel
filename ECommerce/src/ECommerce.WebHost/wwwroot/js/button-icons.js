(function () {
    var enhanceSelector = [
        ".btn",
        ".invenzo-product-link",
        ".invenzo-primary-button",
        ".invenzo-search-submit",
        ".invenzo-auth-link"
    ].join(", ");

    var skipSelector = [
        ".button-icon-auto-enabled",
        ".button-icon-skip",
        ".admin-topbar__toggle",
        ".navbar-toggler",
        ".invenzo-nav-toggler",
        ".quantity-button",
        ".close",
        "[aria-label='Close']",
        "[aria-label='Previous']",
        "[aria-label='Next']"
    ].join(", ");

    function hasExistingIcon(element) {
        return !!element.querySelector("i, svg, .fa, .glyphicon, .material-icons, [class*='icon']");
    }

    function shouldSkip(element) {
        if (!element || !element.matches) {
            return true;
        }

        if (element.matches(skipSelector)) {
            return true;
        }

        if (element.tagName === "INPUT" || element.disabled) {
            return true;
        }

        return !((element.textContent || "").replace(/\s+/g, " ").trim());
    }

    function createIcon() {
        var icon = document.createElement("span");
        icon.className = "button-icon-auto";
        icon.setAttribute("aria-hidden", "true");
        icon.innerHTML = '<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M4 12L12 4M6 4H12V10" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>';
        return icon;
    }

    function enhanceElement(element) {
        if (shouldSkip(element) || hasExistingIcon(element)) {
            return;
        }

        element.classList.add("button-icon-auto-enabled");
        element.insertBefore(createIcon(), element.firstChild);
    }

    function process(root) {
        if (!root || root.nodeType !== 1) {
            return;
        }

        if (root.matches(enhanceSelector)) {
            enhanceElement(root);
        }

        if (!root.querySelectorAll) {
            return;
        }

        Array.prototype.forEach.call(root.querySelectorAll(enhanceSelector), enhanceElement);
    }

    function start() {
        process(document.body);

        var observer = new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                Array.prototype.forEach.call(mutation.addedNodes, function (node) {
                    process(node);
                });
            });
        });

        observer.observe(document.documentElement, {
            childList: true,
            subtree: true
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start);
    } else {
        start();
    }
}());
