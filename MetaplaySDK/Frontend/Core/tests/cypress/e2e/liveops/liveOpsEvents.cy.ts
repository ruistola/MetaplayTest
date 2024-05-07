// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('LiveOps Events', ['liveOpsEvents'], function () {
  let liveOpsEventIds
  let hasLiveOpsEvents
  before(function () {
    cy.task('makeApiRequest', { endpoint: '/api/liveOpsEvents' })
      .then((data: any) => {
        liveOpsEventIds = [...data.ongoingAndPastEvents.map((event: any) => event.eventId), ...data.upcomingEvents.map((event: any) => event.eventId)]
        hasLiveOpsEvents = liveOpsEventIds.length > 0
      })

    cy.visit('/liveOpsEvents')
  })

  it('Checks that an active sidebar link exists to the current page', function () {
    cy.sidebarLinkToCurrentPageShouldExist()
  })

  it('Checks that list page element renders', function () {
    cy.get('[data-testid=overview]')
    cy.get('[data-testid=upcoming-events]')
    cy.get('[data-testid=ongoing-and-past-events]')
  })

  it('Checks that new liveOpsEvent modal can be opened and closed ', function () {
    cy.get('[data-testid=create-event-form-button-button-root]')
      .clickMButton()
    cy.get('[data-testid=create-event-form-modal]')
    cy.get('[data-testid=create-event-form-modal-cancel-button-root]')
      .clickMButton()
  })

  it('Navigates into an event', function () {
    if (hasLiveOpsEvents) {
      cy.visit(`/liveOpsEvents/${liveOpsEventIds[0]}`)
    } else {
      this.skip()
    }
  })

  it('Checks that detail page elements render', function () {
    if (hasLiveOpsEvents) {
      cy.get('[data-testid=overview]')
      cy.get('[data-testid=related-events]')
      cy.get('[data-testid=event-configuration]')
      cy.get('[data-testid=targeting-card]')
    } else {
      this.skip()
    }
  })
})
