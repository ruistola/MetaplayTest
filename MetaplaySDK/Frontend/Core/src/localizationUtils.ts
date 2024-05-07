// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

import { DisplayError } from '@metaplay/meta-ui-next'

import type { LocalizationError, LocalizationErrorException, LocalizationErrorStringException, LocalizationErrorTaskDisappeared, LocalizationErrorBlockingMessages } from './localizationServerTypes'

/**
 * Takes a `LocalizationError` and converts it into a `DisplayError` that can be rendered by an `MErrorCallout`.
 * @param localizationError Error to convert.
 */
export function localizationErrorToDisplayError (localizationError: LocalizationError): DisplayError {
  const phaseTypeTooltips: {[type: string]: string} = {
    Build: 'This error is most likely caused by an issue in the source data.',
  }

  if (localizationError.errorType === 'Exception') {
    const error: LocalizationErrorException = localizationError
    return new DisplayError(
      'Exception',
      'An exception occurred.',
      'Build',
      phaseTypeTooltips.Build,
    )
  } else if (localizationError.errorType === 'StringException') {
    const error: LocalizationErrorStringException = localizationError
    return new DisplayError(
      'Exception',
      'An exception occurred.',
      'Build',
      phaseTypeTooltips.Build,
      [
        {
          title: 'Full Exception',
          content: error.fullException,
        }
      ]
    )
  } else if (localizationError.errorType === 'TaskDisappeared') {
    const error: LocalizationErrorTaskDisappeared = localizationError
    return new DisplayError(
      'Task Disappeared',
      'The build task mysteriously disappeared, likely due to the server stopping or crashing.',
      'Build',
      phaseTypeTooltips.Build,
    )
  } else if (localizationError.errorType === 'BlockingMessages') {
    const error: LocalizationErrorBlockingMessages = localizationError
    return new DisplayError(
      'Task Disappeared',
      'The build messages or validation messages contain one or more messages that are treated as errors.',
      'Build',
      phaseTypeTooltips.Build,
    )
  } else {
    const error: any = localizationError
    return new DisplayError(
      'Unknown Error',
      'Unknown error type',
      'Build',
      phaseTypeTooltips.Build,
      [
        {
          title: 'Source Error',
          content: localizationError
        }
      ]
    )
  }
}
