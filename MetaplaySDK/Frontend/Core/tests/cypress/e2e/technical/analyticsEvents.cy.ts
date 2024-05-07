// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('Analytics Events', [], function () {
  before(function () {
    cy.visit('/analyticsEvents')
  })

  it('Checks that an active sidebar link exists to the current page', function () {
    cy.sidebarLinkToCurrentPageShouldExist()
  })

  it('Finds core events list', function () {
    cy.get('[data-testid=core-events]')
  })

  it('Finds custom events list', function () {
    cy.get('[data-testid=custom-events]')
  })

  it('Navigates into an event', function () {
    cy.get('[data-testid=analytics-details-link]')
      .first()
      .click({ force: true })
  })

  it('Checks that detail page elements render', function () {
    cy.get('[data-testid=overview]')
    cy.get('[data-testid=bq-event]')
  })
})
