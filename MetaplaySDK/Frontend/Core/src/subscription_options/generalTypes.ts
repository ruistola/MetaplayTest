// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

export interface StaticConfig {
  clusterConfig: ClusterConfig
  defaultLanguage: string
  supportedLogicVersions: { minVersion: number, maxVersion: number }
  serverReflection: { activablesMetadata: ActivablesMetadata }
  gameConfigBuildInfo: ServerConfigBuildInfo
  localizationsBuildInfo: ServerConfigBuildInfo
}

type ClusteringMode = 'Static' | 'Kubernetes'

export interface ClusterConfig {
  mode: ClusteringMode
  nodeSets: NodeSetConfig[]
}

export interface NodeSetConfig {
  mode: ClusteringMode
  shardName: string
  hostName: string
  port: number
  nodeCount: number
  EntityKindMask: string[]
}

export interface ActivablesMetadata{
  categories: {[key: string]: ActivableCategoryMetadata}
  kinds: {[key: string]: ActivableCategoryMetadata}
}

export interface ActivableCategoryMetadata{
  displayName: string
  shortSingularDisplayName: string
  description: string
  kinds: string[]
}

export interface ActivableKindMetadata{
  displayName: string
  category: string
  description: string
  gameSpecificConfigDataMembers: string[]
}

export interface ServerConfigBuildInfo {
  buildSupported: boolean
  buildParametersType: string
  buildParametersNamespaceQualifiedName: string
  slotToAvailableSourcesMapping: { [key: string]: Array<{ displayName: string }> }
}

export interface LogEventInfo {
  id: string
  timestamp: string
  message: string
  logEventType: string
  source: string
  sourceType: string
  exception: string
  stackTrace: string
}

export interface ErrorCountResponse {
  collectorRestartedWithinMaxAge: boolean
  maxAge: string
  collectorRestartTime: string
  errorCount: number
  errors: LogEventInfo[]
  overMaxErrorCount: boolean
}
