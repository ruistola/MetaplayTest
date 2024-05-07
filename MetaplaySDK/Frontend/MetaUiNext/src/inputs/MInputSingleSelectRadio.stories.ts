import MInputSingleSelectRadio from './MInputSingleSelectRadio.vue'
import type { Meta, StoryObj } from '@storybook/vue3'

const meta: Meta<typeof MInputSingleSelectRadio> = {
  // @ts-expect-error Storybook doesn't like generics.
  component: MInputSingleSelectRadio,
  tags: ['autodocs'],
  render: (args) => ({
    components: { MInputSingleSelectRadio },
    setup: () => ({ args }),
    data: () => ({ role: 'admin' }),
    template: `<div>
      <MInputSingleSelectRadio v-bind="args" v-model="role"/>
      <pre class="tw-mt-2">Output: {{ role }}</pre>
    </div>`,
  }),
}

export default meta
type Story = StoryObj<typeof MInputSingleSelectRadio>

export const Default: Story = {
  args: {
    label: 'Role',
    options: [
      { label: 'Admin', value: 'admin' },
      { label: 'User', value: 'user' },
      { label: 'Guest', value: 'guest' },
    ],
    hintMessage: 'This component only supports strings and numbers as return values.',
  },
}

export const NumberValues: Story = {
  render: (args) => ({
    components: { MInputSingleSelectRadio },
    setup: () => ({ args }),
    data: () => ({ number: 1 }),
    template: `<div>
      <MInputSingleSelectRadio v-bind="args" v-model="number"/>
      <pre class="tw-mt-2">Output: {{ number }}</pre>
    </div>`,
  }),
  args: {
    label: 'Role',
    options: [
      { label: 'One', value: 1 },
      { label: 'Two', value: 2 },
      { label: 'Three', value: 3 },
    ],
  },
}

export const Vertical: Story = {
  args: {
    label: 'Role',
    options: [
      { label: 'Admin', value: 'admin' },
      { label: 'User', value: 'user' },
      { label: 'Guest', value: 'guest' },
    ],
    vertical: true,
  },
}

export const Small: Story = {
  args: {
    label: 'Role',
    options: [
      { label: 'Admin', value: 'admin' },
      { label: 'User', value: 'user' },
      { label: 'Guest', value: 'guest' },
    ],
    size: 'small',
  },
}

export const Disabled: Story = {
  args: {
    label: 'Role',
    options: [
      { label: 'Admin', value: 'admin' },
      { label: 'User', value: 'user' },
      { label: 'Guest', value: 'guest' },
    ],
    disabled: true,
  },
}

export const Success: Story = {
  args: {
    label: 'Role',
    options: [
      { label: 'Admin', value: 'admin' },
      { label: 'User', value: 'user' },
      { label: 'Guest', value: 'guest' },
    ],
    variant: 'success',
    hintMessage: 'Success hint message',
  },
}

export const Danger: Story = {
  args: {
    label: 'Role',
    options: [
      { label: 'Admin', value: 'admin' },
      { label: 'User', value: 'user' },
      { label: 'Guest', value: 'guest' },
    ],
    variant: 'danger',
    hintMessage: 'Danger hint message',
  },
}

export const NoLabel: Story = {
  args: {
    options: [
      { label: 'Admin', value: 'admin' },
      { label: 'User', value: 'user' },
      { label: 'Guest', value: 'guest' },
    ],
  },
}

export const Overflow: Story = {
  args: {
    label: 'Do not do this',
    options: [
      { label: 'Some labels could be really long and the surrounding layout should not explode because of this', value: 'admin' },
      { label: 'Notmuchwecandoaboutverylonglabelswithoutspacesastheywillsurelyoverflowanycontainer', value: 'guest' },
      { label: 'What if we had a ton of options 1?', value: '1' },
      { label: 'What if we had a ton of options 2?', value: '2' },
      { label: 'What if we had a ton of options 3?', value: '3' },
      { label: 'What if we had a ton of options 4?', value: '4' },
      { label: 'What if we had a ton of options 5?', value: '5' },
      { label: 'What if we had a ton of options 6?', value: '6' },
      { label: 'What if we had a ton of options 7?', value: '7' },
      { label: 'What if we had a ton of options 8?', value: '8' },
      { label: 'What if we had a ton of options 9?', value: '9' },
      { label: 'What if we had a ton of options 10?', value: '10' },
      { label: 'What if we had a ton of options 11?', value: '11' },
      { label: 'What if we had a ton of options 12?', value: '12' },
      { label: 'What if we had a ton of options 13?', value: '13' },
      { label: 'What if we had a ton of options 14?', value: '14' },
      { label: 'What if we had a ton of options 15?', value: '15' },
    ],
  },
}

export const VerticalOverflow: Story = {
  args: {
    vertical: true,
    label: 'Do not do this',
    options: [
      { label: 'Some labels could be really long and the surrounding layout should not explode because of this', value: 'admin' },
      { label: 'Notmuchwecandoaboutverylonglabelswithoutspacesastheywillsurelyoverflowanycontainer', value: 'guest' },
      { label: 'What if we had a ton of options 1?', value: '1' },
      { label: 'What if we had a ton of options 2?', value: '2' },
      { label: 'What if we had a ton of options 3?', value: '3' },
      { label: 'What if we had a ton of options 4?', value: '4' },
      { label: 'What if we had a ton of options 5?', value: '5' },
      { label: 'What if we had a ton of options 6?', value: '6' },
      { label: 'What if we had a ton of options 7?', value: '7' },
      { label: 'What if we had a ton of options 8?', value: '8' },
      { label: 'What if we had a ton of options 9?', value: '9' },
      { label: 'What if we had a ton of options 10?', value: '10' },
      { label: 'What if we had a ton of options 11?', value: '11' },
      { label: 'What if we had a ton of options 12?', value: '12' },
      { label: 'What if we had a ton of options 13?', value: '13' },
      { label: 'What if we had a ton of options 14?', value: '14' },
      { label: 'What if we had a ton of options 15?', value: '15' },
    ],
  },
}
