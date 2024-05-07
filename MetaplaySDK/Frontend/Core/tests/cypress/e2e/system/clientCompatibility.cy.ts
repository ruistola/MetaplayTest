// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

// Temporarily disabled due to flakiness.
// See related ticket: https://www.notion.so/metaplay/Investigate-status-subscription-error-89bf471347514d66a04de0ee9d864220

// cy.maybeDescribe('Client Compatibility', [], function () {
//   before(function () {
//     // We expect the client compatibility settings to be off by default.
//     cy.task('makeApiRequest', {
//       endpoint: '/api/clientCompatibilitySettings',
//       method: 'POST',
//       payload: {
//         redirectEnabled: false
//       }
//     })
//     cy.visit('/system')
//   })

//   it('Opens the client compatibility dialog', function () {
//     cy.get('[data-testid=client-settings-button]')
//       .click({ force: true })
//   })

//   it('Toggles the setting on', function () {
//     cy.get('[data-testid=input-switch-redirect-enabled]')
//       .click()

//     cy.get('[data-testid=input-text-host]')
//       .clear()
//       .type(`host${this.testToken}`)

//     cy.get('[data-testid=input-text-cdn-url]')
//       .clear()
//       .type(`cdn${this.testToken}`)
//   })

//   it('Saves the settings', function () {
//     cy.get('[data-testid=client-settings-modal-ok-button-root]')
//       .clickMButton()
//   })

//   it('Checks that the redirect is on', function () {
//     cy.get('[data-testid=system-redirect-card]')
//       .contains('New Version Redirect')
//       .siblings()
//       .contains('span', 'On')

//     cy.get('[data-testid=system-redirect-card]')
//       .contains('Host')
//       .siblings()
//       .contains(`host${this.testToken}`)

//     cy.get('[data-testid=system-redirect-card]')
//       .contains('CDN URL')
//       .siblings()
//       .contains(`cdn${this.testToken}`)
//   })

//   it('Opens the client compatibility dialog again', function () {
//     cy.get('[data-testid=client-settings-button]')
//       .click({ force: true })

//     cy.wait(100) // A small wait to ensure the DOM updated.
//   })

//   it('Toggles the settings off', function () {
//     cy.get('[data-testid=input-switch-redirect-enabled]')
//       .click()

//     cy.wait(100) // A small wait to ensure the DOM updated.
//   })

//   it('Saves the settings again', function () {
//     cy.get('[data-testid=client-settings-modal-ok-button-root]')
//       .clickMButton()
//   })

//   it('Checks that the redirect is off', function () {
//     cy.get('[data-testid=system-redirect-card]')
//       .contains('New Version Redirect')
//       .siblings()
//       .contains('span', 'Off')
//   })
// })
