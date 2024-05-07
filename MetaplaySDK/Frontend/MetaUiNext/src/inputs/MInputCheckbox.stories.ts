import MInputCheckbox from './MInputCheckbox.vue'
import MInputSwitch from './MInputSwitch.vue'
import { ref } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3'

const meta: Meta<typeof MInputCheckbox> = {
  component: MInputCheckbox,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof MInputCheckbox>

export const Default: Story = {
  render: (args) => ({
    components: { MInputCheckbox },
    setup: () => ({ args }),
    data: () => ({ value: false }),
    template: `<div>
      <MInputCheckbox v-bind="args" v-model="value">I will use this default slot <a target="_blank" class="tw-text-blue-500 hover:tw-underline hover:tw-text-blue-600 active:tw-text-blue-700" href="https://www.youtube.com/watch?v=dQw4w9WgXcQ&ab_channel=RickAstley">responsibly.</a></MInputCheckbox>
      <pre class="tw-mt-2">Output: {{ value }}</pre>
    </div>`,
  }),
  args: {
    label: 'Accept everything',
    hintMessage: 'Checkboxes return booleans.',
  },
}

export const Checked: Story = {
  args: {
    label: 'Accept everything',
    modelValue: true,
    description: 'This checkbox is checked by default.',
  },
}

// TODO: Nicer disabled variant styling.
export const Disabled: Story = {
  args: {
    label: 'Format C:/ ?',
    modelValue: true,
    description: 'This action can not be undone.',
    disabled: true,
  },
}

export const Success: Story = {
  args: {
    label: 'Ok?',
    modelValue: true,
    variant: 'success',
    description: 'This mostly makes sense when checked.',
    hintMessage: 'Success hint message.',
  },
}

// TODO: Nicer disabled variant styling.
export const DisabledSuccess: Story = {
  args: {
    label: 'Ok?',
    modelValue: true,
    variant: 'success',
    description: 'This mostly makes sense when checked.',
    hintMessage: 'Success hint message.',
    disabled: true,
  },
}

export const Danger: Story = {
  args: {
    label: 'Ok?',
    modelValue: false,
    variant: 'danger',
    description: 'This mostly makes sense when not checked.',
    hintMessage: 'Danger hint message.',
  },
}

// TODO: Nicer disabled variant styling.
export const DisabledDanger: Story = {
  args: {
    label: 'Ok?',
    modelValue: true,
    variant: 'danger',
    description: 'This mostly makes sense when not checked.',
    hintMessage: 'Danger hint message.',
    disabled: true,
  },
}

export const LongDescription: Story = {
  args: {
    label: 'Accept everything',
    modelValue: true,
    description: 'This is a very long description that should wrap to multiple lines. It really should be shorter. I mean, who needs this much description for a checkbox?',
  },
}

export const DescriptionOverflow: Story = {
  args: {
    label: 'Accept everything',
    modelValue: true,
    description: 'Thisisastringofwordsthatislongerthanthecheckboxitselfandwilloverflowthecontainer.Whatonearthcouldpossiblybesolongthatitneedsthismanycharacters?Iguesswewillfindout.',
  },
}

export const NoLabelOrDescription: Story = {
  args: {
    modelValue: false,
  },
}

export const EnablingDisabledSingleCheckbox: Story = {
  render: (args) => ({
    components: { MInputSwitch, MInputCheckbox },
    setup: () => {
      const isEnabled = ref(false)
      return { args, isEnabled }
    },
    template: `
      <div>
        <p>Control switch to enable the checkbox below</p>
        <MInputSwitch size="sm" v-model="isEnabled"/>
        <MInputCheckbox :disabled="!isEnabled" label="Single Checkbox"/>
      </div>
    `,
  })
}
