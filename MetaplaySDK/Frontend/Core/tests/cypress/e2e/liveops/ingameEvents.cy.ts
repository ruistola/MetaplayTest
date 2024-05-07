// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('In-Game Events', ['events'], function () {
  before(function () {
    cy.visit('/activables/Event')
  })

  it('Checks that an active sidebar link exists to the current page', function () {
    cy.sidebarLinkToCurrentPageShouldExist()
  })

  it('Checks that list page elements render', function () {
    cy.get('[data-testid=all]')
    cy.get('[data-testid=custom-time]')
  })

  it.skip('TODO: Checks that custom time tool works', function () {
    // TODO
  })

  it('Navigates into an event', function () {
    cy.get('[data-testid=view-activable]')
      .first()
      .click({ force: true })
  })

  it('Checks that detail page elements render', function () {
    cy.get('[data-testid=overview]')
    cy.get('[data-testid=game-configuration]')
    cy.get('[data-testid=activable-configuration]')
    cy.get('[data-testid=segments]')
    cy.get('[data-testid=conditions]')
  })
})
