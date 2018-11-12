using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace PureIP.Portal
{
    public static class EnumExtention
    {
        public static T TryParce<T>(this string data) where T : struct
        {
            T result;
            if (Enum.TryParse<T>(data, out result))
                return result;
            return default(T);
        }

        public static string GetDisplayName(this Enum value)
        {
            Type type = value.GetType();
            var field = type.GetField(value.ToString());
            return field?.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? value.ToString();
        }
    }
}
