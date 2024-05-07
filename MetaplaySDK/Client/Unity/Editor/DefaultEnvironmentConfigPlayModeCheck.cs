using Metaplay.Core.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Metaplay.Core.Client.DefaultEnvironmentConfigProvider;

public class DefaultEnvironmentConfigPlayModeCheck
{
    [InitializeOnLoadMethod]
    public static void Init()
    {
        EditorApplication.playModeStateChanged += ValidateActiveEnvironmentOnPlayMode;
    }

    static void ValidateActiveEnvironmentOnPlayMode(PlayModeStateChange playModeStateChange)
    {
        if (playModeStateChange == PlayModeStateChange.EnteredPlayMode)
        {
            // Check for no environment selected
            try
            {
                string activeEnvironmentId = DefaultEnvironmentConfigProvider.Instance.GetActiveEnvironmentId();
                if (string.IsNullOrEmpty(activeEnvironmentId))
                {
                    if (EditorUtility.DisplayDialog("No active environment selected!", "Select an active environment in the Metaplay environment config editor window under 'Metaplay/Environment Configs'", "Exit play mode"))
                    {
                        EditorApplication.ExitPlaymode();
                    }
                }
            }
            catch (NotDefaultEnvironmentConfigProviderIntegrationException)
            {
                // DefaultEnvironmentConfigProvider is not in use. Doing nothing.
            }

            // Check that current environment id is valid and environment config getting works
            try
            {
                EnvironmentConfig _ = IEnvironmentConfigProvider.Get();
            }
            catch (Exception ex)
            {
                if (EditorUtility.DisplayDialog("Failed to get environment config!", ex.Message, "Exit play mode"))
                {
                    EditorApplication.ExitPlaymode();
                }
            }
        }
    }
}
