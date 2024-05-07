// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('Player Export', [], function () {
  before(function () {
    cy.visit(`/players/${this.testPlayer.id}`)
  })

  it('Opens the player export dialog', function () {
    cy.get('[data-testid=export-player-button]')
      .click({ force: true })
  })

  it('Checks that the text field has some data', function () {
    cy.get('[data-testid=export-payload]')
      .contains(this.testPlayer.id)
  })

  it('Clicks the copy button to get the right payload into the clipboard', function () {
    // True clipboard copy (ie: clicking on the 'copy' button) isn't currently supported
    // in Cypress due to security reasons, so we manually copy the text here
    cy.get('[data-testid=export-payload]')
      .should('not.be.empty')
      .invoke('text').then(contents => {
        const clipboardAsObject = JSON.parse(contents)
        expect(Object.keys(clipboardAsObject.entities.player)).to.contain(this.testPlayer.id)
        expect(Object.keys(clipboardAsObject.entities.player[this.testPlayer.id])).to.contain('payload')
      })
  })

  it('Closes the modal', function () {
    cy.get('[data-testid=export-player-modal-cancel]')
      .click()
  })
})
