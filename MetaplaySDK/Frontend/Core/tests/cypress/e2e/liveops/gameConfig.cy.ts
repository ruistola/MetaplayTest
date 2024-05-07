// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('Game Config', [], function () {
  before(function () {
    cy.visit('/gameConfigs')
  })

  it('Checks that an active sidebar link exists to the current page', function () {
    cy.sidebarLinkToCurrentPageShouldExist()
  })

  it.skip('Checks that list elements render', function () {
    cy.get('[data-testid=available-configs]')
  })

  it.skip('Contains at least one viewable config item that can be clicked on', function () {
    cy.get('[data-testid=view-config]')
      .first()
      .click({ force: true })
  })

  it.skip('Checks that detail page elements render', function () {
    cy.get('[data-testid=game-config-overview]')
    cy.get('[data-testid=audit-log-card]')
  })

  it.skip('Clicks on the first library item in the contents card', function () {
    cy.get('[data-testid=config-contents-card]')
      .find('[data-testid=library-title-row]')
      .first()
      .click()
  })
})
