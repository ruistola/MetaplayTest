// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('Offers Groups', ['offerGroups'], function () {
  before(function () {
    cy.visit('/offerGroups')
  })

  it('Checks that an active sidebar link exists to the current page', function () {
    cy.sidebarLinkToCurrentPageShouldExist()
  })

  it('Checks that list page elements render', function () {
    cy.get('[data-testid=custom-time]')
  })

  it.skip('TODO: Checks that custom time tool works', function () {
    // TODO
  })

  it('Navigates into an offer group', function () {
    cy.get('[data-testid=view-activable]')
      .first()
      .click({ force: true })
  })

  it('Checks that detail page elements render', function () {
    cy.get('[data-testid=overview]')
    cy.get('[data-testid=individual-offers]')
    cy.get('[data-testid=activable-configuration]')
    cy.get('[data-testid=segments]')
    cy.get('[data-testid=conditions]')
  })

  it('Navigates into an offer', function () {
    cy.get('[data-testid=view-offer]')
      .first()
      .click({ force: true })
  })

  it('Checks that detail page elements render', function () {
    cy.get('[data-testid=overview]')
    cy.get('[data-testid=offers]')
    cy.get('[data-testid=references]')
    cy.get('[data-testid=segments]')
    cy.get('[data-testid=conditions]')
  })
})
