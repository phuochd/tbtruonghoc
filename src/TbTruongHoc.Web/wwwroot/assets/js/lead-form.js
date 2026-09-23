/**
 * Story 1.4 (FR-3): vanilla-JS fetch handler for every
 * `_QuoteRequestForm.cshtml` instance on the page (`[data-quote-request-form]`).
 * Submits JSON to POST /api/leads and swaps in an inline success/error state
 * with no page reload/redirect. On a failed submission the visitor's entered
 * field values are left untouched (never cleared) - see clearFieldErrors()/
 * showFieldErrors() below, neither of which ever resets an <input>'s value.
 *
 * No framework/bundler in this repo (see wwwroot/assets - plain CSS/JS
 * served as static files), so this is deliberately dependency-free.
 */
(function () {
    'use strict';

    var LEADS_ENDPOINT = '/api/leads';

    // Maps the request DTO's field names (as returned by ASP.NET Core's
    // ValidationProblemDetails "errors" dictionary, keyed by the
    // LeadSubmissionRequest property name) to this form's input `name`
    // attributes / error-span ids.
    var FIELD_MAP = {
        Name: 'name',
        Phone: 'phone',
        ProductOfInterest: 'productOfInterest',
        Message: 'message'
    };

    function ready(fn) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', fn);
        } else {
            fn();
        }
    }

    ready(function () {
        var forms = document.querySelectorAll('[data-quote-request-form]');
        for (var i = 0; i < forms.length; i++) {
            initForm(forms[i]);
        }
    });

    function initForm(container) {
        var form = container.querySelector('form');
        if (!form) {
            return;
        }

        var submitErrorEl = container.querySelector('.quote-request-form__submit-error');
        var submitButton = form.querySelector('button[type="submit"]');

        form.addEventListener('submit', function (event) {
            event.preventDefault();
            submitForm();
        });

        function submitForm() {
            clearAllErrors();

            var payload = {
                name: getValue('name'),
                phone: getValue('phone'),
                productOfInterest: getValue('productOfInterest'),
                message: getValue('message'),
                formType: getValue('formType')
            };

            setBusy(true);

            fetch(LEADS_ENDPOINT, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload),
                signal: AbortSignal.timeout(15000)
            })
                .then(function (response) {
                    if (response.ok) {
                        showSuccess();
                        return;
                    }

                    if (response.status === 400) {
                        return response.json().then(function (problem) {
                            showFieldErrors(problem && problem.errors);
                        });
                    }

                    showSubmitError();
                })
                .catch(function () {
                    // Network/server error unreachable case from the I/O
                    // matrix - entered values are untouched (nothing here
                    // clears an input), just surface the fallback message.
                    showSubmitError();
                })
                .then(function () {
                    setBusy(false);
                });
        }

        function getValue(name) {
            var field = form.querySelector('[name="' + name + '"]');
            return field ? field.value : '';
        }

        function setBusy(isBusy) {
            if (submitButton) {
                submitButton.disabled = isBusy;
            }
        }

        function clearAllErrors() {
            if (submitErrorEl) {
                submitErrorEl.textContent = '';
            }

            var errorSpans = form.querySelectorAll('.quote-request-form__error');
            for (var i = 0; i < errorSpans.length; i++) {
                errorSpans[i].textContent = '';
            }

            var inputs = form.querySelectorAll('input, textarea');
            for (var j = 0; j < inputs.length; j++) {
                inputs[j].setAttribute('aria-invalid', 'false');
            }
        }

        function showFieldErrors(errors) {
            if (!errors) {
                showSubmitError();
                return;
            }

            for (var dtoField in errors) {
                if (!Object.prototype.hasOwnProperty.call(errors, dtoField)) {
                    continue;
                }

                var inputName = FIELD_MAP[dtoField] || dtoField;
                var input = form.querySelector('[name="' + inputName + '"]');
                var errorId = input ? input.getAttribute('aria-describedby') : null;
                var errorEl = errorId ? document.getElementById(errorId) : null;
                var messages = errors[dtoField];
                var text = Array.isArray(messages) ? messages.join(' ') : String(messages);

                if (errorEl) {
                    // Text adjacent to the invalid field, associated via
                    // aria-describedby - never color-only.
                    errorEl.textContent = text;
                }
                if (input) {
                    input.setAttribute('aria-invalid', 'true');
                }
            }
        }

        function showSubmitError() {
            if (submitErrorEl) {
                submitErrorEl.textContent =
                    'Rất tiếc, không thể gửi yêu cầu ngay lúc này. Vui lòng gọi điện hoặc nhắn Zalo cho chúng tôi để được hỗ trợ.';
            }
        }

        function showSuccess() {
            // Inline confirmation replaces the form in place - no reload, no
            // separate modal.
            container.innerHTML =
                '<div class="quote-request-form__success" role="status">' +
                'Cảm ơn bạn đã gửi yêu cầu! Chúng tôi sẽ liên hệ lại sớm nhất có thể.' +
                '</div>';
        }
    }
})();
