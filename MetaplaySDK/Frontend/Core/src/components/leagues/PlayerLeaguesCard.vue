<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-list-card(
  title="Player Leagues"
  icon="trophy"
  :itemList="playerLeagueItems"
  :searchFields="searchFields"
  :filterSets="filterSets"
  :sortOptions="sortOptions"
  :emptyMessage="`${playerData.model.playerName} isn't participating in any player leagues.`"
  permission="api.leagues.view"
  data-testid="player-leagues-card"
  )
  template(#item-card="{ item }")
    MCollapse(extraMListItemMargin)
      template(#header)
        MListItem(noLeftPadding) {{ item.leagueName }}
          template(#badge)
            MBadge(v-if="item.participantStatus === 'Participant'" variant="success") Participating

          template(#top-right)
            span(class="tw-mr-1") Phase:
            span(v-if="item.enabled")
              MBadge(:variant="getPhaseVariant(item.currentSeasonPhase)") {{ schedulePhaseDisplayString(item.currentSeasonPhase) }}
            span(v-else) #[MBadge Disabled]

          template(#bottom-left)
            span(v-if="item.participantStatus === 'NeverParticipated'") This player has never participated in {{ item.leagueName }}.
            //- TODO: Add some player statistics to the endpoint e.g previous rank, division and season Id/name.
            span(v-else-if="item.participantStatus === 'NotParticipant'")  This player has previously participated in the {{ item.leagueName }} but is not part of the ongoing season.
            span(v-else-if="item.participantStatus === 'Participant' && !item.enabled") This player participated in the last active season of the {{ item.leagueName }} and was ranked #[meta-ordinal-number(:number="item.placeInDivision")] place in #[span(class="tw-font-semibold") Division {{ item.divisionIndex }}] of #[strong {{item.rankName}}] in #[MTextButton(:to="`/leagues/${item.leagueId}/${item.season}`") season {{ item.season }} ].
            span(v-else) This player is currently part of {{ item.leagueName }} and is ranked #[span(class="tw-font-semibold") #[meta-ordinal-number(:number="item.placeInDivision")]] place in #[span(class="tw-font-semibold") Division {{ item.divisionIndex }}] of #[span(class="tw-font-semibold") {{item.rankName}}] in #[MTextButton(:to="`/leagues/${item.leagueId}/${item.season}`") Season {{ item.season }} ].

          template(#bottom-right)
            MTextButton(v-if="item.participantStatus !== 'Participant'" :to="`/leagues/${item.leagueId}`") View league
            MTextButton(v-else-if="item.participantStatus === 'Participant'" :to="`/leagues/${item.leagueId}/${item.season}/${item.divisionId}`") View division

      div.border.rounded-sm.bg-light.px-3.py-2
        div.font-weight-bold.mb-1 #[fa-icon(icon="wrench")] Admin Controls
        div.small.text-muted.my-2(v-if="!item.enabled") This league is currently disabled and there are no active seasons. You cannot add or remove a player from a disabled league.
        div.small.text-muted.my-2(v-else) You can add or remove this player from the league or change their rank and/or division.

        //- Action modals
        div(class="tw-inline-flex tw-justify-end tw-w-full tw-mt-3 tw-space-x-2")
          //- Add Player to league or modify player's rank/division.
          MActionModalButton(
            :modal-title="item.participantStatus !== 'Participant'  ? 'Add to League' : `Modify Rank or Division`"
            :action="() => addOrMovePlayerToLeague(item.leagueIndex, selectedRank?.index, selectedDivision)"
            :trigger-button-label="item.participantStatus !== 'Participant' ? 'Add to League' : 'Modify Rank or Division'"
            :trigger-button-disabled="addOrMoveParticipantDisabled(item)"
            variant="warning"
            :ok-button-disabled="!selectedRank || isAlreadyInDivision(item.leagueIndex, selectedRank?.index, selectedDivision) || item.currentSeasonPhase === 'Inactive' || !doesLeagueHaveActiveSeason(item.leagueIndex)"
            @show="resetModal(item.leagueIndex)"
            permission="api.leagues.participant_edit"
            data-testid="action-add-participant"
            )
            p(v-if="item.participantStatus === 'Participant'") This player is already part of #[span.font-weight-bold {{ item.leagueName }}] and is ranked #[span.font-weight-bold #[meta-ordinal-number(:number="item.placeInDivision")]] place in #[span.font-weight-bold Division {{ item.divisionIndex }}] of #[span.font-weight-bold {{item.rankName}}]. You can move this player to another rank or division in this league.
            p(v-else) You are about to add this player to {{ item.leagueName }}.

            p If the player is currently playing the game, they will be immediately disconnected so that the changes can be applied.

            div(class="tw-font-semi-bold tw-mb-1") New Rank
            meta-input-select(
              :value="selectedRank"
              @input="selectedRank = $event; selectedDivision = undefined"
              :options="rankOptions"
              placeholder="Select a rank ..."
              :searchFields="['rankName']"
              no-clear
              )
              template(#option="{ option }")
                MListItem(class="!tw-py-0 !tw-px-0") {{ option?.rankName }}
                  template(#top-right) {{ option?.description }}
              template(#selectedOption="{ option }")
                div {{ option?.rankName }}

            //- TODO: This element currently shows "Assigned randomly" and an error when there is only one division and when the player already belongs to that division. It should pre-select the only division instead for the error message to make sense.
            MInputNumber(
              class="tw-mt-1"
              label="New Division"
              :model-value="selectedDivision"
              @update:model-value="selectedDivision = $event"
              :disabled="!selectedRank || item.divisionsPerRank[selectedRank?.index || 0] === 0"
              :min="0"
              :max="item.divisionsPerRank[selectedRank?.index || 0] - 1"
              :placeholder="item.divisionsPerRank[selectedRank?.index || 0] > 0 ? 'Assigned randomly' : 'There are no divisions in this rank. A new division will be created.'"
              allowUndefined
              :variant="isAlreadyInDivision(item.leagueIndex, selectedRank?.index, selectedDivision) ? 'danger' : 'default'"
              :hint-message="isAlreadyInDivision(item.leagueIndex, selectedRank?.index, selectedDivision) ? 'Player is already in this division. You cannot move a player to a division that they are already in.' : undefined"
              )

            b-alert(
              class="tw-mt-2 tw-text-center"
              v-if="item.currentSeasonPhase === 'Inactive'"
              show variant="danger"
              ) The season has now concluded. You can no longer add participants to this division.
            meta-no-seatbelts(
              v-else-if="item.participantStatus === 'Participant'"
              message="This action cannot be undone. The player will lose all their progress in the league and will have to start again from scratch."
              class="tw-mt-3"
              )

          //- Remove Player
          MActionModalButton(
            modal-title="Remove Player from League"
            :action="() => removeParticipant(item.leagueId, playerId)"
            trigger-button-label="Remove Player"
            :trigger-button-disabled="removeParticipantDisabled(item)"
            variant="warning"
            permission="api.leagues.participant_edit"
            data-testid="action-remove-participant"
            )
            p You are about to remove this player from {{ item.leagueName }}.
            meta-no-seatbelts(
              message="This action can't be undone. The participant will lose all their progress in the league."
              :name="playerData.model.playerName"
              class="tw-mt-3"
              )

</template>

<script lang="ts" setup>
import { computed, nextTick, onUnmounted, ref, watch } from 'vue'

import { schedulePhaseDisplayString } from '../../coreUtils'
import { useGameServerApi } from '@metaplay/game-server-api'
import { MetaListFilterOption, MetaListFilterSet, MetaListSortDirection, MetaListSortOption, showErrorToast, showSuccessToast } from '@metaplay/meta-ui'
import type { MetaInputSelectOption } from '@metaplay/meta-ui'
import { MActionModalButton, MBadge, MCollapse, MInputNumber, MListItem, MTextButton } from '@metaplay/meta-ui-next'
import { useSubscription, useManuallyManagedStaticSubscription } from '@metaplay/subscriptions'

import { getPhaseVariant } from '../../leagueUtils'
import { getAllLeaguesForSingleParticipant, getSingleLeagueSubscriptionOptions, getSingleLeagueSeasonSubscriptionOptions } from '../../subscription_options/leagues'
import { getSinglePlayerSubscriptionOptions } from '../../subscription_options/players'

const props = defineProps<{
  /**
   * Id of the player whose leagues are shown on the list.
   */
  playerId: string
}>()

const gameServerApi = useGameServerApi()

/**
 * Subscribe to the target player's data.
 */
const {
  data: playerData
} = useSubscription(getSinglePlayerSubscriptionOptions(props.playerId))

/**
 * Subscribe to the target player's league data.
 */
const {
  data: playerLeagueData,
  refresh: playerLeagueDataRefresh
} = useSubscription(getAllLeaguesForSingleParticipant(props.playerId))

/**
 * Subscribe to data for all leagues (and their active seasons) that the player knows about. The number of leagues is
 * unknown ahead of time, so we need to manually subscribe arrays of manual subscriptions. We also need to manually
 * unsubscribe when the page is unmounted.
 */
const singleLeagueSubscriptions = ref<any[]>([])
const singleLeagueSeasonSubscriptions = ref<Array<any | undefined>>([])

const unwatchFetchAllLeagueData = watch(() => playerLeagueData.value, () => {
  // We are looking for the first time that this data is present.
  if (playerLeagueData.value) {
    let leagueIndex = 0
    playerLeagueData.value.forEach((leagueData: any) => {
      // Subscribe to league data.
      singleLeagueSubscriptions.value.push(useManuallyManagedStaticSubscription(getSingleLeagueSubscriptionOptions(leagueData.leagueId)))
      // Subscribe to league season data, if there is an active season.
      if (doesLeagueHaveActiveSeason(leagueIndex)) {
        singleLeagueSeasonSubscriptions.value.push(useManuallyManagedStaticSubscription(getSingleLeagueSeasonSubscriptionOptions(leagueData.leagueId, '$active')))
      } else {
        singleLeagueSeasonSubscriptions.value.push(undefined)
      }
      leagueIndex++
    })

    void nextTick(() => {
      // Now we can stop watching - the number of leagues will not change without a server restart.
      // Note: If we view this page when the subscription data is already cached then the watcher fires immediately.
      // This means that `unwatchFetchAllLeagueData` has not been assigned yet by the time we get here, which causes
      // the following call to fail! So we delay the call by a frame by which time `unwatchFetchAllLeagueData` will
      // be available.
      unwatchFetchAllLeagueData()
    })
  }
}, { immediate: true })

onUnmounted(() => {
  // Unsubscribe all manual subscriptions.
  while (singleLeagueSubscriptions.value.length) {
    const subscription = singleLeagueSubscriptions.value.shift()
    if (subscription) subscription.unsubscribe()
  }
  while (singleLeagueSeasonSubscriptions.value.length) {
    const subscription = singleLeagueSeasonSubscriptions.value.shift()
    if (subscription) subscription.unsubscribe()
  }
})

/**
 * Returns whether the given league has a currently active season.
 * @param leagueIndex Index of the league.
 */
function doesLeagueHaveActiveSeason (leagueIndex: number) {
  return playerLeagueData.value[leagueIndex].hasActiveSeason
}

/**
 * The displayed player's league data.
 */
interface PlayerLeagueItem {
  leagueIndex: number
  leagueName: string
  leagueId: string
  enabled: boolean

  season: string
  currentSeasonPhase: string

  participantStatus: string
  rankName: string
  divisionIndex: number
  divisionId: string
  placeInDivision: number

  divisionsPerRank: number[]
}

/**
 * Pull together data about each league form all available data sources.
 */
const playerLeagueItems = computed((): PlayerLeagueItem[] => {
  const items = []

  for (let index = 0; index < playerLeagueData.value?.length || 0; ++index) {
    const singlePlayerLeagueData: any = playerLeagueData.value[index]
    const singleLeague: any = singleLeagueSubscriptions.value[index].data
    const leagueHsActiveSeason = doesLeagueHaveActiveSeason(index)
    const singleLeagueSeason: any = leagueHsActiveSeason ? singleLeagueSeasonSubscriptions.value[index]?.data : undefined
    if (singlePlayerLeagueData && singleLeague && (!leagueHsActiveSeason || singleLeagueSeason)) {
      items.push({
        leagueIndex: index,
        leagueName: singlePlayerLeagueData.leagueName,
        leagueId: singlePlayerLeagueData.leagueId,
        enabled: singleLeague.enabled,

        season: singlePlayerLeagueData.divisionIndex.season,
        currentSeasonPhase: leagueHsActiveSeason ? singleLeague.currentOrNextSeasonSchedule.currentPhase.phase : 'Inactive',

        participantStatus: singlePlayerLeagueData.participantStatus,
        rankName: leagueHsActiveSeason ? singleLeagueSeason.ranks[singlePlayerLeagueData.divisionIndex.rank].rankName : 'No Current Season',
        divisionIndex: singlePlayerLeagueData.divisionIndex.division, // index
        divisionId: singlePlayerLeagueData.divisionId,
        placeInDivision: singlePlayerLeagueData.placeInDivision,

        divisionsPerRank: leagueHsActiveSeason
          ? singleLeagueSeason.ranks.map((rank: any) => {
            return rank.numDivisions
          })
          : [],
      })
    }
  }

  return items
})

/**
 * Rank info for the MetaInputSelect options.
 */
interface RankInfo {
  index: number
  rankName: string
  description: string
}

/**
 * We store the rank options here. They are generated when the modal is opened.
 */
const rankOptions = ref<Array<MetaInputSelectOption<RankInfo>>>([])

/**
 * Reset the modal.
 */
function resetModal (leagueIndex: number) {
  // Clear inputs.
  selectedRank.value = undefined
  selectedDivision.value = undefined

  // Populate `rankOptions` with information about all ranks in this season.
  const singleLeagueSeason: any = singleLeagueSeasonSubscriptions.value[leagueIndex].data
  let index = 0
  rankOptions.value = singleLeagueSeason?.ranks.map((rank: any): MetaInputSelectOption<RankInfo> => {
    return {
      id: index.toString(),
      value: {
        index: index++,
        rankName: rank.rankName,
        description: rank.description,
      }
    }
  }) || []
}

/**
 * The selected rank option.
 */
const selectedRank = ref<RankInfo>()

/**
 * The selected division number.
 */
const selectedDivision = ref<number>()

/**
 * Check if target player is assigned to a division.
 * @param leagueIndex Index of the league that we want to check.
 * @param rankIndex Optional rank index.
 * @param divisionIndex Optional division index.
 */
function isAlreadyInDivision (leagueIndex: number, rankIndex?: number, divisionIndex?: number) {
  let isAlreadyInDivision = false

  if (doesLeagueHaveActiveSeason(leagueIndex) && rankIndex !== undefined) {
    const leagueItem: PlayerLeagueItem = playerLeagueItems.value[leagueIndex]
    if (leagueItem.participantStatus === 'Participant') {
      const singleLeagueSeason: any = singleLeagueSeasonSubscriptions.value[leagueIndex].data
      const isSameRank = leagueItem.rankName === singleLeagueSeason?.ranks[rankIndex].rankName
      if (isSameRank) {
        // Has a division been selected? Note: We have to check for both `null` and `undefined` because of the way
        //  `MetaInputNumber` works.
        if (divisionIndex === null || divisionIndex === undefined) {
          if (leagueItem.divisionsPerRank[rankIndex] === 1) {
            // No division selected and only one division exists in this rank, so player must be in this
            // division already.
            isAlreadyInDivision = true
          }
        } else {
          if (divisionIndex === leagueItem.divisionIndex) {
            // Player is in the same rank and division already.
            isAlreadyInDivision = true
          }
        }
      }
    }
  }

  return isAlreadyInDivision
}

/**
 * Controls whether the "Add or Move Player" action is disabled or not for a given league.
 * @param leagueItem The league to check.
 * @returns The reason why the action is disabled, or `false` if the action is enabled.
 */
function addOrMoveParticipantDisabled (leagueItem: PlayerLeagueItem) {
  if (leagueItem.enabled) {
    if (doesLeagueHaveActiveSeason(leagueItem.leagueIndex)) {
      // Action is enabled.
      return false
    } else {
      return 'Cannot add or move participants while a league is inactive.'
    }
  } else {
    return 'Cannot add or move participants while a league is disabled.'
  }
}

/**
 * Adds or moves the target player to the selected active division on the game server.
 * @param leagueIndex Index of the league.
 * @param rankIndex Rank index.
 * @param divisionIndex Optional division index.
 */
async function addOrMovePlayerToLeague (leagueIndex: number, rankIndex?: number, divisionIndex?: number) {
  let endpointUrl: string
  const leagueItem: PlayerLeagueItem = playerLeagueItems.value[leagueIndex]
  const leagueId = leagueItem.leagueId
  if (divisionIndex === null || divisionIndex === undefined) {
    // Move player to specific rank only.
    endpointUrl = `/leagues/${leagueId}/participant/${props.playerId}/addRank/${rankIndex}`
  } else {
    // Move player to specific rank and division.
    const result = await gameServerApi.get(`/divisions/id/${leagueId}/${leagueItem.season}/${rankIndex}/${divisionIndex}/`)
    const divisionEntityId = result.data
    endpointUrl = `/leagues/${leagueId}/participant/${props.playerId}/add/${divisionEntityId}`
  }

  // Perform the action on the server.
  const response = await gameServerApi.post(endpointUrl)
  if (response.data.success) {
    showSuccessToast('Player successfully added to division!')
    playerLeagueDataRefresh()
  } else {
    showErrorToast(response.data.errorMessage)
  }
}

/**
 * Controls whether the "Remove Player" action is disabled or not for a given league.
 * @param leagueItem The league to check.
 * @returns The reason why the action is disabled, or `false` if the action is enabled.
 */
function removeParticipantDisabled (leagueItem: PlayerLeagueItem) {
  if (leagueItem.enabled) {
    if (leagueItem.participantStatus === 'Participant') {
      // Action is enabled.
      return false
    } else {
      return 'Participant is not a participant in this league.'
    }
  } else {
    return 'Cannot remove participants while a league is disabled.'
  }
}

/**
 * Removes the target player from the selected active division on the game server.
 */
async function removeParticipant (leagueId: string, playerId: string) {
  if (playerLeagueData.value) {
    const response = await gameServerApi.post(`/leagues/${leagueId}/participant/${playerId}/remove`)
    if (response.data.success) {
      showSuccessToast('Participant successfully removed from division.')
      playerLeagueDataRefresh()
    } else {
      showErrorToast(response.data.errorMessage)
    }
  }
}

// Search, sort, filter
const searchFields = ['leagueName']
const sortOptions = [
  new MetaListSortOption('League name', 'leagueName', MetaListSortDirection.Descending),
  new MetaListSortOption('League name', 'leagueName', MetaListSortDirection.Ascending),
]

const filterSets = [
  new MetaListFilterSet('participating',
    [
      new MetaListFilterOption('Participating', (x: any) => x.participantStatus === 'Participant'),
      new MetaListFilterOption('Not participating', (x: any) => x.participantStatus === 'NotParticipant'),
      new MetaListFilterOption('Never participated', (x: any) => x.participantStatus === 'NeverParticipated'),

    ]
  ),
]
</script>
