import MInputText from './MInputText.vue'
import type { Meta, StoryObj } from '@storybook/vue3'

const meta: Meta<typeof MInputText> = {
  component: MInputText,
  tags: ['autodocs'],
  render: (args) => ({
    components: { MInputText },
    setup: () => ({ args }),
    data: () => ({ content: args.modelValue }),
    template: `<div>
      <MInputText v-bind="args" v-model="content"/>
      <pre class="tw-mt-2">Output: {{ content }}</pre>
    </div>`,
  }),
}

export default meta
type Story = StoryObj<typeof MInputText>

export const Default: Story = {
  args: {
    label: 'Name',
    modelValue: 'Matti Meikäläinen 🥸',
    placeholder: 'Enter your name',
    hintMessage: 'Undefined is also a valid value for text fields.',
  },
}

export const Debounced: Story = {
  args: {
    label: 'Expensive Validation',
    modelValue: 'Matti Meikäläinen?',
    placeholder: 'Enter your name',
    debounce: 1000,
    hintMessage: 'This field has a 1 second debounce.',
  },
}

export const Placeholder: Story = {
  args: {
    label: 'City',
    placeholder: 'For example: Helsinki',
  },
}

export const Email: Story = {
  args: {
    label: 'Email',
    placeholder: 'doesthiswork@metaplay.io',
    type: 'email'
  },
}

export const Disabled: Story = {
  args: {
    label: 'Would you like a raise?',
    modelValue: 'Nah, I\'m good, thanks.',
    disabled: true,
  },
}

export const Loading: Story = {
  args: {
    label: 'Player Name',
    modelValue: '⭐️L3G0L4S_1337⭐️',
    variant: 'loading',
  },
}

export const Danger: Story = {
  args: {
    label: 'This is fine',
    modelValue: 'Commit and run',
    variant: 'danger',
    hintMessage: 'Hints turn red when the variant is danger.',
  },
}

export const Success: Story = {
  args: {
    label: 'This is fine',
    modelValue: 'Lint, test and build',
    variant: 'success',
    hintMessage: 'Hints are still neutral when the variant is success.',
  },
}

export const NoLabel: Story = {
  args: {},
}
