// [CHANGE: Umbraco 17 upgrade - replaces the AngularJS shim + vendored lit + server generated import map] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

// Bare specifiers below are resolved by the backoffice's own import map - the package no longer ships its own copy
// of lit, and no longer generates an import map from the server.
import { LitElement, html, css, repeat, when } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { umbHttpClient } from "@umbraco-cms/backoffice/http-client";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";

const API_BASE = "/umbraco/management/api/v1/limbo/unused-media";

// [CHANGE: bugfix - requests were sent without a bearer token, which logged the user out]
// The backoffice HTTP client only applies the access token when a request declares "security" - internally it does
// `config.security && await applyAuth(...)`. Every operation in Umbraco's own generated client passes this exact
// array, so omitting it makes the request go out anonymously. The API then returns 401, and the backoffice's 401
// interceptor takes that to mean the session expired and restarts the whole authorization flow - i.e. it logs the
// user out. Always go through `apiGet`/`apiPost` below rather than calling `umbHttpClient` directly.
const API_SECURITY = [{ scheme: "bearer", type: "http" }];

function apiGet(options) {
    return umbHttpClient.get({ security: API_SECURITY, ...options });
}

function apiPost(options) {
    return umbHttpClient.post({ security: API_SECURITY, ...options });
}

function formatDate(date, options = {}, locale = undefined) {
    if (!date) return "";
    if (typeof date === "string") date = new Date(date);
    return new Intl.DateTimeFormat(locale, options).format(date);
}

function formatNumber(value, decimals = 0) {
    return new Intl.NumberFormat(undefined, {
        minimumFractionDigits: decimals,
        maximumFractionDigits: decimals
    }).format(value);
}

function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function waitAtLeast(promise, minTime = 500) {
    const [result] = await Promise.all([promise, delay(minTime)]);
    return result;
}

export class LimboUnusedMediaDashboardElement extends UmbElementMixin(LitElement) {

    static styles = css`

        :host {
            display: block;
            padding: var(--uui-size-layout-1, 24px);
        }

        .container {
            min-height: 150px;
            margin-bottom: 250px;
            position: relative;
        }

        uui-loader-circle {
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            font-size: 2em;
            color: var(--unused-media-loader-color, var(--uui-color-default));
        }

        .container.loading .stack {
            opacity: 0.5;
        }

        uui-table {
            border: 1px solid var(--uui-color-border, #e9e9eb);
        }

        uui-table-head-cell {
            white-space: nowrap;
            padding-top: 3px;
            padding-bottom: 2px;
            font-size: 14px;
        }

        uui-table-head-cell button {
            border: 0;
            background: transparent;
            padding: 0;
            font-weight: bold;
            cursor: pointer;
            font-family: inherit;
            font-size: 14px;
        }

        uui-table-head-cell button:hover {
            text-decoration: underline;
        }

        uui-table-head-cell small {
            font-size: 11px;
        }

        uui-table-cell {
            padding-top: 10px;
            padding-bottom: 5px;
            vertical-align: top;
            font-size: 14px;
        }

        uui-table-cell.fw {
            width: 100%;
        }

        uui-table-cell.nw {
            white-space: nowrap;
        }

        .pagination {
            margin: 20px auto 0 auto;
        }

        .pagination .page {
            min-width: 36px;
            max-width: 72px;
        }

        .pagination .nav {
            min-width: 72px;
        }

        .pagination uui-button {
            --uui-button-font-size: 13px;
        }

        .filters {
            display: flex;
            gap: 7px;
            height: 53px;
            align-items: end;
        }

        .filters > :first-child {
            flex: 1;
        }

        .stack {
            display: flex;
            flex-direction: column;
            gap: 1rem;
        }

        .stats {
            display: flex;
            gap: 1rem;
        }

        .stats-numbers {
            flex: 1;
        }

        .path {
            font-size: 11px;
            color: var(--uui-color-text-alt, #666);
        }

        .path a {
            color: inherit;
            text-decoration: none;
        }

        .path a:hover {
            text-decoration: underline;
        }

        .umb-empty-state {
            color: var(--uui-color-text-alt, rgb(104, 103, 107));
            font-size: 17.25px;
            line-height: 1.8em;
            text-align: center;
            padding: 35px 0;
        }

        .page.active {
            z-index: 1;
            pointer-events: none;
            --uui-button-background-color: var(--uui-color-current, #f5c1bc);
        }

        .muted {
            color: var(--uui-color-text-alt, #999);
            font-style: italic;
        }

        .table-actions {
            position: absolute;
            left: 0;
            top: 0;
            right: 0;
            background-color: var(--uui-color-selected, #3544b1);
            border-radius: 3px;
            padding: 10px;
            color: var(--uui-color-selected-contrast, #fff);
            display: flex;
        }

        .table-actions .left,
        .table-actions .right {
            display: flex;
            gap: 10px;
            align-items: center;
        }

        .table-actions .right {
            margin-left: auto;
        }

        .user-name,
        .member-name {
            width: fit-content;
            max-width: 200px;
        }

        @media (min-width: 1850px) {
            .user-name {
                max-width: 350px;
            }
        }

        uui-checkbox {
            width: 18px;
        }

    `;

