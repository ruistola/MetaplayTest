// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

#if NETCOREAPP || UNITY_EDITOR
#   define ENABLE_GENERATED_MEMBER_SETTERS
#   define ENABLE_COMPILED_CONSTRUCTORS
#endif

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using static Metaplay.Core.Config.GameConfigSyntaxTree;

namespace Metaplay.Core.Config
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ParseAsDerivedTypeAttribute : Attribute
    {
        public readonly Type Type;

        public ParseAsDerivedTypeAttribute(Type type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Parse game config output types (types implementing <see cref="IGameConfigData"/>) from the
    /// <see cref="GameConfigSyntaxTree"/>.
    /// </summary>
    public static class GameConfigOutputItemParser
    {
        public class ParseOptions
        {
            public readonly UnknownConfigMemberHandling UnknownMemberHandling;

            public ParseOptions(UnknownConfigMemberHandling unknownMemberHandling)
            {
                UnknownMemberHandling = unknownMemberHandling;
            }
        }

        // Optimized setters for properties, and setters for fields.
#if UNITY_WEBGL && !UNITY_EDITOR
        static WebConcurrentDictionary<MemberInfo, Action<object, object>> _cachedMemberSetters = new WebConcurrentDictionary<MemberInfo, Action<object, object>>();
#else
#pragma warning disable MP_WGL_00 // "Feature is poorly supported in WebGL". False positive, this is non-WebGL.
        static ConcurrentDictionary<MemberInfo, Action<object, object>> _cachedMemberSetters = new ConcurrentDictionary<MemberInfo, Action<object, object>>();
#pragma warning restore MP_WGL_00
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        static WebConcurrentDictionary<Type, Func<object>> _cachedTypeGenerators = new WebConcurrentDictionary<Type, Func<object>>();
#else
#pragma warning disable MP_WGL_00 // "Feature is poorly supported in WebGL". False positive, this is non-WebGL.
        static ConcurrentDictionary<Type, Func<object>> _cachedTypeGenerators = new ConcurrentDictionary<Type, Func<object>>();
#pragma warning restore MP_WGL_00
#endif

        static IList CreateList(Type elemType)
        {
            // \todo [petri] optimize: avoid activator
            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType));
        }

        // Convert an input List<elemType> to the specified dstType.
        static IList ConvertListTo(IList list, Type dstType, Type elemType)
        {
            if (dstType.IsArray)
            {
                Array array = Array.CreateInstance(elemType, list.Count);
                for (int ndx = 0; ndx < list.Count; ndx++)
                    array.SetValue(list[ndx], ndx);
                return array;
            }
            else if (dstType.IsGenericTypeOf(typeof(List<>)))
                return list;
            else
                return (IList)Activator.CreateInstance(dstType, list);
        }

        static object ParseScalarImpl(Type type, ConfigLexer lexer)
        {
            if (ConfigParser.TryParse(type, lexer, out object configParserResult))
                return configParserResult;
            else if (typeof(IList).IsAssignableFrom(type))
            {
                // Figure out element type in container
                Type elemType = type.GetCollectionElementType();
                IList list = CreateList(elemType);

                // Check for an empty list ('[]')
                if (lexer.TryParseToken(ConfigLexer.TokenType.LeftBracket))
                {
                    // Starts with '[', must be followed by ']'
                    lexer.ParseToken(ConfigLexer.TokenType.RightBracket);
                }
                else // Otherwise, comma-separated list of elements
                {
                    while (!lexer.IsAtEnd)
                    {
                        // Parse element and add to list
                        object elem = ParseScalarImpl(elemType, lexer);
                        list.Add(elem);

                        // Stop reading when no more commas found
                        if (!lexer.TryParseToken(ConfigLexer.TokenType.Comma))
                            break;
                    }
                }

                return ConvertListTo(list, type, elemType);
            }
            else
                throw new ParseError($"No registered ConfigParser for type {type.ToGenericTypeString()}");
        }

        static object ParseScalar(GameConfigBuildLog buildLog, Type type, ScalarNode scalar)
        {
            // Strings are returned as raw values
            if (type == typeof(string))
                return scalar.Value;

            ConfigLexer lexer = new ConfigLexer(scalar.Value);
            object result = ParseScalarImpl(type, lexer);

            if (!lexer.IsAtEnd)
            {
                string parsed = lexer.Input.Substring(0, lexer.CurrentToken.StartOffset);
                string remaining = lexer.GetRemainingInputInfo();
                buildLog.WithLocation(scalar.Location).Error($"Extraneous content appears after '{parsed}' of type {type.ToGenericTypeString()}: '{remaining}'");
            }

            return result;
        }

        static IList ParseCollection(ParseOptions options, GameConfigBuildLog buildLog, Type collectionType, IEnumerable<NodeBase> elements)
        {
            // Figure out element type in container
            Type elemType = collectionType.GetCollectionElementType();

            // Parse all elements to a list
            IList list = CreateList(elemType);
            foreach (NodeBase elem in elements)
            {
                if (elem is ScalarNode scalar)
                {
                    try
                    {
                        object val = ParseScalar(buildLog, elemType, scalar);
                        list.Add(val);
                    }
                    catch (Exception ex)
                    {
                        buildLog.WithLocation(scalar.Location).Error($"Failed to parse collection element of type {elemType.ToGenericTypeString()} from string '{scalar.Value}'", ex);
                        return null;
                    }
                }
                else if (elem is ObjectNode obj)
                {
                    object val = ParseObject(options, buildLog, elemType, obj);
                    list.Add(val);
                }
                else if (elem is null)
                {
                    object defaultVal = elemType.GetDefaultValue();
                    list.Add(defaultVal);
                }
                else
                    throw new InvalidOperationException($"Invalid collection element Node type {elem.GetType().ToGenericTypeString()}");
            }

            // Convert to result type
            return ConvertListTo(list, collectionType, elemType);
        }

        static bool TryParseMember(ParseOptions options, GameConfigBuildLog buildLog, MemberInfo memberInfo, NodeBase child, out object member)
        {
            Type memberType = memberInfo.GetDataMemberType();

            // \todo [petri] move null checks outside -> could always return value ??
            if (child == null)
            {
                member = default;
                return false;
            }
            else if (child is ScalarNode scalar)
            {
                try
                {
                    member = ParseScalar(buildLog, memberType, scalar);
                    return true;
                }
                catch (Exception ex)
                {
                    buildLog.WithLocation(scalar.Location).Error($"Failed to parse {memberInfo.ToMemberWithGenericDeclaringTypeString()} (of type {memberType.ToGenericTypeString()}) from string '{scalar.Value}'", ex);
                    member = null;
                    return false;
                }
            }
            else if (child is CollectionNode collection)
            {
                if (!typeof(IList).IsAssignableFrom(memberType))
                {
                    // \todo [petri] what location to give as has multiple? this is a structural error in spreadsheets (though json could have anything)
                    buildLog.Error($"Trying to parse CollectionNode into {memberInfo.ToMemberWithGenericDeclaringTypeString()} which is not a collection type ({memberType.ToGenericTypeString()})"); // \todo [petri] error message
                    member = false;
                    return false;
                }

                member = ParseCollection(options, buildLog, memberType, collection.Elements);
                return true;
            }
            else if (child is ObjectNode childObject)
            {
                member = ParseObject(options, buildLog, memberType, childObject);
                return true;
            }
            else
                throw new InvalidOperationException($"Invalid Node type {child.GetType()}");
        }

#if ENABLE_GENERATED_MEMBER_SETTERS
        // \note The member caching only works in Unity Editor and the server, not in Unity builds
        static Action<object, object> CreateMemberSetter(MemberInfo memberInfo)
        {
            // \note Using ILGenerator for generating the setters as it's a) fast, and b) works with private setters (where Expressions don't)
            Type            dstType     = memberInfo.DeclaringType;
            PropertyInfo    propInfo    = memberInfo as PropertyInfo;
            FieldInfo       fieldInfo   = memberInfo as FieldInfo;
            Type            valueType   = propInfo?.PropertyType ?? fieldInfo.FieldType;

            DynamicMethod method = new DynamicMethod(
                $"SetMember<{dstType.ToGenericTypeString()}, {valueType.ToGenericTypeString()}>",
                typeof(void),
                new[] { typeof(object), typeof(object) },
                dstType.Module,
                skipVisibility: true
            );

            ILGenerator ilGen = method.GetILGenerator();
            Type propertyType = propInfo?.PropertyType;
            Type fieldType = fieldInfo?.FieldType;

            if (!dstType.IsValueType) // dst is a class
            {
                if (fieldInfo != null)
                {
                    //Console.WriteLine("Class / field: {0}.{1} (valueType={2})", dstType.ToGenericTypeString(), memberInfo.Name, fieldType.IsValueType);
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Castclass, dstType);
                    ilGen.Emit(OpCodes.Ldflda, fieldInfo);
                    ilGen.Emit(OpCodes.Ldarg_1);
                    ilGen.Emit(fieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, fieldType);
                    ilGen.Emit(OpCodes.Stobj, fieldType);
                }
                else // propInfo != null
                {
                    //Console.WriteLine("Class / prop: {0}.{1} (valueType={2})", dstType.ToGenericTypeString(), memberInfo.Name, propertyType.IsValueType);
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Castclass, dstType);
                    ilGen.Emit(OpCodes.Ldarg_1);
                    ilGen.Emit(propertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, propertyType);
                    MethodInfo setterMethod = propInfo.GetSetMethodOnDeclaringType();
                    MetaDebug.Assert(setterMethod != null, "Unable to get setter method for property {0}.{1}", dstType.ToGenericTypeString(), memberInfo.Name);
                    ilGen.EmitCall(OpCodes.Callvirt, setterMethod, null);
                }
            }
            else // dst is a value type (struct)
            {
                if (fieldInfo != null)
                {
                    //Console.WriteLine("Struct / field: {0}.{1} (valueType={2})", dstType.ToGenericTypeString(), memberInfo.Name, fieldType.IsValueType);
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Unbox, dstType); // \note Doesn't actually take a copy and the subsequent property setter modifies the on-heap object directly
                    ilGen.Emit(OpCodes.Ldflda, fieldInfo);
                    ilGen.Emit(OpCodes.Ldarg_1);
                    ilGen.Emit(fieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, fieldType);
                    ilGen.Emit(OpCodes.Stobj, fieldType);
                }
                else // propInfo != null
                {
                    //Console.WriteLine("Struct / prop: {0}.{1} (valueType={2})", dstType.ToGenericTypeString(), memberInfo.Name, propertyType.IsValueType);
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Unbox, dstType); // \note Doesn't actually take a copy and the subsequent property setter modifies the on-heap object directly
                    ilGen.Emit(OpCodes.Ldarg_1);
                    ilGen.Emit(propertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, propertyType);
                    MethodInfo setterMethod = propInfo.GetSetMethodOnDeclaringType();
                    MetaDebug.Assert(setterMethod != null, "Unable to get setter method for property {0}.{1}", dstType.ToGenericTypeString(), memberInfo.Name);
                    ilGen.EmitCall(OpCodes.Callvirt, setterMethod, null);
                }
            }

            ilGen.Emit(OpCodes.Ret);

            return (Action<object, object>)method.CreateDelegate(typeof(Action<object, object>));
        }
