import MActionModalButton from '../unstable/MActionModalButton.vue'
import MInputSingleSelectDropdown from './MInputSingleSelectDropdown.vue'
import MListItem from '../primitives/MListItem.vue'
import type { Meta, StoryObj } from '@storybook/vue3'

const meta: Meta<typeof MInputSingleSelectDropdown> = {
  // @ts-expect-error Storybook doesn't like generics.
  component: MInputSingleSelectDropdown,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof MInputSingleSelectDropdown>

export const Default: Story = {
  render: (args) => ({
    components: { MInputSingleSelectDropdown },
    setup: () => ({ args }),
    data: () => ({ role: 'admin' }),
    template: `<div>
      <MInputSingleSelectDropdown v-bind="args" v-model="role"/>
      <pre class="tw-mt-2">Output: {{ role }}</pre>
    </div>`,
  }),
  args: {
    label: 'Role',
    options: [
      { label: 'Admin', value: 'admin' },
      { label: 'User', value: 'user' },
      { label: 'Guest', value: 'guest' },
    ],
    placeholder: 'Choose a role',
    hintMessage: 'This element only supports string values.',
  },
}

export const CustomRendering: Story = {
  render: (args) => ({
    components: { MInputSingleSelectDropdown, MListItem },
    setup: () => ({ args }),
    data: () => ({ role: 'admin' }),
    template: `<div>
      <MInputSingleSelectDropdown v-bind="args" v-model="role">
        <template #selection="{ value }">
          <MListItem class="tw-w-full">
            <span>Selected ID: {{ value }}</span>
            <template #top-right>Lorem ipsum</template>
            <template #bottom-left>Dolor sit amet.</template>
            <template #bottom-right>Consectetur adipiscing elit.</template>
          </MListItem>
        </template>

        <template #option="{ option }">
          <MListItem class="tw-w-full">
            <span>Label: {{ option.label }}</span>
            <template #top-right>ID: {{ option.value }}</template>
            <template #bottom-left>Highlighted: {{ option.highlighted }}</template>
            <template #bottom-right>Selected: {{ option.selected }}</template>
          </MListItem>
        </template>
    </div>`,
  }),
  args: {
    label: 'Custom Rendering Templates (works but this is a bad example)',
    options: [
      { label: 'Admin', value: 'admin' },
      { label: 'User', value: 'user' },
      { label: 'Guest', value: 'guest' },
    ],
    placeholder: 'Choose a role',
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
    modelValue: 'admin',
    placeholder: 'Choose a role',
    disabled: true,
  },
}

export const Placeholder: Story = {
  args: {
    label: 'Role',
    options: [
      { label: 'Admin', value: 'admin' },
      { label: 'User', value: 'user' },
      { label: 'Guest', value: 'guest' },
    ],
    placeholder: 'Choose a role',
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
    modelValue: 'admin',
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
    modelValue: 'admin',
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
    modelValue: 'admin',
  },
}

export const LotsOfOptions: Story = {
  args: {
    label: 'Select a member',
    options: [
      { label: 'Member 1', value: 'member1' },
      { label: 'Member 2', value: 'member2' },
      { label: 'Member 3', value: 'member3' },
      { label: 'Member 4', value: 'member4' },
      { label: 'Member 5', value: 'member5' },
      { label: 'Member 6', value: 'member6' },
      { label: 'Member 7', value: 'member7' },
      { label: 'Member 8', value: 'member8' },
      { label: 'Member 9', value: 'member9' },
      { label: 'Member 10', value: 'member10' },
      { label: 'Member 11', value: 'member11' },
      { label: 'Member 12', value: 'member12' },
      { label: 'Member 13', value: 'member13' },
      { label: 'Member 14', value: 'member14' },
      { label: 'Member 15', value: 'member15' },
      { label: 'Member 16', value: 'member16' },
      { label: 'Member 17', value: 'member17' },
      { label: 'Member 18', value: 'member18' },
      { label: 'Member 19', value: 'member19' },
      { label: 'Member 20', value: 'member20' },
      { label: 'Member 21', value: 'member21' },
      { label: 'Member 22', value: 'member22' },
      { label: 'Member 23', value: 'member23' },
      { label: 'Member 24', value: 'member24' },
      { label: 'Member 25', value: 'member25' },
      { label: 'Member 26', value: 'member26' },
      { label: 'Member 27', value: 'member27' },
      { label: 'Member 28', value: 'member28' },
      { label: 'Member 29', value: 'member29' },
      { label: 'Member 30', value: 'member30' },
      { label: 'Member 31', value: 'member31' },
      { label: 'Member 32', value: 'member32' },
    ],
  },
}

export const OptionLabelOverflow: Story = {
  args: {
    label: 'Select a member',
    options: [
      { label: 'Super long member name that will overflow lorem ipsum dolor sit amet lorem ipsum dolor sit amet lorem ipsum dolor sit amet lorem ipsum dolor sit amet', value: 'member1' },
      { label: 'An even longer member name that will overflow for sure. No idea why you would expect this to look nice. lorem ipsum dolor sit amet lorem ipsum dolor sit amet lorem ipsum dolor sit amet lorem ipsum dolor sit amet lorem ipsum dolor sit amet lorem ipsum dolor sit amet', value: 'member2' },
      { label: 'ANameWithNoSpacesThatWillOverflowSuperBadlyLoremIpsumDolorSitAmetLoremIpsumDolorSitAmetLoremIpsumDolorSitAmetLoremIpsumDolorSitAmetLoremIpsumDolorSitAmetLoremIpsumDolorSitAmet', value: 'member3' },
    ],
  },
}

export const InsideModal: Story = {
  render: (args) => ({
    components: { MInputSingleSelectDropdown, MActionModalButton },
    setup: () => ({ args }),
    template: `<div>
      <MActionModalButton modalTitle="Example Modal" triggerButtonLabel="Open" :action="() => {}">
      <MInputSingleSelectDropdown v-bind="args"/>
      </MActionModalButton>
    </div>`,
  }),
  args: {
    options: [
      { label: 'Admin', value: 'admin' },
      { label: 'User', value: 'user' },
      { label: 'Guest', value: 'guest' },
    ],
    modelValue: 'admin',
  },
}
