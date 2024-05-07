// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

import { type TargetConditionContent } from './components/mails/mailUtils'

/**
 *
 */
export interface LiveOpsEventContent {
  [key: string]: unknown
}

/**
 *
 */
export interface LiveOpsEventTypeInfo {
  /**
   * C# type.
   */
  contentClass: string
  /**
   * Title to display for this item.
   */
  eventTypeName: string
  /**
   *
   */
  templates: {[key: string]: LiveOpsEventContent}
}

export interface LiveOpsEventScheduleInfo {
  isPlayerLocalTime: boolean
  previewDuration: string
  enabledStartTime: string
  endingSoonDuration: string
  enabledEndTime: string
  reviewDuration: string
}

export interface LiveOpsEventParams {
  displayName: string
  description: string
  eventType: string
  templateId: string
  content: LiveOpsEventContent
  schedule: LiveOpsEventScheduleInfo | null
  targetPlayers: string[]
  targetCondition: TargetConditionContent | null
}

export enum LiveOpsEventPhase {
  NotYetStarted = 'NotYetStarted',
  InPreview = 'InPreview',
  Active = 'Active',
  EndingSoon = 'EndingSoon',
  InReview = 'InReview',
  Ended = 'Ended',
}

export interface LiveOpsEventBriefInfo {
  eventId: string
  isArchived: boolean
  isForceDisabled: boolean
  createdAt: string
  eventTypeName: string
  displayName: string
  description: string
  sequenceNumber: number
  tags: string[] | null
  templateId: string
  schedule: LiveOpsEventScheduleInfo | null
  currentPhase: LiveOpsEventPhase
  nextPhase: LiveOpsEventPhase
  nextPhaseTime: string
}

export interface LiveOpsEventDetailsInfo {
  eventId: string
  eventParams: LiveOpsEventParams
  isArchived: boolean
  isForceDisabled: boolean
  createdAt: string
  sequenceNumber: number
  tags: string[] | null
  relatedEvents: LiveOpsEventBriefInfo[]
  currentPhase: LiveOpsEventPhase
  nextPhase: LiveOpsEventPhase
  nextPhaseTime: string
}

export interface GetLiveOpsEventsListApiResult {
  upcomingEvents: LiveOpsEventBriefInfo[]
  ongoingAndPastEvents: LiveOpsEventBriefInfo[]
}

export interface CreateLiveOpsEventRequest {
  validateOnly: boolean
  parameters: LiveOpsEventParams
}

export enum LiveOpsEventCreationDiagnosticLevel {
  Error = 'Error',
  Warning = 'Warning',
}

export interface LiveOpsEventCreationDiagnostic {
  level: LiveOpsEventCreationDiagnosticLevel
  message: string
}

export interface CreateLiveOpsEventResponse {
  isValid: boolean
  eventId: string
  relatedEvents: LiveOpsEventBriefInfo[]
  diagnostics: LiveOpsEventCreationDiagnostic[]
}

export interface UpdateLiveOpsEventRequest {
  validateOnly: boolean
  occurrenceId: string
  parameters: LiveOpsEventParams
}

export interface UpdateLiveOpsEventResponse {
  isValid: boolean
  relatedEvents: LiveOpsEventBriefInfo[]
  diagnostics: LiveOpsEventCreationDiagnostic[]
}
