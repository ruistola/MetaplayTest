import MListItem from '../primitives/MListItem.vue'
import MBadge from '../primitives/MBadge.vue'
import MList from '../primitives/MList.vue'
import MCard from '../primitives/MCard.vue'
import MCollapse from '../primitives/MCollapse.vue'
import MTwoColumnLayout from '../layouts/MTwoColumnLayout.vue'
import MViewContainer from '../layouts/root/MViewContainer.vue'
import type { Meta, StoryObj } from '@storybook/vue3'

const meta: Meta<typeof MListItem> = {
  component: MListItem,
  tags: ['autodocs'],
  argTypes: {},
  parameters: {
    docs: {
      description: {
        component: 'The `MListItem` is a flexible component used to display content in a list. The component comes with four distinct slots that can be used to consistenly position your content. You can assign content to all slots or just a few, depending on your needs.',
      },
    },
  },
}

export default meta
type Story = StoryObj<typeof MListItem>

export const Default: Story = {
  render: (args) => ({
    components: {
      MCard,
      MList,
      MListItem,
    },
    setup: () => ({ args }),
    template: `
    <MListItem>
      <p>Primary Content</p>
      <template #top-right>Player:123456789</template>
      <template #bottom-left>Secondary content: Add a short descriptions here.</template>
      <template #bottom-right>Link here?</template>
    </MListItem>
    `,
  }),
}

/**
 * Set the `clickable`prop to true to make the content interactive.
 * This will add a hover effect and cursor pointer to the component.
 */
export const Clickable: Story = {
  render: (args) => ({
    components: {
      MList,
      MListItem,
    },
    setup: () => ({ args }),
    template: `
    <MList>
      <MListItem clickable>
        <p>Example of clickable content</p>
        <template #top-right>Player:123456789</template>
        <template #bottom-left>Usually item descriptions are short.</template>
        <template #bottom-right>Link here?</template>
      </MListItem>
      <MListItem clickable>
        <p>Example of clickable content</p>
        <template #top-right>Player:123456789</template>
        <template #bottom-left>Usually item descriptions are short.</template>
        <template #bottom-right>Link here?</template>
      </MListItem>
    </MList>
    `,
  })
}

/**
 * Use the `striped` prop to add a subtle background color to alternate list items.
 * This will aide the user in visually and improves overall readability.
 */
export const StripedList: Story = {
  render: (args) => ({
    components: {
      MTwoColumnLayout,
      MCard,
      MList,
      MListItem,
    },
    setup: () => ({ args }),
    template: `
    <div class="tw-@container">
      <MTwoColumnLayout>
        <MCard title="Example Card" noBodyPadding>
          <MList v-bind="args">
            <MListItem>
              <p>Example 1 striped content</p>
              <template #top-right>Player:123456789</template>
              <template #bottom-left>Usually item descriptions are short.</template>
              <template #bottom-right>Link here?</template>
            </MListItem>
            <MListItem>
              <p>Example 2 striped content</p>
              <template #top-right>Player:123456789</template>
              <template #bottom-left>Usually item descriptions are short.</template>
              <template #bottom-right>Link here?</template>
            </MListItem>
            <MListItem>
              <p>Example 3 striped content</p>
              <template #top-right>Player:123456789</template>
              <template #bottom-left>Usually item descriptions are short.</template>
              <template #bottom-right>Link here?</template>
            </MListItem>
             <MListItem>
              <p>Example 4 striped content</p>
              <template #top-right>Player:123456789</template>
              <template #bottom-left>Usually item descriptions are short.</template>
              <template #bottom-right>Link here?</template>
            </MListItem>
          </MList>
        </MCard>
      </MTwoColumnLayout>
    </div>
    `,
  }),
  args: {
    striped: true
  }
}

/**
 * The left side slots `#top-left and #bottom-left` are the primary and secondary slots and are ideal for adding
 * the main content of the list item such as titles and description of the item.
 *
 * The right-side slots ´#top-right and #bottom-right`are ideal for supplementary details that enhance the overall context and complement
 * such as id's, timestamps or interactive elements such as links.
 */
