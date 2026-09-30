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

    window.siteA = { initNav: initNav, initCarousel: initCarousel };

    function start() {
        var nav = document.querySelector('[data-sa-nav]');
        if (nav) {
            initNav(nav);
        }
        toArray(document.querySelectorAll('[data-sa-carousel]')).forEach(initCarousel);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