#endif

#if ENABLE_COMPILED_CONSTRUCTORS

        // \note The constructor caching only works in Unity Editor and the server, not in Unity builds
        static Func<object> CreateILConstructor(Type type)
        {
            // \note Using ILGenerator for generating constructors as it's a) fast, and b) works with private constructors (where Expressions don't)

            DynamicMethod method = new DynamicMethod(
                $"InstantiateObject<{type.ToGenericTypeString()}>",
                typeof(object),
                Type.EmptyTypes,
                type.Module,
                skipVisibility: true);

            ILGenerator     ilGen           = method.GetILGenerator();
            ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);

            if (type.IsValueType && constructorInfo == null)
            {
                // If structs have no constructor defined, initialize an empty struct.
                ilGen.DeclareLocal(type);
                ilGen.Emit(OpCodes.Ldloca_S, 0);
                ilGen.Emit(OpCodes.Initobj, type);
                ilGen.Emit(OpCodes.Ldloc_0);
                ilGen.Emit(OpCodes.Box, type);
            }
            else if(constructorInfo != null)
            {
                // If empty constructor is defined, new an object as you would normally.
                ilGen.Emit(OpCodes.Newobj, constructorInfo);
                ilGen.Emit(OpCodes.Castclass, typeof(object));
            }
            else
            {
                // No constructor defined and not a value type
                throw new InvalidOperationException($"'{type.ToGenericTypeString()}' does not have an parameterless constructor defined.");
            }

            ilGen.Emit(OpCodes.Ret);
            return (Func<object>)method.CreateDelegate(typeof(Func<>).MakeGenericType(typeof(object)));
        }
