// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('Player Matchmaker', ['asyncMatchmaker'], function () {
  before(function () {
    cy.visit(`/players/${this.testPlayer.id}`)
  })

  it('Opens matchmaker panel', function () {
    cy.get('[data-testid=matchmaker-list-entry]')
      .first()
      .click({ force: true })
  })

  it.skip('Opens a matchmaker simulation modal', function () {
    cy.get('[data-testid=simulate-matchmaker-button]')
      .click({ force: true })
  })

  it.skip('Checks that there is no error message', function () {
    cy.get('[data-testid=meta-api-error]')
      .should('not.exist')
  })

  it.skip('Closes the simulation modal', function () {
    cy.get('[data-testid=simulate-matchmaking-close-button]')
      .clickMetaButton()
  })

  it('Join matchmaker if not already', function () {
    cy.get('body').then((body) => {
      if (body.find('[data-testid=enter-matchmaker-button]').length !== 0) {
        cy.get('[data-testid=enter-matchmaker-button]').click({ force: true })
        cy.get('[data-testid=enter-matchmaker-modal-ok-button-root]').clickMButton()
      }
    })
  })

  it('Opens a matchmaker exit modal', function () {
    cy.get('[data-testid=exit-matchmaker-button]')
      .click({ force: true })
    cy.get('[data-testid=exit-matchmaker-modal')
      .should('exist')
  })

  it('Exits a matchmaker', function () {
    cy.get('[data-testid=exit-matchmaker-modal-ok-button-root]')
      .clickMButton()
    cy.get('[data-testid=exit-matchmaker-modal')
      .should('not.exist')
  })

  it('Opens a matchmaker join modal', function () {
    cy.get('[data-testid=enter-matchmaker-button]')
      .click({ force: true })
    cy.get('[data-testid=enter-matchmaker-modal')
      .should('exist')
  })

  it('Enters a matchmaker', function () {
    cy.get('[data-testid=enter-matchmaker-modal-ok-button-root]')
      .clickMButton()
    cy.get('[data-testid=enter-matchmaker-modal')
      .should('not.exist')
  })
})
