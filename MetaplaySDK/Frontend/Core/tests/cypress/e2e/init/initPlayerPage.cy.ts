// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('Player Detail Init', [], function () {
  it('Initializes Player Detail Page (by waiting it to render)', function () {
    cy.visit(`/players/${this.testPlayer.id}`)

    // We know the page is ready when the inbox card renders.
    cy.get('[data-testid=player-inbox-card]', { timeout: 20000 })
  })
})
