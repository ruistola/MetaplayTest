import MButton from './MButton.vue'
import type { Meta, StoryObj } from '@storybook/vue3'
import { usePermissions } from '../composables/usePermissions'

const meta = {
  component: MButton,
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: {
        type: 'select',
      },
      options: ['neutral', 'success', 'danger', 'warning', 'primary'],
    },
    size: {
      control: {
        type: 'radio',
      },
      options: ['text', 'smallIconOnly', 'small', 'default']
    },
    disabled: {
      control: {
        type: 'radio'
      },
      options: [true, false]
    }
  },
  parameters: {
    docs: {
      description: {
        component: 'MButton is a control element used to trigger an action or event. The component can be used as a HTML `<button>` or `<a>` element for example to open a dialog, start, pause or cancel an action, submit a form or to navigate to a new page or view.',
      },
    },
  },
  render: (args) => ({
    components: { MButton },
    setup: () => ({ args }),
    template: `
    <MButton v-bind="args">
      I am a button
    </MButton>
    `,
  }),
} satisfies Meta<typeof MButton>

export default meta
type Story = StoryObj<typeof MButton>

export const Default: Story = {
  args: {},
}

/**
 * You can use the `MButton` as a link by setting the `to` prop to navigate to both internal and external links.
 * Despite its visual apprerance as a button, it internally functions as a link tag.
 */
export const LinkStyledAsAButton: Story = {
  render: (args) => ({
    components: { MButton },
    setup: () => ({ args }),
    template: `
    <div class="tw-flex tw-flex-col tw-gap-10">
      <div class="tw-flex tw-gap-2">
        <MButton v-bind="args" variant="primary"> Link button </MButton>
      </div>
    </div>
    `,
  }),
  args: {
    to: 'https://docs.metaplay.io/',
  },
}

/**
 * `MButton` includes a default slot where you can add both text and image content such as an icon to the button label.
 * Icons provide good visual cues and can enhance overall usability of your button.
 */
export const ButtonsWithAnIcon: Story = {
  render: (args) => ({
    components: { MButton },
    setup: () => ({ args }),
    template: `
    <div class="tw-flex tw-gap-2">
      <MButton variant="primary">
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="tw-w-4 tw-h-4">
            <path d="M10 2a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 2zM10 15a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 15zM10 7a3 3 0 100 6 3 3 0 000-6zM15.657 5.404a.75.75 0 10-1.06-1.06l-1.061 1.06a.75.75 0 001.06 1.06l1.06-1.06zM6.464 14.596a.75.75 0 10-1.06-1.06l-1.06 1.06a.75.75 0 001.06 1.06l1.06-1.06zM18 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 0118 10zM5 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 015 10zM14.596 15.657a.75.75 0 001.06-1.06l-1.06-1.061a.75.75 0 10-1.06 1.06l1.06 1.06zM5.404 6.464a.75.75 0 001.06-1.06l-1.06-1.06a.75.75 0 10-1.061 1.06l1.06 1.06z" />
          </svg>
        </template>
        Icon Button
      </MButton>

      <MButton v-bind="args" variant="success">
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="tw-w-4 tw-h-4">
            <path d="M10 2a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 2zM10 15a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 15zM10 7a3 3 0 100 6 3 3 0 000-6zM15.657 5.404a.75.75 0 10-1.06-1.06l-1.061 1.06a.75.75 0 001.06 1.06l1.06-1.06zM6.464 14.596a.75.75 0 10-1.06-1.06l-1.06 1.06a.75.75 0 001.06 1.06l1.06-1.06zM18 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 0118 10zM5 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 015 10zM14.596 15.657a.75.75 0 001.06-1.06l-1.06-1.061a.75.75 0 10-1.06 1.06l1.06 1.06zM5.404 6.464a.75.75 0 001.06-1.06l-1.06-1.06a.75.75 0 10-1.061 1.06l1.06 1.06z" />
          </svg>
        </template>
        Icon Button
      </MButton>

      <MButton v-bind="args" variant="danger">
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="tw-w-4 tw-h-4">
            <path d="M10 2a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 2zM10 15a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 15zM10 7a3 3 0 100 6 3 3 0 000-6zM15.657 5.404a.75.75 0 10-1.06-1.06l-1.061 1.06a.75.75 0 001.06 1.06l1.06-1.06zM6.464 14.596a.75.75 0 10-1.06-1.06l-1.06 1.06a.75.75 0 001.06 1.06l1.06-1.06zM18 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 0118 10zM5 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 015 10zM14.596 15.657a.75.75 0 001.06-1.06l-1.06-1.061a.75.75 0 10-1.06 1.06l1.06 1.06zM5.404 6.464a.75.75 0 001.06-1.06l-1.06-1.06a.75.75
            0 10-1.061 1.06l1.06 1.06z" />
          </svg>
        </template>
        Icon Button
      </MButton>

      <MButton v-bind="args" variant="warning">
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="tw-w-4 tw-h-4">
            <path d="M10 2a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 2zM10 15a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 15zM10 7a3 3 0 100 6 3 3 0 000-6zM15.657 5.404a.75.75 0 10-1.06-1.06l-1.061 1.06a.75.75 0 001.06 1.06l1.06-1.06zM6.464 14.596a.75.75 0 10-1.06-1.06l-1.06 1.06a.75.75 0 001.06 1.06l1.06-1.06zM18 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 0118 10zM5 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 015 10zM14.596 15.657a.75.75 0 001.06-1.06l-1.06-1.061a.75.75 0 10-1.06 1.06l1.06 1.06zM5.404 6.464a.75.75 0 001.06-1.06l-1.06-1.06a.75.75
            0 10-1.061 1.06l1.06 1.06z" />
          </svg>
        </template>
        Icon Button
      </MButton>

      <MButton v-bind="args" variant="neutral">
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="tw-w-4 tw-h-4">
            <path d="M10 2a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 2zM10 15a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 15zM10 7a3 3 0 100 6 3 3 0 000-6zM15.657 5.404a.75.75 0 10-1.06-1.06l-1.061 1.06a.75.75 0 001.06 1.06l1.06-1.06zM6.464 14.596a.75.75 0 10-1.06-1.06l-1.06 1.06a.75.75 0 001.06 1.06l1.06-1.06zM18 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 0118 10zM5 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 015 10zM14.596 15.657a.75.75 0 001.06-1.06l-1.06-1.061a.75.75 0 10-1.06 1.06l1.06 1.06zM5.404 6.464a.75.75 0 001.06-1.06l-1.06-1.06a.75.75
            0 10-1.061 1.06l1.06 1.06z" />
          </svg>
        </template>
        Icon Button
      </MButton>
    </div>`,
  }),
}

