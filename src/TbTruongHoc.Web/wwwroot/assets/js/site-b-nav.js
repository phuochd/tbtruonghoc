/**
 * Story 2.1: Site B nav behavior (_SiteBNav.cshtml). Progressive
 * enhancement - the layout adds a "js" class to <html> up front, and
 * site-b.css only collapses the mobile sheet / tablet submenus under that
 * class, so without this script the whole menu stays reachable.
 *
 * - Hamburger ([data-sb-nav-toggle]) opens/closes the mobile sheet: flips
 *   its aria-expanded and the nav root's data-menu-open.
 * - Each submenu disclosure button ([data-sb-submenu-toggle]) flips its own
 *   aria-expanded (CSS shows the adjacent submenu); opening one closes the
 *   others. Click / Enter / Space only - never hover.
 * - Escape with focus inside the nav closes an open submenu (focus back to
 *   its button), otherwise the open sheet (focus back to the hamburger).
 *   Escape elsewhere closes both without moving focus.
 * - Focus leaving the nav (focusout to an element outside it) closes both.
 * - A click outside the nav closes any open submenu.
 *
 * No framework/bundler, same as lead-form.js.
 */
(function () {
    'use strict';

    function isOpen(button) {
        return button.getAttribute('aria-expanded') === 'true';
    }

    function init(root) {
        var toggle = root.querySelector('[data-sb-nav-toggle]');
        var subToggles = Array.prototype.slice.call(root.querySelectorAll('[data-sb-submenu-toggle]'));

        function setMenu(open) {
            if (!toggle) {
                return;
            }
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
            root.setAttribute('data-menu-open', open ? 'true' : 'false');
        }

        function setSubmenu(button, open) {
            button.setAttribute('aria-expanded', open ? 'true' : 'false');
        }

        function closeAllSubmenus(except) {
            subToggles.forEach(function (b) {
                if (b !== except) {
                    setSubmenu(b, false);
                }
            });
        }

        if (toggle) {
            toggle.addEventListener('click', function () {
                setMenu(!isOpen(toggle));
            });
        }

        subToggles.forEach(function (button) {
            button.addEventListener('click', function () {
                var open = !isOpen(button);
                closeAllSubmenus(button);
                setSubmenu(button, open);
            });
        });

        document.addEventListener('keydown', function (event) {
            if (event.key !== 'Escape' && event.key !== 'Esc') {
                return;
            }
            // Escape pressed elsewhere on the page (e.g. in the quote form)
            // must never pull focus back into the nav: just close quietly.
            if (!root.contains(document.activeElement)) {
                closeAllSubmenus(null);
                setMenu(false);
                return;
            }
            for (var i = 0; i < subToggles.length; i++) {
                if (isOpen(subToggles[i])) {
                    setSubmenu(subToggles[i], false);
                    subToggles[i].focus();
                    return;
                }
            }
            if (toggle && isOpen(toggle)) {
                setMenu(false);
                toggle.focus();
            }
        });

        // Tabbing out of the nav closes whatever is open, so focus never
        // lands on content still covered by the mobile sheet or a submenu.
        // A null relatedTarget (focus went nowhere, e.g. a tap on the sheet's
        // own padding) is not "outside".
        root.addEventListener('focusout', function (event) {
            var next = event.relatedTarget;
            if (next && !root.contains(next)) {
                closeAllSubmenus(null);
                setMenu(false);
            }
        });

        document.addEventListener('click', function (event) {
            if (!root.contains(event.target)) {
                closeAllSubmenus(null);
            }
        });
    }

    window.siteBNav = { init: init };

    function start() {
        var root = document.querySelector('[data-sb-nav]');
        if (root) {
            init(root);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
