// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('Overwrite Player', [], function () {
  let clipboard = ''

  before(function () {
    this.newName = `Overwrite${this.testToken}`

    cy.visit(`/players/${this.testPlayer.id}`)
  })

  it('Opens the export player dialog', function () {
    cy.get('[data-testid=export-player-button]')
      .click({ force: true })
    cy.get('[data-testid=export-player-modal]')
      .should('exist')
  })

  it('Copy player to clipboard', function () {
    // True clipboard copy (ie: clicking on the 'copy' button) isn't currently supported
    // in Cypress due to security reasons, so we manually copy the text here
    cy.get('[data-testid=export-payload]')
      .should('not.be.empty')
      .invoke('text').then(contents => {
        clipboard = contents
        expect(clipboard).to.contain('entities')
      })
  })

  it('Closes the modal', function () {
    cy.get('[data-testid=export-player-modal-cancel]')
      .click({ force: true })
    cy.get('[data-testid=export-player-modal')
      .should('not.exist')
  })

  it('Changes the player name', function () {
    cy.get('[data-testid=action-edit-name-button]')
      .click({ force: true })
    cy.get('[data-testid=action-edit-name-modal')
      .should('exist')

    cy.get('[data-testid=name-input]')
      .type(this.newName)

    cy.get('[data-testid=action-edit-name-modal-ok-button-root]')
      .clickMButton()
    cy.get('[data-testid=action-edit-name-modal')
      .should('not.exist')

    cy.get('[data-testid=player-overview-card]')
      .contains(this.newName)
  })

  it('Opens the overwrite player dialog', function () {
    cy.get('[data-testid=action-overwrite-player-button]')
      .click({ force: true })
    cy.get('[data-testid=action-overwrite-player-modal')
      .should('exist')
  })

  it('Paste player data', function () {
    cy.get('[data-testid=entity-archive-text]')
      .paste(clipboard)
  })

  it('Closes the modal', function () {
    cy.get('[data-testid=action-overwrite-player-modal-ok-button-root]')
      .clickMButton()
    cy.get('[data-testid=action-overwrite-player-modal')
      .should('not.exist')
  })

  it('Checks that name was restored', function () {
    cy.get('[data-testid=player-overview-card]')
      .should('exist')
      .should('not.contain', this.newName)
  })
})