/**
 * The `MButton` provides five main contextual variants that can be used to visually convey the severity of an action.
 * By default the `MButton` renders in the `primary` variant but you can easily customize this by assigning a different
 * variant to the `variant` prop. For example use the `danger` variant for buttons that enable destructive actions.
 */
export const ButtonVariants: Story = {
  render: (args) => ({
    components: { MButton },
    setup: () => ({ args }),
    template: `
    <div class="tw-flex tw-gap-2">
      <MButton v-bind="args" variant="primary"> Primary button </MButton>
      <MButton v-bind="args" variant="success"> Success button </MButton>
      <MButton v-bind="args" variant="danger"> Danger button </MButton>
      <MButton v-bind="args" variant="warning"> Warning button </MButton>
      <MButton v-bind="args" variant="neutral"> Neutral button </MButton>
    </div>
    `,
  }),
}

/**
 * Set the `size` prop to change the padding inside of the button. The smallest button is the `smallIconOnly`.
 */
export const ButtonSizes: Story = {
  render: (args) => ({
    components: { MButton },
    setup: () => ({ args }),
    template: `
    <div class="tw-flex tw-gap-4">
      <MButton size="small"> Small Button </MButton>
      <MButton> Default Button</MButton>
    </div>
    `,
  }),
}

/**
 * Set the `disabled` attribute to `true` to prevent button from triggering it's underlying action.
 * It is important to note that this does not hide the button bur rather makes it appears greyed out and is not clickable.
 * This feature is useful for preventing users from triggering an action until a specified condition has been met.
 */
export const DisabledButton: Story = {
  render: (args) => ({
    components: { MButton },
    setup: () => ({ args }),
    template: `
    <div class="tw-flex tw-gap-2">
      <MButton v-bind="args" variant="primary"> Disabled button </MButton>
      <MButton v-bind="args" variant="success"> Disabled button </MButton>
      <MButton v-bind="args" variant="danger"> Disabled button </MButton>
      <MButton v-bind="args" variant="warning"> Disabled button </MButton>
      <MButton v-bind="args" variant="neutral"> Disabled button </MButton>
    </div>
    `,
  }),
  args: {
    disabled: true,
  },
}

/**
 * Best practice guidelines reccomend providing users with an explaination when a particular action/feature is disabled.
 * Utilise the `disabledTooltip` props to include a helpful hint explaining why the button is disabled when users hover over it.
 */
