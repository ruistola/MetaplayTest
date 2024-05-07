// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('NFT Collections List', ['web3'], function () {
  before(function () {
    cy.visit('/web3')
  })

  it('Checks that an active sidebar link exists to the current page', function () {
    cy.sidebarLinkToCurrentPageShouldExist()
  })

  it('Checks that elements render', function () {
    cy.get('[data-testid=web3-overview-card]')
    cy.get('[data-testid=nft-collections-list-card]')
  })

  it('Navigates into a collection', function () {
    cy.get('[data-testid=view-nft-collection]')
      .first()
      .click({ force: true })
  })

  it('Checks that collection detail page elements render', function () {
    cy.get('[data-testid=nft-collection-overview-card]')
    cy.get('[data-testid=nft-collection-nft-list]')
    cy.get('[data-testid=nft-collection-uninitialized-nfts-card]')
    cy.get('[data-testid=nft-collection-audit-log-card]')
  })

  it('Refreshes collection metadata', function () {
    cy.get('[data-testid=refresh-nft-collection-button]')
      .click({ force: true })
    cy.get('[data-testid=refresh-nft-collection-modal-ok-button-root]')
      .clickMButton()
  })

  // TODO: how to automatically test batch initialization?

  it('Initializes a new NFT', function () {
    cy.get('[data-testid=initialize-nft-button]')
      .click({ force: true })
      .wait(1000) // TODO: replace with better code when generated UI supports it.
    cy.get('[data-testid=initialize-nft-modal-ok-button-root]')
      .clickMButton()
  })

  it('Navigates into an NFT', function () {
    cy.get('[data-testid=view-nft]')
      .first()
      .click({ force: true })
  })

  it('Checks that NFT detail page elements render', function () {
    cy.get('[data-testid=nft-overview-card]')
    // cy.get('[data-testid=nft-game-state-card]')
    cy.get('[data-testid=nft-public-data-preview-card]')
    cy.get('[data-testid=nft-audit-log-card]')
  })

  it('Refreshes NFT ownership', function () {
    cy.get('[data-testid=refresh-nft-button]')
      .click({ force: true })
    cy.get('[data-testid=refresh-nft-modal-ok-button-root]')
      .clickMButton()
  })

  it('Re-saves NFT metadata', function () {
    cy.get('[data-testid=republish-nft-metadata-button]')
      .click({ force: true })
    cy.get('[data-testid=republish-nft-metadata-modal-ok-button-root]')
      .clickMButton()
  })

  it('Re-initializes the NFT', function () {
    cy.get('[data-testid=edit-nft-button]')
      .click({ force: true })
      .wait(1000) // TODO: replace with better code when generated UI supports it.
    cy.get('[data-testid=edit-nft-modal-ok-button-root]')
      .clickMButton()
  })
})
