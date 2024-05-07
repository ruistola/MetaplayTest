// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

cy.maybeDescribe('Experiments', [], function () {
  before(function () {
    cy.task('makeApiRequest', { endpoint: '/api/experiments' })
      .then((data: any) => {
        this.hasExperiments = data.experiments.length > 0
      })

    cy.visit('/experiments')
  })

  it('Checks that an active sidebar link exists to the current page', function () {
    cy.sidebarLinkToCurrentPageShouldExist()
  })

  it('Checks that list page element renders', function () {
    cy.get('[data-testid=all-experiments]')
  })

  it('Navigates into an experiment', function () {
    if (this.hasExperiments) {
      cy.get('[data-testid=view-experiment]')
        .first()
        .click({ force: true })
    } else {
      this.skip()
    }
  })

  it('Checks that detail page elements render', function () {
    if (this.hasExperiments) {
      cy.get('[data-testid=overview]')
      cy.get('[data-testid=segments]')
      cy.get('[data-testid=variants]')
      cy.get('[data-testid=testers]')
      cy.get('[data-testid=audit-log]')
      // When an experiment is removed from game config but the data still exists in the database, the experiment detail page will show a message saying the experiment has been removed from the game config.
      // However if this does not happen, the experiment detail page will show the experiment config contents.
      // The test below looks for one or the other to exist.
      cy.get('[data-testid=experiment-removed], [data-testid=config-contents]').should('exist')
    } else {
      this.skip()
    }
  })
})
