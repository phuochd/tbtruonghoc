/**
 * Story 4.2 (FR-11): reads the visitor's selections from a "Bảng cấu hình"
 * block ([data-sb-config], Views/Cms/DisplayTemplates/ConfigBlock.cshtml)
 * as one readable line, e.g. "Kích thước: 60cm; Loại: 2; Bánh xe: Có".
 *
 * Exposes window.sbConfigSummary(section) for lead-form.js, which prepends
 * the summary to the lead message at submit. Rules:
 *  - Option: the chosen entry (the empty "— Chưa chọn —" is skipped).
 *  - Multi-option: the ticked choices, joined ", ".
 *  - Check: "Có" when ticked.
 *  - Text: the trimmed text; blank is skipped.
 *  - Chỉ hiển thị rows carry no data-sb-config-row, so they are excluded.
 * Nothing selected = "" (the message is then sent unchanged).
 */
(function () {
    'use strict';

    function rowValue(row) {
        var type = row.getAttribute('data-sb-config-row');
        var i;

        if (type === 'option') {
            var select = row.querySelector('select');
            return select ? select.value.trim() : '';
        }

        if (type === 'text') {
            var input = row.querySelector('input[type="text"]');
            return input ? input.value.trim() : '';
        }

        if (type === 'check') {
            var box = row.querySelector('input[type="checkbox"]');
            return box && box.checked ? 'Có' : '';
        }

        if (type === 'multi') {
            var ticked = [];
            var boxes = row.querySelectorAll('input[type="checkbox"]');
            for (i = 0; i < boxes.length; i++) {
                if (boxes[i].checked) {
                    ticked.push(boxes[i].value);
                }
            }
            return ticked.join(', ');
        }

        return '';
    }

    window.sbConfigSummary = function (section) {
        if (!section) {
            return '';
        }

        var parts = [];
        var rows = section.querySelectorAll('[data-sb-config-row]');
        for (var i = 0; i < rows.length; i++) {
            var value = rowValue(rows[i]);
            if (value) {
                parts.push(rows[i].getAttribute('data-label') + ': ' + value);
            }
        }

        return parts.join('; ');
    };
})();