export const UsingSlots: Story = {
  render: (args) => ({
    components: {
      MCard,
      MTwoColumnLayout,
      MList,
      MListItem,
      MBadge
    },
    setup: () => ({ args }),
    template: `
    <div class="tw-@container">
      <MTwoColumnLayout>
        <MCard title="Two slot UI Example" noBodyPadding>
          <MList>
            <MListItem>
              <p>Primary text area. Add a title here</p>
              <template #bottom-left>This is the secondary text area. Add a summary description to give the user an overviw of what the list item is about. The two slot system is excellent for List views where you want to </template>
            </MListItem>
            <MListItem>
              <p>Early game funnel</p>
              <template #bottom-left>If we tweak the early game funnel, then we can discover which settings work best to retain players, because players will either prefer a slower or faster early game experience.</template>
            </MListItem>
          </MList>
        </MCard>
        <MCard title="Four slot UI Example" noBodyPadding>
          <MList>
            <MListItem>
              <p>Add a title here</p>
              <template #bottom-left>Add a summary description to give the user an overviw of what the list item is about.</template>
              <template #bottom-right><p>Additional content</p><p>A Link?</p>View experiment</template>
            </MListItem>
            <MListItem>
              <p>Autumn sale</p>
              <template #badge><MBadge>Dynamic</MBadge></template>
              <template #top-right>Revenue:348€</template>
              <template #bottom-left>lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</template>
              <template #bottom-right>Link view sale</template>
            </MListItem>
          </MList>
        </MCard>
      </MTwoColumnLayout>
    </div>
    `,
  }),
}

/**
 * Use the `#badge`slot to visually highlight specific characteristics of the content, such as indicating a new item,
 * a status, or any other relevant information.
 */
export const BadgeSlot: Story = {
  render: (args) => ({
    components: {
      MTwoColumnLayout,
      MCard,
      MList,
      MListItem,
      MBadge
    },
    setup: () => ({ args }),
    template: `
    <MList>
      <MListItem>
        <p>Example of bagde slot</p>
        <template #badge><MBadge>Simple</MBadge></template>
        <template #top-right>Player:123456789</template>
        <template #bottom-left>Usually item descriptions are short.</template>
        <template #bottom-right>Link here?</template>
      </MListItem>
      <MListItem>
        <p>Example of multiple badges in the badge slot</p>
        <template #badge><MBadge>one</MBadge><MBadge>two</MBadge></template>
        <template #top-right>Player:123456789</template>
        <template #bottom-left>Usually item descriptions are short.</template>
        <template #bottom-right>Link here?</template>
      </MListItem>
    </MList>
    `,
  }),
}

/**
 * The badge slot is designed for short and consise content, and we therefore recommend keeping it short for optimal display purposes.
 * If the content exceeds the allotted space, it may overflow and affect the visual coherence of the component.
 */
export const BadgeSlotOverflow: Story = {
  render: (args) => ({
    components: {
      MViewContainer,
      MTwoColumnLayout,
      MCard,
      MList,
      MListItem,
      MBadge
    },
    setup: () => ({ args }),
    template: `
    <div class="tw-@container">
      <MTwoColumnLayout>
        <MCard title="Badge overflow Examples" noBodyPadding>
          <MList>
            <MListItem>
              <p>Example of badge slot wrapping</p>
              <template #badge><MBadge>This is meant for short and consise text.</MBadge></template>
              <template #top-right>Player:123456789</template>
              <template #bottom-left>Usually item descriptions are short.</template>
              <template #bottom-right>Link here?</template>
            </MListItem>
            <MListItem>
              <p>Bad example of badge slot</p>
              <template #badge><MBadge>Thisisalongbadgewithnowhitespace</MBadge></template>
              <template #top-right>Player:123456789</template>
              <template #bottom-left>Usually item descriptions are short.</template>
              <template #bottom-right>Link here?</template>
            </MListItem>
            <MListItem>
              <p>Example of multiple badges in the badge slot</p>
              <template #badge><MBadge>one</MBadge><MBadge>two</MBadge><MBadge>three</MBadge></template>
              <template #top-right>Player:123456789</template>
              <template #bottom-left>Usually item descriptions are short.</template>
              <template #bottom-right>Link here?</template>
            </MListItem>
          </MList>
        </MCard>
      </MTwoColumnLayout>
    </div>
    `,
  }),
}

/**
 * The left-side slots are flexible and purposefully designed to gracefully manage lengthy and detailed text.
 * Content will wrap and overflow to the next line seamlessly or become scrollable so as to minimize the impact on the right slot content.
 *
 * Ideally the right-side slots are reserved for brief and consise content.
 * While the content will wrap and overflow to the next line if it's too long,
 * this is not the intended utilization of the right slots.
 */
