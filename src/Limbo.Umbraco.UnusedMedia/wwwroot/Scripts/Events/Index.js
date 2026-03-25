export class UnusedMediaDashboardLoadEvent extends Event {
    constructor(evName, eventInit = {}) {
        super(evName, { ...eventInit })
        this.dashboard = eventInit.dashboard || {};
    }
}
