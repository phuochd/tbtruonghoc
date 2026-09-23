/*global piranha, Vue, $ */

/**
 * Story 1.5 (AD-3): vanilla Vue 2 instance driving the Manager "Danh sách
 * khách để lại thông tin" screen (Leads.cshtml). Mirrors piranha.core v12.0's
 * own assets/src/js/piranha.alias.js fetch-based load pattern (see this
 * story's Code Map) - no Vue-Router, no build step, plain script tag loaded
 * from this app's own wwwroot (this repo has no JS build pipeline).
 *
 * Read-only: list, site filter, and a detail modal only - no save/delete
 * method exists here (Boundaries & Constraints - "Never add edit/delete/
 * export actions for leads").
 */
(function () {
    'use strict';

    window.managerLeads = new Vue({
        el: '#leads',
        data: {
            loading: true,
            siteId: null,
            siteTitle: 'Tất cả site',
            items: [],
            detail: null,
            error: false
        },
        methods: {
            /**
             * Click handler for the site-filter dropdown entries. Reads the
             * target site id/title from data-* attributes rather than
             * interpolating values into the v-on directive expression, so a
             * site title containing an apostrophe can never break the
             * handler.
             */
            selectSite: function (event) {
                var target = event.currentTarget;
                var siteId = target.getAttribute('data-site-id') || null;
                var siteTitle = target.getAttribute('data-site-title') || 'Tất cả site';
                this.load(siteId, siteTitle);
            },
            load: function (siteId, siteTitle) {
                var self = this;
                self.siteId = siteId || null;
                self.siteTitle = siteTitle || self.siteTitle;

                var url = piranha.baseUrl + 'manager/api/lead/list' + (self.siteId ? '/' + self.siteId : '');

                fetch(url)
                    .then(function (response) {
                        if (!response.ok) {
                            throw new Error('Failed to load leads: ' + response.status);
                        }
                        return response.json();
                    })
                    .then(function (result) {
                        self.items = result;
                        self.error = false;
                    })
                    .catch(function (error) {
                        // A mid-visit session expiry makes this fetch follow
                        // the 302 to the login page's HTML instead of JSON,
                        // which throws here (either from the !response.ok
                        // check above or from response.json() itself) -
                        // surface that on-screen rather than leaving a
                        // silent stale/blank table with no hint to
                        // re-authenticate.
                        console.log('error:', error);
                        self.error = true;
                    });
            },
            showDetail: function (id) {
                var self = this;

                fetch(piranha.baseUrl + 'manager/api/lead/' + id)
                    .then(function (response) {
                        if (!response.ok) {
                            throw new Error('Lead not found: ' + id);
                        }
                        return response.json();
                    })
                    .then(function (result) {
                        self.detail = result;
                        self.error = false;
                        $('#leadDetailModal').modal('show');
                    })
                    .catch(function (error) {
                        console.log('error:', error);
                        self.error = true;
                    });
            },
            formatDate: function (value) {
                if (!value) {
                    return '';
                }
                return new Date(value).toLocaleString('vi-VN');
            },
            formatOutsideServiceArea: function (value) {
                if (value === null || value === undefined) {
                    return '—';
                }
                return value ? 'Có' : 'Không';
            }
        },
        updated: function () {
            this.loading = false;
        }
    });
})();