export const DisabledWithATooltip: Story = {
  render: (args) => ({
    components: { MButton },
    setup: () => ({ args }),
    template: `
    <div class="tw-flex tw-gap-2">
      <MButton v-bind="args" variant="primary"> Disabled button </MButton>
      <MButton v-bind="args" variant="success"> Disabled button </MButton>
      <MButton v-bind="args" variant="danger"> Disabled button </MButton>
      <MButton v-bind="args" variant="warning"> Disabled button </MButton>
      <MButton v-bind="args" variant="neutral"> Disabled button </MButton>
    </div>
    `,
  }),
  args: {
    disabled: true,
    disabledTooltip: 'This is why it is disabled.',
  },
}

/**
 * When creating features and/or actions it is important to consider the neccessary permissions for access.
 * The `MButton` component includes a built-in `HasPermission` prop that when set, ensures the features and/or
 * actions are only available to users with the required permission.
 */
export const HasPermission: Story = {
  render: (args) => ({
    components: { MButton },
    setup: () => {
      usePermissions().setPermissions(['example-permission'])
      return {
        args
      }
    },
    template: `
    <div class="tw-flex tw-gap-2">
      <MButton v-bind="args">
        This Should Work
      </MButton>
      <MButton v-bind="args" variant="success">
        This Should Work
      </MButton>
      <MButton v-bind="args" variant="danger">
        This Should Work
      </MButton>
      <MButton v-bind="args" variant="warning">
        This Should Work
      </MButton>
      <MButton v-bind="args" variant="neutral">
        This Should Work
      </MButton>
    </div>
    `,
  }),
  args: {
    permission: 'example-permission',
  },
}

/**
 * If a user lacks the necessary permission, the `MButton` component is automatically disabled and a tooltip,
 * explaining which permission is missing, is displayed when a user hovers their mouse cursor on the button.
 */
export const NoPermission: Story = {
  render: (args) => ({
    components: { MButton },
    setup: () => {
      usePermissions().setPermissions(['example-permission'])

      return {
        args
      }
    },
    template: `
    <div class="tw-flex tw-gap-2">
      <MButton v-bind="args">
        This Should Not Work
      </MButton>
      <MButton v-bind="args" variant="success">
        This Should Not Work
      </MButton>
      <MButton v-bind="args" variant="danger">
        This Should Not Work
      </MButton>
      <MButton v-bind="args" variant="warning">
        This Should Not Work
      </MButton>
      <MButton v-bind="args" variant="neutral">
        This Should Not Work
      </MButton>
    </div>
    `,
  }),
  args: {
    permission: 'example-permission2',
  },
}

/**
 * The `MButton` component includes a `safetyLock` feature. When set, it prevents accidental button triggers.
 * This feature adds an extra layer of security, requiring users to 'unlock' an action before it can be triggered.
 * By default, the `safetyLock` feature is disabled in local development environments, but we recommend enabling it in production.
 */
export const SafetyLock: Story = {
  render: (args) => ({
    components: { MButton },
    setup: () => ({ args }),
    template: `
    <div class="tw-flex tw-gap-4">
      <MButton v-bind="args"> Button With Safety Lock </MButton>
      <MButton v-bind="args" size="small"> Small Button With Safety Lock </MButton>
    </div>
    `,
  }),
  args: {
    safetyLock: true
  }
}

// TODO: Harmonize colors.
export const SafetyLockVariants: Story = {
  render: (args) => ({
    components: { MButton },
    setup: () => ({ args }),
    template: `
    <div class="tw-flex tw-gap-4">
      <MButton v-bind="args" variant="primary"> Button With Safety Lock </MButton>
      <MButton v-bind="args" variant="success"> Button With Safety Lock </MButton>
      <MButton v-bind="args" variant="warning"> Button With Safety Lock </MButton>
      <MButton v-bind="args" variant="danger"> Button With Safety Lock </MButton>
      <MButton v-bind="args" variant="neutral"> Button With Safety Lock </MButton>
    </div>
    `,
  }),
  args: {
    safetyLock: true,
  }
}

// TODO: Harmonize colors
export const SafetyLockWithADisabledOKButton: Story = {
  render: (args) => ({
    components: { MButton },
    setup: () => ({ args }),
    template: `
    <div class="tw-flex tw-gap-4">
      <MButton v-bind="args"> Button With Safety Lock </MButton>
      <MButton v-bind="args" size="small"> Small Button With Safety Lock </MButton>
    </div>
    `,
  }),
  args: {
    safetyLock: true,
    disabled: true
  }
}
