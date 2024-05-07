// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Core;
using Metaplay.Core.Client;
using Metaplay.Core.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Metaplay.Unity
{
    public class EnvironmentConfigEditor : EditorWindow
    {
        DefaultEnvironmentConfigProvider                      _provider;
        [SerializeField] EnvironmentConfig[]                  _environmentConfigs;
        [SerializeField] string[]                             _environmentIds;
        [SerializeField] int                                  _selectedEnvironmentIndex;
        [SerializeField] EnvironmentConfigEditorConfigWrapper _configsWrapper;

        [SerializeField] string _builtEnvironmentsJson;
        [SerializeField] bool   _showBuiltConfigs = false;

        int     _controlLabelWidth  = 120;
        int     _controlButtonWidth = 200;
        Vector2 _scrollPosition     = Vector2.zero;
        Editor  _configEditor;

        float  _saveEditsDelay = 0.5f; // Amount of time in seconds to wait before saving edits to provider
        double _nextSaveTime;
        bool   _editorDirty;

        bool _initialized = false;

        public EnvironmentConfigEditor() { }

        void OnEnable()
        {
            // Ensure Metaplay Core is initialized
            MetaplayCore.Initialize();

            LoadAll();
        }

        void OnDisable()
        {
            SaveConfigsToProvider();

            DestroyImmediate(_configsWrapper);
        }

        [MenuItem("Metaplay/Environment Configs")]
        public static void ShowWindow()
        {
            var wnd = EditorWindow.GetWindow<EnvironmentConfigEditor>();
            wnd.titleContent = new GUIContent("Metaplay Environments");
            wnd.minSize      = new Vector2(200, 200);
            wnd.Show();
        }

        void LoadAll()
        {
            try
            {
                _provider = DefaultEnvironmentConfigProvider.Instance;
            }
            catch (DefaultEnvironmentConfigProvider.NotDefaultEnvironmentConfigProviderIntegrationException)
            {
                _provider = null;
                return;
            }

            if (!_provider.Initialized)
            {
                return;
            }

            _environmentIds     = _provider.GetAllEnvironmentIds();
            _environmentConfigs = _provider.GetAllEnvironmentConfigs();

            if (_configsWrapper != null)
            {
                _configsWrapper.Environments = _environmentConfigs;
            }

            _selectedEnvironmentIndex = Array.IndexOf(_environmentIds, _provider.GetActiveEnvironmentId());

            _initialized = true;
        }

        void OnInspectorUpdate()
        {
            if (_provider != null && !_initialized)
            {
                LoadAll();
            }

            if (_showBuiltConfigs && _provider != null)
            {
                _builtEnvironmentsJson = _provider.GetEditorDefinedBuildEnvironmentsPreview();
            }
        }

        void OnGUI()
        {
            EditorGUILayout.Space(3);
            
            if (!MetaplayCore.IsInitialized)
            {
                EditorGUILayout.HelpBox("MetaplayCore is not initialized! Check the logs for errors related to MetaplayCore.Initialize.", MessageType.Error);
                return;
            }

            if (_provider == null)
            {
                IEnvironmentConfigProvider nonDefaultProvider = IntegrationRegistry.Get<IEnvironmentConfigProvider>();
                EditorGUILayout.HelpBox($"This Editor works only with DefaultEnvironmentConfigProvider or a provider deriving from it.\nThe current active environment config provider integration is {nonDefaultProvider.GetType().ToGenericTypeString()}", MessageType.Info);
                return;
            }

            if (!_provider.Initialized)
            {
                EditorGUILayout.HelpBox("DefaultEnvironmentConfigProvider is not initialized! Check the logs for error related to DefaultEnvironmentConfigProvider.", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginVertical();

            GUILayout.Space(5);

            // Selected environment dropdown
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Active environment", GUILayout.MinWidth(_controlLabelWidth));
            _selectedEnvironmentIndex = EditorGUILayout.Popup(_selectedEnvironmentIndex, _environmentIds, GUILayout.MaxWidth(_controlButtonWidth));
            if (EditorGUI.EndChangeCheck())
            {
                try
                {
                    // Save to provider
                    _provider.SetActiveEnvironmentId(_environmentIds[_selectedEnvironmentIndex]);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Metaplay environment config editor failed to save changes: " + ex);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); // Simple horizontal line

            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            // Paste environment config button
            if (GUILayout.Button("Paste Config From Clipboard"))
            {
                string data = GUIUtility.systemCopyBuffer;

                if (string.IsNullOrWhiteSpace(data))
                {
                    Debug.LogError("Paste environment config failed: Empty payload");
                    EditorUtility.DisplayDialog("Paste failed", "Paste environment config failed: Empty payload", "Ok");
                }
                else
                {
                    MetaplayEnvironmentConfigInfo config = null;
                    try
                    {
                        config = MetaplayEnvironmentConfigInfo.ParseFromJson(data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Paste environment config failed: " + ex);
                        EditorUtility.DisplayDialog("Paste failed", "Paste environment config failed: " + ex.Message + ". See logs for more information.", "Ok");
                    }

                    if (config != null)
                    {
                        string configId = config.EnvironmentId;
                        ConnectionEndpointConfig connectionConfig = ParseConnectionEndpointConfigFromData(config);
                        ClientDebugConfig clientDebugConfig = CreateClientDebugConfig(config.EnvironmentFamily);
                        ClientGameConfigBuildApiConfig clientGameConfigBuildApiConfig = CreateClientGameConfigBuildApiConfig(config, config.EnvironmentFamily);

                        bool foundMatch = false;
                        foreach (EnvironmentConfig environmentConfig in _configsWrapper.Environments)
                        {
                            if (environmentConfig.Id == configId)
                            {
                                environmentConfig.ConnectionEndpointConfig = connectionConfig;
                                environmentConfig.ClientDebugConfig = clientDebugConfig;
                                environmentConfig.ClientGameConfigBuildApiConfig = clientGameConfigBuildApiConfig;
                                foundMatch = true;
                                EditorUtility.DisplayDialog("Paste successful", "Paste environment config successful: Existing environment overwritten.", "Ok");
                                break;
                            }
                        }

                        // Create new environment if no matching id is found
                        if (!foundMatch)
                        {
                            Debug.Log($"No environment config with the id '{configId}' was found when trying to paste environment config, adding new environment config.");

                            List<EnvironmentConfig> configs    = new List<EnvironmentConfig>(_configsWrapper.Environments);
                            Type                    configType = IntegrationRegistry.GetSingleIntegrationType<EnvironmentConfig>();
                            EnvironmentConfig       newConfig  = Activator.CreateInstance(configType) as EnvironmentConfig;

                            newConfig.Id                             = configId;
                            newConfig.ConnectionEndpointConfig       = connectionConfig;
                            newConfig.ClientDebugConfig              = clientDebugConfig;
                            newConfig.ClientGameConfigBuildApiConfig = clientGameConfigBuildApiConfig;
                            
                            configs.Add(newConfig);
                            _configsWrapper.Environments = configs.ToArray();
                            EditorUtility.DisplayDialog("Paste successful", "Paste environment config successful: New environment created.", "Ok");
                        }

                        _editorDirty = true;
                    }
                }
            }
            
            if (GUILayout.Button("Refresh Editor From File"))
            {
                // Reload configs from file into provider
                _provider.ReloadConfigs();
                // Reload all values from provider
                LoadAll();
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); // Simple horizontal line

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // Preview built configs foldout
            bool wasShowBuiltConfigs = _showBuiltConfigs;
            Rect nextRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            _showBuiltConfigs = EditorGUI.Foldout(nextRect, _showBuiltConfigs, "Preview Built Configs");
            if (_showBuiltConfigs && !wasShowBuiltConfigs)
            {
                _builtEnvironmentsJson = _provider.GetEditorDefinedBuildEnvironmentsPreview();
            }

            // Built configs JSON preview
            if (_showBuiltConfigs)
            {
                EditorGUILayout.LabelField(_builtEnvironmentsJson, EditorStyles.textArea);
            }

            // Editing the config specs
            if (_configsWrapper == null || _configEditor == null)
            {
                _configsWrapper              = ScriptableObject.CreateInstance<EnvironmentConfigEditorConfigWrapper>();
                _configsWrapper.hideFlags    = HideFlags.DontSaveInEditor;
                _configsWrapper.Environments = _environmentConfigs;
                _configEditor                = Editor.CreateEditor(_configsWrapper);
            }

            EditorGUI.BeginChangeCheck();
            _configEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                // Reset save delay timer
                _editorDirty  = true;
                _nextSaveTime = EditorApplication.timeSinceStartup + (double)_saveEditsDelay;

                // Populate added configs
                for (int i = 0; i < _configsWrapper.Environments.Length; i++)
                {
                    Type configType = IntegrationRegistry.GetSingleIntegrationType<EnvironmentConfig>();
                    if (_configsWrapper.Environments[i] == null)
                    {
                        _configsWrapper.Environments[i] = Activator.CreateInstance(configType) as EnvironmentConfig;
                    }
                }

                //_configEditor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        static ClientGameConfigBuildApiConfig CreateClientGameConfigBuildApiConfig(MetaplayEnvironmentConfigInfo config, EnvironmentFamily environmentFamily)
        {
            List<string> OAuth2LocalCallbackUrls;
            if (!string.IsNullOrEmpty(config.OAuth2LocalCallback))
                OAuth2LocalCallbackUrls = new List<string>() {config.OAuth2LocalCallback};
            else
                OAuth2LocalCallbackUrls = new List<string>();

            switch (environmentFamily)
            {
                case EnvironmentFamily.Local:
                    return new ClientGameConfigBuildApiConfig()
                    {
                        AdminApiBaseUrl                     = "http://localhost:5550/api/",
                        GameConfigPublishPolicy             = GameConfigPublishPolicy.Allowed,
                        AdminApiAuthenticationTokenProvider = AuthenticationTokenProvider.None,
                        AdminApiCredentialsPath             = "",
                        AdminApiUseOpenIdConnectIdToken     = false,
                        AdminApiOAuth2Settings = new OAuth2AuthorizationServerSpec()
                        {
                            ClientId              = "",
                            ClientSecret          = "",
                            AuthorizationEndpoint = "",
                            TokenEndpoint         = "",
                            Audience              = "",
                            LocalCallbackUrls     = new List<string>(),
                            UseStateParameter     = false,
                        },
                    };
                case EnvironmentFamily.Development:
                    return new ClientGameConfigBuildApiConfig()
                    {
                        AdminApiBaseUrl                     = config.AdminApiBaseUrl,
                        GameConfigPublishPolicy             = GameConfigPublishPolicy.AskForConfirmation,
                        AdminApiAuthenticationTokenProvider = AuthenticationTokenProvider.OAuth2,
                        AdminApiCredentialsPath             = "",
                        AdminApiUseOpenIdConnectIdToken     = config.AdminApiUseOpenIdConnectIdToken,
                        AdminApiOAuth2Settings = new OAuth2AuthorizationServerSpec()
                        {
                            ClientId              = config.OAuth2ClientID,
                            ClientSecret          = config.OAuth2ClientSecret,
                            AuthorizationEndpoint = config.OAuth2AuthorizationEndpoint,
                            TokenEndpoint         = config.OAuth2TokenEndpoint,
                            Audience              = config.OAuth2Audience,
                            LocalCallbackUrls     = OAuth2LocalCallbackUrls,
                            UseStateParameter     = config.OAuth2UseStateParameter,
                        },
                    };
                case EnvironmentFamily.Staging:
                    return new ClientGameConfigBuildApiConfig()
                    {
                        AdminApiBaseUrl                     = config.AdminApiBaseUrl,
                        GameConfigPublishPolicy             = GameConfigPublishPolicy.UploadOnly,
                        AdminApiAuthenticationTokenProvider = AuthenticationTokenProvider.OAuth2,
                        AdminApiCredentialsPath             = "",
                        AdminApiUseOpenIdConnectIdToken     = config.AdminApiUseOpenIdConnectIdToken,
                        AdminApiOAuth2Settings              = new OAuth2AuthorizationServerSpec()
                        {
                            ClientId              = config.OAuth2ClientID,
                            ClientSecret          = config.OAuth2ClientSecret,
                            AuthorizationEndpoint = config.OAuth2AuthorizationEndpoint,
                            TokenEndpoint         = config.OAuth2TokenEndpoint,
                            Audience              = config.OAuth2Audience,
                            LocalCallbackUrls     = OAuth2LocalCallbackUrls,
                            UseStateParameter     = config.OAuth2UseStateParameter,
                        },
                    };
                case EnvironmentFamily.Production:
                    return new ClientGameConfigBuildApiConfig()
                    {
                        AdminApiBaseUrl                     = config.AdminApiBaseUrl,
                        GameConfigPublishPolicy             = GameConfigPublishPolicy.UploadOnly,
                        AdminApiAuthenticationTokenProvider = AuthenticationTokenProvider.OAuth2,
                        AdminApiCredentialsPath             = "",
                        AdminApiUseOpenIdConnectIdToken     = config.AdminApiUseOpenIdConnectIdToken,
                        AdminApiOAuth2Settings = new OAuth2AuthorizationServerSpec()
                        {
                            ClientId              = config.OAuth2ClientID,
                            ClientSecret          = config.OAuth2ClientSecret,
                            AuthorizationEndpoint = config.OAuth2AuthorizationEndpoint,
                            TokenEndpoint         = config.OAuth2TokenEndpoint,
                            Audience              = config.OAuth2Audience,
                            LocalCallbackUrls     = OAuth2LocalCallbackUrls,
                            UseStateParameter     = config.OAuth2UseStateParameter,
                        },
                    };
                default:
                    throw new ArgumentException($"Invalid EnvironmentFamily: {environmentFamily}");
            }
        }

        /// <summary>
        /// Creates a new <see cref="ClientDebugConfig"/> based on <see cref="environmentFamily"/>, which needs to have a value found in
        /// RuntimeOptions.EnvironmentFamily. Valid values are: "Local", "Development", "Staging", "Production".
        /// Throws if <see cref="environmentFamily"/> does not have a valid value.
        /// </summary>
        /// <param name="environmentFamily"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        static ClientDebugConfig CreateClientDebugConfig(EnvironmentFamily environmentFamily)
        {
            switch (environmentFamily)
            {
                case EnvironmentFamily.Local:
                    return new ClientDebugConfig
                    {
                        EnablePlayerConsistencyChecks = true,
                        LogLevel                      = LogLevel.Debug,
                        LogLevelOverrides             = new List<LogLevelOverrideSpec>(),
                        PlayerChecksumGranularity     = ChecksumGranularity.PerOperation,
                    };
                case EnvironmentFamily.Development:
                    return new ClientDebugConfig
                    {
                        EnablePlayerConsistencyChecks = true,
                        LogLevel                      = LogLevel.Debug,
                        LogLevelOverrides             = new List<LogLevelOverrideSpec>(),
                        PlayerChecksumGranularity     = ChecksumGranularity.PerOperation,
                    };
                case EnvironmentFamily.Staging:
                    return new ClientDebugConfig
                    {
                        EnablePlayerConsistencyChecks = false,
                        LogLevel                      = LogLevel.Debug,
                        LogLevelOverrides             = new List<LogLevelOverrideSpec>(),
                        PlayerChecksumGranularity     = ChecksumGranularity.PerActionSingleTickPerFrame,
                    };
                case EnvironmentFamily.Production:
                    return new ClientDebugConfig
                    {
                        EnablePlayerConsistencyChecks = false,
                        LogLevel                      = LogLevel.Debug,
                        LogLevelOverrides             = new List<LogLevelOverrideSpec>(),
                        PlayerChecksumGranularity     = ChecksumGranularity.PerActionSingleTickPerFrame,
                    };
                default:
                    throw new ArgumentException($"Invalid EnvironmentFamily: {environmentFamily}");
            }
        }

        static ConnectionEndpointConfig ParseConnectionEndpointConfigFromData(MetaplayEnvironmentConfigInfo data)
        {
            int serverPort = 0;
            if (data.ServerPorts != null && data.ServerPorts.Length > 0)
                serverPort = data.ServerPorts[0];

            int serverPortForWebsocket = 0;
            if (data.ServerPortsForWebSocket != null && data.ServerPortsForWebSocket.Length > 0)
                serverPortForWebsocket = data.ServerPortsForWebSocket[0];

            ConnectionEndpointConfig connectionEndpointConfig = new ConnectionEndpointConfig
            {
                ServerHost             = data.ServerHost,
                ServerPort             = serverPort,
                ServerPortForWebSocket = serverPortForWebsocket,
                EnableTls              = data.EnableTls,
                CdnBaseUrl             = data.CdnBaseUrl,
            };

            if (data.EnvironmentFamily == EnvironmentFamily.Development)
                connectionEndpointConfig.CommitIdCheckRule = ClientServerCommitIdCheckRule.OnlyIfDefined;
            else
                connectionEndpointConfig.CommitIdCheckRule = ClientServerCommitIdCheckRule.Disabled;

            // Backup gateways
            connectionEndpointConfig.BackupGateways = new List<ServerGatewaySpec>();
            if (data.ServerPorts != null && data.ServerPorts.Length > 1)
            {
                for (int i = 1; i < data.ServerPorts.Length; i++)
                {
                    connectionEndpointConfig.BackupGateways.Add(new ServerGatewaySpec("", data.ServerHost, data.ServerPorts[i]));
                }
            }

            return connectionEndpointConfig;
        }

        void Update()
        {
            // Save edits after delay
            if (_editorDirty && EditorApplication.timeSinceStartup > _nextSaveTime)
            {
                SaveConfigsToProvider();
                _editorDirty = false;

                LoadAll();
            }
        }

        void SaveConfigsToProvider()
        {
            if (_configsWrapper != null && _configsWrapper.Environments != null)
            {
                // Set edited configs to provider
                _environmentConfigs = _configsWrapper.Environments;
                _provider.SetAllEnvironmentConfigs(_environmentConfigs);
            }
        }
    }
}
