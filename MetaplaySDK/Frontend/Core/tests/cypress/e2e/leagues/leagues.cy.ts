// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('Leagues', ['playerLeagues'], function () {
  before(function () {
    cy.visit('/leagues')
  })

  it('Checks that an active sidebar link exists to the current page', function () {
    cy.sidebarLinkToCurrentPageShouldExist()
  })

  it('Checks that elements render', function () {
    cy.get('[data-testid=league-list-overview-card]')
    cy.get('[data-testid=league-list-card]')
  })

  it('Navigates into a league', function () {
    cy.get('[data-testid=view-league]')
      .first()
      .click({ force: true })
  })

  it('Checks that league detail page elements render', function () {
    cy.get('[data-testid=league-detail-overview-card]')
    cy.get('[data-testid=league-seasons-list-card]')
    cy.get('[data-testid=league-schedule-card]')
    cy.get('[data-testid=audit-log-card]')
  })

  it('Navigates into latest season', function () {
    cy.get('[data-testid=latest-season-button-link]')
      .click({ force: true })
  })

  it('Checks that league season detail page elements render', function () {
    cy.get('[data-testid=league-season-detail-overview-card]')
    cy.get('[data-testid=league-season-ranks-card]')
  })
})
