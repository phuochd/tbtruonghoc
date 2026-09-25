/*
    Story 1.8 (AD-1): per-site tabs on Piranha Manager's Pages screen.

    Client-side state for the app-local PageList.cshtml override. Piranha's
    own piranha.pagelist Vue app (and its API/JS) stays untouched: the
    override's in-DOM template reaches this object as a global, exactly the
    way it already reaches piranha.permissions.

    Must load BEFORE piranha.pagelist.min.js, because that script compiles
    and renders the #pagelist template immediately.

    The active tab is deliberately NOT persisted to any browser storage
    (decision 2026-09-25): every full page load starts on the first
    (default) site from the API's `sites` array.
*/
(function (window, Vue) {
    "use strict";

    // Reactive, so reading activeId inside piranha.pagelist's render
    // registers a dependency and switching tabs re-renders without any
    // fetch or reload.
    var tabs = Vue.observable({ activeId: null });

    // Effective active site id for the given sites array. Pure - never
    // writes back to activeId, so transient states like Piranha's own
    // drag-drop reload (which briefly sets sites = []) don't lose the
    // selected tab. If the stored id is no longer among the sites (e.g. that
    // Site was deleted), fall back to the first site.
    tabs.resolveId = function (sites) {
        var activeId = tabs.activeId;

        if (!sites || sites.length === 0) {
            return null;
        }
        for (var n = 0; n < sites.length; n++) {
            if (sites[n].id === activeId) {
                return activeId;
            }
        }
        return sites[0].id;
    };

    tabs.isActive = function (siteId, sites) {
        return siteId === tabs.resolveId(sites);
    };

    tabs.select = function (siteId) {
        tabs.activeId = siteId;
    };

    // WAI-ARIA tabs keyboard pattern: Left/Right (wrapping), Home, End move
    // selection and focus to the target tab.
    tabs.onKeydown = function (event, sites) {
        // Leave modified keys (e.g. Alt+Arrow = browser Back/Forward) alone.
        if (event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) {
            return;
        }
        if (!sites || sites.length === 0) {
            return;
        }

        var current = 0;
        var activeId = tabs.resolveId(sites);
        for (var n = 0; n < sites.length; n++) {
            if (sites[n].id === activeId) {
                current = n;
                break;
            }
        }

        var target;
        switch (event.key) {
            case "ArrowRight":
            case "Right":
                target = (current + 1) % sites.length;
                break;
            case "ArrowLeft":
            case "Left":
                target = (current - 1 + sites.length) % sites.length;
                break;
            case "Home":
                target = 0;
                break;
            case "End":
                target = sites.length - 1;
                break;
            default:
                return;
        }

        event.preventDefault();
        tabs.select(sites[target].id);

        Vue.nextTick(function () {
            var el = document.getElementById("site-tab-" + sites[target].id);
            if (el) {
                el.focus();
            }
        });
    };

    // Vue 2 templates resolve identifiers against the instance, not window
    // (a bare global renders as undefined). Putting it on Vue.prototype makes
    // `managerPageTabs` visible to piranha.pagelist's in-DOM template.
    Vue.prototype.managerPageTabs = tabs;
    window.managerPageTabs = tabs;
})(window, Vue);