#endif

        static object CreateObject(Type type)
        {
            Func<object> generatedConstructor = _cachedTypeGenerators.GetOrAdd(type, CreateGeneratedConstructorInvoker);

            try
            {
                return generatedConstructor.Invoke();
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                return null; // unreachable
            }
        }

        static Func<object> CreateGeneratedConstructorInvoker(Type type)
        {
#if ENABLE_COMPILED_CONSTRUCTORS
            return CreateILConstructor(type);
#else
            return new Func<object>(() => Activator.CreateInstance(type, nonPublic: true));
#endif
        }

        static void SetMemberValue(MemberInfo memberInfo, object obj, object value)
        {
#if ENABLE_GENERATED_MEMBER_SETTERS
            // Generate member setter and cache it for efficiency
            Action<object, object> memberSetter = _cachedMemberSetters.GetOrAdd(memberInfo, CreateMemberSetter);
#else
            // In Unity builds, ILGenerator is not supported, but game config parsing shouldn't be happening there anyway
            Action<object, object> memberSetter;
            if (memberInfo is PropertyInfo propInfo)
                memberSetter = propInfo.GetSetValueOnDeclaringType();
            else
                memberSetter = ((FieldInfo)memberInfo).SetValue;
#endif

            // Set the member value in obj
            memberSetter(obj, value);
        }

        public static object ParseObject(ParseOptions options, GameConfigBuildLog buildLog, Type objDeclaredType, ObjectNode src)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            Type objType = objDeclaredType;

            // \todo For flexibility, support also other ways of specifying the derived type:
            //       - Per member, at type level - maybe also allow specifying for nested member?
            //         E.g. when a user type contains a MetaActivableParams field, specify that its .Schedule
            //         should be parsed as MetaRecurringCalendarSchedule within this user type specifically.
            //       - With some Field.$Type notation in sheet.
            if (objDeclaredType.IsAbstract)
            {
                ParseAsDerivedTypeAttribute parseAsDerivedTypeAttribute = objDeclaredType.GetCustomAttribute<ParseAsDerivedTypeAttribute>();
                if (parseAsDerivedTypeAttribute != null)
                    objType = parseAsDerivedTypeAttribute.Type;
            }

            // \note If creating a struct here, we keep it boxed while setting the members so that we're not modifying temporary copies
            object dst = CreateObject(objType);

            foreach ((NodeMemberId memberId, NodeBase memberNode) in src.Members)
            {
                if (memberId.VariantId != null)
                    throw new MetaAssertException($"Unexpected variant member during {nameof(ObjectNode)} parsing: {memberId}");

                MemberInfo memberInfo = TryGetMemberInfo(options, buildLog, memberNode.Location, objType, memberId.Name);
                if (memberInfo != null)
                {
                    if (TryParseMember(options, buildLog, memberInfo, memberNode, out object member))
                        SetMemberValue(memberInfo, dst, member);
                }
            }

            return dst;
        }

        /// <summary>
        /// Get the <see cref="MemberInfo"/> for the member named <paramref name="memberName"/> in type <paramref name="objType"/>.
        /// If zero or multiple members were found, an appropriate message is logged to <paramref name="buildLog"/> (according to <paramref name="options"/>)
        /// and null is returned.
        /// </summary>
        /// <returns>
        /// The <see cref="MemberInfo"/> if it was unambiguously found, and null otherwise.
        /// </returns>
        static MemberInfo TryGetMemberInfo(ParseOptions options, GameConfigBuildLog buildLog, GameConfigSourceLocation location, Type objType, string memberName)
        {
            MemberInfo[] memberInfos = objType.GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (memberInfos.Length == 1)
                return memberInfos[0];
            else if (memberInfos.Length > 1)
            {
                buildLog.WithLocation(location).Error($"Multiple members with name '{memberName}' found in {objType.ToGenericTypeString()}.");
                return null;
            }
            else
            {
                switch (options.UnknownMemberHandling)
                {
                    case UnknownConfigMemberHandling.Ignore: break;
                    case UnknownConfigMemberHandling.Warning: buildLog.WithLocation(location).Warning(CreateUnknownMemberLogMessage(memberName, objType)); break;
                    case UnknownConfigMemberHandling.Error:   buildLog.WithLocation(location).Error  (CreateUnknownMemberLogMessage(memberName, objType)); break;
                    default:
                        throw new InvalidOperationException($"Unknown {nameof(UnknownConfigMemberHandling)}: {options.UnknownMemberHandling}");
                }

                return null;
            }
        }

        static string CreateUnknownMemberLogMessage(string memberName, Type objType)
        {
            // \todo The "//" comment syntax might be specific to sheets,
            //       so how can we have a useful error message while keeping this general?
            //       And the example //{memberName} in the message is already incorrect if
            //       this is not a scalar, because then we're not dealing with a {memberName}
            //       column but possibly multiple colums like {memberName}.Something
            return $"No member '{memberName}' found in {objType.ToGenericTypeString()}. If the member is meant to be ignored, prefix it with two slashes: // {memberName}";
        }

        public static VariantConfigItem<TKey, TItem> ParseItem<TKey, TItem>(ParseOptions options, GameConfigBuildLog buildLog, RootObject src) where TItem : IHasGameConfigKey<TKey>, new()
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            ObjectId itemId = src.Id;
            string variantId = src.VariantId;

            buildLog = buildLog.WithItemId(itemId).WithVariantId(variantId);

            TItem item = (TItem)ParseObject(options, buildLog, typeof(TItem), src.Node);

            List<TKey> aliases;
            if (src.Aliases != null)
                aliases = (List<TKey>)ParseScalarImpl(typeof(List<TKey>), new ConfigLexer(src.Aliases));
            else
                aliases = null;

            return new VariantConfigItem<TKey, TItem>(item, variantId, aliases, src.Location);
        }

        /// <summary>
        /// Given a list of objects (<paramref name="objects"/>) (variants and baseline),
        /// parse their members according to <typeparamref name="TKeyValue"/> and return a list of
        /// top-level members (both baseline and variant members in the same list).
        /// </summary>
        public static List<VariantConfigStructureMember> ParseKeyValueStructureMembers<TKeyValue>(ParseOptions options, GameConfigBuildLog buildLog, List<RootObject> objects)
        {
            List<VariantConfigStructureMember> members = new List<VariantConfigStructureMember>();

            // Parse each object's members according to TKeyValue and put them into `members`.
            foreach (RootObject obj in objects)
            {
                GameConfigBuildLog objBuildLog = buildLog.WithVariantId(obj.VariantId);

                foreach ((NodeMemberId memberId, NodeBase memberNode) in obj.Node.Members)
                {
                    if (memberId.VariantId != null)
                        throw new MetaAssertException($"Unexpected variant member during {nameof(ObjectNode)} parsing: {memberId}");

                    MemberInfo memberInfo = TryGetMemberInfo(options, objBuildLog, memberNode.Location, typeof(TKeyValue), memberId.Name);
                    if (memberInfo != null)
                    {
                        if (TryParseMember(options, objBuildLog, memberInfo, memberNode, out object memberValue))
                        {
                            members.Add(new VariantConfigStructureMember(
                                new ConfigStructureMember(memberInfo, memberValue),
                                obj.VariantId,
                                obj.Location));
                        }
                    }
                }
            }

            return members;
        }
    }
}
