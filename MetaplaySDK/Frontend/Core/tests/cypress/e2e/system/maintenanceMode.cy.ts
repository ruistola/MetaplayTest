// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('Maintenance Mode', [], function () {
  before(function () {
    cy.visit('/system')
  })

  it('Opens the maintenance mode dialog', function () {
    cy.get('[data-testid=system-maintenance-mode-card]')
      .find('[data-testid=maintenance-mode-button]')
      .click({ force: true })
  })

  it('Toggles the settings', function () {
    cy.get('[data-testid=input-switch-maintenance-enabled]')
      .click()
  })

  it('Saves the maintenance-mode settings', function () {
    cy.get('[data-testid=maintenance-mode-modal-ok-button-root]')
      .clickMButton()
  })

  // it('Toggles maintenance mode on through the API', function () {
  //   const payload = {
  //     StartAt: '2100-01-01T00:00:00.000+00:00',
  //     EstimatedDurationInMinutes: 0,
  //     EstimationIsValid: false,
  //     PlatformExclusions: []
  //   }
  //   cy.task('makeApiRequest', { endpoint: '/api/maintenanceMode', method: 'put', payload })
  // })

  it('Checks that the banner alert is visible', function () {
    cy.get('[data-testid=maintenance-mode-header-notification]')
      .find('[data-testid=maintenance-scheduled-label]')
  })

  it('Opens the maintenance mode dialog again', function () {
    cy.get('[data-testid=system-maintenance-mode-card]')
      .find('[data-testid=maintenance-mode-button]')
      .click({ force: true })
    cy.wait(100) // A small wait to ensure the modal is open.
  })

  it('Toggles the settings again', function () {
    cy.get('[data-testid=input-switch-maintenance-enabled]')
      .click()
    cy.wait(100) // A small wait to ensure the DOM updated.
  })

  it('Saves the maintenance-mode settings again', function () {
    cy.get('[data-testid=maintenance-mode-modal-ok-button-root]')
      .clickMButton()
  })

  // it('Toggles maintenance mode off through the API', function () {
  //   cy.task('makeApiRequest', { endpoint: '/api/maintenanceMode', method: 'delete' })
  // })

  it('Checks that the banner alert is no longer visible', function () {
    cy.get('[data-testid=maintenance-mode-header-notification]')
      .should('not.exist')
  })
})
