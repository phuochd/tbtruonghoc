/**
 * Story 1.10: consent gate for GA4. Included by _Analytics.cshtml only in
 * Production with a valid measurement ID (data-ga4-id on this script tag),
 * alongside _CookieConsent.cshtml's banner. The server never emits gtag.js
 * itself - this file injects it, and only after the visitor accepts (now,
 * or on an earlier visit within MAX_AGE_MS). Declining, ignoring the banner
 * or unusable storage all mean no request to Google at all.
 *
 * The choice is kept in localStorage as { choice, at }. Each site is its own
 * hostname, so storage is already per site. Every storage access is wrapped
 * in try/catch: when it throws (e.g. private mode) the banner still works
 * for the current page, the choice just isn't remembered - fail closed.
 *
 * No framework/bundler in this repo, same as lead-form.js.
 */
(function () {
    'use strict';

    var STORAGE_KEY = 'analyticsConsent';
    var MAX_AGE_MS = 180 * 24 * 60 * 60 * 1000;
    var GRANTED = 'granted';
    var DENIED = 'denied';

    // Must be read synchronously: document.currentScript is only set while
    // this file is executing, not inside the ready() callback.
    var script = document.currentScript;
    var ga4Id = script ? script.getAttribute('data-ga4-id') : null;
    var ga4Loaded = false;
    var reopened = false;

    function ready(fn) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', fn);
        } else {
            fn();
        }
    }

    function readChoice() {
        try {
            var raw = window.localStorage.getItem(STORAGE_KEY);
            if (!raw) {
                return null;
            }
            var stored = JSON.parse(raw);
            if (!stored || (stored.choice !== GRANTED && stored.choice !== DENIED) || typeof stored.at !== 'number') {
                return null;
            }
            // A future (clock moved back, tampered) or non-finite timestamp
            // would otherwise never expire - treat it as no choice.
            var age = Date.now() - stored.at;
            if (!isFinite(age) || age < 0 || age > MAX_AGE_MS) {
                return null;
            }
            return stored.choice;
        } catch (e) {
            return null;
        }
    }

    function writeChoice(choice) {
        try {
            window.localStorage.setItem(STORAGE_KEY, JSON.stringify({ choice: choice, at: Date.now() }));
        } catch (e) {
            // Not remembered; the current page still honours the choice.
        }
    }

    function loadGa4() {
        if (ga4Loaded || !ga4Id) {
            return;
        }
        ga4Loaded = true;

        window.dataLayer = window.dataLayer || [];
        // gtag.js only processes real Arguments objects, not arrays.
        window.gtag = function () { window.dataLayer.push(arguments); };
        window.gtag('js', new Date());
        window.gtag('config', ga4Id);

        var tag = document.createElement('script');
        tag.async = true;
        tag.src = 'https://www.googletagmanager.com/gtag/js?id=' + encodeURIComponent(ga4Id);
        document.head.appendChild(tag);
    }

    function banner() {
        return document.querySelector('[data-consent-banner]');
    }

    function reopenLink() {
        return document.querySelector('[data-consent-reopen]');
    }

    // The banner is fixed to the bottom of the viewport; while it is shown,
    // pad the body by its height so it never covers the end of the page
    // (contact block, footer link).
    function showBanner(moveFocus) {
        var el = banner();
        if (!el) {
            return;
        }
        el.hidden = false;
        document.body.style.paddingBottom = el.offsetHeight + 'px';
        if (moveFocus) {
            var accept = el.querySelector('[data-consent-accept]');
            if (accept) {
                accept.focus();
            }
        }
    }

    // After a choice made from a reopened banner, focus goes back to the
    // "Cài đặt cookie" trigger rather than being lost with the hidden button.
    function hideBanner() {
        var el = banner();
        if (el) {
            el.hidden = true;
        }
        document.body.style.paddingBottom = '';
        if (reopened) {
            reopened = false;
            var link = reopenLink();
            if (link) {
                link.focus();
            }
        }
    }

    function accept() {
        writeChoice(GRANTED);
        hideBanner();
        loadGa4();
    }

    // Declining after an accept on the same page cannot un-send what was
    // already sent; it stops gtag.js being injected on later page loads.
    function decline() {
        writeChoice(DENIED);
        hideBanner();
    }

    function reopen() {
        reopened = true;
        showBanner(true);
    }

    function init() {
        var el = banner();
        if (el) {
            var acceptButton = el.querySelector('[data-consent-accept]');
            var declineButton = el.querySelector('[data-consent-decline]');
            if (acceptButton) {
                acceptButton.addEventListener('click', accept);
            }
            if (declineButton) {
                declineButton.addEventListener('click', decline);
            }
        }
        // Rendered hidden: without this script (blocked, failed to load) the
        // link could do nothing, so it only appears once it is wired up.
        var link = reopenLink();
        if (link) {
            link.addEventListener('click', reopen);
            link.hidden = false;
        }

        var choice = readChoice();
        if (choice === GRANTED) {
            loadGa4();
        } else if (choice === null) {
            showBanner(false);
        }
    }

    window.analyticsConsent = {
        STORAGE_KEY: STORAGE_KEY,
        MAX_AGE_MS: MAX_AGE_MS,
        accept: accept,
        decline: decline,
        reopen: reopen
    };

    ready(init);
})();
