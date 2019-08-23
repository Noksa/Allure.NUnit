using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Allure.Commons.Helpers
{
    internal static class ReflectionHelper
    {
        internal static object GetMemberValueFromAssembly(Assembly assembly, string memberName)
        {
            object memberValue = null;
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                var member = type.GetMember(memberName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy |
                    BindingFlags.NonPublic).FirstOrDefault();
                if (member == null)

                    member = type
                        .GetFields(BindingFlags.Public | BindingFlags.Static |
                                   BindingFlags.FlattenHierarchy | BindingFlags.NonPublic)
                        .FirstOrDefault(fi =>
                            fi.IsLiteral && !fi.IsInitOnly &&
                            fi.Name == memberName);
                if (member != null)
                {
                    memberValue = GetMemberValue(member, null);
                    break;
                }
            }

            return memberValue;
        }

        internal static object GetMemberValue(MemberInfo member, object obj)
        {
            var memberType = member.MemberType;
            object value = null;
            switch (memberType)
            {
                case MemberTypes.Field:
                    var fieldInfo = (FieldInfo) member;
                    value = fieldInfo.GetValue(fieldInfo.IsStatic ? null : obj);
                    break;
                case MemberTypes.Property:
                    var propInfo = (PropertyInfo) member;
                    value = propInfo.GetValue(propInfo.GetAccessors().Any(a => a.IsStatic) ? null : obj);
                    break;
                case MemberTypes.Method:
                    var methodInfo = (MethodInfo) member;
                    value = methodInfo.IsDefined(typeof(ExtensionAttribute), false) ? methodInfo.Invoke(null, new[] {obj}) : methodInfo.Invoke(methodInfo.IsStatic ? null : obj, null);

                    break;
            }

            return value;
        }

        internal static MethodInfo GetExtensionMethod(Type paramType, string methodName)
        {
            foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = item.GetTypes().Where(t => t.IsSealed && !t.IsGenericType && !t.IsNested);

                foreach (var type in types)
                {
                    var method = type.GetMethod(methodName,
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    if (method == null) continue;
                    if (method.GetParameters().Length == 0) continue;
                    if (!method.IsDefined(typeof(ExtensionAttribute), false)) continue;
                    if (method.GetParameters()[0].ParameterType != paramType) continue;
                    return method;
                }
            }

            return null;
        }

        internal static MemberInfo GetMember(Type type, string memberName)
        {
            var member = type.GetMember(memberName,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy |
                BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
            if (member == null)

                member = type
                    .GetFields(BindingFlags.Public | BindingFlags.Static |
                               BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(fi =>
                        fi.IsLiteral && !fi.IsInitOnly &&
                        fi.Name == memberName);

            if (member == null) member = GetExtensionMethod(type, memberName);

            return member;
        }

        internal static object GetMemberValueFromAssembly(Assembly assembly, string nameSpace, string memberName)
        {
            object memberValue = null;
            var type = assembly.GetType(nameSpace);
            if (type != null)
            {
                var member = type.GetMember(memberName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy |
                    BindingFlags.NonPublic).FirstOrDefault();
                if (member == null)

                    member = type
                        .GetFields(BindingFlags.Public | BindingFlags.Static |
                                   BindingFlags.FlattenHierarchy | BindingFlags.NonPublic)
                        .FirstOrDefault(fi =>
                            fi.IsLiteral && !fi.IsInitOnly && fi.Name == memberName);
                if (member != null)
                {
                    var memberType = member.MemberType;
                    switch (memberType)
                    {
                        case MemberTypes.Field:
                            memberValue = ((FieldInfo) member).GetValue(null);
                            break;
                        case MemberTypes.Property:
                            memberValue = ((PropertyInfo) member).GetValue(null);
                            break;
                    }
                }
            }

            return memberValue;
        }

        internal static FieldInfo GetBackingField(this Type type, string name, bool partialMatch = false)
        {
            var msg = partialMatch ? ", which contains" : "";
            var field = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(p =>
                    p.Name.EndsWith("__BackingField") && partialMatch ? p.Name.Contains(name) : p.Name.Equals(name));
            if (field != null) return field;
            throw new MissingFieldException(
                $"Cant find backing field in type {nameof(type)} with name{msg} \"{name}\"");
        }
    }
}