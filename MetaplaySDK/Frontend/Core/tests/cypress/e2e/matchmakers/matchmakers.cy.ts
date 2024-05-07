// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('Matchmaker List', ['asyncMatchmaker'], function () {
  before(function () {
    cy.visit('/matchmakers')
  })

  it('Checks that an active sidebar link exists to the current page', function () {
    cy.sidebarLinkToCurrentPageShouldExist()
  })

  it('Checks that elements render', function () {
    cy.get('[data-testid=matchmakers-overview-card]')
    cy.get('[data-testid=async-matchmakers-list-card]')
    cy.get('[data-testid=realtime-matchmakers-list-card]')
  })

  it('Navigates into a matchmaker', function () {
    cy.get('[data-testid=view-matchmaker]')
      .first()
      .click({ force: true })
  })

  it('Checks that detail page elements render', function () {
    cy.get('[data-testid=matchmaker-overview-card]')
    cy.get('[data-testid=matchmaker-bucket-chart]')
    cy.get('[data-testid=matchmaker-buckets-list-card]')
    cy.get('[data-testid=matchmaker-top-players-list-card]')
    cy.get('[data-testid=audit-log-card]')
  })

  it('Opens the simulation modal', function () {
    cy.get('[data-testid=simulate-matchmaking-button]')
      .click({ force: true })
  })

  it('Types in a new MMR', function () {
    cy.get('[data-testid=attackmmr-input]')
      .clear()
      .type('9001')
      .should('have.value', '9001')
  })

  it('Clicks simulate', function () {
    cy.get('[data-testid=simulate-matchmaking-modal-cancel]')
      .click({ force: true })
  })

  it('Closes the modal', function () {
    cy.get('[data-testid=simulate-matchmaking-modal-cancel]')
      .click({ force: true })
  })

  it.skip('Opens the rebalancing modal', function () {
    cy.get('[data-testid=rebalance-matchmaker-button]')
      .click({ force: true })
  })

  it.skip('Triggers rebalancing', function () {
    cy.get('[data-testid=rebalance-matchmaker-modal-ok-button-root]')
      .clickMButton()
  })

  it('Opens the reset modal', function () {
    cy.get('[data-testid=reset-matchmaker-button]')
      .click({ force: true })
  })

  it('Triggers reset', function () {
    cy.get('[data-testid=reset-matchmaker-modal-ok-button-root]')
      .clickMButton()
  })
})