    constructor() {

        super();

        this.loaded = false;
        this.loading = false;
        this.allowed = true;
        this.params = {};
        this.filters = [];
        this.items = null;
        this.columns = [];
        this.reports = null;
        this.showReports = false;
        this.allSelected = false;
        this.selectedCount = 0;
        this.activeFilters = 0;
        this.timeout = null;

        // [CHANGE: code review fix - responses of overlapping "updateList" calls could resolve out of order and
        // render a stale page] Related: Controllers/BackOffice/UnusedMediaBackOfficeController.cs
        this.requestId = 0;

        this.dateOptions = {
            day: "numeric",
            hour: "numeric",
            minute: "numeric",
            month: "long",
            second: "numeric",
            year: "numeric"
        };

        // The notification context replaces the AngularJS "notificationsService" of the old backoffice
        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
            this._notificationContext = context;
        });

    }

    connectedCallback() {
        super.connectedCallback();
        this.init();
    }

    async init() {

        const { data } = await tryExecute(this, apiGet({ url: `${API_BASE}/config` }));

        // The "Dashboard:AllowedGroups" setting is enforced by the API - the dashboard just reflects it
        this.allowed = data?.allowed !== false;
        if (!this.allowed) {
            this.loaded = true;
            this.requestUpdate();
            return;
        }

        await Promise.all([this.updateFilters(), this.updateList()]);

    }

    async updateFilters() {

        const { data } = await tryExecute(this, apiGet({ url: `${API_BASE}/filters` }));
        if (!data) return;

        // [CHANGE: code review fix - localization keys are resolved in "renderFilter" instead of here. Resolving
        // them once, right after the fetch, froze whatever "localize.term" returned at that point - which is the
        // raw key when the localization extension hasn't finished loading, and the wrong language after the user
        // switches language] Related: Lang/en.js, Lang/da-dk.js
        this.filters = data.map((filter) => ({ ...filter, value: "" }));

        this.requestUpdate();

    }

    /**
     * Resolves the label of a server supplied item, preferring its localization key. Called from "render" so the
     * label follows the currently loaded localizations.
     */
    localizeLabel(key, fallback) {
        if (!key) return fallback ?? "";
        const term = this.localize.term(key);
        return term && term !== key ? term : (fallback ?? term ?? "");
    }

    async updateList(page) {

        this.loading = true;
        this.requestUpdate();

        if (page) this.params.page = page;

        // Empty values are dropped so they don't count towards "activeFilters"
        const query = {};
        Object.keys(this.params).forEach((key) => {
            if (this.params[key] !== "" && this.params[key] !== null && this.params[key] !== undefined) query[key] = this.params[key];
        });

        const requestId = ++this.requestId;

        const { data, error } = await tryExecute(this, apiGet({ url: API_BASE, query }));

        // A newer request was started while this one was in flight - discard the response so the list doesn't
        // fall back to a stale filter/page
        if (requestId !== this.requestId) return;

        this.loading = false;

        if (error || !data) {
            this._notificationContext?.peek("danger", {
                data: {
                    headline: this.localize.term("unusedMedia_title"),
                    message: this.localize.term("unusedMedia_loadError")
                }
            });
            this.requestUpdate();
            return;
        }

        this.allSelected = false;
        this.selectedCount = 0;

        this.reports = data.reports;

        // The column names are localized in "renderColumn" rather than here - see "updateFilters"
        this.columns = data.columns;
        const columnsByAlias = {};
        this.columns.forEach((column) => {
            columnsByAlias[column.alias] = column;
        });

        this.items = data.items;
        this.items.forEach((item) => {
            item.selected = false;
            item.cells.forEach((cell) => {
                cell.column = columnsByAlias[cell.alias];
                if (cell.column) cell.type = cell.column.type;
                if (!(cell.type === "user" || cell.type === "member")) {
                    cell.classes = cell.type === "name" ? "fw" : "nw";
                }
            });
        });

        this.stats = {
            total: data.total,
            unused: data.unused,
            used: data.used,
            unusedPercent: data.unusedPercent,
            usedPercent: data.usedPercent,
            limit: data.limit,
            offset: data.offset,
            page: data.page,
            pages: data.pages,
            sortField: data.sortField,
            sortOrder: data.sortOrder
        };

        this.activeFilters = Object.keys(query).filter((x) => x !== "page").length;

        this.pagination = {
            from: this.stats.offset + 1,
            to: Math.min(this.stats.offset + this.stats.limit, this.stats.unused),
            page: this.stats.page,
            pages: this.stats.pages,
            total: this.stats.unused,
            pagination: []
        };

        for (let i = Math.max(1, this.stats.page - 5); i <= Math.min(this.stats.page + 5, this.stats.pages); i++) {
            this.pagination.pagination.push({ page: i, active: i === this.stats.page });
        }

        this.loaded = true;

        this.requestUpdate();

    }

    onFilterChange(e, filter) {
        filter.value = e.target.value;
        this.params[filter.alias] = filter.value;
        this.params.page = 1;
        if (filter.type === "text") {
            clearTimeout(this.timeout);
            this.timeout = setTimeout(() => this.updateList(), 250);
        } else {
            // The selected state of the options is derived from "filter.value" in "renderFilter"
            this.updateList();
        }
    }

    async trashMedia(media) {

        const list = Array.isArray(media) ? media : [media];
        const multi = list.length > 1;

        const body = multi
            ? { mediaKeys: list.map((x) => x.key) }
            : { mediaKey: list[0].key };

        list.forEach((x) => { x.trashMediaButtonState = "waiting"; });
        this.requestUpdate();

        const { error } = await tryExecute(this, apiPost({ url: `${API_BASE}/trash`, body }));

        if (error) {
            list.forEach((x) => { x.trashMediaButtonState = "failed"; });
            this._notificationContext?.peek("danger", {
                data: {
                    headline: this.localize.term("unusedMedia_title"),
                    message: this.localize.term(multi ? "unusedMedia_deleteErrorMultiple" : "unusedMedia_deleteErrorSingle")
                }
            });
            // [CHANGE: code review fix - a bulk trash is not atomic server side, so some of the selected media may
            // already have been moved to the recycle bin. Refresh so the list doesn't keep showing them]
            // Related: Controllers/BackOffice/UnusedMediaBackOfficeController.cs
            if (multi) await this.updateList();
            this.requestUpdate();
            return false;
        }

        this._notificationContext?.peek("positive", {
            data: {
                headline: this.localize.term("unusedMedia_title"),
                message: this.localize.term(multi ? "unusedMedia_deleteSuccessMultiple" : "unusedMedia_deleteSuccessSingle")
            }
        });

        await this.updateList();

        return true;

    }

    async requestDelete(media) {

        const list = Array.isArray(media) ? media : [media];
        const multi = list.length > 1;

        // "umbConfirmModal" replaces the AngularJS "overlayService" of the old backoffice
        // [CHANGE: code review fix - the promise rejects when the user cancels or dismisses the modal. Without the
        // "catch" that surfaced as an unhandled promise rejection] Related: Controllers/BackOffice/UnusedMediaBackOfficeController.cs
        try {
            await umbConfirmModal(this, {
                headline: this.localize.term("unusedMedia_deleteMediaTitle"),
                content: multi
                    ? this.localize.term("unusedMedia_deleteMediaMultiple")
                    : this.localize.term("unusedMedia_deleteMediaSingle", list[0].name),
                confirmLabel: this.localize.term("actions_delete"),
                color: "danger"
            });
        } catch {
            return;
        }

        await this.trashMedia(list);

    }

    requestDeleteSelected() {
        const selected = (this.items ?? []).filter((x) => x.selected);
        if (selected.length === 0) return;
        this.requestDelete(selected);
    }

    toggleReports() {
        this.showReports = !this.showReports;
        this.requestUpdate();
    }

    setSortOrder(column, sortOrder) {
        this.params.sortField = column.alias;
        this.params.sortOrder = sortOrder;
        // Re-sorting reorders the whole result set, so staying on the current page would show an arbitrary slice
        this.params.page = 1;
        this.updateList();
    }

    async startScan(report) {

        report.buttonState = "waiting";
        this.requestUpdate();

        const { data, error } = await waitAtLeast(
            tryExecute(this, apiPost({ url: `${API_BASE}/scan`, query: { provider: report.alias } })),
            1000
        );

        if (error || !data) {
            report.buttonState = "failed";
            this.requestUpdate();
            return;
        }

        report.createDate = data.createDate;
        report.keys = data.keys;
        report.buttonState = "success";
        this.requestUpdate();

        await this.updateList();

    }

    clearSelection() {
        this.allSelected = false;
        (this.items ?? []).forEach((item) => { item.selected = false; });
        this.selectedCount = 0;
        this.requestUpdate();
    }

    toggleAll(e) {
        this.allSelected = e.target.checked;
        (this.items ?? []).forEach((item) => { item.selected = this.allSelected; });
        this.selectedCount = (this.items ?? []).filter((i) => i.selected).length;
        this.requestUpdate();
    }

    toggleItem(e, item) {
        item.selected = e.target.checked;
        this.selectedCount = this.items.filter((i) => i.selected).length;
        this.allSelected = this.items.every((i) => i.selected);
        this.requestUpdate();
    }

    renderStats() {

        if (!this.stats || !this.items || this.items.length === 0) return;

        return html`
            <div class="stats">
                <div class="stats-numbers">
                    ${this.localize.term(
                        "unusedMedia_statsSummary",
                        formatNumber(this.stats.unused),
                        formatNumber(this.stats.total),
                        formatNumber(this.stats.unusedPercent, 2)
                    )}
                </div>
                ${when(this.reports?.length > 0, () => html`
                    <div class="stats-report">
                        ${when(this.reports.length === 1, () => html`
                            <span>
                                ${this.localize.term("unusedMedia_basedOnSingle")}
                                <strong style="cursor: pointer;" @click=${() => this.toggleReports()}>${formatDate(this.reports[0].createDate, this.dateOptions)}</strong>
                            </span>
                        `, () => html`
                            <span>
                                <strong style="cursor: pointer;" @click=${() => this.toggleReports()}>${this.localize.term("unusedMedia_basedOnMultiple", this.reports.length)}</strong>
                            </span>
                        `)}
                    </div>
                `)}
            </div>
        `;

    }

    renderReports() {

        if (!this.reports || !this.showReports) return;

        return html`
            <uui-box headline="${this.localize.term("unusedMedia_reports")}">
                <uui-table role="table">
                    <uui-table-head role="row">
                        <uui-table-head-cell role="columnheader">${this.localize.term("unusedMedia_reportName")}</uui-table-head-cell>
                        <uui-table-head-cell role="columnheader">${this.localize.term("unusedMedia_reportLastScan")}</uui-table-head-cell>
                        <uui-table-head-cell role="columnheader">&nbsp;</uui-table-head-cell>
                    </uui-table-head>
                    ${repeat(this.reports, (report) => report.alias, (report) => html`
                        <uui-table-row role="row">
                            <uui-table-cell role="cell" class="fw">
                                <strong>${report.name}</strong>
                            </uui-table-cell>
                            <uui-table-cell role="cell" class="nw">
                                ${formatDate(report.createDate, this.dateOptions)}
                            </uui-table-cell>
                            <uui-table-cell role="cell" class="nw">
                                <uui-button
                                    look="secondary"
                                    compact
                                    state="${report.buttonState ?? ""}"
                                    @click=${() => this.startScan(report)}
                                    label="${this.localize.term("unusedMedia_rescan")}"></uui-button>
                            </uui-table-cell>
                        </uui-table-row>
                    `)}
                </uui-table>
            </uui-box>
        `;

    }

    renderFilters() {

        if (!this.filters || this.filters.length === 0) return;

        return html`
            <div class="filters">
                ${repeat(this.filters, (filter) => filter.alias, (filter) => this.renderFilter(filter))}
            </div>
        `;

    }

    renderFilter(filter) {

        switch (filter.type) {

            case "text": {
                const placeholder = this.localizeLabel(filter.placeholderKey, filter.placeholder);
                return html`
                    <uui-input
                        .value=${filter.value ?? ""}
                        label="${placeholder}"
                        placeholder="${placeholder}"
                        @input=${(e) => this.onFilterChange(e, filter)}></uui-input>
                `;
            }

            case "dropdown": {
                // "uui-select" expects "name"/"value" options, and the server supplies a localization key plus an
                // English fallback. Mapped here rather than after the fetch so it follows the loaded localizations.
                const value = filter.value ?? "";
                const options = (filter.items ?? []).map((item) => ({
                    ...item,
                    name: this.localizeLabel(item.labelKey, item.label),
                    selected: item.value === value
                }));
                const placeholder = options.length > 0 ? options[0].name : "";
                return html`
                    <uui-select
                        label="${placeholder}"
                        placeholder="${placeholder}"
                        .options=${options}
                        @change=${(e) => this.onFilterChange(e, filter)}></uui-select>
                `;
            }

            default:
                return html`${filter.type}`;

        }

    }

    renderItems() {

        if (!Array.isArray(this.items)) return;

        if (this.items.length === 0) {
            return html`
                <div class="umb-empty-state">
                    ${this.localize.term(this.activeFilters === 0 ? "unusedMedia_emptyNoFilters" : "unusedMedia_emptyFiltered")}
                </div>
            `;
        }

        return html`
            <uui-table role="table">
                <uui-table-column style="width: 1px;"></uui-table-column>
                ${repeat(this.columns, (column) => column.alias, () => html`<uui-table-column></uui-table-column>`)}
                <uui-table-column style="width: 1px;"></uui-table-column>
                <uui-table-head role="row">
                    <uui-table-head-cell role="columnheader">
                        <uui-checkbox .checked=${this.allSelected} @change=${(e) => this.toggleAll(e)}></uui-checkbox>
                    </uui-table-head-cell>
                    ${repeat(this.columns, (column) => column.alias, (column) => this.renderColumn(column))}
                    <uui-table-head-cell role="columnheader">&nbsp;</uui-table-head-cell>
                </uui-table-head>
                ${repeat(this.items, (item) => item.key, (item) => html`
                    <uui-table-row role="row">
                        <uui-table-cell role="cell">
                            <uui-checkbox .checked=${item.selected} @change=${(e) => this.toggleItem(e, item)}></uui-checkbox>
                        </uui-table-cell>
                        ${repeat(item.cells, (cell) => cell.alias, (cell, index) => this.renderCell(cell, index, item))}
                        <uui-table-cell role="cell">
                            <uui-action-bar>
                                <uui-button
                                    look="secondary"
                                    color="danger"
                                    state="${item.trashMediaButtonState ?? ""}"
                                    label="${this.localize.term("actions_delete")}"
                                    ?disabled=${item.trashMediaButtonState === "waiting"}
                                    @click=${() => this.requestDelete(item)}>
                                    <uui-icon name="icon-trash"></uui-icon>
                                </uui-button>
                            </uui-action-bar>
                        </uui-table-cell>
                    </uui-table-row>
                `)}
            </uui-table>
        `;

    }

    renderColumn(column) {

        const isSortField = this.stats?.sortField === column.alias;
        const nextOrder = isSortField ? (this.stats.sortOrder === "descending" ? "ascending" : "descending") : column.defaultOrder;

        // Localized here rather than after the fetch - see "updateFilters"
        const displayName = this.localizeLabel(column.nameKey, column.name);

        return html`
            <uui-table-head-cell role="columnheader">
                ${when(column.allowSort, () => html`
                    <button type="button" @click=${() => this.setSortOrder(column, nextOrder)}>${displayName}</button>
                    <small>${isSortField ? (this.stats.sortOrder === "ascending" ? "▲" : "▼") : ""}</small>
                `, () => html`
                    ${displayName}
                `)}
            </uui-table-head-cell>
        `;

    }

    renderCell(cell, index, item) {

        if (cell.type === "name") {
            const fileName = item.url ? item.url.split("/").pop() : "";
            return html`
                <uui-table-cell role="cell" class="${cell.classes ?? ""}">
                    <a href="/umbraco/section/media/workspace/media/edit/${item.key}">${cell.text || cell.value}</a>
                    <div class="path">
                        ${repeat(item.path, (folder, i) => `${i}-${folder}`, (folder) => html`
                            <span>${folder}</span>
                            <span class="separator">/</span>
                        `)}
                        <a href="${item.url}" target="_blank" rel="noreferrer noopener">${fileName}</a>
                    </div>
                </uui-table-cell>
            `;
        }

        if (cell.type === "user" || cell.type === "member") {
            return html`
                <uui-table-cell role="cell" class="${cell.classes ?? ""}">
                    ${when(cell.value, () => html`
                        <div class="${cell.type}-name">${cell.text || cell.value}</div>
                    `, () => html`
                        <span class="muted">N/A</span>
                    `)}
                </uui-table-cell>
            `;
        }

        return html`
            <uui-table-cell role="cell" class="${cell.classes ?? ""}">${cell.text || cell.value}</uui-table-cell>
        `;

    }

    renderPagination() {

        if (!this.pagination || this.pagination.pages <= 1) return;

        return html`
            <div class="pagination">
                <uui-button-group role="list">
                    <uui-button
                        compact
                        look="outline"
                        class="nav"
                        role="listitem"
                        label=${this.localize.term("general_first")}
                        ?disabled=${this.pagination.page === 1}
                        @click=${() => this.updateList(1)}></uui-button>
                    <uui-button
                        compact
                        look="outline"
                        class="nav"
                        role="listitem"
                        label=${this.localize.term("general_previous")}
                        ?disabled=${this.pagination.page === 1}
                        @click=${() => this.updateList(this.pagination.page - 1)}></uui-button>

                    ${repeat(this.pagination.pagination, (page) => page.page, (page) => html`
                        <uui-button
                            compact
                            look="outline"
                            role="listitem"
                            label="Go to page ${page.page}"
                            class=${"page" + (page.active ? " active" : "")}
                            @click=${() => { if (!page.active) this.updateList(page.page); }}>
                            ${page.page}
                        </uui-button>
                    `)}

                    <uui-button
                        compact
                        look="outline"
                        class="nav"
                        role="listitem"
                        label=${this.localize.term("general_next")}
                        ?disabled=${this.pagination.page === this.pagination.pages}
                        @click=${() => this.updateList(this.pagination.page + 1)}></uui-button>
                    <uui-button
                        compact
                        look="outline"
                        class="nav"
                        role="listitem"
                        label=${this.localize.term("general_last")}
                        ?disabled=${this.pagination.page === this.pagination.pages}
                        @click=${() => this.updateList(this.pagination.pages)}></uui-button>
                </uui-button-group>
            </div>
        `;

    }

    renderTableActions() {

        if (!this.items || this.items.length === 0) return;
        if (!this.items.some((x) => x.selected)) return;

        return html`
            <div class="table-actions">
                <div class="left">
                    <uui-button look="secondary" @click=${() => this.clearSelection()} label="${this.localize.term("buttons_clearSelection")}"></uui-button>
                    <strong>${this.localize.term("unusedMedia_selectedCount", this.selectedCount, this.items.length)}</strong>
                </div>
                <div class="right">
                    <uui-button look="primary" color="danger" @click=${() => this.requestDeleteSelected()} label="${this.localize.term("actions_delete")}"></uui-button>
                </div>
            </div>
        `;

    }

    render() {

        if (!this.allowed) {
            return html`
                <umb-body-layout header-transparent>
                    <div class="umb-empty-state">${this.localize.term("unusedMedia_notAllowed")}</div>
                </umb-body-layout>
            `;
        }

        return html`
            <div>
                <h1>${this.localize.term("unusedMedia_title")}</h1>
                <p>${this.localize.term("unusedMedia_description")}</p>
                <div class="container ${this.loading ? "loading" : ""}">
                    <div class="stack">
                        <div style="position: relative; height: 53px;">
                            ${this.renderFilters()}
                            ${this.renderTableActions()}
                        </div>
                        ${this.renderStats()}
                        ${this.renderReports()}
                        ${this.renderItems()}
                        ${this.renderPagination()}
                    </div>
                </div>
                ${when(this.loading, () => html`<uui-loader-circle></uui-loader-circle>`)}
            </div>
        `;

    }

}

customElements.define("limbo-unused-media-dashboard", LimboUnusedMediaDashboardElement);

export default LimboUnusedMediaDashboardElement;