export const LeftAndRightSlotContentOverflow: Story = {
  render: (args) => ({
    components: {
      MCard,
      MTwoColumnLayout,
      MList,
      MListItem,
      MBadge
    },
    setup: () => ({ args }),
    template: `
    <div class="tw-@container">
      <MTwoColumnLayout>
        <MCard title="Left slot overflow example" noBodyPadding>
        <MList>
          <MListItem>
            <p>This is the first story with a very long title which should wrap to the next line without affecting the content on the right column</p>
            <template #badge><MBadge>Testing</MBadge> </template>
            <template #top-right>since 6 days ago</template>
            <template #bottom-left>If we tweak the early game funnel, then we can discover which settings work best to retain players, because players will either prefer a slower or faster early game experience.</template>
            <template #bottom-right><p>Total runtime: 12 days</p><p>54 participants</p>View experiment</template>
          </MListItem>
          <MListItem>
            <p>ThisMListitemhasaverylongtitlewithnowhitespaceanditshouldwrapandnotoverflowloremipsumdolorsitamet</p>
            <template #badge><MBadge>Testing</MBadge> </template>
            <template #top-right>since 6 days ago</template>
            <template #bottom-left>If we tweak the early game funnel, then we can discover which settings work best to retain players, because players will either prefer a slower or faster early game experience.</template>
            <template #bottom-right><p>Total runtime: 12 days</p><p>54 participants</p>View experiment</template>
          </MListItem>
          <MListItem>
              <p>Scrollable content example</p>
              <template #badge><MBadge>Testing</MBadge></template>
              <template #top-right>since 6 days ago</template>
              <template #bottom-left>If the description includes long text with no whitespace like a token then the content will automatically become scrollable. Theverylastsentencecontainsnowhitespaceanditshouldnotoverflowthecomponent.</template>
              <template #bottom-right><p>Total runtime: 12 days</p><p>54 participants</p>View experiment</template>
            </MListItem>
        </MList>
      </MCard>
    </MTwoColumnLayout>

    <MTwoColumnLayout>
      <MCard title="Right slot overflow example" noBodyPadding>
          <MList>
            <MListItem>
              <p>Top right slot overflow</p>
              <template #top-right>Ideally the text on the right should never be this long.</template>
              <template #bottom-left>Some text also on the left.</template>
              <template #bottom-right>Link?</template>
            </MListItem>
            <MListItem>
              <p>Top right slot overflow</p>
              <template #top-right>ThisIsASampleLongTokenWithNoWhiteSpaceIdeallyContentInThisSlotShouldNotBeThisLong</template>
              <template #bottom-left>Some text also on the left.</template>
              <template #bottom-right>Link?</template>
            </MListItem>
            <MListItem>
              <p>List token</p>
               <template #top-right>since 6 days ago</template>
              <template #bottom-right>ThisIsASampleLongTokenWithNoWhiteSpaceIdeallyContentInThisSlotShouldNotBeThisLong</template>
              <template #bottom-left>Some text also on the left.</template>
            </MListItem>
            <MListItem>
              <p>List token</p>
               <template #top-right>since 6 days ago</template>
              <template #bottom-right>Ideally content in this slot should never be this long, but it will wrap to the next line</template>
              <template #bottom-left>Some text also on the left.</template>
            </MListItem>
          </MList>
        </MCard>
      </MTwoColumnLayout>
    </div>
    `,
  }),
}

/**
 * The `MListItem` component is designed to maintain a consistent visual coherence even when displaying multiple list items with varying content length.
 * However in some cases it may be necessary to move the content around to ensure optimal display. In the `Not so bad example` below you observe the impact of varying content length
 * on the overall visual apprearance of the list. While this is not so bad one potential solution is to relocate the `#badge` content to the `#top-right` slot (see `Good Example`).
 */
