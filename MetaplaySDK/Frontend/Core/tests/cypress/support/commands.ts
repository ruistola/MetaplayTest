// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

// NOTE: When editing this file, please make sure to also update the typings in cypress.d.ts and make sure it ends up getting copied to userland projects.

import { recurse } from 'cypress-recurse'

cy.maybeDescribe = (name, dependencies, testFunction) => {
  const skipFeatureFlags = Cypress.env('metaplay').skipFeatureFlags as string[]

  const skipReasons = [
    ...skipFeatureFlags.filter(skipFeature => dependencies.find(feature => feature.toLowerCase() === skipFeature.toLowerCase())).map(skipFeature => `SkipFeatureFlag '${skipFeature}'`)
  ]
  if (skipReasons.length === 0) {
    describe(name, { testIsolation: false }, testFunction)
  } else {
    describe(name, () => { it('Skipped by configuration: ' + skipReasons.map(reason => '"' + reason + '"').join(', ')) })
  }
}

Cypress.Commands.add('paste', { prevSubject: true }, ($element, text) => {
  const subString = text.slice(0, -1)
  const lastChar = text.slice(-1)
  recurse(
    () => cy.get($element).then(() => $element.val(subString)).type(lastChar),
    ($input) => $input.val() === text,
  ).should('have.value', text)
})

Cypress.Commands.add('clickMetaButton', { prevSubject: true }, (subject: JQuery<HTMLElement>) => {
  // Disable the safety-lock if it is on. I hate this code so much. Let's revisit after MetaButton is migrated to MButton as that critically affects this business logic.
  // Short explanation: Click the button mutates multiple parent nodes so Cypress loses the handle. We need to store the non-mutating root node.
  let parent = cy.wrap(subject).parent().parent().parent()

  cy.wrap(subject)
    .should('have.attr', 'safety-lock-active').then((attr) => {
      if (attr as unknown as string === 'yes') {
        parent
          .find('[data-testid="safety-lock-button"]')
          .trigger('click', { force: true })

        // The above code re-assigns the parent (ugh!) so we reset it here back to the initial value.
        parent = parent.parent()
      }
    })

  // Now click the button.
  parent
    .find('button')
    .should('not.be.disabled')
    .click({ force: true })
})

Cypress.Commands.add('clickMButton', { prevSubject: true }, (subject: JQuery<HTMLElement>) => {
  // Disable the safety-lock if it is on. I hate this code so much. Let's revisit after MetaButton is migrated to MButton as that critically affects this business logic.

  cy.wrap(subject)
    .should('have.attr', 'safety-lock-active').then((attr) => {
      if (attr as unknown as string === 'yes') {
        subject
          .find('[data-testid="safety-lock-button"]')
          .trigger('click', { force: true })
      }
    })

  // Now click the button.
  cy.wrap(subject)
    .find('button')
    .should('not.be.disabled')
    .click({ force: true })
})

Cypress.Commands.add('sidebarLinkToCurrentPageShouldExist', () => {
  cy.url().then((fullUrl) => {
    // Remove baseurl from full url
    const url = fullUrl.replace(String(Cypress.config().baseUrl), '')

    cy.get('[data-testid="sidebar"]')
      .find(`[href="${url}"]`)
      .should('exist')
      .should('not.be.disabled')
  })
})

/**
 * Command to type into an element and make sure that the value is set correctly.
 * Used to fix the issue with Cypress sometimes not being able to type into input fields because the element loses
 * focus or the key events go missing.
 */
Cypress.Commands.add('typeCarefully', { prevSubject: false }, (selectorElement: string, text: string) => {
  recurse(
    () => cy.get(selectorElement).clear().type(text),
    ($input) => $input.val() === text,
  ).should('have.value', text)
})

export {}
