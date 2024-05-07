// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using System;

namespace Metaplay.Cloud.RuntimeOptions
{
    /// <summary>
    /// Mark a class as containing RuntimeOptions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RuntimeOptionsAttribute : Attribute
    {
        public readonly string  SectionName;        // Name of the section in the config files
        public readonly string  SectionDescription; // Description of the section
        public readonly bool    IsStatic;           // Is the options block static (i.e., cannot be updated during runtime)

        public RuntimeOptionsAttribute(string sectionName, bool isStatic, string sectionDescription = null)
        {
            SectionName        = sectionName ?? throw new ArgumentNullException(nameof(sectionName));
            SectionDescription = sectionDescription;
            IsStatic           = isStatic;
        }
    }

    /// <summary>
    /// Define a command line alias for the given RuntimeOption.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CommandLineAliasAttribute : Attribute
    {
        public readonly string Alias;

        public CommandLineAliasAttribute(string alias) { Alias = alias; }
    }

    /// <summary>
    /// Define the RuntimeOption as Computed. Computed values cannot be assigned
    /// from runtime options and instead are computed from other runtime options.
    /// Properties with no setter are automatically interpreted as Computed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ComputedValueAttribute : Attribute
    {
    }
}
