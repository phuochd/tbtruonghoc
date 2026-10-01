/**
 * Story 6.1: Site A shell behavior (_SiteANav.cshtml, _SiteAHero.cshtml).
 * Progressive enhancement - the layout adds a "js" class to <html> up
 * front, and site-a.css only collapses the sheet / dropdown / accordion and
 * shows carousel controls under that class, so without this script every
 * link stays reachable and the first hero photo still shows.
 *
 * Nav ([data-sa-nav]):
 * - Hamburger ([data-sa-nav-toggle]) opens/closes the < 1024px sheet
 *   (aria-expanded + the root's data-menu-open); the sheet's close button
 *   ([data-sa-nav-close]) closes it and returns focus to the hamburger.
 * - "Sản phẩm" ([data-sa-dropdown] item, [data-sa-dropdown-toggle] button):
 *   click / Enter / Space toggles it (desktop dropdown or mobile accordion,
 *   CSS decides); ArrowDown on the button opens it and focuses the first
 *   row; ArrowDown / ArrowUp / Home / End move through the rows
 *   ([data-sa-dropdown-item]); Tab moves naturally. At >= 1024px it also
 *   opens on hover and a hover-opened dropdown closes when the pointer
 *   leaves. Focus leaving it (desktop) or a click outside it closes it.
 * - Escape with focus inside the nav closes an open dropdown (focus back to
 *   its button), otherwise the open sheet (focus back to the hamburger).
 *   Escape elsewhere closes both without moving focus.
 *
 * Hero carousel ([data-sa-carousel]): manual only - prev / next / dot
 * buttons show one slide ([data-sa-slide], the others get `hidden`) and
 * mark the current dot with aria-current. No auto-advance, no timers.
 *
 * Story 6.2 - aggregate page category search ([data-sa-catalog]): the
 * search input ([data-sa-cat-search]) and the filter chips
 * ([data-sa-cat-chip="key"], aria-pressed toggles) filter the tiles
 * ([data-sa-cat-tile], `hidden` when filtered out) live. A tile shows when
 * its data-sa-search text contains the query (case- and diacritic-
 * insensitive, "đ" = "d") AND - when any chip is pressed - its
 * data-sa-groups (JSON array of keys) holds at least one pressed chip's
 * key. The placeholder tile is not a [data-sa-cat-tile], so it always
 * shows. No match -> the [data-sa-cat-empty] live region gets its
 * data-message; otherwise it is emptied.
 *
 * Story 6.4 - modal opener ([data-sa-modal-open="{dialog id}"]): a click
 * opens that native <dialog> with showModal() (the opener's aria-expanded
 * follows it). The dialog closes on a [data-sa-modal-close] button inside
 * it, on Esc (native), and on a click on its backdrop (a click whose target
 * is the <dialog> itself - the visible panel fills it). On close, focus
 * returns to the opener. Generic, so 6.5's survey modal reuses it.
 *
 * Story 6.4 - PDP photo badge ([data-sa-pdp-gallery="true"]): after a click in the
 * gallery (sb-gallery.js has already swapped the image on the thumbnail's
 * own listener), the main image's [data-sa-photo-badge] shows only while the
 * current thumbnail (aria-current="true") is a genuine project photo
 * (data-genuine="true"). sb-gallery.js itself is untouched.
 *
 * No framework/bundler, same as site-b-nav.js.
 */
