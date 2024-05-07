// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('Edit Name', [], function () {
  before(function () {
    this.newName = `Edit${this.testToken}`

    cy.visit(`/players/${this.testPlayer.id}`)
  })

  it('Opens the edit name dialog', function () {
    cy.get('[data-testid=action-edit-name-button]')
      .click({ force: true })
  })

  it('Types in a new name', function () {
    cy.get('[data-testid=name-input]')
      .type(this.newName)
      .should('have.value', this.newName)
  })

  it('Saves the new name after server validation', function () {
    cy.get('[data-testid=action-edit-name-modal-ok-button-root]')
      .clickMButton()
    cy.get('[data-testid=action-edit-name-modal')
      .should('not.exist')
  })

  it('Checks that name was changed', function () {
    cy.get('[data-testid=player-overview-card]')
      .contains(this.newName)
  })

  it('Navigates to tab 1', function () {
    cy.get('[data-testid=player-details-tab-1]')
      .click()
  })

  it('Checks that a player log entry was created', function () {
    cy.get('[data-testid=audit-log-card]')
      .contains(this.newName)
  })
})
