/**
 * Story 2.4: product-detail-page gallery (Views/Cms/ProductPost.cshtml).
 * Progressive enhancement - each thumbnail is a plain link to the large
 * image, so without this script a tap simply opens that image.
 *
 * For every [data-sb-gallery] root: a click on a [data-sb-gallery-thumb]
 * link swaps the [data-sb-gallery-main] image's src (the link's href), alt
 * (data-alt) and width/height (data-width/data-height) in place, and moves
 * aria-current="true" to that thumbnail. Modified clicks (Ctrl/Cmd/Shift/
 * Alt, non-primary button) are left alone so "open in new tab" still works.
 * Nothing runs on a timer - never auto-advances.
 *
 * No framework/bundler, same as site-b-nav.js.
 */
(function () {
    'use strict';

    function setOrRemove(el, name, value) {
        if (value) {
            el.setAttribute(name, value);
        } else {
            el.removeAttribute(name);
        }
    }

    function init(root) {
        var main = root.querySelector('[data-sb-gallery-main]');
        var thumbs = Array.prototype.slice.call(root.querySelectorAll('[data-sb-gallery-thumb]'));
        if (!main) {
            return;
        }

        function select(thumb) {
            main.setAttribute('src', thumb.getAttribute('href'));
            main.setAttribute('alt', thumb.getAttribute('data-alt') || '');
            setOrRemove(main, 'width', thumb.getAttribute('data-width'));
            setOrRemove(main, 'height', thumb.getAttribute('data-height'));
            thumbs.forEach(function (t) {
                if (t === thumb) {
                    t.setAttribute('aria-current', 'true');
                } else {
                    t.removeAttribute('aria-current');
                }
            });
        }

        thumbs.forEach(function (thumb) {
            thumb.addEventListener('click', function (event) {
                if (event.defaultPrevented || (event.button && event.button !== 0) ||
                    event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
                    return;
                }
                event.preventDefault();
                select(thumb);
            });
        });
    }

    window.sbGallery = { init: init };

    function start() {
        var roots = document.querySelectorAll('[data-sb-gallery]');
        for (var i = 0; i < roots.length; i++) {
            init(roots[i]);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