export const OverflowOfMultipleListItems: Story = {
  render: (args) => ({
    components: {
      MTwoColumnLayout,
      MCard,
      MList,
      MListItem,
      MBadge,
    },
    setup: () => ({ args }),
    template: `
    <div class="tw-@container">
      <MTwoColumnLayout>
        <MCard title="Not so bad example" noBodyPadding>
          <MList>
            <MListItem>
              <p>Normal length title</p>
              <template #badge><MBadge>Testing</MBadge></template>
              <template #top-right>since 6 days ago</template>
              <template #bottom-left>If we tweak the early game funnel, then we can discover which settings work best to retain players.</template>
              <template #bottom-right><p>Total runtime: 12 days</p><p>54 participants</p>View experiment</template>
            </MListItem>
            <MListItem>
              <p>Second title is slightly longer than the first one</p>
              <template #badge><MBadge>Testing</MBadge></template>
              <template #top-right>since 6 days ago</template>
              <template #bottom-left>If we tweak the early game funnel, then we can discover which settings work best to retain players</template>
              <template #bottom-right><p>Total runtime: 12 days</p><p>54 participants</p>View experiment</template>
            </MListItem>
            <MListItem>
              <p>The third list item title is even longer than the first two and should also wrap in a consistent manner</p>
              <template #badge><MBadge>Testing</MBadge></template>
              <template #top-right>since 6 days ago</template>
              <template #bottom-left>If we tweak the early game funnel, then we can discover which settings work best to retain players, because players will either prefer a slower or faster early game experience.</template>
              <template #bottom-right><p>Total runtime: 12 days</p><p>54 participants</p>View experiment</template>
            </MListItem>
            <MListItem>
              <p>The fourth list item has a long title is even longer description than the first three and should also wrap in a consistent manner</p>
              <template #badge><MBadge>Testing</MBadge></template>
              <template #top-right>since 6 days ago</template>
              <template #bottom-left>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</template>
              <template #bottom-right><p>Total runtime: 12 days</p><p>54 participants</p>View experiment</template>
            </MListItem>
          </MList>
        </MCard>
      </MTwoColumnLayout>

      <MTwoColumnLayout>
        <MCard title="Good example" noBodyPadding>
          <MList>
            <MListItem>
              <p>Normal length title</p>
              <template #top-right><MBadge>Testing</MBadge></template>
              <template #top-right>since 6 days ago</template>
              <template #bottom-left>If we tweak the early game funnel, then we can discover which settings work best to retain players.</template>
              <template #bottom-right><p>Total runtime: 12 days</p><p>54 participants</p>View experiment</template>
            </MListItem>
            <MListItem>
              <p>Slightly longer title than ther first one</p>
              <template #top-right><MBadge>Testing</MBadge></template>
              <template #top-right>since 6 days ago</template>
              <template #bottom-left>If we tweak the early game funnel, then we can discover which settings work best to retain players</template>
              <template #bottom-right><p>Total runtime: 12 days</p><p>54 participants</p>View experiment</template>
            </MListItem>
            <MListItem>
              <p>The third list item title is even longer than the first two and should also wrap in a consistent manner</p>
              <template #top-right><MBadge>Testing</MBadge></template>
              <template #top-right>since 6 days ago</template>
              <template #bottom-left>If we tweak the early game funnel, then we can discover which settings work best to retain players, because players will either prefer a slower or faster early game experience.</template>
              <template #bottom-right><p>Total runtime: 12 days</p><p>54 participants</p>View experiment</template>
            </MListItem>
            <MListItem>
              <p>The fourth list item has a long title is even longer description than the first three and should also wrap in a consistent manner</p>
              <template #top-right><MBadge>Testing</MBadge></template>
              <template #top-right>since 6 days ago</template>
              <template #bottom-left>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</template>
              <template #bottom-right><p>Total runtime: 12 days</p><p>54 participants</p>View experiment</template>
            </MListItem>
          </MList>
        </MCard>
      </MTwoColumnLayout>
    </div>
    `,
  }),
}

export const CollapsibleList: Story = {
  render: (args) => ({
    components: {
      MTwoColumnLayout,
      MCard,
      MCollapse,
      MList,
      MListItem,
    },
    setup: () => ({ args }),
    template: `
    <div class="tw-@container">
      <MTwoColumnLayout>
        <MCard title="Example Collapsible List" noBodyPadding>
          <MCollapse extraMListItemMargin>
            <template #header>
              <MListItem noLeftPadding>
                Item 1
                <template #top-right>Something small</template>
                <template #bottom-left>Lorem ipsum dolor sit amet.</template>
                <template #bottom-right>Link here?</template>
              </MListItem>
            </template>
            <Pre>Some content here</Pre>
          </MCollapse>

          <MCollapse extraMListItemMargin>
            <template #header>
              <MListItem noLeftPadding>
                Item 2
                <template #top-right>Something small</template>
                <template #bottom-left>Lorem ipsum dolor sit amet.</template>
                <template #bottom-right>Link here?</template>
              </MListItem>
            </template>
            <Pre>Some content here</Pre>
        </MCollapse>
      </MTwoColumnLayout>
    </div>
    `,
  }),
}
