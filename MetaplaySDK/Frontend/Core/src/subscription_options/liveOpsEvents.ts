// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

import {
  type SubscriptionOptions,
  getFetcherPolicyGet,
  getCacheRetentionPolicyKeepForever,
  getPollingPolicyTimer,
  getPollingPolicyOnceOnly,
  getCacheRetentionPolicyTimed
} from '@metaplay/subscriptions'

/**
 * The options to get all Live Ops Events in the game.
 */
export function getAllLiveOpsEventsSubscriptionOptions (): SubscriptionOptions {
  return {
    permission: 'api.liveops_events.view',
    pollingPolicy: getPollingPolicyTimer(5000),
    fetcherPolicy: getFetcherPolicyGet('/liveOpsEvents'),
    cacheRetentionPolicy: getCacheRetentionPolicyKeepForever(),
  }
}

/**
 * The options to get all Live Ops Events in the game.
 */
export function getLiveOpsEventTypesSubscriptionOptions (): SubscriptionOptions {
  return {
    permission: 'api.liveops_events.view',
    pollingPolicy: getPollingPolicyOnceOnly(),
    fetcherPolicy: getFetcherPolicyGet('/liveOpsEventTypes'),
    cacheRetentionPolicy: getCacheRetentionPolicyKeepForever(),
  }
}

/**
 * The options to get all Live Ops Events in the game.
 */
export function getSingleLiveOpsEventsSubscriptionOptions (eventId: string): SubscriptionOptions {
  return {
    permission: 'api.liveops_events.view',
    pollingPolicy: getPollingPolicyTimer(5000),
    fetcherPolicy: getFetcherPolicyGet(`/liveOpsEvent/${eventId}`),
    cacheRetentionPolicy: getCacheRetentionPolicyTimed(10000),
  }
}