(function () {
    'use strict';

    var DESKTOP_QUERY = '(min-width: 1024px)';

    function isOpen(button) {
        return !!button && button.getAttribute('aria-expanded') === 'true';
    }

    function isDesktop() {
        return !!(window.matchMedia && window.matchMedia(DESKTOP_QUERY).matches);
    }

    function toArray(list) {
        return Array.prototype.slice.call(list || []);
    }

    function initNav(root) {
        var toggle = root.querySelector('[data-sa-nav-toggle]');
        var closeButton = root.querySelector('[data-sa-nav-close]');

        var dropdowns = toArray(root.querySelectorAll('[data-sa-dropdown]')).map(function (item) {
            return {
                item: item,
                button: item.querySelector('[data-sa-dropdown-toggle]'),
                rows: toArray(item.querySelectorAll('[data-sa-dropdown-item]')),
                byHover: false
            };
        }).filter(function (d) { return !!d.button; });

        function setMenu(open) {
            if (!toggle) {
                return;
            }
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
            root.setAttribute('data-menu-open', open ? 'true' : 'false');
        }

        function setDropdown(d, open, byHover) {
            d.button.setAttribute('aria-expanded', open ? 'true' : 'false');
            d.byHover = open && !!byHover;
        }

        function closeDropdowns(except) {
            dropdowns.forEach(function (d) {
                if (d !== except) {
                    setDropdown(d, false);
                }
            });
        }

        function focusRow(d, index) {
            if (d.rows.length === 0) {
                return;
            }
            var i = (index + d.rows.length) % d.rows.length;
            d.rows[i].focus();
        }

        if (toggle) {
            toggle.addEventListener('click', function () {
                setMenu(!isOpen(toggle));
            });
        }

        if (closeButton) {
            closeButton.addEventListener('click', function () {
                setMenu(false);
                if (toggle) {
                    toggle.focus();
                }
            });
        }

        dropdowns.forEach(function (d) {
            d.button.addEventListener('click', function () {
                var open = !isOpen(d.button) || d.byHover;
                closeDropdowns(d);
                setDropdown(d, open, false);
            });

            d.button.addEventListener('keydown', function (event) {
                if (event.key === 'ArrowDown' || event.key === 'Down') {
                    if (event.preventDefault) {
                        event.preventDefault();
                    }
                    closeDropdowns(d);
                    setDropdown(d, true, false);
                    focusRow(d, 0);
                }
            });

            d.rows.forEach(function (row, index) {
                row.addEventListener('keydown', function (event) {
                    var target = null;
                    if (event.key === 'ArrowDown' || event.key === 'Down') {
                        target = index + 1;
                    } else if (event.key === 'ArrowUp' || event.key === 'Up') {
                        target = index - 1;
                    } else if (event.key === 'Home') {
                        target = 0;
                    } else if (event.key === 'End') {
                        target = d.rows.length - 1;
                    }
                    if (target !== null) {
                        if (event.preventDefault) {
                            event.preventDefault();
                        }
                        focusRow(d, target);
                    }
                });
            });

            d.item.addEventListener('mouseenter', function () {
                if (!isDesktop() || isOpen(d.button)) {
                    return;
                }
                closeDropdowns(d);
                setDropdown(d, true, true);
            });

            d.item.addEventListener('mouseleave', function () {
                if (isDesktop() && d.byHover) {
                    setDropdown(d, false);
                }
            });

            // Desktop: tabbing out of the dropdown closes it. (In the mobile
            // accordion it stays expanded while the user tabs on.)
            d.item.addEventListener('focusout', function (event) {
                var next = event.relatedTarget;
                if (next && !d.item.contains(next) && isDesktop()) {
                    setDropdown(d, false);
                }
            });
        });

        document.addEventListener('keydown', function (event) {
            if (event.key !== 'Escape' && event.key !== 'Esc') {
                return;
            }
            // Escape pressed elsewhere on the page must never pull focus
            // back into the nav: just close quietly.
            if (!root.contains(document.activeElement)) {
                closeDropdowns(null);
                setMenu(false);
                return;
            }
            for (var i = 0; i < dropdowns.length; i++) {
                if (isOpen(dropdowns[i].button)) {
                    setDropdown(dropdowns[i], false);
                    dropdowns[i].button.focus();
                    return;
                }
            }
            if (toggle && isOpen(toggle)) {
                setMenu(false);
                toggle.focus();
            }
        });

        // Tabbing out of the nav closes whatever is open, so focus never
        // lands on content still covered by the sheet or the dropdown. A
        // null relatedTarget (focus went nowhere) is not "outside".
        root.addEventListener('focusout', function (event) {
            var next = event.relatedTarget;
            if (next && !root.contains(next)) {
                closeDropdowns(null);
                setMenu(false);
            }
        });

        // Click-away: a click outside a dropdown's item closes it.
        document.addEventListener('click', function (event) {
            dropdowns.forEach(function (d) {
                if (!d.item.contains(event.target)) {
                    setDropdown(d, false);
                }
            });
        });
    }

    function initCarousel(root) {
        var slides = toArray(root.querySelectorAll('[data-sa-slide]'));
        var dots = toArray(root.querySelectorAll('[data-sa-carousel-dot]'));
        var prev = root.querySelector('[data-sa-carousel-prev]');
        var next = root.querySelector('[data-sa-carousel-next]');
        var current = 0;

        if (slides.length < 2) {
            return;
        }

        function go(index) {
            current = (index + slides.length) % slides.length;
            slides.forEach(function (slide, i) {
                if (i === current) {
                    slide.removeAttribute('hidden');
                } else {
                    slide.setAttribute('hidden', 'hidden');
                }
            });
            dots.forEach(function (dot, i) {
                if (i === current) {
                    dot.setAttribute('aria-current', 'true');
                } else {
                    dot.removeAttribute('aria-current');
                }
            });
        }

        if (prev) {
            prev.addEventListener('click', function () { go(current - 1); });
        }
        if (next) {
            next.addEventListener('click', function () { go(current + 1); });
        }
        dots.forEach(function (dot, i) {
            dot.addEventListener('click', function () { go(i); });
        });
    }

    function fold(text) {
        var s = String(text || '').toLowerCase();
        if (typeof s.normalize === 'function') {
            s = s.normalize('NFD').replace(/[\u0300-\u036f]/g, '');
        }
        return s.replace(/\u0111/g, 'd').replace(/\s+/g, ' ').trim();
    }

    function initCatalog(root) {
        var input = root.querySelector('[data-sa-cat-search]');
        var empty = root.querySelector('[data-sa-cat-empty]');
        var chips = toArray(root.querySelectorAll('[data-sa-cat-chip]'));
        var tiles = toArray(root.querySelectorAll('[data-sa-cat-tile]')).map(function (el) {
            var groups = [];
            try {
                groups = JSON.parse(el.getAttribute('data-sa-groups') || '[]') || [];
            } catch (e) {
                groups = [];
            }
            return { el: el, text: fold(el.getAttribute('data-sa-search')), groups: groups };
        });

        function apply() {
            var query = fold(input ? input.value : '');
            var active = chips.filter(function (chip) {
                return chip.getAttribute('aria-pressed') === 'true';
            }).map(function (chip) {
                return chip.getAttribute('data-sa-cat-chip');
            });
            var shown = 0;

            tiles.forEach(function (tile) {
                var matchesQuery = query === '' || tile.text.indexOf(query) >= 0;
                var matchesChip = active.length === 0 || active.some(function (key) {
                    return tile.groups.indexOf(key) >= 0;
                });
                if (matchesQuery && matchesChip) {
                    tile.el.removeAttribute('hidden');
                    shown++;
                } else {
                    tile.el.setAttribute('hidden', 'hidden');
                }
            });

            if (empty) {
                empty.textContent = shown === 0 ? (empty.getAttribute('data-message') || '') : '';
            }
        }

        if (input) {
            input.addEventListener('input', apply);
        }
        chips.forEach(function (chip) {
            chip.addEventListener('click', function () {
                var pressed = chip.getAttribute('aria-pressed') === 'true';
                chip.setAttribute('aria-pressed', pressed ? 'false' : 'true');
                apply();
            });
        });

        // A value restored by the browser (back/forward) filters right away.
        if (input && input.value) {
            apply();
        }
    }

    function initModal(dialog) {
        var opener = null;

        toArray(dialog.querySelectorAll('[data-sa-modal-close]')).forEach(function (button) {
            button.addEventListener('click', function () {
                dialog.close();
            });
        });

        // Backdrop: the panel fills the dialog box, so a click whose target
        // is the dialog element itself landed on the ::backdrop.
        dialog.addEventListener('click', function (event) {
            if (event.target === dialog) {
                dialog.close();
            }
        });

        // Fires for every close path (button, Esc, backdrop).
        dialog.addEventListener('close', function () {
            if (opener) {
                opener.setAttribute('aria-expanded', 'false');
                opener.focus();
            }
        });

        return {
            open: function (from) {
                if (dialog.open) {
                    return;
                }
                opener = from;
                dialog.showModal();
                from.setAttribute('aria-expanded', 'true');
            }
        };
    }

    function initModalOpeners(openers) {
        var modals = {};
        openers.forEach(function (opener) {
            var id = opener.getAttribute('data-sa-modal-open');
            var dialog = id ? document.getElementById(id) : null;
            if (!dialog || typeof dialog.showModal !== 'function') {
                return;
            }
            var modal = modals[id] || (modals[id] = initModal(dialog));
            opener.setAttribute('aria-expanded', 'false');
            opener.addEventListener('click', function () {
                modal.open(opener);
            });
        });
    }

    function initPhotoBadge(root) {
        var badge = root.querySelector('[data-sa-photo-badge]');
        var thumbs = toArray(root.querySelectorAll('[data-sb-gallery-thumb]'));
        if (!badge || thumbs.length === 0) {
            return;
        }

        function sync() {
            var current = null;
            thumbs.forEach(function (t) {
                if (t.getAttribute('aria-current') === 'true') {
                    current = t;
                }
            });
            if (current && current.getAttribute('data-genuine') === 'true') {
                badge.removeAttribute('hidden');
            } else {
                badge.setAttribute('hidden', 'hidden');
            }
        }

        // Bubbles up after sb-gallery.js's own thumbnail listener ran.
        root.addEventListener('click', sync);
    }

    window.siteA = {
        initNav: initNav, initCarousel: initCarousel, initCatalog: initCatalog, fold: fold,
        initModalOpeners: initModalOpeners, initPhotoBadge: initPhotoBadge
    };

    function start() {
        var nav = document.querySelector('[data-sa-nav]');
        if (nav) {
            initNav(nav);
        }
        toArray(document.querySelectorAll('[data-sa-carousel]')).forEach(initCarousel);
        toArray(document.querySelectorAll('[data-sa-catalog]')).forEach(initCatalog);
        initModalOpeners(toArray(document.querySelectorAll('[data-sa-modal-open]')));
        toArray(document.querySelectorAll('[data-sa-pdp-gallery="true"]')).forEach(initPhotoBadge);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
