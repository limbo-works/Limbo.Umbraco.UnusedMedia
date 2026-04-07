import { LitElement, html, css, repeat, when, until } from "@limbo/unused-media/lit";

import { UnusedMediaDashboardLoadEvent } from "@limbo/unused-media/events";
import { UnusedMediaService } from "@limbo/unused-media/service";

function formatDate(date, options = {}, locale = undefined) {
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
    const [result] = await Promise.all([
        promise,
        delay(minTime)
    ]);
    return result;
}

export class LimboUnusedMediaDashboardElement extends LitElement {

    static styles = css`

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
        }

        .container.loading .stack {
            opacity: 0.5;
        }

        :hooooooost {
            display: block;
            padding: 16px;
            background: var(--dashboard-background, #fff);
        }

        a {
            color: black;
            &:hover {
                text-decoration: none;
            }
        }

        uui-table {
            border: 1px solid #e9e9eb;
        }


        uui-table-head-cell {
            white-space: nowrap;
            padding-top: 3px;
            padding-bottom: 2px;
            font-size: 14px;
            button {
                border: 0;
                background: transparent;
                padding: 0;
                font-weight: bold;
                cursor: pointer;
                font-family: Lato, "Helvetica Neue", Helvetica, Arial, sans-serif;
                font-size: 14px;
                &:hover {
                    text-decoration: underline;
                }
            }
            small {
                font-size: 11px;
            }
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
            margin: 20px auto 0 auto
        }

        .filters {
            display: flex;
            gap: 7px;
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

        uui-loader-circle {
            color: #006eff; /* default blue */
            color: #FF9D7E; /* Citi orange */
            color: #4A44B7; /* City blue'ish' */
            font-size: 2em;
        }

        .path {
            font-size: 11px;
            color: #666;
            a {
                color: #666;
                text-decoration: none;
                &:hover {
                    text-decoration: underline;
                }
            }
        }

        .umb-empty-state {
            color: rgb(104, 103, 107);
            font-size: 17.25px;
            line-height: 1.8em;
            text-align: center;
            padding: 35px 0;
        }


        .active {
            pointer-events: none;
            /*--uui-button-font-weight: 700;
            --uui-button-contrast: var(--uui-color-selected);
            --uui-button-border-color: var(--uui-color-selected);
            --uui-button-border-width: 2px;*/
            --uui-button-background-color: var(--uui-color-current, #f5c1bc);
        }

        .page.active {
            z-index: 1;
        }

        .pagination {
            .page {
                min-width: 36px;
                max-width: 72px;
            }
          .nav {
            min-width: 72px;
          }
          uui-button {
            --uui-button-font-size: 13px;
          }
        }

        .muted {
            color: #999;
            font-style: italic;
        }


        .filters {
            height: 53px;
            align-items: end;
        }

        .table-actions {
            position: absolute;
            left: 0; top: 0; right: 0;
            background-color: #3544b1;
            border-color: #3544b1;
            border-radius: 3px;
            padding: 10px;
            color: #fff;
            display: flex;
            .left, right {
                display: flex;
                gap: 10px;
                align-items: center;
            }
            .right {
                margin-left: auto;
            }
        }

        .user-name, .member-name {
            width: fit-content;
            max-width: 200px;
        }

        @media (min-width : 1850px) {
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

        const self = this;

        this.loaded = false;
        this.loading = false;
        this.params = {};
        this.filters = [];
        this.timeout = null;

        this.updateFilters();
        this.updateList();

        const injector = angular.element(document).injector();
        this.localizationService = injector.get("localizationService");
        this.notificationsService = injector.get("notificationsService");
        this.overlayService = injector.get("overlayService");

        this.localize = {
            term: function (key) {
                return until(self.localizationService.localize(key));
            }
        };

        this.dashboard = {
            element: this,
            title: this.localize.term("unusedMediaDashboard_title"),
            description: this.localize.term("unusedMediaDashboard_description"),
            overwrites: {}
        };

        this.dateOptions = {
            day: "numeric",
            hour: "numeric",
            minute: "numeric",
            month: "long",
            second: "numeric",
            year: "numeric"
        };

        window.dispatchEvent(new UnusedMediaDashboardLoadEvent("limbo.unusedMedia.onDashboardLoad", {
            dashboard: self.dashboard
        }));

    }

    updateFilters() {
        const self = this;
        UnusedMediaService.getFilters().then(res => {
            self.filters = res.data;
            self.filters.forEach(function (filter) {
                if (filter.type === "dropdown" && filter.items?.length > 0) {
                    filter.items[0].selected = true;
                    if (!filter.placeholder) filter.placeholder = filter.items[0].label;
                    filter.items.forEach(function (item) {
                        item.name = item.label;
                    });
                }
            });
            self.requestUpdate();
        });
    }

    updateSites() {
        const self = this;
        get("/umbraco/backoffice/api/UnusedMediaBackOffice/GetSites").then(res => {
            self.sites = res.data;
            self.requestUpdate();
            if (self.sites.filter(x => x.mediaFolderId > 0).length === 0) return;
            const first = { value: "", name: "Vælg site", checked: true, selected: true };
            self.filters.push({
                alias: "siteIds",
                type: "dropdown",
                weight: -10,
                value: first,
                options: [first, ...self.sites.map(function (site) {
                    return {
                        value: site.id,
                        name: site.name,// + (site.domain ? ` (${site.domain})` : ""),
                        disabled: site.mediaFolderId === null
                    };
                })]
            });
        });
    }

    updateList(page) {

        const self = this;

        this.loading = true;
        self.requestUpdate();

        if (page) this.params.page = page;

        const config = {
            params: this.params
        };

        UnusedMediaService.getUnusedMedia(config).then(res => {

            this.allSelected = false;

            self.reports = res.data.reports;

            self.columns = res.data.columns;
            self.columns.forEach(function (column) {
                self.columns[column.alias] = column;
            });

            self.items = res.data.items;
            self.items.forEach(function (item) {
                item.cells.forEach(function (cell) {
                    cell.column = self.columns[cell.alias];
                    if (cell.column) cell.type = cell.column.type;
                    if (!(cell.type === "user" || cell.type === "member" || cell.type === "memberPicker")) {
                        cell.classes = cell.type === "name" ? "" : "nw";
                    }
                });
            });

            this.stats = res.data;
            delete this.stats.reports;
            delete this.stats.columns;
            delete this.stats.items;

            this.activeFilters = Object.keys(this.params).filter(x => !!self.params[x]).length;

            this.pagination = {
                from: this.stats.offset + 1,
                to: Math.min(this.stats.offset + this.stats.limit, this.stats.unused),
                page: this.stats.page,
                pages: this.stats.pages,
                total: this.stats.unused,
                pagination: []
            };

            for (let i = Math.max(1, this.stats.page - 5); i <= Math.min(this.stats.page + 5, this.stats.pages); i++) {
                this.pagination.pagination.push({
                    page: i,
                    active: i === this.stats.page
                });
            }

            this.loading = false;
            this.loaded = true;

            self.requestUpdate();

        });

    }

    onPageChange(e) {
        this.updateList(e.target.current);
    }

    onFilterChange(e, filter) {
        filter.value = e.target.value;
        this.params[filter.name] = filter.value;
        if (filter.type === "text") {
            clearTimeout(this.timeout);
            this.timeout = setTimeout(() => this.updateList(), 250);
        } else if (filter.type === "dropdown") {
            filter.items.forEach(function (o) {
                o.selected = o.value === filter.value;
                o.checked = o.value === filter.value;
            });
            this.updateList();
        } else {
            this.updateList();
        }
    }

    trashMedia(media) {
        const self = this;
        media.trashMediaButtonState = "waiting";
        self.requestUpdate();
        const multi = Array.isArray(media) && media.length > 1;
        return UnusedMediaService.trashMedia(media).then(function (response) {
            self.notificationsService.success("Ikke-brugte medier", multi ? "De valgte medier er nu blevet flyttet til papirkurven." : "Det valgte medie er nu blevet flyttet til papirkurven.");
            self.updateList();
            media.trashMediaButtonState = "success";
            self.requestUpdate();
        }, function (error) {
            self.notificationsService.error("Ikke-brugte medier", multi ? "Der opstod en fejl i forbindelse med sletningen af de valgte medier." : "Der skete en fejl i forbindelse med sletningen af mediet.");
            media.trashMediaButtonState = "failed";
            self.requestUpdate();
        });
    }

    requestDelete(media) {

        const self = this;

        // TODO: introduce a setting whether we should delete or trash?

        const multi = Array.isArray(media) && media.length > 1;

        let title;
        let content;
        if (Array.isArray(media)) {
            if (media.length > 1) {
                title = "Slet medier";
                content = "Er du sikker på, at du vil flytte de valgte medier til papirkurven?";
            } else {
                title = "Slet medie";
                content = "Er du sikker på, at du vil flytte mediet \"" + media[0].name + "\" til papirkurven?";
            }
        } else {
            title = "Slet medie";
            content = "Er du sikker på, at du vil flytte mediet \"" + media.name + "\" til papirkurven?";
        }

        self.overlayService.confirm({
            title,
            content,
            submitButtonLabel: "Slet",
            closeButtonLabel: "Annuller",
            submitButtonStyle: "danger",
            submit: function (model) {
                model.submitButtonState = "busy";
                self.trashMedia(media).then(function () {
                    self.overlayService.close();
                });
            },
            close: function () {
                self.overlayService.close();
            }
        });

    }

    requestDeleteSelected() {
        const self = this;
        const selected = this.items.filter(x => x.selected);
        if (selected.length === 0) return;
        this.requestDelete(selected);
    }

    toggleReports() {
        this.showReports = !this.showReports;
        this.requestUpdate();
    }

    openReports() {
        this.showReports = true;
        this.requestUpdate();
    }

    closeReports() {
        this.showReports = true;
        this.requestUpdate();
    }

    setSortOrder(column, sortOrder) {
        this.params.sortField = column.alias;
        this.params.sortOrder = sortOrder;
        this.updateList();
    }

    async startScan(report) {

        report.buttonState = "waiting";
        this.requestUpdate();

        const response = await waitAtLeast(UnusedMediaService.startScan(report), 1000);

        if (response.status === 200) {

            report.createDate = response.data.createDate;
            report.keys = response.data.keys;

            report.buttonState = "success";
            this.requestUpdate();

        } else {

            report.buttonState = "failed";
            this.requestUpdate();

        }

    }

    toggleAll(e) {
        this.allSelected = e.target.checked;
        this.items.forEach(item => { item.selected = this.allSelected; });
        this.selectedCount = this.items.filter(i => i.selected).length;
        this.requestUpdate();
    }

    toggleItem(e, item) {
        item.selected = e.target.checked;
        this.selectedCount = this.items.filter(i => i.selected).length;
        this.allSelected = this.items.every(i => i.selected);
        this.requestUpdate();
    };

    renderStats() {

        if (!this.stats || !this.items || this.items.length === 0) return;

        return html`
            <div class="stats">
                <div class="stats-numbers">
                    <strong>${formatNumber(this.stats.unused)}</strong>
                    af
                    <strong>${formatNumber(this.stats.total)}</strong>
                    medier
                    (${formatNumber(this.stats.unusedPercent, 2)}%)
                    er ikke direkte i brug.
                </div>
                ${when(this.reports?.length > 0, () => html`
                    <div class="stats-report">
                        ${when(this.reports?.length == 1, () => html`
                            <span>
                                Baseret på udtræk fra
                                <strong style="cursor: pointer;" @click=${() => this.toggleReports()}>${formatDate(this.reports[0].createDate, this.dateOptions)}</strong>
                            </span>
                        `)}
                        ${when(this.reports?.length > 1, () => html`
                            <span>
                                Baseret på data fra
                                <strong style="cursor: pointer;" @click=${() => this.toggleReports()}>${this.reports.length} udtræk</strong>
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
            <uui-box headline="Udtræk">
                <uui-table role="table">
                    <uui-table-head role="row">
                        <uui-table-head-cell role="columnheader">Navn</uui-table-head-cell>
                        <uui-table-head-cell role="columnheader">Sidste scan</uui-table-head-cell>
                        <uui-table-head-cell role="columnheader">&nbsp;</uui-table-head-cell>
                    </uui-table-head>
                    ${repeat(this.reports, (report) => html`
                        <uui-table-row role="row">
                            <uui-table-cell role="cell" class="fw">
                                <strong>${report.name}</strong>
                            </uui-table-cell>
                            <uui-table-cell role="cell" class="nw">
                                ${formatDate(report.createDate, this.dateOptions)}
                            </uui-table-cell>
                            <uui-table-cell role="cell" class="nw">
                                <uui-button look="secondary" compact="true" state="${report.buttonState}" @click=${() => this.startScan(report)} label="Scan igen"></uui-button>
                            </uui-table-cell>
                        </uui-table-row>
                    `)}
                </uui-table>
            </uui-box>
        `;

    }

    renderFilters() {

        if (!this.filters || this.filters.length == 0) return;

        return html`
            <div class="filters">
                ${repeat(this.filters, (filter) => this.renderFilter(filter))}
                <!--
                <uui-button type="button" compact look="outline" @csdlick=${() => this.updateList()}>
                    <umb-icon name="filter">
                        <uui-icon name="icon-filter"></uui-icon>
                    </umb-icon>
                </uui-button>
                -->
            </div>
        `;

    }

    renderFilter(filter) {

        switch (filter.type) {

            case "text":
                return html`
                    <uui-input
                        value="${filter.value}"
                        label="${filter.placeholder}"
                        placeholder="${filter.placeholder}"
                        @input="${(e) => this.onFilterChange(e, filter)}"></uui-input>
                `;

            case "dropdown":
                return html`
                    <uui-select
                        label="${filter.placeholder}"
                        placeholder="${filter.placeholder}"
                        .options=${filter.items}
                        @change="${(e) => this.onFilterChange(e, filter)}"></uui-select>
                `;

            case "memberPicker":
                return html`
                    <div>

                    </div>
                `;


            default:
                return `{${filter.type}}`;

        }

    }

    renderItems() {

        if (!Array.isArray(this.items)) return;

        if (this.items.length === 0) {
            if (this.activeFilters === 0) {
                return html`
                    <div class="umb-empty-state -center">
                        Godt gået! Der ser ikke ud til at være nogle medier, der ikke er i brug.
                    </div>
                `;
            } else {
                return html`
                    <div class="umb-empty-state -center">
                        Din søgning matchede ikke nogle medier, der ikke er i brug.
                    </div>
                `;
            }
        }

        return html`
            <uui-table role="table">
                <uui-table-column style="width: 1px;"></uui-table-column>
                ${repeat(this.columns, (column) => html`
                    <uui-table-column></uui-table-column>
                `)}
                <uui-table-column style="width: 1px;"></uui-table-column>
                <uui-table-head role="row">
                    <uui-table-head-cell role="columnheader">
                        <uui-checkbox .checked=${this.allSelected} @change=${(e) => this.toggleAll(e)}></uui-checkbox>
                    </uui-table-head-cell>
                    ${repeat(this.columns, (column) => this.renderColumn(column))}
                </uui-table-head>
                ${repeat(this.items, (item) => html`
                    <uui-table-row role="row">
                        <uui-table-cell role="cell">
                            <uui-checkbox .checked=${item.selected} @change=${(e) => this.toggleItem(e, item)}></uui-checkbox>
                        </uui-table-cell>
                        ${repeat(item.cells, (cell, index) => this.renderCell(cell, index, item))}
                        <uui-table-cell role="cell">
                            <uui-action-bar>
                                <uui-button
                                    look="secondary"
                                    color="invalid"
                                    state="${item.trashMediaButtonState}"
                                    label="Delete media"
                                    @disabled=${item.trashMediaButtonState === "waiting"}
                                    @click="${() => this.requestDelete(item)}"
                                >
                                    <uui-icon name="delete"></uui-icon>
                                </uui-button>
                            </uui-action-bar>
                        </uui-table-cell>
                    </uui-table-row>
                `)}
            </uui-table>
        `;

    }

    renderColumn(column) {

        const isSortField = this.stats.sortField === column.alias;

        const nextOrder = isSortField ? (this.stats.sortOrder === "descending" ? "ascending" : "descending") : column.defaultOrder;

        return html`
            <uui-table-head-cell role="columnheader">
                ${when(column.allowSort, () => html`
                    <button type="button" @click="${() => this.setSortOrder(column, nextOrder)}">${column.name}</button>
                    <small>${isSortField ? (this.stats.sortOrder === "ascending" ? "▲" : "▼") : ""}</small>
                `, () => html`
                    ${column.name}
                `)}
            </uui-table-head-cell>
        `;

    }

    renderCell(cell, index, item) {

        if (cell.type === "name") {
            return html`
                <uui-table-cell role="cell" class="${cell.classes}">
                    <a href="/umbraco/#/media/media/edit/${item.id}" target="_blank">${cell.text || cell.value}</a>
                    <div class="path">
                        ${repeat(item.path, (folder) => html`
                            <span>${folder}</span>
                            <span class="separator">/</span>
                        `)}
                        <a href="${item.url}" target="_blank" rel="noreferrer noopener">${item.url.split('/')[item.url.split('/').length - 1]}</a>
                    </div>
                </uui-table-cell>
            `;
        }

        if (cell.type === "user" || cell.type == "member") {
            return html`
                <uui-table-cell role="cell" class="${cell.classes}">
                    ${when(cell.value, () => html`
                        <div class="${cell.type}-name">${cell.text || cell.value}</div>
                    `, () => html`
                        <span class="muted">N/A</span>
                    `)}
                </uui-table-cell>
            `;
        }

        return html`
            <uui-table-cell role="cell" class="${cell.classes}">${cell.text || cell.value}</uui-table-cell>
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

                        ${repeat(this.pagination.pagination, (page) => html`
                            <uui-button
                              compact
                              look="outline"
                              role="listitem"
                              label="Go to page ${page.page}"
                              class=${'page' + (page.active ? ' active' : '')}
                              tabindex=${page === this.pagination.page ? '-1' : ''}
                              @click=${() => {
                if (page === this.pagination.page) return;
                this.updateList(page.page);
            }}>
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

        if (!this.items) return;
        if (this.items.length === 0) return;
        if (!this.items.some(x => x.selected)) return;

        return html`
            <div class="table-actions">
                <div class="left">
                    <uui-button look="secondary" @click="${(e) => this.clearSelection(e)}" label="${this.localize.term("buttons_clearSelection")}"></uui-button>
                    <strong>
                        ${this.selectedCount} af ${this.items.length} valgt
                    </strong>
                </div>
                <div class="right">
                    <uui-button look="primary" color="danger" @click="${(e) => this.requestDeleteSelected(e)}" label="${this.localize.term("actions_delete")}"></uui-button>
                </div>
            </div>
        `;

    }

    render() {
        return html`
            <div>
                <h1>${this.dashboard.title}</h1>
                <p>${this.dashboard.description}</p>
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