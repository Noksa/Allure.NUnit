using System;
using System.Linq;
using System.Reflection;

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
                    var memberType = member.MemberType;
                    object value = null;
                    switch (memberType)
                    {
                        case MemberTypes.Field:
                            value = ((FieldInfo)member).GetValue(null);
                            break;
                        case MemberTypes.Property:
                            value = ((PropertyInfo)member).GetValue(null);
                            break;
                    }

                    memberValue = value;
                    break;
                }
            }

            return memberValue;
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
                            memberValue = ((FieldInfo)member).GetValue(null);
                            break;
                        case MemberTypes.Property:
                            memberValue = ((PropertyInfo)member).GetValue(null);
                            break;
                    }
                }
            }
            return memberValue;
        }

        internal static FieldInfo GetBackingField(this Type type, string name, bool partialMatch = false)
        {
            var msg = partialMatch ? ", which contains" : "";
            var field = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).FirstOrDefault(p =>
                 p.Name.EndsWith("__BackingField") && partialMatch ? p.Name.Contains(name) : p.Name.Equals(name));
            if (field != null) return field;
            throw new MissingFieldException($"Cant find backing field in type {nameof(type)} with name{msg} \"{name}\"");
        }
    }
}