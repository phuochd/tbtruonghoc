/**
 * Story 3.2 (FR-13): landing-page variant CTAs. Each card's CTA is a plain
 * `<a href="#dat-hang" data-lp-variant="...">`; on click this copies the
 * variant's prefill text ("<name> – <price>") into the inline quote form's
 * "Sản phẩm quan tâm" field and lets the anchor scroll to the form.
 *
 * Without JS the anchor still jumps to the form, which stays prefilled with
 * the page title. Once lead-form.js has replaced the form with its success
 * state the field no longer exists and this does nothing.
 *
 * Dependency-free, like lead-form.js.
 */
(function () {
    'use strict';

    document.addEventListener('click', function (event) {
        var target = event.target;
        var link = target && target.closest ? target.closest('a[data-lp-variant]') : null;
        if (!link) {
            return;
        }

        var field = document.querySelector('#dat-hang [name="productOfInterest"]');
        if (!field) {
            return;
        }

        field.value = link.getAttribute('data-lp-variant') || '';
    });
})();
